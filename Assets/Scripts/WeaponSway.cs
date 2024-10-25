using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    public FPSController playerController;
    public float swayIntensity = 0.5f;
    public float swaySmooth = 10f;
    public float swayMaxAmount = 0.1f;
    public float rotationFactor = 3f;
    public float yRotationFactor = 1.5f;
    public float bobbingSpeed = 14f;
    public float verticalBobbingAmount = 0.05f;
    public float horizontalBobbingAmount = 0.02f;
    public float rotationalBobbingAmount = 0.02f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float swayTimer = 0;
    private bool isAiming = false;

    void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
    }

    void Update()
    {
        HandleSway();
        HandleBob();
    }

    void HandleSway()
    {
        if (isAiming) return; // Skip sway while aiming

        float factorX = -Input.GetAxis("Mouse X") * swayIntensity;
        float factorY = -Input.GetAxis("Mouse Y") * swayIntensity;

        factorX = Mathf.Clamp(factorX, -swayMaxAmount, swayMaxAmount);
        factorY = Mathf.Clamp(factorY, -swayMaxAmount, swayMaxAmount);

        Quaternion targetRotation = Quaternion.Euler(new Vector3(factorY, factorX * yRotationFactor, -factorX * rotationFactor));
        Vector3 targetPosition = new Vector3(factorX, factorY, 0);

        transform.localRotation = Quaternion.Slerp(transform.localRotation, initialRotation * targetRotation, Time.deltaTime * swaySmooth);
        transform.localPosition = Vector3.Lerp(transform.localPosition, initialPosition + targetPosition, Time.deltaTime * swaySmooth);
    }


    void HandleBob()
    {
        if (isAiming) return; // Skip bob while aiming

        float waveslice = 0.0f;
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (Mathf.Abs(horizontal) != 0 || Mathf.Abs(vertical) != 0)
        {
            waveslice = Mathf.Sin(swayTimer);

            // Get current speed from the FPSController
            float currentSpeed = playerController.CurrentSpeed; // Assuming CurrentSpeed is a method or property you added to FPSController

            // Use the walkingSpeed from the FPSController script
            float baseSpeed = playerController.walkingSpeed; // This line should reference the walkingSpeed from FPSController

            float adjustedBobbingSpeed = bobbingSpeed * (currentSpeed / baseSpeed);

            swayTimer += adjustedBobbingSpeed * Time.deltaTime;
            if (swayTimer > Mathf.PI * 2)
            {
                swayTimer -= (Mathf.PI * 2);
            }
        }
        else
        {
            swayTimer = 0;
        }

        if (waveslice != 0)
        {
            float translateChange = waveslice * verticalBobbingAmount;
            float horizontalTranslateChange = Mathf.Cos(swayTimer) * horizontalBobbingAmount;
            float rotationChange = waveslice * rotationalBobbingAmount;

            transform.localPosition += new Vector3(horizontalTranslateChange, translateChange, 0);
            transform.localRotation *= Quaternion.Euler(0, 0, rotationChange);
        }
    }
}