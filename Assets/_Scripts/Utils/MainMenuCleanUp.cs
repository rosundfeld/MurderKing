using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MainMenuCleanUp : MonoBehaviour
{
    private void Awake()
    {
        if(NetworkManager.Singleton != null)
        {
            Destroy(NetworkManager.Singleton.gameObject);
        }

        if(GameManagerMultiplayer.Instance != null)
        {
            Destroy(GameManagerMultiplayer.Instance.gameObject);
        }

        if(KitchenGameLobby.Instance != null)
        {
            Destroy(KitchenGameLobby.Instance.gameObject);
        }
    }
}
