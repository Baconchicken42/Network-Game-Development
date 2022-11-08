using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class chatUI : NetworkBehaviour
{
    const ulong SYSTEM_ID = 999;
    public TMPro.TMP_Text txtChatLog;
    public Button btnSend;
    public TMPro.TMP_InputField inputArea;

    private ulong[] singleClient = new ulong[1];

    private void Start()
    {
        btnSend.onClick.AddListener(ClientOnSendClicked);
        inputArea.onSubmit.AddListener(ClientOnInputSubmit);
    }

    public override void OnNetworkSpawn()
    {
        txtChatLog.text = "-- Start Chat Log --";

        if (IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HostOnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HostOnClientDisconnected;
            DisplayMessageLocally("You are the  Host!", SYSTEM_ID);
        }
        else
        {
            DisplayMessageLocally($"You are Player #{NetworkManager.Singleton.LocalClientId}!", SYSTEM_ID);
        }
    }

    private void SendUIMessage()
    {
        string msg = inputArea.text;
        inputArea.text = "";
        SendChatMessageServerRpc(msg);
    }

    public void DisplayMessageLocally(string message, ulong from, bool whisper = false)
    {
        if (from == SYSTEM_ID)
            txtChatLog.text += $"\n[SYSTEM]: {message}";
        else if (from == NetworkManager.Singleton.LocalClientId)
            txtChatLog.text += whisper ? $"\n<WHISPER>[YOU]: {message}" : $"\n[YOU]: {message}";
        else
            txtChatLog.text += whisper ? $"\n<WHISPER>[Player #{from}]: {message}" : $"\n[Player #{from}]: {message}";
    }

    //-------
    //Events
    //-------
    public void ClientOnSendClicked()
    {
        SendUIMessage();
    }

    public void ClientOnInputSubmit(string text)
    {
        SendUIMessage();
    }

    public void HostOnClientConnected(ulong clientId)
    {
        SendChatMessageClientRpc($"Client {clientId} connected");
    }

    public void HostOnClientDisconnected(ulong clientId)
    {
        SendChatMessageClientRpc($"Client {clientId} disconnected");
    }


    //------
    //RPC
    //------

    [ClientRpc]
    public void SendChatMessageClientRpc(string message, ulong from = SYSTEM_ID, bool whisper = false, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log(message);
        DisplayMessageLocally(message, from, whisper);
    }

    private void SendDirectMessage(string message, ulong from, ulong to)
    {
        ClientRpcParams rpcParams = default;
        
        singleClient[0] = from;
        rpcParams.Send.TargetClientIds = singleClient;
        SendChatMessageClientRpc(message, from, true, rpcParams);

        singleClient[0] = to;
        rpcParams.Send.TargetClientIds = singleClient;
        SendChatMessageClientRpc(message, from, true, rpcParams);
    }


    [ServerRpc(RequireOwnership = false)]
    public void SendChatMessageServerRpc(string message, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log($"Host got message: {message}");
        //string newMessage = $"[Player #{serverRpcParams.Receive.SenderClientId}]: {message}";

        if (message.StartsWith("@"))
        {
            string[] parts = message.Split(" ");
            string clientIdStr = parts[0].Replace("@", "");
            ulong toClientId = ulong.Parse(clientIdStr);

            SendDirectMessage(message, serverRpcParams.Receive.SenderClientId, toClientId);
        }
        else
        {
            SendChatMessageClientRpc(message, serverRpcParams.Receive.SenderClientId);
        }
    }
}
