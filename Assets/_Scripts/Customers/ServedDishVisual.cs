using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

//One cooked dish in the table service, shown stacked on the DeliveryCounter or on a waiter's tray.
//Lives on a visual-only copy of the Plate (model + PlateCompleteVisual, no NetworkObject / PlateKitchenObject).
//Dishes aren't tied to a table: the name tells the waiters what it is, they decide who gets it.
public class ServedDishVisual : MonoBehaviour
{
    [SerializeField] private PlateCompleteVisual plateCompleteVisual;
    [Tooltip("Recipe name in bold so it reads from the top-down camera")]
    [FormerlySerializedAs("tableNumberText")]
    [SerializeField] private TextMeshProUGUI recipeNameText;

    public void SetDish(CustomerManager.DishInfo dishInfo)
    {
        if (dishInfo.recipeSO == null)
        {
            recipeNameText.text = "";
            return;
        }
        plateCompleteVisual.ShowIngredients(dishInfo.recipeSO.kitchenObjectSOList);
        recipeNameText.text = $"<b>{dishInfo.recipeSO.recipeName}</b>";
    }
}
