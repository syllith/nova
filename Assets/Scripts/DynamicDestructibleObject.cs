using UnityEngine;

public class DynamicDestructibleObject : MonoBehaviour
{
    [Header("Bullet Hole Settings")]
    public Texture2D bulletHoleTexture;
    public float dentDepth = 0.1f;
    public float bulletHoleSize = 0.05f;
    public GameObject bulletHoleEffectorPrefab;  // Prefab of the Shape Effector

    private Material material;

    private void Start()
    {
        material = GetComponent<MeshRenderer>().material;

        // Enable POM for depth effect
        material.EnableKeyword("_PARALLAX_OCCLUSION_MAPPING");

        // Set bullet hole texture and parameters on the specific layer
        material.SetTexture("_Layer1Tex", bulletHoleTexture);
        material.SetFloat("_Layer1ParallaxDepth", dentDepth);
        material.SetFloat("_Layer1Scale", bulletHoleSize);
        
        // Initial invisibility for bullet holes
        material.SetFloat("_Layer1Opacity", 0.0f);
    }

    public void ApplyBulletHole(Vector3 impactPoint)
    {
        // Create and configure effector at impact point
        GameObject effector = Instantiate(bulletHoleEffectorPrefab, impactPoint, Quaternion.identity);
        effector.transform.SetParent(transform);
        effector.transform.localScale = Vector3.one * bulletHoleSize;

        // Make bullet hole texture layer visible
        material.SetFloat("_Layer1Opacity", 1.0f);
    }
}
