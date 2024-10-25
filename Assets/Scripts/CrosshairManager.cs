using UnityEngine;
using UnityEngine.UI;

public class CrosshairManager : MonoBehaviour
{
    [Header("Crosshair UI Elements")]
    public RectTransform crosshairUp;
    public RectTransform crosshairDown;
    public RectTransform crosshairLeft;
    public RectTransform crosshairRight;
    public RectTransform crosshairCenter; // Include center crosshair

    [Header("Opacity Settings")]
    public float defaultCrosshairOpacity = 0.75f;   // Default crosshair opacity
    public float reloadingOpacity = 0.3f;           // Crosshair opacity when reloading
    public float aimingOpacity = 0f;                // Crosshair opacity when aiming
    public float fadeSpeed = 5f;                    // Speed at which the crosshairs fade in/out

    private float currentCrosshairDistance;         // Current distance from the center
    private float targetCrosshairDistance;          // Target distance to smoothly move towards
    private float smoothTime = 0.05f;               // Smooth time for damping
    private Vector2 velocityUp, velocityDown, velocityLeft, velocityRight;
    private float currentAlpha = 1f;                // Current alpha value
    private float targetAlpha = 1f;                 // Target alpha value

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;

        // Initialize crosshair positions and alpha
        currentCrosshairDistance = CalculateCrosshairDistance(0f);
        targetCrosshairDistance = currentCrosshairDistance;
        SetCrosshairAlpha(defaultCrosshairOpacity);
        currentAlpha = defaultCrosshairOpacity;
    }

    public void UpdateCrosshair(float spreadAngle, bool isAiming, bool isReloading)
    {
        // Set the target alpha based on whether we are aiming or reloading
        if (isAiming)
            targetAlpha = aimingOpacity;        // Fade to aiming opacity (0 when aiming)
        else if (isReloading)
            targetAlpha = reloadingOpacity;     // Fade to reloading opacity (0.3 when reloading)
        else
            targetAlpha = defaultCrosshairOpacity; // Default crosshair opacity

        // Calculate the distance in pixels based on the spread angle
        float distance = CalculateCrosshairDistance(spreadAngle);

        // Smoothly interpolate to the target distance
        targetCrosshairDistance = isAiming ? 0f : distance;
        currentCrosshairDistance = Mathf.Lerp(currentCrosshairDistance, targetCrosshairDistance, Time.deltaTime / smoothTime);

        // Smoothly interpolate the alpha value
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);

        // Update crosshair positions
        crosshairUp.anchoredPosition = Vector2.SmoothDamp(crosshairUp.anchoredPosition, new Vector2(0, currentCrosshairDistance), ref velocityUp, smoothTime);
        crosshairDown.anchoredPosition = Vector2.SmoothDamp(crosshairDown.anchoredPosition, new Vector2(0, -currentCrosshairDistance), ref velocityDown, smoothTime);
        crosshairLeft.anchoredPosition = Vector2.SmoothDamp(crosshairLeft.anchoredPosition, new Vector2(-currentCrosshairDistance, 0), ref velocityLeft, smoothTime);
        crosshairRight.anchoredPosition = Vector2.SmoothDamp(crosshairRight.anchoredPosition, new Vector2(currentCrosshairDistance, 0), ref velocityRight, smoothTime);

        // Update crosshair alpha
        SetCrosshairAlpha(currentAlpha);
    }

    private void SetCrosshairAlpha(float alpha)
    {
        SetAlpha(crosshairUp, alpha);
        SetAlpha(crosshairDown, alpha);
        SetAlpha(crosshairLeft, alpha);
        SetAlpha(crosshairRight, alpha);
        SetAlpha(crosshairCenter, alpha); // Include center crosshair in alpha adjustment
    }

    private void SetAlpha(RectTransform crosshairPart, float alpha)
    {
        if (crosshairPart != null)
        {
            Image image = crosshairPart.GetComponent<Image>();
            if (image != null)
            {
                Color color = image.color;
                color.a = alpha;
                image.color = color;
            }
        }
    }

    private float CalculateCrosshairDistance(float spreadAngle)
    {
        // Convert spread angle to radians
        float spreadRadians = spreadAngle * Mathf.Deg2Rad;

        // Assume a distance from the camera to the target plane
        float distanceToPlane = 10f;

        // Calculate the offset in world units
        float offset = Mathf.Tan(spreadRadians / 2f) * distanceToPlane;

        // Convert the world offset to screen space
        Vector3 offsetPosition = mainCamera.transform.position + mainCamera.transform.forward * distanceToPlane + mainCamera.transform.up * offset;
        Vector3 screenCenter = mainCamera.WorldToScreenPoint(mainCamera.transform.position + mainCamera.transform.forward * distanceToPlane);
        Vector3 screenOffset = mainCamera.WorldToScreenPoint(offsetPosition);

        // Calculate the pixel distance from center to offset
        float pixelDistance = Vector2.Distance(new Vector2(screenCenter.x, screenCenter.y), new Vector2(screenOffset.x, screenOffset.y));

        return pixelDistance;
    }
}
