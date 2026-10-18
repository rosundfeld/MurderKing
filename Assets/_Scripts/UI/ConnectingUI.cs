using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectingUI : MonoBehaviour
{

    private void Start()
    {
        GameManagerMultiplayer.Instance.OnTryingToJoinGame += GameManagerMultiplayer_OnTryingToJoinGame;
        GameManagerMultiplayer.Instance.FailedToJoinGame += GameManagerMultiplayer_FailedToJoinGame;
        Hide();
    }

    private void OnDestroy()
    {
        GameManagerMultiplayer.Instance.OnTryingToJoinGame -= GameManagerMultiplayer_OnTryingToJoinGame;
        GameManagerMultiplayer.Instance.FailedToJoinGame -= GameManagerMultiplayer_FailedToJoinGame;
    }
    
    private void GameManagerMultiplayer_OnTryingToJoinGame(object sender, System.EventArgs e)
    {
        Show();
    }

    private void GameManagerMultiplayer_FailedToJoinGame(object sender, System.EventArgs e)
    {
        Hide();
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
