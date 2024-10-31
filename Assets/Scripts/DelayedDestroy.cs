using System.Collections;
using UnityEngine;

public class DelayedDestroy : MonoBehaviour
{
    [SerializeField]
    private float destroyDelay = 5f; // Set the delay in seconds in the Inspector

    void Start()
    {
        Destroy(gameObject, destroyDelay);
    }
}
