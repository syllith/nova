using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    public Sprite crosshairSprite; // Assign this in the Inspector

    void Start()
    {
        // Create a new Canvas
        GameObject canvasObject = new GameObject("CrosshairCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        // Create a new Image using the crosshair sprite
        GameObject imageObject = new GameObject("CrosshairImage");
        imageObject.transform.SetParent(canvasObject.transform);
        Image crosshairImage = imageObject.AddComponent<Image>();
        crosshairImage.sprite = crosshairSprite;

        // Set the position and size of the crosshair
        RectTransform rectTransform = crosshairImage.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = Vector2.zero; // Center of the screen
        rectTransform.sizeDelta = new Vector2(10, 10); // Size of the crosshair, adjust as needed
    }
}
