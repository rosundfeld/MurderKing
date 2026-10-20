using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Local player's ShotDog: shell counter, one icon per shell, empty warning and the log explaining every ammo change
public class ShotDogAmmoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI shellsText;
    [SerializeField] private Transform shellContainer;
    [SerializeField] private Image shellTemplate;
    //[SerializeField] private FeedLinesUI ammoFeedLinesUI;
    [SerializeField] private Color shellFullColor = new Color(0.85f, 0.35f, 0.2f);
    [SerializeField] private Color shellEmptyColor = new Color(1f, 1f, 1f, 0.15f);

    private PlayerShotDog shotDog;
    private readonly List<Image> shellImageList = new List<Image>();

    private void Awake()
    {
        shellTemplate.gameObject.SetActive(false);
    }

    private void Start()
    {
        //Hidden until the local player (and its ShotDog) exists
        Hide();
        if (Player.LocalInstance != null)
        {
            BindPlayer(Player.LocalInstance);
        }
        else
        {
            Player.OnAnyPlayerSpawned += Player_OnAnyPlayerSpawned;
        }
    }

    private void OnDestroy()
    {
        Player.OnAnyPlayerSpawned -= Player_OnAnyPlayerSpawned;
    }

    private void Player_OnAnyPlayerSpawned(object sender, EventArgs e)
    {
        if (Player.LocalInstance != null && shotDog == null)
        {
            BindPlayer(Player.LocalInstance);
        }
    }

    private void BindPlayer(Player player)
    {
        if (!player.TryGetComponent(out shotDog))
        {
            return;
        }

        shotDog.OnShellsChanged += ShotDog_OnShellsChanged;
        shotDog.OnAmmoEvent += ShotDog_OnAmmoEvent;

        for (int i = 0; i < shotDog.GetMagazineSize(); i++)
        {
            Image shellImage = Instantiate(shellTemplate, shellContainer);
            shellImage.gameObject.SetActive(true);
            shellImageList.Add(shellImage);
        }

        UpdateVisual();
        Show();
    }

    private void ShotDog_OnShellsChanged(object sender, EventArgs e)
    {
        UpdateVisual();
    }

    private void ShotDog_OnAmmoEvent(object sender, PlayerShotDog.OnAmmoEventArgs e)
    {
        //ammoFeedLinesUI.AddLine(e.isWarning ? $"<color=#FF8080>{e.message}</color>" : e.message);
    }

    private void UpdateVisual()
    {
        int shells = shotDog.GetShells();
        shellsText.text = $"{shells}/{shotDog.GetMagazineSize()}";

        for (int i = 0; i < shellImageList.Count; i++)
        {
            shellImageList[i].color = i < shells ? shellFullColor : shellEmptyColor;
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
