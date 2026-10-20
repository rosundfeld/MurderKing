using System;
using Unity.Netcode;
using UnityEngine;

//Shared team money (GDD section 14). Most of it is meant to be earned in the calm period with real plates;
//feeding customers with the gun only pays a fraction of the order, and nothing extra for patience.
public class RushEconomy : NetworkBehaviour
{
    //---------------SINGLETON NETCODE-----------------
    public static RushEconomy Instance { get; private set; }

    //---------------EVENTS-----------------
    public event EventHandler OnMoneyChanged;
    public event EventHandler<OnMoneyEarnedEventArgs> OnMoneyEarned;
    public class OnMoneyEarnedEventArgs : EventArgs
    {
        public int amount;
        public bool byGun;
        public bool hadTip;
        public Vector3 position;
    }

    //---------------FIELDS-----------------
    [Header("Plate price")]
    [SerializeField] private int moneyPerIngredient = 5;
    [Tooltip("Tip over the order price when the customer was still Normal / Faminto")]
    [SerializeField, Range(0f, 1f)] private float normalTip = 0.5f;
    [SerializeField, Range(0f, 1f)] private float famintoTip = 0.25f;

    [Header("Feeding by gun")]
    [Tooltip("Fraction of the order price paid when the customer is fed with the ShotDog")]
    [SerializeField, Range(0f, 1f)] private float gunCalmMultiplier = 0.2f;
    [SerializeField, Range(0f, 1f)] private float gunRushMultiplier = 0.2f;

    //---------------NETWORK VARIABLES-----------------
    private NetworkVariable<int> money = new NetworkVariable<int>(0);
    private NetworkVariable<int> earnedByPlate = new NetworkVariable<int>(0);
    private NetworkVariable<int> earnedByGun = new NetworkVariable<int>(0);

    //---------------UNITY METHODS-----------------
    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        money.OnValueChanged += Money_OnValueChanged;
    }

    private void Money_OnValueChanged(int previousValue, int newValue)
    {
        OnMoneyChanged?.Invoke(this, EventArgs.Empty);
    }

    //---------------PUBLIC API-----------------
    //Server only
    public void RegisterFeed(RecipeSO recipeSO, CustomerMood mood, bool byGun, bool isRushHour, Vector3 position)
    {
        int orderPrice = recipeSO.kitchenObjectSOList.Count * moneyPerIngredient;

        int amount;
        bool hadTip = false;
        if (byGun)
        {
            amount = Mathf.Max(1, Mathf.RoundToInt(orderPrice * (isRushHour ? gunRushMultiplier : gunCalmMultiplier)));
            earnedByGun.Value += amount;
        }
        else
        {
            float tip = mood == CustomerMood.Normal ? normalTip : mood == CustomerMood.Faminto ? famintoTip : 0f;
            hadTip = tip > 0f;
            amount = Mathf.RoundToInt(orderPrice * (1f + tip));
            earnedByPlate.Value += amount;
        }

        money.Value += amount;
        MoneyEarnedClientRpc(amount, byGun, hadTip, position);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void MoneyEarnedClientRpc(int amount, bool byGun, bool hadTip, Vector3 position)
    {
        OnMoneyEarned?.Invoke(this, new OnMoneyEarnedEventArgs
        {
            amount = amount,
            byGun = byGun,
            hadTip = hadTip,
            position = position
        });
    }

    public int GetMoney()
    {
        return money.Value;
    }

    public int GetEarnedByPlate()
    {
        return earnedByPlate.Value;
    }

    public int GetEarnedByGun()
    {
        return earnedByGun.Value;
    }
}
