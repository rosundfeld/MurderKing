using System;
using Unity.Netcode;
using UnityEngine;

//GDD section 21: the run is lost when the cart collapses, not when a player "dies"
public class CartHealth : NetworkBehaviour
{
    //---------------SINGLETON NETCODE-----------------
    public static CartHealth Instance { get; private set; }

    //---------------EVENTS-----------------
    public event EventHandler OnDamaged;

    //---------------FIELDS-----------------
    [SerializeField] private float maxHealth = 100f;

    //---------------NETWORK VARIABLES-----------------
    private NetworkVariable<float> health = new NetworkVariable<float>(100f);

    //---------------PRIVATE VARIABLES-----------------
    private float lastWarningHealth;

    //---------------UNITY METHODS-----------------
    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            health.Value = maxHealth;
        }
        lastWarningHealth = maxHealth;
        health.OnValueChanged += Health_OnValueChanged;
    }

    private void Health_OnValueChanged(float previousValue, float newValue)
    {
        //Only pulse the warning every 5 HP, damage ticks every frame
        if (newValue <= lastWarningHealth - 5f)
        {
            lastWarningHealth = newValue;
            OnDamaged?.Invoke(this, EventArgs.Empty);
        }
    }

    //---------------PUBLIC API-----------------
    //Server only
    public void TakeDamage(float amount)
    {
        if (IsDestroyed())
        {
            return;
        }

        health.Value = Mathf.Max(0f, health.Value - amount);
        if (IsDestroyed())
        {
            GameManager.Instance.TriggerGameOver();
        }
    }

    public bool IsDestroyed()
    {
        return health.Value <= 0f;
    }

    public float GetHealthNormalized()
    {
        return health.Value / maxHealth;
    }
}
