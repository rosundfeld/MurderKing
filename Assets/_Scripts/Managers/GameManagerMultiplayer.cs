using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System;
using Unity.Services.Authentication;
using Unity.Services.Core;

public class GameManagerMultiplayer : NetworkBehaviour
{
    public const int MAX_PLAYER_AMOUNT = 4;
    public const string PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER = "PlayerNameMultiplayer";
    //---------------SINGLETON NETCODE-----------------
    public static GameManagerMultiplayer Instance { get; private set; }


    //---------------FIELDS-----------------
    [SerializeField] private KitchenObjectListSO kitchenObjectListSO;
    [SerializeField] private List<Color> playerColorList;

    public static bool playMultiplayer;
    private NetworkList<PlayerData> playerDataNetworkList;
    private string playerName;

    //---------------EVENTS-----------------
    public event EventHandler OnTryingToJoinGame;
    public event EventHandler FailedToJoinGame;
    public event EventHandler OnPlayerDataNetworkListChanged;

    //---------------UNITY METHODS-----------------
    public void Awake()
    {
        Instance = this;

        playerName = PlayerPrefs.GetString(PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER, "PlayerName" + UnityEngine.Random.Range(100, 999));
        playerDataNetworkList = new NetworkList<PlayerData>();
        playerDataNetworkList.OnListChanged += PlayerDataNetworkList_OnListChanged;

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (!playMultiplayer)
        {
            //SinglePlayer
            StartHost();
            //Loader.LoadNetwork(Loader.Scene.GameScene);
            Loader.LoadNetwork(Loader.Scene.Game);
        }
    }

    public string GetPlayerName()
    {
        return playerName;
    }

    public void SetPlayerName(string playerName)
    {
        this.playerName = playerName;
        PlayerPrefs.SetString(PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER, playerName);
    }


    //---------------PLAYER DATA-----------------
    private void PlayerDataNetworkList_OnListChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        OnPlayerDataNetworkListChanged?.Invoke(this, EventArgs.Empty);
    }

    public PlayerData GetPlayerDataFromPlayerIndex(int playerIndex)
    {
        return playerDataNetworkList[playerIndex];
    }

    public Color GetPlayerColor(int colorId)
    {
        return playerColorList[colorId];
    }


    public PlayerData GetPlayerData()
    {
        return GetPlayerDataFromClientID(NetworkManager.Singleton.LocalClientId);
    }

    public PlayerData GetPlayerDataFromClientID(ulong clientId)
    {
        foreach (PlayerData playerData in playerDataNetworkList)
        {
            if (playerData.clientId == clientId)
            {
                return playerData;
            }
        }
        return default;
    }

    public int GetPlayerDataIndexFromClientID(ulong clientId)
    {
        for (int i = 0; i < playerDataNetworkList.Count; i++)
        {
            if (playerDataNetworkList[i].clientId == clientId)
            {
                return i;
            }
        }
        return -1;
    }

    //---------------MULTIPLAYER OBJECT SPAWNING-----------------

    public void SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent)
    {
        int kitchenObjectSOIndex = GetKitchenObjectSOIndex(kitchenObjectSO);
        SpawnKitchenObjectServerRpc(kitchenObjectSOIndex, new NetworkObjectReference(kitchenObjectParent.GetNetworkObject()));
    }

    [Rpc(SendTo.Server)]
    private void SpawnKitchenObjectServerRpc(int kitchenObjectSOIndex, NetworkObjectReference kitchenObjectParentNetworkObjectReference)
    {
        KitchenObjectSO kitchenObjectSO = GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);

        Transform kitchenObjectTransform = Instantiate(kitchenObjectSO.prefab);

        kitchenObjectParentNetworkObjectReference.TryGet(out NetworkObject kitchenObjectParentNetworkObject);
        IKitchenObjectParent kitchenObjectParent = kitchenObjectParentNetworkObject.GetComponent<IKitchenObjectParent>();

        if(kitchenObjectParent.HasKitchenObject())
        {
            //Parent already has a kitchen object, cannot spawn another one here.
            return;
        }

        NetworkObject networkObject = kitchenObjectTransform.GetComponent<NetworkObject>();
        networkObject.Spawn(true);

        KitchenObject kitchenObject = kitchenObjectTransform.GetComponent<KitchenObject>();

        kitchenObject.SetKitchenObjectParent(kitchenObjectParent);

    }

    public void StartHost()
    {
        //This object survives scene loads, so a second lobby would register everything twice;
        //NGO 2 also only allows one ConnectionApprovalCallback, so assign it instead of adding
        NetworkManager.Singleton.ConnectionApprovalCallback = NetworkManager_ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_Server_OnClientDisconnectCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_OnClientDisconnectCallback;
        NetworkManager.Singleton.StartHost();
    }

    private void NetworkManager_Server_OnClientDisconnectCallback(ulong clientId)
    {
        for (int i = 0; i < playerDataNetworkList.Count; i++)
        {
            PlayerData playerData = playerDataNetworkList[i];
            if (playerData.clientId == clientId)
            {
                //Disconnected
                playerDataNetworkList.RemoveAt(i);
                break;
            }
        }
    }

    private void NetworkManager_OnClientConnectedCallback(ulong clientId)
    {
        PlayerData playerData = new PlayerData
        {
            clientId = clientId,
            colorId = GetFirstUnusedColorId()
        };

        if (clientId == NetworkManager.ServerClientId)
        {
            //The host's own data is set directly: during StartHost this object isn't spawned yet, so RPCs throw.
            //Remote clients send theirs through NetworkManager_Client_OnClientConnectedCallback
            playerData.playerName = GetPlayerName();
            playerData.playerId = GetAuthenticationPlayerId();
        }

        playerDataNetworkList.Add(playerData);
    }

    private static string GetAuthenticationPlayerId()
    {
        //Singleplayer can start the host before Unity Services finished signing in
        if (UnityServices.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn)
        {
            return "";
        }
        return AuthenticationService.Instance.PlayerId;
    }

    private void NetworkManager_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest connectionApprovalRequest, NetworkManager.ConnectionApprovalResponse connectionApprovalResponse)
    {
        if (connectionApprovalRequest.ClientNetworkId == NetworkManager.ServerClientId)
        {
            //The host approves itself while still in the LobbyScene
            connectionApprovalResponse.Approved = true;
            return;
        }
        if (SceneManager.GetActiveScene().name != Loader.Scene.CharacterSelectionScene.ToString())
        {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Game is already in progress!";
            return;
        }
        if (NetworkManager.Singleton.ConnectedClientsIds.Count >= MAX_PLAYER_AMOUNT)
        {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Maximum player amount reached!";
            return;
        }
        connectionApprovalResponse.Approved = true;
    }

    public void StartClient()
    {
        OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);

        NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_Client_OnClientDisconnectCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Client_OnClientDisconnectCallback;
        NetworkManager.Singleton.OnClientConnectedCallback -= NetworkManager_Client_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Client_OnClientConnectedCallback;
        NetworkManager.Singleton.StartClient();
    }

    private void NetworkManager_Client_OnClientConnectedCallback(ulong clientId)
    {
        SetPlayerNameServerRpc(GetPlayerName());
        SetPlayerIdServerRpc(GetAuthenticationPlayerId());
    }

    [Rpc(SendTo.Server)]
    private void SetPlayerNameServerRpc(string playerName, RpcParams serverRpcParams = default)
    {
        int playerDataIndex = GetPlayerDataIndexFromClientID(serverRpcParams.Receive.SenderClientId);
        if (playerDataIndex == -1)
        {
            //Player not found
            return;
        }

        //Ele nao te deixa atualizar direto no Dicionário, vc tem que instanciar o PLayerData e dps modificar e inserir
        PlayerData playerdata = playerDataNetworkList[playerDataIndex];
        playerdata.playerName = playerName;

        playerDataNetworkList[playerDataIndex] = playerdata;
    }

    [Rpc(SendTo.Server)]
    private void SetPlayerIdServerRpc(string playerId, RpcParams serverRpcParams = default)
    {
        int playerDataIndex = GetPlayerDataIndexFromClientID(serverRpcParams.Receive.SenderClientId);
        if (playerDataIndex == -1)
        {
            //Player not found
            return;
        }

        //Ele nao te deixa atualizar direto no Dicionário, vc tem que instanciar o PLayerData e dps modificar e inserir
        PlayerData playerdata = playerDataNetworkList[playerDataIndex];
        playerdata.playerId = playerId;

        playerDataNetworkList[playerDataIndex] = playerdata;
    }

    public int GetKitchenObjectSOIndex(KitchenObjectSO kitchenObjectSO)
    {
        return kitchenObjectListSO.kitchenObjectSOList.IndexOf(kitchenObjectSO);
    }

    public KitchenObjectSO GetKitchenObjectSOFromIndex(int kitchenObjectSOIndex)
    {
        return kitchenObjectListSO.kitchenObjectSOList[kitchenObjectSOIndex];
    }

    public void DestroyKitchenObject(KitchenObject kitchenObject)
    {
        DestroyKitchenObjectServerRpc(new NetworkObjectReference(kitchenObject.NetworkObject));
    }

    [Rpc(SendTo.Server)]
    private void DestroyKitchenObjectServerRpc(NetworkObjectReference kitchenObjectNetworkObjectReference)
    {
        kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject);

        if (kitchenObjectNetworkObject == null)
        {
            //KitchenObject already destroyed or not found
            return;
        }

        KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();

        ClearKitchenObjectClientRpc(kitchenObjectNetworkObjectReference);

        kitchenObject.DestroySelf();
    }

    [ClientRpc]
    private void ClearKitchenObjectClientRpc(NetworkObjectReference kitchenObjectNetworkObjectReference)
    {
        kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject);
        KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();

        kitchenObject.ClearKitchenObjectFromParent();
    }
    private void NetworkManager_Client_OnClientDisconnectCallback(ulong clientId)
    {
        FailedToJoinGame?.Invoke(this, EventArgs.Empty);
    }

    public bool IsPlayerIndexConnected(int playerIndex)
    {
        return playerIndex < playerDataNetworkList.Count;
    }

    public void ChangePlayerColor(int colorId)
    {
        ChangePlauyerColorServerRpc(colorId);
    }

    [Rpc(SendTo.Server)]
    private void ChangePlauyerColorServerRpc(int colorId, RpcParams serverRpcParams = default)
    {
        if (!IsColorAvaliable(colorId))
        {
            //Color not available
            return;
        }
        int playerDataIndex = GetPlayerDataIndexFromClientID(serverRpcParams.Receive.SenderClientId);
        if (playerDataIndex == -1)
        {
            //Player not found
            return;
        }

        //Ele nao te deixa atualizar direto no Dicionário, vc tem que instanciar o PLayerData e dps modificar e inserir
        PlayerData playerdata = playerDataNetworkList[playerDataIndex];
        playerdata.colorId = colorId;

        playerDataNetworkList[playerDataIndex] = playerdata;
    }

    private bool IsColorAvaliable(int colorId)
    {
        foreach (var playerData in playerDataNetworkList)
        {
            if (playerData.colorId == colorId)
            {
                return false;
            }
        }
        return true;
    }

    private int GetFirstUnusedColorId()
    {
        for (int i = 0; i < playerColorList.Count; i++)
        {
            if (IsColorAvaliable(i))
            {
                return i;
            }
        }
        return -1;
    }

    public void KickPlayer(ulong clientId)
    {
        NetworkManager.Singleton.DisconnectClient(clientId);
        NetworkManager_Server_OnClientDisconnectCallback(clientId);
    }
}