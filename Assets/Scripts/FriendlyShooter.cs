using System.Collections;
using UnityEngine;

public class FriendlyShooter : MonoBehaviour
{
    public float fireRate = 1f; // Shots per second
    public int damage = 25;
    public LineRenderer bulletTrace; // Assign a LineRenderer in the Inspector

    private float nextTimeToFire = 0f;
    private Coroutine shootingCoroutine;

    // Define the shooting range limits
    public float minShootingDistance = 2f; // Minimum distance to shoot
    public float maxShootingDistance = 20f; // Maximum distance to shoot

    void Update()
    {
        if (Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + 1f / fireRate;
            EnemyAI targetEnemy = FindClosestEnemy();
            if (targetEnemy != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, targetEnemy.transform.position);
                if (distanceToTarget >= minShootingDistance && distanceToTarget <= maxShootingDistance)
                {
                    Shoot(targetEnemy);
                }
            }
        }
    }

    void Shoot(EnemyAI target)
    {
        target.TakeDamage(damage);

        if (shootingCoroutine != null)
        {
            StopCoroutine(shootingCoroutine); // Stop the current coroutine if it's running
        }
        shootingCoroutine = StartCoroutine(ShowBulletTrace(target.transform.position));
    }

    EnemyAI FindClosestEnemy()
    {
        EnemyAI[] enemies = FindObjectsOfType<EnemyAI>();
        EnemyAI closest = null;
        float closestDistance = float.MaxValue;

        foreach (EnemyAI enemy in enemies)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy < closestDistance)
            {
                closest = enemy;
                closestDistance = distanceToEnemy;
            }
        }

        // Check if the closest enemy is within the shooting range limits
        if (closestDistance >= minShootingDistance && closestDistance <= maxShootingDistance)
        {
            return closest;
        }
        else
        {
            return null; // No valid target within range
        }
    }

    IEnumerator ShowBulletTrace(Vector3 targetPosition)
    {
        bulletTrace.SetPosition(0, transform.position);
        bulletTrace.SetPosition(1, targetPosition);
        bulletTrace.enabled = true;

        float duration = 0.05f; // Shorter visibility duration
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            bulletTrace.material.SetColor("_Color", new Color(1f, 1f, 1f, alpha));
            yield return null;
        }

        bulletTrace.enabled = false;
    }
}
