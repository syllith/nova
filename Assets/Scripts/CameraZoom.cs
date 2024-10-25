using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    public float zoomSpeed = 10.0f;
    public float originalFOV = 60.0f;
    public float zoomedFOV = 30.0f;

    private bool isZooming = false;

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isZooming = true;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            isZooming = false;
        }

        if (isZooming)
        {
            float currentFOV = Camera.main.fieldOfView;
            float targetFOV = zoomedFOV;
            float newFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * zoomSpeed);
            Camera.main.fieldOfView = newFOV;
        }
        else
        {
            float currentFOV = Camera.main.fieldOfView;
            float targetFOV = originalFOV;
            float newFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * zoomSpeed);
            Camera.main.fieldOfView = newFOV;
        }
    }
}
