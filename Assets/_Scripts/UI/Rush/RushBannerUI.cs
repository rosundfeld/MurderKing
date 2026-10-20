using System;
using TMPro;
using UnityEngine;

//Big temporary announcements: Rush Hour started, a new wave of customers arrived
public class RushBannerUI : MonoBehaviour
{
    [SerializeField] private GameObject bannerGameObject;
    [SerializeField] private TextMeshProUGUI bannerText;
    [SerializeField] private float rushStartedDuration = 4f;
    [SerializeField] private float burstDuration = 2f;
    [SerializeField, TextArea] private string rushStartedMessage = "HORA DO RUSH!\nlarguem a cozinha e peguem a ShotDog";
    [SerializeField, TextArea] private string burstMessage = "MAIS UMA LEVA!\num escritório inteiro acabou de sair...";

    private float bannerTimer;

    private void Start()
    {
        CustomerManager.Instance.OnRushHourStarted += CustomerManager_OnRushHourStarted;
        CustomerManager.Instance.OnBurst += CustomerManager_OnBurst;
        Hide();
    }

    private void CustomerManager_OnRushHourStarted(object sender, EventArgs e)
    {
        Show(rushStartedMessage, rushStartedDuration);
    }

    private void CustomerManager_OnBurst(object sender, EventArgs e)
    {
        Show(burstMessage, burstDuration);
    }

    private void Update()
    {
        if (bannerTimer <= 0f)
        {
            return;
        }

        bannerTimer -= Time.deltaTime;
        if (bannerTimer <= 0f)
        {
            Hide();
        }
    }

    private void Show(string message, float duration)
    {
        bannerText.text = message;
        bannerTimer = duration;
        bannerGameObject.SetActive(true);
    }

    private void Hide()
    {
        bannerGameObject.SetActive(false);
    }
}
