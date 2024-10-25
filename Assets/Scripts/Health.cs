using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    // Method to apply damage
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        // Clamp health to zero
        currentHealth = Mathf.Max(currentHealth, 0);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Method called when health reaches zero
    private void Die()
    {
        // Destroy the game object or implement death behavior
        Destroy(gameObject);
    }
}
