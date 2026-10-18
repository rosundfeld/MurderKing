using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HostDisconnectedUI : MonoBehaviour
{
    [SerializeField] private Button playAgainButton;


    private void Start()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
        Hide();    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        if (clientId == NetworkManager.ServerClientId)
        {
            Show();
        }
    }
    private void Awake()
    {
        playAgainButton.onClick.AddListener(() =>
        {
            Loader.Load(Loader.Scene.GameScene);
        });
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_OnClientDisconnectCallback;
        }
    }
}
