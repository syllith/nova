using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    private Image healthBarImage;

    private void Awake()
    {
        healthBarImage = GetComponent<Image>();
    }

    public void SetHealth(float healthNormalized)
    {
        healthBarImage.fillAmount = healthNormalized;
    }
}
