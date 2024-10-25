using UnityEngine;
using TMPro; // Namespace for TextMeshPro

public class BaseHealth : MonoBehaviour
{
    public int health = 100;
    public TextMeshProUGUI healthText; // Assign this in the Inspector

    private void UpdateHealthDisplay()
    {
        healthText.text = "Health: " + health;
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        UpdateHealthDisplay();

        if (health <= 0)
        {
            // Handle the base's destruction or game over logic here
        }
    }

    private void Start()
    {
        UpdateHealthDisplay();
    }
}
