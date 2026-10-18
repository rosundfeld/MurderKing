using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class TestingNetcodeUI : MonoBehaviour
{
    [SerializeField] private Button startHostBtn;
    [SerializeField] private Button startClientBtn;


    //NetworkManager and GameManagerMultiplayer live in the LobbyScene (DontDestroyOnLoad), so pressing Play
    //here has neither. The buttons boot through the lobby, which loads this scene back over the network.
    private void Awake()
    {
        if (GameManagerMultiplayer.Instance != null)
        {
            //Arrived through the normal flow, the game is already networked
            Hide();
            return;
        }

        startHostBtn.onClick.AddListener(() =>
        {
            Debug.Log("Starting Host...");
            //Singleplayer mode: GameManagerMultiplayer.Start hosts and loads the Game scene by itself
            GameManagerMultiplayer.playMultiplayer = false;
            LoadLobby();
        });

        startClientBtn.onClick.AddListener(() =>
        {
            Debug.Log("Starting Client...");
            //Clients are only approved in the CharacterSelectionScene, so joining has to go through the lobby
            GameManagerMultiplayer.playMultiplayer = true;
            LoadLobby();
        });
    }

    private void LoadLobby()
    {
        //This path skips the MainMenuScene's ResetStaticDataManager, so this scene's objects would stay
        //subscribed to static events after being destroyed (MissingReferenceException on the reload)
        ResetStaticDataManager.ResetAll();
        Loader.Load(Loader.Scene.LobbyScene);
    }


    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
