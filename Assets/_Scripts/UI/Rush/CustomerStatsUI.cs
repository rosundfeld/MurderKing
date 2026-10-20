using TMPro;
using UnityEngine;

//How many customers were served and how many are still out there (and how many are furious)
public class CustomerStatsUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI customersServedText;
    [SerializeField] private TextMeshProUGUI customersOnStreetText;
    [SerializeField] private TextMeshProUGUI furiousCustomersText;

    private void Update()
    {
        CustomerManager customerManager = CustomerManager.Instance;

        customersServedText.text = customerManager.GetCustomersServed().ToString();
        customersOnStreetText.text = customerManager.GetCustomerCount().ToString();
        furiousCustomersText.text = customerManager.GetFuriousCustomerCount().ToString();
    }
}
