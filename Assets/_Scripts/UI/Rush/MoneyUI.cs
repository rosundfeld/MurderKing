using TMPro;
using UnityEngine;

//Team cash register and the feed of what each customer paid (plate vs gun)
public class MoneyUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private FeedLinesUI moneyFeedLinesUI;

    private void Start()
    {
        RushEconomy.Instance.OnMoneyChanged += RushEconomy_OnMoneyChanged;
        RushEconomy.Instance.OnMoneyEarned += RushEconomy_OnMoneyEarned;
        UpdateVisual();
    }

    private void RushEconomy_OnMoneyChanged(object sender, System.EventArgs e)
    {
        UpdateVisual();
    }

    private void RushEconomy_OnMoneyEarned(object sender, RushEconomy.OnMoneyEarnedEventArgs e)
    {
        string line = e.byGun
            ? $"<color=#FFD700>+${e.amount}</color> na bala"
            : $"<color=#7CFC00>+${e.amount}</color> prato" + (e.hadTip ? " (com gorjeta)" : "");
        moneyFeedLinesUI.AddLine(line);
    }

    private void UpdateVisual()
    {
        moneyText.text = $"${RushEconomy.Instance.GetMoney()}";
    }
}
