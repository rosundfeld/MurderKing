using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class LobbyMessageUI : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        closeButton.onClick.AddListener(Hide);
    }

    private void Start()
    {
        GameManagerMultiplayer.Instance.FailedToJoinGame += GameManagerMultiplayer_FailedToJoinGame;
        KitchenGameLobby.Instance.OnCreateLobbyStarted += KitchenGameLobby_OnCreateLobbyStarted;
        KitchenGameLobby.Instance.OnCreateLobbyFailed += KitchenGameLobby_OnCreateLobbyFailed;
        KitchenGameLobby.Instance.OnQuickJoinLobbyFailed += KitchenGameLobby_OnQuickJoinLobbyFailed;
        KitchenGameLobby.Instance.OnJoinStarted += KitchenGameLobby_OnJoinStarted;
        KitchenGameLobby.Instance.OnJoinFailed += KitchenGameLobby_OnJoinFailed;

        Hide();
    }

    private void OnDestroy()
    {
        GameManagerMultiplayer.Instance.FailedToJoinGame -= GameManagerMultiplayer_FailedToJoinGame;
        KitchenGameLobby.Instance.OnCreateLobbyStarted -= KitchenGameLobby_OnCreateLobbyStarted;
        KitchenGameLobby.Instance.OnCreateLobbyFailed -= KitchenGameLobby_OnCreateLobbyFailed;
        KitchenGameLobby.Instance.OnQuickJoinLobbyFailed -= KitchenGameLobby_OnQuickJoinLobbyFailed;
        KitchenGameLobby.Instance.OnJoinStarted -= KitchenGameLobby_OnJoinStarted;
        KitchenGameLobby.Instance.OnJoinFailed -= KitchenGameLobby_OnJoinFailed;
    }

    private void KitchenGameLobby_OnCreateLobbyStarted(object sender, System.EventArgs e)
    {
        messageText.text = "Creating lobby...";
    }

    private void KitchenGameLobby_OnCreateLobbyFailed(object sender, System.EventArgs e)
    {
        messageText.text = "Failed to create lobby.";
    }

    private void KitchenGameLobby_OnQuickJoinLobbyFailed(object sender, System.EventArgs e)
    {
        messageText.text = "Failed to quick join lobby.";
    }

    private void KitchenGameLobby_OnJoinStarted(object sender, System.EventArgs e)
    {
        messageText.text = "Joining lobby...";
    }

    private void KitchenGameLobby_OnJoinFailed(object sender, System.EventArgs e)
    {
        messageText.text = "Failed to join lobby.";
    }

    private void ShowMessage(string message)
    {
        Show();
        messageText.text = message;
    }

    private void GameManagerMultiplayer_FailedToJoinGame(object sender, System.EventArgs e)
    {
        if (NetworkManager.Singleton.DisconnectReason == null || NetworkManager.Singleton.DisconnectReason == "")
        {
            ShowMessage("Failed to join the game.");
        }
        else
        {
            messageText.text = NetworkManager.Singleton.DisconnectReason;
        }
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }
    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
