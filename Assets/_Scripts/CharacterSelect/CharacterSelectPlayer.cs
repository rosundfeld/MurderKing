using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class CharacterSelectPlayer : MonoBehaviour
{

    [SerializeField] private int playerIndex;
    [SerializeField] private GameObject readyGameObject;
    [SerializeField] private TextMeshPro playerNameText;
    [SerializeField] private PlayerVisual playerVisual;
    [SerializeField] private Button kickButton;

    private void Awake()
    {
        kickButton.onClick.AddListener(() =>
        {
            PlayerData playerData = GameManagerMultiplayer.Instance.GetPlayerDataFromPlayerIndex(playerIndex);
            KitchenGameLobby.Instance.KickPlayer(playerData.playerId.ToString());
            GameManagerMultiplayer.Instance.KickPlayer(playerData.clientId);
        });
    }

    public void Start()
    {
        GameManagerMultiplayer.Instance.OnPlayerDataNetworkListChanged += GameManagerMultiplayer_OnPlayerDataNetworkListChanged;
        CharacterSelectReady.Instance.OnReadyChanged += CharacterSelectReady_OnReadyChanged;

        UpdatePlayer();
    }

    private void GameManagerMultiplayer_OnPlayerDataNetworkListChanged(object sender, System.EventArgs e)
    {
        UpdatePlayer();
    }

    private void CharacterSelectReady_OnReadyChanged(object sender, System.EventArgs e)
    {
        UpdatePlayer();
    }

    private void UpdatePlayer()
    {
        if (GameManagerMultiplayer.Instance.IsPlayerIndexConnected(playerIndex))
        {
            Show();

            PlayerData playerData = GameManagerMultiplayer.Instance.GetPlayerDataFromPlayerIndex(playerIndex);
            readyGameObject.SetActive(CharacterSelectReady.Instance.IsPlayerReady(playerData.clientId));

            playerNameText.text = playerData.playerName.ToString();

            // Only the host sees kick buttons, and never on their own slot
            kickButton.gameObject.SetActive(
                NetworkManager.Singleton.IsServer &&
                playerData.clientId != NetworkManager.Singleton.LocalClientId);

            playerVisual.SetPlayerColor(GameManagerMultiplayer.Instance.GetPlayerColor(playerData.colorId));
        }
        else
        {
            Hide();
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

    private void OnDestroy()
    {
        GameManagerMultiplayer.Instance.OnPlayerDataNetworkListChanged -= GameManagerMultiplayer_OnPlayerDataNetworkListChanged;
        CharacterSelectReady.Instance.OnReadyChanged -= CharacterSelectReady_OnReadyChanged;
    }
}
