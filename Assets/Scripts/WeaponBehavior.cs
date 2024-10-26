using UnityEngine;
using System.Collections;
using TMPro;

public class WeaponBehavior : MonoBehaviour
{
    [System.Serializable]
    public class SetupSettings
    {
        public Animator weaponAnimator;
        public AudioClip gunshotSound,
            reloadSound,
            emptyClickSound;
        public ParticleSystem muzzleFlash;
        public Light muzzleLight;
        public GameObject swayObject,
            slideObject;
        public int damage = 60;
        public FPSController playerController;
        public GameObject shellCasingPrefab;
        public Transform shellEjectionPoint;
        public float shellEjectionForce = 3.3f;
        public CrosshairManager crosshairManager;
    }

    [System.Serializable]
    public class ADSSettings
    {
        public float adsSpeed = 15f;
        public float adsZoomAmount = 40f;
        public float adsZoomSpeed = 15f;
        public Vector3 aimPosition = new Vector3(0.2277f, 0.0755f, 0.0999f);
        public Quaternion aimRotation = Quaternion.Euler(355.605f, 359.595f, 0f);
    }

    [System.Serializable]
    public struct SwaySettings
    {
        public float intensity,
            smooth,
            maxAmount,
            rotationFactor,
            yRotationFactor;
        public float bobbingSpeed,
            verticalBobAmount,
            horizontalBobAmount,
            rotationalBobAmount;
    }

    [System.Serializable]
    public class FOVSettings
    {
        public float fovChangeAmountADS = 0.5f;
        public float fovChangeAmountHipFire = 1.0f;
        public float fovChangeSpeedShared = 5.0f;
    }

    [System.Serializable]
    public class AmmoSettings
    {
        public int maxAmmoInMagazine = 7;
        public int maxTotalAmmo = 14;
    }

    [System.Serializable]
    public class ShootingSettings
    {
        public float range = 100f;
        public float baseAccuracyHip = 1f;
        public float baseAccuracyADS = 0.5f;
        public float recoilPenaltyHip = 0.5f;
        public float recoilPenaltyADS = 0.25f;
        public float recoilResetTime = 5f;
        public float recoilRecoveryDelay = 0.5f;
        public float maxRecoil = 3f;
    }

    [System.Serializable]
    public class MovementSpreadSettings
    {
        public float standingBaseSpread = 1f;
        public float walkingBaseSpread = 2f;
        public float runningBaseSpread = 4f;
        public float crouchingBaseSpread = 0.5f;

        // New settings for ADS
        public float standingADSBaseSpread = 0.5f;
        public float walkingADSBaseSpread = 1f;
        public float runningADSBaseSpread = 2f;
        public float crouchingADSBaseSpread = 0.25f;
    }

    [System.Serializable]
    public class HitParticles
    {
        public GameObject metalHitParticle,
            dirtHitParticle,
            fleshHitParticle,
            defaultHitParticle;
    }

    [System.Serializable]
    public class HUDSettings
    {
        public TextMeshProUGUI ammoText;
    }

    [Header("Setup")]
    public SetupSettings setup = new SetupSettings();

    [Header("ADS")]
    public ADSSettings ads = new ADSSettings();

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

    [Header("FOV Change Settings")]
    public FOVSettings fovSettings = new FOVSettings();

    [Header("Ammo Settings")]
    public AmmoSettings ammoSettings = new AmmoSettings();

    [Header("Shooting Settings")]
    public ShootingSettings shootingSettings = new ShootingSettings();

    [Header("Movement Spread Settings")]
    public MovementSpreadSettings movementSpread = new MovementSpreadSettings();

    [Header("Hit Particles")]
    public HitParticles hitParticles = new HitParticles();

    [Header("HUD")]
    public HUDSettings hudSettings = new HUDSettings();

    private int currentAmmo,
        totalAmmo;
    private bool isReloading = false,
        isAiming,
        isFiring = false;
    private bool lockSlideDuringReload = false;
    private Camera mainCamera;
    private float originalFOV,
        swayTimer,
        currentSpread,
        recoilAmount,
        lastFireTime;
    private Vector3 initialSwayPosition,
        targetSwayPosition,
        slideOriginalPosition;
    private Quaternion initialSwayRotation,
        targetSwayRotation;
    private SwaySettings currentSwaySettings;
    private AudioSource audioSource;

    private void Start()
    {
        mainCamera = Camera.main;
        originalFOV = mainCamera?.fieldOfView ?? 60f;
        initialSwayPosition = setup.swayObject?.transform.localPosition ?? Vector3.zero;
        initialSwayRotation = setup.swayObject?.transform.localRotation ?? Quaternion.identity;
        currentSwaySettings = swaySettings;
        audioSource = GetComponent<AudioSource>();
        currentAmmo = ammoSettings.maxAmmoInMagazine;
        totalAmmo = ammoSettings.maxTotalAmmo;
        currentSpread = GetCurrentBaseAccuracy();

        if (setup.slideObject != null)
            slideOriginalPosition = setup.slideObject.transform.localPosition;
        else
            Debug.LogError("Slide Object is not assigned.");

        if (setup.crosshairManager == null)
        {
            setup.crosshairManager = FindObjectOfType<CrosshairManager>();
            if (setup.crosshairManager == null)
                Debug.LogError("CrosshairManager not found in the scene.");
        }

        if (hudSettings.ammoText == null)
        {
            Debug.LogError("Ammo Text UI element is not assigned in HUD Settings.");
        }
    }

    private void Update()
    {
        HandleAiming();
        HandleFiring();
        HandleReloading();
        UpdateCameraAndWeaponPosition();

        if (setup.swayObject)
        {
            HandleSway();
            HandleBob();
        }

        if (recoilAmount > 0)
        {
            float timeSinceLastShot = Time.time - lastFireTime;
            if (timeSinceLastShot >= shootingSettings.recoilRecoveryDelay)
            {
                float recoilRecoveryRate =
                    shootingSettings.recoilResetTime > 0
                        ? (shootingSettings.maxRecoil / shootingSettings.recoilResetTime)
                            * Time.deltaTime
                        : shootingSettings.maxRecoil;
                recoilAmount = Mathf.Max(recoilAmount - recoilRecoveryRate, 0f);
            }
        }

        float movementSpreadValue = GetBaseSpreadForCurrentMovementState();
        currentSpread = movementSpreadValue + recoilAmount;
        float maxTotalSpread = movementSpreadValue + shootingSettings.maxRecoil;
        currentSpread = Mathf.Min(currentSpread, maxTotalSpread);

        if (setup.crosshairManager != null)
            setup.crosshairManager.UpdateCrosshair(currentSpread, isAiming, isReloading);

        UpdateSlidePosition();
        UpdateHUD();
    }

    private void UpdateHUD()
    {
        if (hudSettings.ammoText != null)
        {
            hudSettings.ammoText.text = $"{currentAmmo} / {totalAmmo}";
        }
    }

    private float GetBaseSpreadForCurrentMovementState()
    {
        if (isAiming)
        {
            switch (setup.playerController.CurrentMovementState)
            {
                case FPSController.MovementState.Standing:
                    return movementSpread.standingADSBaseSpread;
                case FPSController.MovementState.Walking:
                    return movementSpread.walkingADSBaseSpread;
                case FPSController.MovementState.Running:
                    return movementSpread.runningADSBaseSpread;
                case FPSController.MovementState.Crouching:
                    return movementSpread.crouchingADSBaseSpread;
                default:
                    return movementSpread.standingADSBaseSpread;
            }
        }
        else
        {
            switch (setup.playerController.CurrentMovementState)
            {
                case FPSController.MovementState.Standing:
                    return movementSpread.standingBaseSpread;
                case FPSController.MovementState.Walking:
                    return movementSpread.walkingBaseSpread;
                case FPSController.MovementState.Running:
                    return movementSpread.runningBaseSpread;
                case FPSController.MovementState.Crouching:
                    return movementSpread.crouchingBaseSpread;
                default:
                    return movementSpread.standingBaseSpread;
            }
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
        setup.swayObject.transform.localRotation = Quaternion.Slerp(
            setup.swayObject.transform.localRotation,
            initialSwayRotation * targetSwayRotation,
            Time.deltaTime * currentSwaySettings.smooth
        );
        setup.swayObject.transform.localPosition = Vector3.Lerp(
            setup.swayObject.transform.localPosition,
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
                * (setup.playerController.CurrentSpeed / setup.playerController.walkingSpeed);
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
            setup.swayObject.transform.localPosition += new Vector3(
                horizontalTranslateChange,
                translateChange,
                0
            );
            setup.swayObject.transform.localRotation *= Quaternion.Euler(0, 0, rotationChange);
        }
    }

    private void HandleFiring()
    {
        if (Input.GetMouseButtonDown(0) && !isReloading)
        {
            if (currentAmmo > 0)
            {
                setup.weaponAnimator.Play("Fire", 0, 0.0f);
                if (setup.gunshotSound && audioSource)
                {
                    audioSource.pitch = 1.0f + Random.Range(-0.05f, 0.05f);
                    audioSource.PlayOneShot(setup.gunshotSound);
                }
                if (setup.muzzleFlash)
                {
                    setup.muzzleFlash.transform.localRotation = Quaternion.Euler(
                        0f,
                        0f,
                        Random.Range(0f, 360f)
                    );
                    setup.muzzleFlash.Play();
                }
                if (setup.muzzleLight)
                {
                    StopCoroutine("FlashMuzzleLight");
                    StartCoroutine(FlashMuzzleLight());
                }
                currentAmmo--;
                isFiring = true;
                StartCoroutine(ResetFOVAfterDelay());
                EjectShellCasing();

                float recoilIncrement = isAiming
                    ? shootingSettings.recoilPenaltyADS
                    : shootingSettings.recoilPenaltyHip;
                recoilAmount = Mathf.Min(
                    recoilAmount + recoilIncrement,
                    shootingSettings.maxRecoil
                );
                lastFireTime = Time.time;

                Vector3 shootDirection = ApplySpread(mainCamera.transform.forward, currentSpread);
                if (
                    Physics.Raycast(
                        mainCamera.transform.position,
                        shootDirection,
                        out RaycastHit hit,
                        shootingSettings.range
                    )
                )
                    HandleHit(hit);
            }
            else if (setup.emptyClickSound && audioSource)
                audioSource.PlayOneShot(setup.emptyClickSound);
        }
    }

    private void HandleReloading()
    {
        if (
            Input.GetKeyDown(KeyCode.R) // Only reload when pressing R
            && !isReloading
            && currentAmmo < ammoSettings.maxAmmoInMagazine
            && totalAmmo > 0
        )
        {
            isAiming = false;
            setup.weaponAnimator.SetBool("IsAiming", false);

            StartCoroutine(Reload());
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        lockSlideDuringReload = true; // Lock slide updates during reload
        setup.weaponAnimator.SetBool("isReloading", true);
        setup.weaponAnimator.SetTrigger("Reload");

        // Turn off slide held back as soon as reloading starts
        setup.weaponAnimator.SetBool("IsSlideHeldBack", false);

        if (setup.reloadSound && audioSource)
            audioSource.PlayOneShot(setup.reloadSound);

        yield return new WaitUntil(
            () => setup.weaponAnimator.GetCurrentAnimatorStateInfo(0).IsName("Reload")
        );
        yield return new WaitUntil(
            () => setup.weaponAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        );

        int ammoToReload = Mathf.Min(ammoSettings.maxAmmoInMagazine - currentAmmo, totalAmmo);
        currentAmmo += ammoToReload;
        totalAmmo -= ammoToReload;
        isReloading = false;
        lockSlideDuringReload = false; // Allow slide updates again after reload
        setup.weaponAnimator.SetBool("isReloading", false);

        UpdateSlidePosition(); // Ensure slide position is updated post-reload
    }

    private void EjectShellCasing()
    {
        if (setup.shellCasingPrefab && setup.shellEjectionPoint)
        {
            GameObject shell = Instantiate(
                setup.shellCasingPrefab,
                setup.shellEjectionPoint.position,
                setup.shellEjectionPoint.rotation
            );
            Rigidbody shellRigidbody = shell.GetComponent<Rigidbody>();
            if (shellRigidbody)
            {
                Vector3 ejectDirection =
                    (
                        setup.shellEjectionPoint.transform.right
                        + setup.shellEjectionPoint.transform.up
                    ).normalized
                    + Random.insideUnitSphere * 0.1f;
                shellRigidbody.AddForce(
                    ejectDirection * setup.shellEjectionForce,
                    ForceMode.Impulse
                );
                shellRigidbody.AddTorque(Random.insideUnitSphere, ForceMode.Impulse);
            }
        }
    }

    private void HandleAiming()
    {
        if (isReloading)
            return;

        isAiming = Input.GetMouseButton(1);
        setup.weaponAnimator.SetBool("IsAiming", isAiming);
    }

    private void UpdateCameraAndWeaponPosition()
    {
        float desiredFOV = originalFOV;
        Vector3 targetPosition = Vector3.zero;
        Quaternion targetRotation = Quaternion.identity;
        if (isAiming && !isReloading)
        {
            desiredFOV -= ads.adsZoomAmount;
            targetPosition = ads.aimPosition;
            targetRotation = ads.aimRotation;
        }
        if (isFiring)
            desiredFOV += isAiming
                ? fovSettings.fovChangeAmountADS
                : fovSettings.fovChangeAmountHipFire;

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            Time.deltaTime * ads.adsSpeed
        );
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            Time.deltaTime * ads.adsSpeed
        );
        if (mainCamera != null)
            mainCamera.fieldOfView = Mathf.Lerp(
                mainCamera.fieldOfView,
                desiredFOV,
                Time.deltaTime * fovSettings.fovChangeSpeedShared
            );
    }

    private IEnumerator ResetFOVAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        isFiring = false;
    }

    private IEnumerator FlashMuzzleLight()
    {
        setup.muzzleLight.enabled = true;
        yield return new WaitForSeconds(0.1f);
        setup.muzzleLight.enabled = false;
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
            targetHealth.TakeDamage(setup.damage);

        string hitTag = hit.collider.tag;
        GameObject particleToSpawn =
            hitTag == "Metal"
                ? hitParticles.metalHitParticle
                : hitTag == "Dirt"
                    ? hitParticles.dirtHitParticle
                    : hitTag == "Flesh"
                        ? hitParticles.fleshHitParticle
                        : hitParticles.defaultHitParticle;

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

    private float GetCurrentBaseAccuracy() =>
        isAiming ? shootingSettings.baseAccuracyADS : shootingSettings.baseAccuracyHip;

    private void UpdateSlidePosition()
    {
        if (setup.weaponAnimator == null)
        {
            Debug.LogError("Weapon Animator is not assigned.");
            return;
        }

        if (lockSlideDuringReload)
            return;

        if (currentAmmo <= 0)
            setup.weaponAnimator.SetBool("IsSlideHeldBack", true);
        else
            setup.weaponAnimator.SetBool("IsSlideHeldBack", false);
    }
}
