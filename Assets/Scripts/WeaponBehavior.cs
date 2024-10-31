using UnityEngine;
using TMPro;
using UnityEngine.Rendering.Universal;
using System.Collections;

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
        public GameObject swayObject;
        public FPSController playerController;
        public GameObject shellCasingPrefab;
        public Transform shellEjectionPoint;
        public float shellEjectionForce = 3.3f;
        public CrosshairManager crosshairManager;
        public Transform bulletOrigin;
    }

    [System.Serializable]
    public class ADSSettings
    {
        public float adsSpeed = 15f;
        public float adsZoomAmount = 40f;
        public Vector3 aimPosition = new Vector3(0.2277f, 0.0755f, 0.0999f);
        public Quaternion aimRotation = Quaternion.Euler(355.65f, 359.92f, 2f); // Updated for exact rotation
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
        public int maxTotalAmmo = 50; // Updated to match screenshot
    }

    [System.Serializable]
    public class ShootingSettings
    {
        public float range = 100f;
        public float baseAccuracyHip = 1f;
        public float baseAccuracyADS = 0.5f;
        public float recoilPenaltyHip = 1f; // Updated to match screenshot
        public float recoilPenaltyADS = 0.5f; // Updated to match screenshot
        public float recoilResetTime = 0.15f; // Updated to match screenshot
        public float recoilRecoveryDelay = 0.15f; // Updated to match screenshot
        public float maxRecoil = 1.5f; // Updated to match screenshot
    }

    [System.Serializable]
    public class MovementSpreadSettings
    {
        public float standingBaseSpread = 3f; // Updated to match screenshot
        public float walkingBaseSpread = 7f; // Updated to match screenshot
        public float runningBaseSpread = 8f; // Updated to match screenshot
        public float crouchingBaseSpread = 2f; // Updated to match screenshot
        public float standingADSBaseSpread = 3f; // Updated to match screenshot
        public float walkingADSBaseSpread = 2f; // Updated to match screenshot
        public float runningADSBaseSpread = 4f; // Updated to match screenshot
        public float crouchingADSBaseSpread = 0.25f; // Updated to match screenshot
    }

    [System.Serializable]
    public class HitParticles
    {
        public GameObject concreteHitParticle;
        public GameObject metalHitParticle;
        public GameObject woodHitParticle;
        public GameObject dirtHitParticle;
        public GameObject fleshHitParticle;
        public GameObject defaultHitParticle;
    }

    [System.Serializable]
    public class HUDSettings
    {
        public TextMeshProUGUI ammoText;
    }

    [System.Serializable]
    public class DecalSettings
    {
        public GameObject concreteDecalPrefab;
        public GameObject genericDecalPrefab;
        public GameObject metalDecalPrefab;
        public GameObject woodDecalPrefab;
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

    [Header("Decals")]
    public DecalSettings decals = new DecalSettings();

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
        targetSwayPosition;
    private Quaternion initialSwayRotation,
        targetSwayRotation;
    private SwaySettings currentSwaySettings;
    private AudioSource audioSource;
    private LineRenderer bulletTracer;

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

        if (setup.weaponAnimator == null)
            Debug.LogError("Weapon Animator is not assigned.");

        if (setup.crosshairManager == null)
        {
            setup.crosshairManager = FindObjectOfType<CrosshairManager>();
            if (setup.crosshairManager == null)
                Debug.LogError("CrosshairManager not found in the scene.");
        }

        if (hudSettings.ammoText == null)
            Debug.LogError("Ammo Text UI element is not assigned in HUD Settings.");

        bulletTracer = GetComponent<LineRenderer>();
        if (bulletTracer != null)
        {
            bulletTracer.enabled = false; // Hide initially
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

        UpdateRecoil();
        UpdateSpread();
        UpdateCrosshair();
        UpdateSlidePosition();
        UpdateHUD();
    }

    private void UpdateRecoil()
    {
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
    }

    private void UpdateSpread()
    {
        float movementSpreadValue = GetBaseSpreadForCurrentMovementState();
        currentSpread = movementSpreadValue + recoilAmount;
        float maxTotalSpread = movementSpreadValue + shootingSettings.maxRecoil;
        currentSpread = Mathf.Min(currentSpread, maxTotalSpread);
    }

    private void UpdateCrosshair()
    {
        if (setup.crosshairManager != null)
            setup.crosshairManager.UpdateCrosshair(currentSpread, isAiming, isReloading);
    }

    private void UpdateHUD()
    {
        if (hudSettings.ammoText != null)
            hudSettings.ammoText.text = $"{currentAmmo} / {totalAmmo}";
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
            factorY,
            factorX * currentSwaySettings.yRotationFactor,
            -factorX * currentSwaySettings.rotationFactor
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
                FireWeapon();
            }
            else if (setup.emptyClickSound && audioSource)
                audioSource.PlayOneShot(setup.emptyClickSound);
        }
    }

    private void FireWeapon()
    {
        setup.weaponAnimator.Play("Fire", 0, 0.0f);
        PlayGunshotEffects();
        currentAmmo--;
        isFiring = true;
        StartCoroutine(ResetFOVAfterDelay());
        EjectShellCasing();
        ApplyRecoil();

        Vector3 shootDirection = ApplySpread(mainCamera.transform.forward, currentSpread);

        // Use the camera's position for the actual bullet (raycast)
        Vector3 raycastStartPoint = mainCamera.transform.position;
        Vector3 endPoint = raycastStartPoint + (shootDirection * shootingSettings.range);

        if (
            Physics.Raycast(
                raycastStartPoint,
                shootDirection,
                out RaycastHit hit,
                shootingSettings.range
            )
        )
        {
            endPoint = hit.point;
            HandleHit(hit);
        }

        // Use the bulletOrigin for the visual bullet tracer
        Vector3 tracerStartPoint = setup.bulletOrigin
            ? setup.bulletOrigin.position
            : mainCamera.transform.position;

        if (bulletTracer != null)
        {
            StartCoroutine(DisplayBulletTracer(tracerStartPoint, endPoint));
        }
    }

    private IEnumerator DisplayBulletTracer(Vector3 start, Vector3 end)
    {
        bulletTracer.enabled = true;
        bulletTracer.SetPosition(0, start);
        bulletTracer.SetPosition(1, end);

        yield return new WaitForSeconds(0.05f); // Adjust duration as needed

        bulletTracer.enabled = false;
    }

    private void PlayGunshotEffects()
    {
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
    }

    private void ApplyRecoil()
    {
        float recoilIncrement = isAiming
            ? shootingSettings.recoilPenaltyADS
            : shootingSettings.recoilPenaltyHip;
        recoilAmount = Mathf.Min(recoilAmount + recoilIncrement, shootingSettings.maxRecoil);
        lastFireTime = Time.time;
    }

    private void HandleReloading()
    {
        if (
            Input.GetKeyDown(KeyCode.R)
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
        lockSlideDuringReload = true;
        setup.weaponAnimator.SetBool("isReloading", true);
        setup.weaponAnimator.SetTrigger("Reload");

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
        lockSlideDuringReload = false;
        setup.weaponAnimator.SetBool("isReloading", false);

        UpdateSlidePosition();
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
        string hitTag = hit.collider.tag;

        // === Spawn Hit Particle Effect ===
        GameObject particleToSpawn = hitTag switch
        {
            "Metal" => hitParticles.metalHitParticle,
            "Dirt" => hitParticles.dirtHitParticle,
            "Flesh" => hitParticles.fleshHitParticle,
            "Concrete" => hitParticles.concreteHitParticle,
            "Wood" => hitParticles.woodHitParticle,
            _ => hitParticles.defaultHitParticle,
        };

        if (particleToSpawn != null)
        {
            GameObject hitParticle = Instantiate(
                particleToSpawn,
                hit.point,
                Quaternion.LookRotation(hit.normal)
            );
            Destroy(hitParticle, 2f);
        }

        // === Apply Bullet Hole Decal ===
        GameObject selectedDecalPrefab = hitTag switch
        {
            "Concrete" => decals.concreteDecalPrefab,
            "Metal" => decals.metalDecalPrefab,
            "Wood" => decals.woodDecalPrefab,
            _ => decals.genericDecalPrefab
        };

        if (selectedDecalPrefab != null)
        {
            Vector3 decalPosition = hit.point + hit.normal * 0.01f;
            Quaternion decalRotation =
                Quaternion.LookRotation(-hit.normal) * Quaternion.Euler(0, 0, Random.Range(0, 360));

            GameObject decal = Instantiate(selectedDecalPrefab, decalPosition, decalRotation);
            decal.transform.SetParent(hit.collider.transform);

            StartCoroutine(FadeOutAndDestroy(decal, 10f, 10f));
        }
    }

    private IEnumerator FadeOutAndDestroy(GameObject decal, float delay, float fadeDuration)
    {
        yield return new WaitForSeconds(delay);

        var projector = decal.GetComponent<DecalProjector>();
        if (projector != null)
        {
            for (float t = 0; t < fadeDuration; t += Time.deltaTime)
            {
                projector.fadeFactor = Mathf.Lerp(1f, 0f, t / fadeDuration);
                yield return null;
            }
            projector.fadeFactor = 0f;
        }

        Destroy(decal);
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

        setup.weaponAnimator.SetBool("IsSlideHeldBack", currentAmmo <= 0);
    }
}
