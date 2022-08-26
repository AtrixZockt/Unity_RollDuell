using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerNetwork : NetworkBehaviour
{
    private readonly NetworkVariable<PlayerNetworkData> netState = new(writePerm: NetworkVariableWritePermission.Owner);

    private Vector2 _vel;
    private float _rotVel;
    [SerializeField] private float _cheapInterpolationTime = 0.1f;
    void Update()
    {
        if(IsOwner)
        {
            netState.Value = new PlayerNetworkData()
            {
                Position = transform.position,
                Rotation = transform.rotation.eulerAngles
            };
        } else
        {
            transform.position = Vector2.SmoothDamp(transform.position, netState.Value.Position, ref _vel, _cheapInterpolationTime);
            transform.rotation = Quaternion.Euler(
                0,
                Mathf.SmoothDampAngle(transform.rotation.eulerAngles.z, netState.Value.Rotation.z, ref _rotVel, _cheapInterpolationTime),
                0);
        }
    }

    struct PlayerNetworkData : INetworkSerializable
    {
        private float _x, _y;
        private float _zRot;

        internal Vector2 Position
        {
            get => new Vector2(_x, _y);
            set
            {
                _x = value.x;
                _y = value.y;
            }
        }

        internal Vector3 Rotation
        {
            get => new Vector3(0, 0, _zRot);
            set => _zRot = value.z;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _x);
            serializer.SerializeValue(ref _y);

            serializer.SerializeValue(ref _zRot);
        }
    }
}
