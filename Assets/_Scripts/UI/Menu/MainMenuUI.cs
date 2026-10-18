using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button playMultiplayerButton;
    [SerializeField] private Button playSingleplayerButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button optionsButton;


    private void Awake()
    {
        playMultiplayerButton.onClick.AddListener(() =>
        {
            //Static, so it keeps the last choice: set it explicitly or a previous singleplayer run skips the lobby
            GameManagerMultiplayer.playMultiplayer = true;
            Loader.Load(Loader.Scene.LobbyScene);
        });

        playSingleplayerButton.onClick.AddListener(() =>
        {
            GameManagerMultiplayer.playMultiplayer = false;
            Loader.Load(Loader.Scene.LobbyScene);
        });

        quitButton.onClick.AddListener(() =>
        {
            Application.Quit();
        });

        // optionsButton.onClick.AddListener(() =>
        // {
        //     OptionsUI.Instance.Show(Show);
        // });

        Time.timeScale = 1f;
    }

}
