using UnityEngine;
using System.Collections;

public class Weapon : MonoBehaviour
{
    public GameObject gun;
    public GameObject slide;
    public GameObject hammer;
    public float hammerDropSpeed = 200f; // Speed in milliseconds for the hammer to drop

    private Quaternion originalHammerRotation;
    private Vector3 originalSlidePosition;
    private Vector3 originalGunPosition;
    private Quaternion originalGunRotation;

    void Start()
    {
        // Save the original positions and rotations
        if (hammer != null)
            originalHammerRotation = hammer.transform.localRotation;

        if (slide != null)
            originalSlidePosition = slide.transform.localPosition;

        if (gun != null)
        {
            originalGunPosition = gun.transform.localPosition;
            originalGunRotation = gun.transform.localRotation;
        }

        // Set the initial hammer rotation
        if (hammer != null)
            hammer.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
    }

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            StartCoroutine(FireWeapon());
        }
    }

    private IEnumerator FireWeapon()
    {
        // Animate the hammer dropping
        float hammerDropDuration = hammerDropSpeed / 1000f; // Convert milliseconds to seconds
        float timer = 0;
        while (timer < hammerDropDuration)
        {
            if (hammer != null)
            {
                float rotationX = Mathf.Lerp(70f, 0f, timer / hammerDropDuration);
                hammer.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // Begin firing sequence
        StartCoroutine(FiringSequence());
    }

    private IEnumerator FiringSequence()
    {
        float duration = 0.5f; // Duration of the firing sequence, adjust as needed

        // Animate gun, slide, and hammer
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;

            // Animate slide
            if (slide != null)
            {
                float slideZ = Mathf.Lerp(0f, 0.1f, t);
                slide.transform.localPosition = new Vector3(originalSlidePosition.x, originalSlidePosition.y, slideZ);
            }

            // Animate gun
            if (gun != null)
            {
                float gunZ = Mathf.Lerp(originalGunPosition.z, 0.35f, t);
                float gunXRotation = Mathf.Lerp(0f, 3f, t);
                gun.transform.localPosition = new Vector3(originalGunPosition.x, originalGunPosition.y, gunZ);
                gun.transform.localRotation = Quaternion.Euler(gunXRotation, originalGunRotation.y, originalGunRotation.z);
            }

            // Reset hammer position
            if (hammer != null)
                hammer.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Reset positions and rotations
        if (slide != null)
            slide.transform.localPosition = originalSlidePosition;

        if (hammer != null)
            hammer.transform.localRotation = originalHammerRotation;

        if (gun != null)
        {
            gun.transform.localPosition = originalGunPosition;
            gun.transform.localRotation = originalGunRotation;
        }
    }
}
