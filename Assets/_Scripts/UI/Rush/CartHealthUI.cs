using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CartHealthUI : MonoBehaviour
{
    [SerializeField] private Image healthBarImage;
    [SerializeField] private Gradient healthColorGradient;

    private void Update()
    {
        float healthNormalized = CartHealth.Instance.GetHealthNormalized();

        healthBarImage.fillAmount = healthNormalized;
        healthBarImage.color = healthColorGradient.Evaluate(healthNormalized);
    }
}
