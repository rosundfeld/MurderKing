using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public static class Loader
{

    public enum Scene
    {
        MainMenuScene,
        GameScene,
        Game,
        LoadingScene,
        LobbyScene,
        CharacterSelectionScene
    }
    private static Scene targetSceneIndex;

    public static void Load(Scene targetScene)
    {
        Loader.targetSceneIndex = targetScene;

        SceneManager.LoadScene(Scene.LoadingScene.ToString());
    }

    public static void LoadNetwork(Scene targetScene)
    {
        NetworkManager.Singleton.SceneManager.LoadScene(targetScene.ToString(), LoadSceneMode.Single);
    }

    public static void LoaderCallback()
    {
        SceneManager.LoadScene(targetSceneIndex.ToString());
    }
}
