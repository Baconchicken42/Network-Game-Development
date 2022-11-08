using UnityEngine;
using Unity.Netcode;
using UnityEngine.Serialization;
using Unity.Collections;

public struct PlayerInfo : INetworkSerializable, System.IEquatable<PlayerInfo> {
    public ulong clientId;
    public Color color;
    public bool isReady;

    public PlayerInfo(ulong id, Color c, bool Ready=false) {
        clientId = id;
        color = c;
        isReady = Ready;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref color);
        serializer.SerializeValue(ref isReady);
    }

    public bool Equals(PlayerInfo other) {
        return other.clientId == clientId;
    }
}