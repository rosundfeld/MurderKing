using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterColorSelectSingleUI : MonoBehaviour
{
    [SerializeField] int colorId;
    [SerializeField] Image image;
    [SerializeField] GameObject selectedGameObject;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            GameManagerMultiplayer.Instance.ChangePlayerColor(colorId);
            UpdateIsSelected();
        });
    }

    private void Start()
    {
        GameManagerMultiplayer.Instance.OnPlayerDataNetworkListChanged += GameManagerMultiplayer_OnPlayerDataNetworkListChanged;
        image.color = GameManagerMultiplayer.Instance.GetPlayerColor(colorId);
        UpdateIsSelected();
    }

    private void UpdateIsSelected()
    {
        if (GameManagerMultiplayer.Instance.GetPlayerData().colorId == colorId)
        {
            selectedGameObject.SetActive(true);
        }
        else
        {
            selectedGameObject.SetActive(false);
        }
    }

    private void GameManagerMultiplayer_OnPlayerDataNetworkListChanged(object sender, System.EventArgs e)
    {
        UpdateIsSelected();
    }

    private void OnDestroy()
    {
        GameManagerMultiplayer.Instance.OnPlayerDataNetworkListChanged -= GameManagerMultiplayer_OnPlayerDataNetworkListChanged;
    }
}
