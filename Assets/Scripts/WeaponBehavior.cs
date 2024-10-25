using UnityEngine;
using System.Collections;

public class WeaponBehavior : MonoBehaviour
{
    [Header("Setup")]
    public Animator weaponAnimator;
    public AudioClip gunshotSound,
        reloadSound,
        emptyClickSound;
    public ParticleSystem muzzleFlash;
    public Light muzzleLight;
    public GameObject swayObject;
    public int damage = 60;
    public FPSController playerController;
    public GameObject shellCasingPrefab;
    public Transform shellEjectionPoint;
    public float shellEjectionForce = 3.3f;
    public CrosshairManager crosshairManager; // Reference to CrosshairManager

    [Header("ADS")]
    public float adsSpeed = 15f;
    public float adsZoomAmount = 40.0f;
    public float adsZoomSpeed = 15.0f;

    [System.Serializable]
    public struct SwaySettings
    {
        public float intensity,
            smooth,
            maxAmount,
            rotationFactor,
            yRotationFactor,
            bobbingSpeed,
            verticalBobAmount,
            horizontalBobAmount,
            rotationalBobAmount;
    }

    [Header("Sway")]
    public SwaySettings swaySettings = new SwaySettings
    {
        intensity = 0.1f,
        smooth = 10f,
        maxAmount = 0.05f,
        rotationFactor = 100f,
        yRotationFactor = -100f,
        bobbingSpeed = 10f,
        verticalBobAmount = 0.0005f,
        horizontalBobAmount = 0.0005f,
        rotationalBobAmount = 0.04f
    };

    [Header("ADS Sway")]
    public SwaySettings aimSwaySettings = new SwaySettings
    {
        intensity = 0.02f,
        smooth = 10f,
        maxAmount = 0.03f,
        rotationFactor = 75f,
        yRotationFactor = -75f,
        bobbingSpeed = 5f,
        verticalBobAmount = 0.0001f,
        horizontalBobAmount = 0.0001f,
        rotationalBobAmount = 0.0001f
    };

    [Header("ADS Position")]
    public Vector3 aimPosition = new Vector3(0.2277f, 0.0755f, 0.0999f);
    public Quaternion aimRotation = Quaternion.Euler(355.605f, 359.595f, 0f);

    [Header("FOV Change Settings")]
    public float fovChangeAmountADS = 0.5f;
    public float fovChangeAmountHipFire = 1.0f;
    public float fovChangeSpeedShared = 5.0f;

    [Header("Ammo Settings")]
    public int maxAmmoInMagazine = 7;
    public int maxTotalAmmo = 14;

    [Header("Shooting Settings")]
    public float range = 100f;
    public float baseAccuracyHip = 1f; // In degrees
    public float baseAccuracyADS = 0.5f; // In degrees
    public float recoilPenaltyHip = 0.5f; // In degrees per shot
    public float recoilPenaltyADS = 0.25f; // In degrees per shot
    public float recoilResetTime = 5f; // Time in seconds to recover from max recoil
    public float recoilRecoveryDelay = 0.5f; // Time before recoil recovery starts
    public float maxSpread = 5f; // Maximum spread in degrees

    [Header("Hit Particles")]
    public GameObject metalHitParticle,
        dirtHitParticle,
        fleshHitParticle,
        defaultHitParticle;

    [Header("Movement Spread Settings")]
    public float speedSpreadFactor = 0.1f; // Adjust this to control how much speed affects spread
    public float maxMovementSpread = 2f; // Maximum additional spread from movement

    private int currentAmmo,
        totalAmmo;
    private bool isReloading = false,
        isAiming,
        isFiring = false;
    private Camera mainCamera;
    private float originalFOV;
    private Vector3 initialSwayPosition;
    private Quaternion initialSwayRotation;
    private SwaySettings currentSwaySettings;
    private AudioSource audioSource;
    private Vector3 targetSwayPosition;
    private Quaternion targetSwayRotation;
    private float swayTimer,
        currentSpread,
        recoilAmount;
    private float lastFireTime; // Tracks the time of the last shot fired

    private void Start()
    {
        mainCamera = Camera.main;
        originalFOV = mainCamera?.fieldOfView ?? 60f;
        initialSwayPosition = swayObject?.transform.localPosition ?? Vector3.zero;
        initialSwayRotation = swayObject?.transform.localRotation ?? Quaternion.identity;
        currentSwaySettings = swaySettings;
        audioSource = GetComponent<AudioSource>();
        currentAmmo = maxAmmoInMagazine;
        totalAmmo = maxTotalAmmo;
        currentSpread = GetCurrentBaseAccuracy();

        // Assign CrosshairManager if not set
        if (crosshairManager == null)
        {
            crosshairManager = FindObjectOfType<CrosshairManager>();
            if (crosshairManager == null)
            {
                Debug.LogError("CrosshairManager not found in the scene.");
            }
        }
    }

    private void Update()
    {
        HandleAiming();
        HandleFiring();
        HandleReloading();
        UpdateCameraAndWeaponPosition();
        if (swayObject)
        {
            HandleSway();
            HandleBob();
        }

        // Recoil recovery after a delay
        if (recoilAmount > 0)
        {
            float timeSinceLastShot = Time.time - lastFireTime;
            if (timeSinceLastShot >= recoilRecoveryDelay)
            {
                float recoilRecoveryRate =
                    recoilResetTime > 0
                        ? (maxSpread / recoilResetTime) * Time.deltaTime
                        : maxSpread;
                recoilAmount = Mathf.Max(recoilAmount - recoilRecoveryRate, 0f);
            }
        }

        // Movement spread based on player's speed
        float speedSpread =
            playerController != null ? playerController.CurrentSpeed * speedSpreadFactor : 0f;
        speedSpread = Mathf.Clamp(speedSpread, 0f, maxMovementSpread);

        // Update current spread
        currentSpread = Mathf.Min(GetCurrentBaseAccuracy() + recoilAmount + speedSpread, maxSpread);

        // Update the crosshair each frame
        if (crosshairManager != null)
        {
            crosshairManager.UpdateCrosshair(currentSpread, isAiming, isReloading);
        }
    }

    private void HandleSway()
    {
        currentSwaySettings = isAiming ? aimSwaySettings : swaySettings;
        float factorX = Mathf.Clamp(
            -Input.GetAxis("Mouse X") * currentSwaySettings.intensity,
            -currentSwaySettings.maxAmount,
            currentSwaySettings.maxAmount
        );
        float factorY = Mathf.Clamp(
            -Input.GetAxis("Mouse Y") * currentSwaySettings.intensity,
            -currentSwaySettings.maxAmount,
            currentSwaySettings.maxAmount
        );
        targetSwayRotation = Quaternion.Euler(
            new Vector3(
                factorY,
                factorX * currentSwaySettings.yRotationFactor,
                -factorX * currentSwaySettings.rotationFactor
            )
        );
        targetSwayPosition = new Vector3(factorX, factorY, 0);
        swayObject.transform.localRotation = Quaternion.Slerp(
            swayObject.transform.localRotation,
            initialSwayRotation * targetSwayRotation,
            Time.deltaTime * currentSwaySettings.smooth
        );
        swayObject.transform.localPosition = Vector3.Lerp(
            swayObject.transform.localPosition,
            initialSwayPosition + targetSwayPosition,
            Time.deltaTime * currentSwaySettings.smooth
        );
    }

    private void HandleBob()
    {
        float waveslice = 0.0f,
            horizontal = Input.GetAxis("Horizontal"),
            vertical = Input.GetAxis("Vertical");
        if (Mathf.Abs(horizontal) != 0 || Mathf.Abs(vertical) != 0)
        {
            waveslice = Mathf.Sin(swayTimer);
            float adjustedBobbingSpeed =
                currentSwaySettings.bobbingSpeed
                * (playerController.CurrentSpeed / playerController.walkingSpeed);
            swayTimer += adjustedBobbingSpeed * Time.deltaTime;
            if (swayTimer > Mathf.PI * 2)
                swayTimer -= Mathf.PI * 2;
        }
        else
            swayTimer = 0;
        if (waveslice != 0)
        {
            float translateChange = waveslice * currentSwaySettings.verticalBobAmount;
            float horizontalTranslateChange =
                Mathf.Cos(swayTimer) * currentSwaySettings.horizontalBobAmount;
            float rotationChange = waveslice * currentSwaySettings.rotationalBobAmount;
            swayObject.transform.localPosition += new Vector3(
                horizontalTranslateChange,
                translateChange,
                0
            );
            swayObject.transform.localRotation *= Quaternion.Euler(0, 0, rotationChange);
        }
    }

    private void HandleFiring()
    {
        if (Input.GetMouseButtonDown(0) && !isReloading)
        {
            if (currentAmmo > 0)
            {
                weaponAnimator.Play("Fire", 0, 0.0f);
                if (gunshotSound && audioSource)
                {
                    audioSource.pitch = 1.0f + Random.Range(-0.05f, 0.05f);
                    audioSource.PlayOneShot(gunshotSound);
                }
                if (muzzleFlash)
                {
                    muzzleFlash.transform.localRotation = Quaternion.Euler(
                        0f,
                        0f,
                        Random.Range(0f, 360f)
                    );
                    muzzleFlash.Play();
                }
                if (muzzleLight)
                {
                    StopCoroutine("FlashMuzzleLight");
                    StartCoroutine(FlashMuzzleLight());
                }
                currentAmmo--;
                isFiring = true;
                StartCoroutine(ResetFOVAfterDelay());
                EjectShellCasing();

                // Increase recoil amount
                float recoilIncrement = isAiming ? recoilPenaltyADS : recoilPenaltyHip;
                recoilAmount = Mathf.Min(
                    recoilAmount + recoilIncrement,
                    maxSpread - GetCurrentBaseAccuracy()
                );

                // Update last fire time
                lastFireTime = Time.time;

                Vector3 shootDirection = ApplySpread(mainCamera.transform.forward, currentSpread);
                if (
                    Physics.Raycast(
                        mainCamera.transform.position,
                        shootDirection,
                        out RaycastHit hit,
                        range
                    )
                )
                    HandleHit(hit);
            }
            else if (emptyClickSound && audioSource)
                audioSource.PlayOneShot(emptyClickSound);
        }
    }

    private void HandleReloading()
    {
        if (
            Input.GetKeyDown(KeyCode.R)
            && !isReloading
            && currentAmmo < maxAmmoInMagazine
            && totalAmmo > 0
        )
        {
            if (isAiming)
            {
                isAiming = false;
                weaponAnimator.SetBool("IsAiming", false);
            }
            StartCoroutine(Reload());
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        weaponAnimator.SetBool("isReloading", true);
        weaponAnimator.SetTrigger("Reload");

        // Play reload sound
        if (reloadSound && audioSource)
        {
            audioSource.PlayOneShot(reloadSound);
        }

        yield return new WaitUntil(
            () => weaponAnimator.GetCurrentAnimatorStateInfo(0).IsName("Reload")
        );
        yield return new WaitUntil(
            () => weaponAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        );

        int ammoToReload = Mathf.Min(maxAmmoInMagazine - currentAmmo, totalAmmo);
        currentAmmo += ammoToReload;
        totalAmmo -= ammoToReload;
        isReloading = false;
        weaponAnimator.SetBool("isReloading", false);
    }

    private void EjectShellCasing()
    {
        if (shellCasingPrefab && shellEjectionPoint)
        {
            GameObject shell = Instantiate(
                shellCasingPrefab,
                shellEjectionPoint.position,
                shellEjectionPoint.rotation
            );
            Rigidbody shellRigidbody = shell.GetComponent<Rigidbody>();
            if (shellRigidbody)
            {
                Vector3 ejectDirection =
                    (
                        shellEjectionPoint.transform.right + shellEjectionPoint.transform.up
                    ).normalized
                    + Random.insideUnitSphere * 0.1f;
                shellRigidbody.AddForce(ejectDirection * shellEjectionForce, ForceMode.Impulse);
                shellRigidbody.AddTorque(Random.insideUnitSphere, ForceMode.Impulse);
            }
        }
    }

    private void HandleAiming()
    {
        isAiming = !isReloading && Input.GetMouseButton(1);
        weaponAnimator.SetBool("IsAiming", isAiming);
    }

    private void UpdateCameraAndWeaponPosition()
    {
        float desiredFOV = originalFOV;
        Vector3 targetPosition = Vector3.zero;
        Quaternion targetRotation = Quaternion.identity;
        if (isAiming && !isReloading)
        {
            desiredFOV -= adsZoomAmount;
            targetPosition = aimPosition;
            targetRotation = aimRotation;
        }
        if (isFiring)
            desiredFOV += isAiming ? fovChangeAmountADS : fovChangeAmountHipFire;
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            Time.deltaTime * adsSpeed
        );
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            Time.deltaTime * adsSpeed
        );
        if (mainCamera != null)
            mainCamera.fieldOfView = Mathf.Lerp(
                mainCamera.fieldOfView,
                desiredFOV,
                Time.deltaTime * fovChangeSpeedShared
            );
    }

    private IEnumerator ResetFOVAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        isFiring = false;
    }

    private IEnumerator FlashMuzzleLight()
    {
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(0.1f);
        muzzleLight.enabled = false;
    }

    private Vector3 ApplySpread(Vector3 direction, float spreadAngle)
    {
        float spreadRadius = Mathf.Tan(spreadAngle * Mathf.Deg2Rad / 2f);
        Vector2 randomPoint = Random.insideUnitCircle * spreadRadius;
        Vector3 spreadDirection =
            direction
            + mainCamera.transform.right * randomPoint.x
            + mainCamera.transform.up * randomPoint.y;
        return spreadDirection.normalized;
    }

    private void HandleHit(RaycastHit hit)
    {
        Health targetHealth = hit.collider.GetComponent<Health>();
        if (targetHealth)
            targetHealth.TakeDamage(damage);
        string hitTag = hit.collider.tag;
        GameObject particleToSpawn =
            hitTag == "Metal"
                ? metalHitParticle
                : hitTag == "Dirt"
                    ? dirtHitParticle
                    : hitTag == "Flesh"
                        ? fleshHitParticle
                        : defaultHitParticle;
        if (particleToSpawn)
        {
            GameObject hitParticle = Instantiate(
                particleToSpawn,
                hit.point,
                Quaternion.LookRotation(hit.normal)
            );
            Destroy(hitParticle, 2f);
        }
    }

    private float GetCurrentBaseAccuracy() => isAiming ? baseAccuracyADS : baseAccuracyHip;
}
