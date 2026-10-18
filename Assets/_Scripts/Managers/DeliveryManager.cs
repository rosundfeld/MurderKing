using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;

public class DeliveryManager : NetworkBehaviour
{
    //---------------SINGLETON NETCODE-----------------
    public static DeliveryManager Instance { get; private set; }

    //---------------EVENTS-----------------
    public event EventHandler OnRecipeSpawned;
    public event EventHandler OnRecipeCompleted;
    public event EventHandler OnRecipeSuccess;
    public event EventHandler OnRecipeFailed;

    //---------------FIELDS-----------------
    [SerializeField] private RecipeListSO recipeListSO;

    //---------------PRIVATE VARIABLES-----------------
    private List<RecipeSO> waitingRecipeSOList;
    private float spawnRecipeTimer;
    private float spawnRecipeTimerMax = 4f;
    private int waitingRecipesMax = 4;
    private int successfulRecipesAmount;

    //---------------UNITY METHODS-----------------
    private void Awake()
    {
        waitingRecipeSOList = new List<RecipeSO>();
        Instance = this;
    }

    private void Update()
    {
        if (!IsServer) return;

        spawnRecipeTimer -= Time.deltaTime;
        if (spawnRecipeTimer <= 0f)
        {
            spawnRecipeTimer = spawnRecipeTimerMax;

            if (waitingRecipeSOList.Count < waitingRecipesMax && GameManager.Instance.IsGamePlaying())
            {
                int waitingRecipeSOIndex = UnityEngine.Random.Range(0, recipeListSO.recipeSOList.Count);

                SpawnNewWaitingRecipeClientRpc(waitingRecipeSOIndex);
            }
        }
    }

    [ClientRpc]
    private void SpawnNewWaitingRecipeClientRpc(int waitingRecipeSOIndex)
    {
        RecipeSO waitingRecipeSO = recipeListSO.recipeSOList[waitingRecipeSOIndex];

        waitingRecipeSOList.Add(waitingRecipeSO);
        OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
    }

    public void DeliverRecipe(PlateKitchenObject plateKitchenObject)
    {

        for (int i = 0; i < waitingRecipeSOList.Count; i++)
        {
            RecipeSO waitingRecipeSO = waitingRecipeSOList[i];


            if (waitingRecipeSO.kitchenObjectSOList.Count == plateKitchenObject.GetKitchenObjectSOList().Count)
            {
                // Has the same number of ingredients
                bool plateContentsMatchesRecipe = true;
                foreach (KitchenObjectSO recipeKitchenObjectSO in waitingRecipeSO.kitchenObjectSOList)
                {
                    // Cycling through all ingredients in the recipe
                    bool ingredientFound = false;
                    foreach (KitchenObjectSO plateKitchenObjectSO in plateKitchenObject.GetKitchenObjectSOList())
                    {
                        // Cycling through all ingredients on the plate
                        if (plateKitchenObjectSO == recipeKitchenObjectSO)
                        {
                            // Ingredient matches!
                            ingredientFound = true;
                            break;
                        }
                    }

                    if (!ingredientFound)
                    {
                        // This recipe ingredient was not found on the plate
                        plateContentsMatchesRecipe = false;
                    }
                }

                if (plateContentsMatchesRecipe)
                {
                    // Player delivered the correct recipe!
                    DeliverCorrectRecipeServerRpc(i);
                    return;
                }
            }
        }
        // Player did not deliver a correct recipe
        OnRecipeFailed?.Invoke(this, EventArgs.Empty);
        DeliverIncorrectRecipeServerRpc();
    }

    // Fluxo de rede: o cliente solicita a entrega correta via ServerRpc, o servidor valida/autoritiza,
    // e então o ClientRpc replica o resultado para todos (remove receita, soma pontuação e dispara eventos)
    // mantendo o estado sincronizado entre host e clientes.
    [Rpc(SendTo.Server)]
    private void DeliverCorrectRecipeServerRpc(int waitingRecipeSOIndex)
    {
        // Implementation for delivering the correct recipe on the server
        DeliverCorrectRecipeClientRpc(waitingRecipeSOIndex);

    }

    [ClientRpc]
    private void DeliverCorrectRecipeClientRpc(int waitingRecipeSOIndex)
    {
        // Implementation for delivering the correct recipe on the client
        waitingRecipeSOList.RemoveAt(waitingRecipeSOIndex);
        successfulRecipesAmount++;
        OnRecipeCompleted?.Invoke(this, EventArgs.Empty);
        OnRecipeSuccess?.Invoke(this, EventArgs.Empty);
    }

    [Rpc(SendTo.Server)]
    private void DeliverIncorrectRecipeServerRpc()
    {
        // Implementation for delivering the incorrect recipe on the server
        DeliverIncorrectRecipeClientRpc();

    }

    [ClientRpc]
    private void DeliverIncorrectRecipeClientRpc()
    {
        // Implementation for delivering the incorrect recipe on the client
        //waitingRecipeSOList.RemoveAt(waitingRecipeSOIndex);
        OnRecipeFailed?.Invoke(this, EventArgs.Empty);
    }

    public List<RecipeSO> GetWaitingRecipeSOList()
    {
        return waitingRecipeSOList;
    }

    public int GetSuccessfulRecipesAmount()
    {
        return successfulRecipesAmount;
    }

}
