using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI recipesDeliveredText;
    [SerializeField] private Button playAgainButton;

    [Header("Rush (optional, only in scenes with CustomerManager)")]
    [SerializeField] private TextMeshProUGUI rushResultText;
    [SerializeField] private TextMeshProUGUI moneyEarnedText;

    private void Start()
    {
        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
        Hide();
    }

    private void Awake()
    {
        playAgainButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.Shutdown();
            Loader.Load(Loader.Scene.GameScene);
        });
    }

    private void GameManager_OnStateChanged(object sender, System.EventArgs e)
    {
        if (GameManager.Instance.IsGameOver())
        {
            Show();

            if (CustomerManager.Instance != null)
            {
                ShowRushResult();
            }
            else
            {
                recipesDeliveredText.text = DeliveryManager.Instance.GetSuccessfulRecipesAmount().ToString();
            }
        }
        else
        {
            Hide();
        }
    }
    private void ShowRushResult()
    {
        recipesDeliveredText.text = $"{CustomerManager.Instance.GetCustomersServed()} ({CustomerManager.Instance.GetCustomersFedByGun()} na bala)";

        if (rushResultText != null)
        {
            rushResultText.text = CartHealth.Instance.IsDestroyed() ? "CARRINHO DESTRUÍDO" : "VOCÊ SOBREVIVEU AO RUSH!";
        }
        if (moneyEarnedText != null)
        {
            RushEconomy rushEconomy = RushEconomy.Instance;
            moneyEarnedText.text = $"${rushEconomy.GetMoney()} (pratos ${rushEconomy.GetEarnedByPlate()} · bala ${rushEconomy.GetEarnedByGun()})";
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
