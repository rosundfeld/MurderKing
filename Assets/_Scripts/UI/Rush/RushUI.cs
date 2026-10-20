using TMPro;
using UnityEngine;

//Current day phase (Calmaria / Hora do Rush) and how long until it changes
public class RushUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI phaseText;
    [SerializeField] private TextMeshProUGUI phaseTimerText;
    [SerializeField] private GameObject calmGameObject;
    [SerializeField] private GameObject rushGameObject;

    private void Start()
    {
        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
        Hide();
    }

    private void GameManager_OnStateChanged(object sender, System.EventArgs e)
    {
        if (GameManager.Instance.IsGamePlaying())
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    private void Update()
    {
        bool isRushHour = CustomerManager.Instance.IsRushHour();

        calmGameObject.SetActive(!isRushHour);
        rushGameObject.SetActive(isRushHour);

        if (isRushHour)
        {
            phaseText.text = "HORA DO RUSH";
            phaseTimerText.text = "termina em " + FormatTime(GameManager.Instance.GetGamePlayingTimeRemaining());
        }
        else
        {
            phaseText.text = "CALMARIA";
            phaseTimerText.text = "rush em " + FormatTime(CustomerManager.Instance.GetSecondsUntilRush());
        }
    }

    private static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.CeilToInt(seconds);
        return $"{totalSeconds / 60}:{totalSeconds % 60:00}";
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
