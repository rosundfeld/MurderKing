using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class CharacterSelectReady : NetworkBehaviour
{

    public static CharacterSelectReady Instance { get; private set; }

    public event EventHandler OnReadyChanged;
    private Dictionary<ulong, bool> playerReadyDictionary;

    private void Awake()
    {
        Instance = this;
        playerReadyDictionary = new Dictionary<ulong, bool>();
    }



    public void SetPlayerReady()
    {
        SetPlayerReadyServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void SetPlayerReadyServerRpc(RpcParams serverRpcParams = default)
    {
        SetPlayerReadyClientRpc(serverRpcParams.Receive.SenderClientId);

        playerReadyDictionary[serverRpcParams.Receive.SenderClientId] = true;

        bool allPlayersReady = true;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Debug.Log($"Checking readiness for client {clientId}");
            Debug.Log($"Player ready status for client {clientId}: {playerReadyDictionary.ContainsKey(clientId) && playerReadyDictionary[clientId]}");
            if (!playerReadyDictionary.ContainsKey(clientId) || !playerReadyDictionary[clientId])
            {
                allPlayersReady = false;
                break; // Exit the loop early since not all players are ready
            }
        }

        if (allPlayersReady)
        {
            KitchenGameLobby.Instance.DeleteLobby();
            //Loader.LoadNetwork(Loader.Scene.GameScene);
            Loader.LoadNetwork(Loader.Scene.Game);
        }
    }

    [ClientRpc]
    private void SetPlayerReadyClientRpc(ulong clientId)
    {
        playerReadyDictionary[clientId] = true;

        OnReadyChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool IsPlayerReady(ulong clientId)
    {
        return playerReadyDictionary.ContainsKey(clientId) && playerReadyDictionary[clientId];
    }

}
