using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class StoveCounter : BaseCounter, IHasProgress
{

    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;

    public class OnStateChangedEventArgs : EventArgs
    {
        public State state;
    }
    public enum State
    {
        Idle,
        Frying,
        Fried,
        Burned
    }

    [SerializeField] private FryingRecipeSO[] fryingRecipeSOArray;
    [SerializeField] private BurningRecipeSO[] burningRecipeSOArray;

    private NetworkVariable<State> state = new NetworkVariable<State>(State.Idle);
    private NetworkVariable<float> fryingTimer = new NetworkVariable<float>(0f);
    private NetworkVariable<float> burningTimer = new NetworkVariable<float>(0f);
    private FryingRecipeSO fryingRecipeSO;
    private BurningRecipeSO burningRecipeSO;

    //-----------------------COROUTINES-----------------------//
    // private IEnumerator HandleFryTimer()
    // {
    //     yield return new WaitForSeconds(1f);
    // }

    // private void Start()
    // {
    //     StartCoroutine(HandleFryTimer());
    // }


    public override void OnNetworkSpawn()
    {
        fryingTimer.OnValueChanged += FryingTimer_OnValueChanged;
        burningTimer.OnValueChanged += BurningTimer_OnValueChanged;
        state.OnValueChanged += State_OnValueChanged;

    }

    private void State_OnValueChanged(State previousValue, State newValue)
    {
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs
        {
            state = newValue
        });

        if (state.Value == State.Idle || state.Value == State.Burned)
        {
            OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
            {
                progressNormalized = 0f
            });
        }
    }

    private void FryingTimer_OnValueChanged(float previousValue, float newValue)
    {
        float fryingTimerMax = fryingRecipeSO != null ? fryingRecipeSO.fryingTimerMax : 1f;

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
        {
            progressNormalized = fryingTimer.Value / fryingTimerMax
        });
    }

    private void BurningTimer_OnValueChanged(float previousValue, float newValue)
    {
        float burningTimerMax = burningRecipeSO != null ? burningRecipeSO.burningTimerMax : 1f;

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
        {
            progressNormalized = burningTimer.Value / burningTimerMax
        });
    }


    private void Update()
    {
        if (!IsServer)
            return;

        if (HasKitchenObject())
        {
            switch (state.Value)
            {
                case State.Idle:
                    break;
                case State.Frying:
                    Frying();
                    break;
                case State.Fried:
                    Fried();
                    break;
                case State.Burned:
                    break;
            }
        }
    }

    private void Frying()
    {
        fryingTimer.Value += Time.deltaTime;

        if (fryingTimer.Value > fryingRecipeSO.fryingTimerMax)
        {
            //Fried
            KitchenObject.DestroyKitchenObject(GetKitchenObject());

            KitchenObject.SpawnKitchenObject(fryingRecipeSO.output, this);

            state.Value = State.Fried;

            burningTimer.Value = 0f;

            SetBurningRecipeSOClientRpc(GameManagerMultiplayer.Instance.GetKitchenObjectSOIndex(GetKitchenObject().GetKitchenObjectSO()));
        }
    }

    private void Fried()
    {
        burningTimer.Value += Time.deltaTime;

        if (burningTimer.Value > burningRecipeSO.burningTimerMax)
        {
            //Burned
            KitchenObject.DestroyKitchenObject(GetKitchenObject());

            KitchenObject.SpawnKitchenObject(burningRecipeSO.output, this);

            state.Value = State.Burned;
        }
    }

    override public void Interact(Player player)
    {
        if (!HasKitchenObject())
        {
            //There is no kitchen object here
            if (player.HasKitchenObject())
            {
                //Player is carrying something
                if (HasRecipeWithInput(player.GetKitchenObject().GetKitchenObjectSO()))
                {
                    KitchenObject kitchenObject = player.GetKitchenObject();
                    kitchenObject.SetKitchenObjectParent(this);

                    InteractLogicPlaceObjectOnCounterServerRpc(
                        GameManagerMultiplayer.Instance.GetKitchenObjectSOIndex(kitchenObject.GetKitchenObjectSO())
                    );
                }
            }
            else
            {
                //Player is not carrying something
            }
        }
        else
        {
            //There is a kitchen Object here
            if (player.HasKitchenObject())
            {
                if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject))
                {
                    //Player is holding a plate
                    if (plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO()))
                    {
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());

                        SetStateIdleServerRpc();
                    }
                }
            }
            else
            {
                //Player is not carrying something
                GetKitchenObject().SetKitchenObjectParent(player);

                SetStateIdleServerRpc();
            }
        }

    }

    [Rpc(SendTo.Server)]
    private void SetStateIdleServerRpc()
    {
        state.Value = State.Idle;
    }

    [Rpc(SendTo.Server)]
    private void InteractLogicPlaceObjectOnCounterServerRpc(int kitchenObjectSOIndex)
    {
        fryingTimer.Value = 0f;
        state.Value = State.Frying;

        SetFryingRecipeSOClientRpc(kitchenObjectSOIndex);
    }

    [ClientRpc]
    private void SetFryingRecipeSOClientRpc(int kitchenObjectSOIndex)
    {
        KitchenObjectSO kitchenObjectSO = GameManagerMultiplayer.Instance.GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);

        fryingRecipeSO = GetFryingRecipeSOWithInput(kitchenObjectSO);
    }

    [ClientRpc]
    private void SetBurningRecipeSOClientRpc(int kitchenObjectSOIndex)
    {
        KitchenObjectSO kitchenObjectSO = GameManagerMultiplayer.Instance.GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);

        burningRecipeSO = GetBurningRecipeSOWithInput(kitchenObjectSO);
    }

    private bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        FryingRecipeSO fryingRecipeSO = GetFryingRecipeSOWithInput(inputKitchenObjectSO);
        if (fryingRecipeSO != null)
        {
            if (fryingRecipeSO.input == inputKitchenObjectSO)
            {
                return true;
            }
        }

        return false;
    }

    private KitchenObjectSO GetOutputForInput(KitchenObjectSO inputKitchenObjectSO)
    {
        FryingRecipeSO fryingRecipeSO = GetFryingRecipeSOWithInput(inputKitchenObjectSO);
        if (fryingRecipeSO != null)
        {
            return fryingRecipeSO.output;
        }
        return null;
    }

    private FryingRecipeSO GetFryingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        foreach (FryingRecipeSO fryingRecipeSO in fryingRecipeSOArray)
        {
            if (fryingRecipeSO.input == inputKitchenObjectSO)
            {
                return fryingRecipeSO;
            }
        }
        return null;
    }

    private BurningRecipeSO GetBurningRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        foreach (BurningRecipeSO burningRecipeSO in burningRecipeSOArray)
        {
            if (burningRecipeSO.input == inputKitchenObjectSO)
            {
                return burningRecipeSO;
            }
        }
        return null;
    }

    public bool IsFried()
    {
        return state.Value == State.Fried;
    }
}
