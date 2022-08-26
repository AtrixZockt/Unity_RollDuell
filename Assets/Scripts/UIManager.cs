using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject playButton;
    [SerializeField] private Text stateText;

    [SerializeField] private GameObject GameSelector;
    [SerializeField] private GameObject CreatingGamePanel;
    [SerializeField] private GameObject Lobby;
    [SerializeField] private GameObject ServerList;

    [SerializeField] private Text LobbyCode;

    [SerializeField] private Text JoinLobbyCode;

    //Events

    public static UIManager _instance;
    public static UIManager Instance => _instance;

    public UnityAction<string> JoinWithCode;

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
    void Start()
    {
        //GameManager.Instance.MatchFound += MatchFound;
        //GameManager.Instance.UpdateState += UpdateState;
        GameManager.Instance.ServerCreated += ServerCreated;
        GameManager.Instance.CreatingServer += CreatingServer;
    }

    /*private void UpdateState(string newState)
    {
        stateText.text = newState;
    }

    private void MatchFound()
    {
        playButton.SetActive(false);
    }*/

    public void LoadServerList()
    {
        GameSelector.SetActive(false);
        ServerList.SetActive(true);
    }

    private void CreatingServer(bool state)
    {
        if (state)
        {
            CreatingGamePanel.SetActive(true);
            Cursor.visible = false;
        }
        else
        {
            CreatingGamePanel.SetActive(false);
            Cursor.visible = true;
        }
    }

    private void ServerCreated(string LobbyC)
    {
        GameSelector.SetActive(false);
        Lobby.SetActive(true);
        LobbyCode.text = LobbyC;
    }

    private void OnDestroy()
    {
        //Unsubscrbe from events
        //GameManager.Instance.MatchFound -= MatchFound;
        //GameManager.Instance.UpdateState -= UpdateState;

        GameManager.Instance.ServerCreated -= ServerCreated;
        GameManager.Instance.CreatingServer -= CreatingServer;
    }

    public void CopyLobbyCode()
    {

        Debug.Log("Knopf gedrückt!");
        string Lcode = LobbyCode.text;

        GUIUtility.systemCopyBuffer = Lcode;
    }

    public void JoinViaCodeButton()
    {
        string lobbyJoinCode = JoinLobbyCode.text;
        Debug.Log(lobbyJoinCode);

        JoinWithCode?.Invoke(lobbyJoinCode);
    }
}
