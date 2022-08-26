using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    //Singleton
    public static GameManager _instance;
    public static GameManager Instance => _instance;

    private string _lobbyId;

    private RelayHostData _hostData;
    private RelayJoinData _joinData;

    //Setup Events

    // Notify state update
    public UnityAction<string> UpdateState;
    //Notify match found
    public UnityAction MatchFound;

    //Create a Server
    public UnityAction<bool> CreatingServer;
    public UnityAction<string> ServerCreated;


    private void Awake()
    {
        //Just a badic singleton
        if (_instance is null)
        {
            _instance = this;
            return;
        }

        Destroy(this);
    }

    // Start is called before the first frame update
    async void Start()
    {
        // Initialize Unity services
        await UnityServices.InitializeAsync();

        // Setup event listiners
        SetupEvents();

        //Unity login
        await SignInAnonymouslyAsync();

        //Subscribe to NetworkManager events
        NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;


        UIManager.Instance.JoinWithCode += JoinLobbyByCode;
    }

    #region Network Events

    private void ClientConnected(ulong id)
    {
        //Player with id connected to our session

        Debug.Log("Connected player with id: " + id);

        UpdateState?.Invoke("Player Found!");
        MatchFound?.Invoke();
    }

    #endregion

    #region UnityLoging

    void SetupEvents()
    {
        AuthenticationService.Instance.SignedIn += () =>
        {
            //Show how to get playerid
            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");

            // Show hot to get an access token
            Debug.Log($"Access Token: {AuthenticationService.Instance.AccessToken}");
        };

        AuthenticationService.Instance.SignInFailed += (err) =>
        {
            Debug.LogError(err);
        };

        AuthenticationService.Instance.SignedOut += () =>
        {
            Debug.Log("Player signed out");
        };
    }

    async Task SignInAnonymouslyAsync()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sign in anonymously succeded!");
        }
        catch (Exception ex)
        {
            //NOtify the player with the proper error message
            Debug.LogException(ex);
        }
    }

    #endregion


    #region Lobby

    public async void JoinLobbyByCode(string Code)
    {
        string lobbyCode = Code;

        try
        {
            JoinAllocation allocation = await Relay.Instance.JoinAllocationAsync(lobbyCode);

            // Create Object
            _joinData = new RelayJoinData
            {
                Key = allocation.Key,
                Port = (ushort)allocation.RelayServer.Port,
                AllocationID = allocation.AllocationId,
                AllocationIDBytes = allocation.AllocationIdBytes,
                ConnectionData = allocation.ConnectionData,
                HostConnectionData = allocation.HostConnectionData,
                IPv4Address = allocation.RelayServer.IpV4
            };

            //Set transport data
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                _joinData.IPv4Address,
                _joinData.Port,
                _joinData.AllocationIDBytes,
                _joinData.Key,
                _joinData.ConnectionData,
                _joinData.HostConnectionData);
            

            NetworkManager.Singleton.StartClient();

        }
        catch (LobbyServiceException e)
        {
            
            Debug.Log("Cannt find a lobby: " + e);
        }
    }

    public async void FindMatch()
    {
        Debug.Log("Looking for a lobby...");

        UpdateState?.Invoke("Looging for a match...");

        try
        {
            //looking for a lobby

            //Add options to the matchmaking (mode, rank, etc)
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();

            //Quick-join a random lobby
            Lobby lobby = await Lobbies.Instance.QuickJoinLobbyAsync(options);

            Debug.Log("Joined lobby: " + lobby.Id);
            Debug.Log("Lobby Players: " + lobby.Players.Count);

            // Retrieve the Relay code set in the create Match
            string joinCode = lobby.Data["joinCode"].Value;

            Debug.Log("Recieved code: " + joinCode);

            JoinAllocation allocation = await Relay.Instance.JoinAllocationAsync(joinCode);

            // Create Object
            _joinData = new RelayJoinData
            {
                Key = allocation.Key,
                Port = (ushort)allocation.RelayServer.Port,
                AllocationID = allocation.AllocationId,
                AllocationIDBytes = allocation.AllocationIdBytes,
                ConnectionData = allocation.ConnectionData,
                HostConnectionData = allocation.HostConnectionData,
                IPv4Address = allocation.RelayServer.IpV4
            };

            //Set transport data
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                _joinData.IPv4Address,
                _joinData.Port,
                _joinData.AllocationIDBytes,
                _joinData.Key,
                _joinData.ConnectionData,
                _joinData.HostConnectionData);

            NetworkManager.Singleton.StartClient();

            // Trigger events
            UpdateState?.Invoke("Match found!");
            MatchFound?.Invoke();
        }
        catch (LobbyServiceException e)
        {
            //if we dont find a lobby, create one
            Debug.Log("Cannt find a lobby: " + e);
            CreateMatch();
        }
    }

    public async void CreateMatch()
    {
        CreatingServer?.Invoke(true);
        Debug.Log("Creating a new Lobby...");

        UpdateState?.Invoke("Creating new lobby...");

        //external Connections (Host + maxconnectoins)
        int maxConnections = 1;

        try
        {
            //Create Relay object
            Allocation allocation = await Relay.Instance.CreateAllocationAsync(maxConnections);

            _hostData = new RelayHostData
            {
                Key = allocation.Key,
                Port = (ushort)allocation.RelayServer.Port,
                AllocationID = allocation.AllocationId,
                AllocationIDBytes = allocation.AllocationIdBytes,
                ConnectionData = allocation.ConnectionData,
                IPv4Address = allocation.RelayServer.IpV4
            };

            //Retrieve JoinCode
            _hostData.JoinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);

            string lobbyName = "game_lobby";
            int maxPlayers = 2;
            CreateLobbyOptions options = new CreateLobbyOptions();
            options.IsPrivate = false;

            // Put the JoinCode in the lobby data, visible by every member
            options.Data = new Dictionary<string, DataObject>()
            {
                {
                    "joinCode", new DataObject(
                        visibility: DataObject.VisibilityOptions.Member,
                        value: _hostData.JoinCode)
                },
            };

            var lobby = await Lobbies.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

            _lobbyId = lobby.Id;

            Debug.Log("Lobby created: " + lobby.Id);

            //Heartbeat the lobby every 15 secounds
            StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id, 15));

            // Now that Rela and Lobby are set...

            //Set Trnaports data
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                _hostData.IPv4Address,
                _hostData.Port,
                _hostData.AllocationIDBytes,
                _hostData.Key,
                _hostData.ConnectionData);

            //Finally start host
            NetworkManager.Singleton.StartHost();
            ///

            string joinCode = lobby.Data["joinCode"].Value;

            ServerCreated?.Invoke(joinCode);
            CreatingServer?.Invoke(false);
            UpdateState?.Invoke("Waiting for players");
        }
        catch (LobbyServiceException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    IEnumerator HeartbeatLobbyCoroutine(string lobbyid, float waitTimeScounds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeScounds);
        while (true)
        {
            Lobbies.Instance.SendHeartbeatPingAsync(lobbyid);
            Debug.Log("Lobby Heartbit");
            yield return delay;
        }
    }

    private void OnDestroy()
    {
        //Delete the lobby if no one uses it
        Lobbies.Instance.DeleteLobbyAsync(_lobbyId);
        UIManager.Instance.JoinWithCode -= JoinLobbyByCode;
    }

    #endregion


    public struct RelayHostData
    {
        public string JoinCode;
        public string IPv4Address;
        public ushort Port;
        public Guid AllocationID;
        public byte[] AllocationIDBytes;
        public byte[] ConnectionData;
        public byte[] Key;
    }

    public struct RelayJoinData
    {
        public string JoinCode;
        public string IPv4Address;
        public ushort Port;
        public Guid AllocationID;
        public byte[] AllocationIDBytes;
        public byte[] ConnectionData;
        public byte[] HostConnectionData;
        public byte[] Key;
    }

}
