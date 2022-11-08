using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class LobbyManager : NetworkBehaviour
{

    public LobbyPlayerPanel playerPanelPrefab;
    public GameObject playersPanel;

    public GameObject playerScrollContent;
    public TMPro.TMP_Text txtPlayerNumber;
    public Button btnStart;
    public Button btnReady;

    public Player playerPrefab;

    private NetworkList<PlayerInfo> allPlayers = new NetworkList<PlayerInfo>();
    private List<LobbyPlayerPanel> playerPanels = new List<LobbyPlayerPanel>();

    private Color[] playerColors = new Color[]
    {
        Color.blue,
        Color.green,
        Color.yellow,
        Color.magenta,
        Color.cyan
    };
    private int colorIndex = 0;


    void Start()
    {
        if (IsHost)
        {
            AddPlayerToList(NetworkManager.LocalClientId);
            RefreshPlayerPanels();
        }
    }


    public override void OnNetworkSpawn() {

        if (IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HostOnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HostOnClientDisconnected;
            btnReady.gameObject.SetActive(false);
        }

        //must be after for some reason
        base.OnNetworkSpawn();

        if (IsClient && !IsHost)
        {
            allPlayers.OnListChanged += ClientOnAllPlayersChanged;
            txtPlayerNumber.text = $"Player {NetworkManager.LocalClientId}";
            btnStart.gameObject.SetActive(false);
            btnReady.onClick.AddListener(ClientOnReadyClicked);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        //if (IsHost)
        //    return;
        ulong clientID = serverRpcParams.Receive.SenderClientId;
        int playerIndex = FindPlayerIndex(clientID);
        PlayerInfo info = allPlayers[playerIndex];

        info.isReady = !info.isReady;
        allPlayers[playerIndex] = info;

        int readyCount = 0;
        foreach (PlayerInfo readyinfo in allPlayers)
        {
            if (readyinfo.isReady)
                readyCount++;
        }

        btnStart.enabled = readyCount == allPlayers.Count - 1;

        RefreshPlayerPanels();
    }

    private void ClientOnAllPlayersChanged(NetworkListEvent<PlayerInfo> changeEvent)
    {
        RefreshPlayerPanels();
    }

    private void HostOnClientConnected(ulong clientID)
    {
        btnStart.enabled = false;
        AddPlayerToList(clientID);
        RefreshPlayerPanels();
    }

    private void HostOnClientDisconnected(ulong clientId)
    {
        int index = FindPlayerIndex(clientId);
        if (index != -1)
        {
            allPlayers.RemoveAt(index);
            RefreshPlayerPanels();
        }
    }

    private void ClientOnReadyClicked()
    {
        ToggleReadyServerRpc();
    }

    private void AddPlayerToList(ulong clientID)
    {
        allPlayers.Add(new PlayerInfo(clientID, nextColor(), false));
    }

    private int FindPlayerIndex(ulong clientID)
    {
        var idx = 0;
        var found =false;

        while (idx < allPlayers.Count && !found)
        {
            if (allPlayers[idx].clientId == clientID)
                found = true;
            else
                idx++;
        }

        if (!found)
            return -1;
        else
            return idx;
    }

    private Color nextColor()
    {
        Color newColor = playerColors[colorIndex];
        colorIndex++;
        if (colorIndex > playerColors.Length - 1)
            colorIndex = 0;
        return newColor;
    }

    private void AddPlayerPanel(PlayerInfo info)
    {
        LobbyPlayerPanel newPanel = Instantiate(playerPanelPrefab);
        newPanel.transform.SetParent(playerScrollContent.transform, false);
        newPanel.SetName($"Player {info.clientId.ToString()}");
        newPanel.SetColor(info.color);
        newPanel.SetReady(info.isReady);
        playerPanels.Add(newPanel);
    }

    private void RefreshPlayerPanels()
    {
        foreach (LobbyPlayerPanel panel in playerPanels)
        {
            Destroy(panel.gameObject);
        }
        playerPanels.Clear();

        foreach (PlayerInfo pi in allPlayers)
        {
            AddPlayerPanel(pi);
        }
    }

}
