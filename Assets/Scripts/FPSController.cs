using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    public float walkingSpeed = 7.5f;
    public float runningSpeed = 11.5f;
    public float crouchWalkingSpeed = 3.5f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public Camera playerCamera;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;
    public float crouchHeight = 0.5f;
    public float standingHeight = 2.0f;
    public float crouchSpeed = 10.0f;
    public float standingSpeed = 5.0f;

    CharacterController characterController;
    public Vector3 moveDirection = Vector3.zero;
    float rotationX = 0;

    [HideInInspector]
    public bool canMove = true;

    // Movement state variables
    private bool isRunning;
    private bool isCrouching;
    private bool hasJumped = false; // Track if the player has jumped
    private bool isFalling = false; // Track if the player is falling
    private float fallStartY;

    // Footstep sound variables
    [System.Serializable]
    public class FootstepSound
    {
        public string tag;
        public List<AudioClip> walkClips;
        public List<AudioClip> runClips;
        public List<AudioClip> jumpStartClips;
        public List<AudioClip> jumpLandClips;
    }

    public AudioSource footstepAudioSource;
    public List<FootstepSound> footstepSounds;
    public float baseStepIntervalWalking = 0.5f;
    public float baseStepIntervalRunning = 0.3f;

    private Dictionary<string, FootstepSound> footstepSoundsDict =
        new Dictionary<string, FootstepSound>();
    private float footstepTimer = 0f;
    private float footstepInterval;

    private bool previouslyGrounded = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize footstep sounds dictionary
        foreach (FootstepSound fs in footstepSounds)
        {
            if (!footstepSoundsDict.ContainsKey(fs.tag))
            {
                footstepSoundsDict.Add(fs.tag, fs);
            }
        }

        // Ensure footstepAudioSource is assigned
        if (footstepAudioSource == null)
        {
            footstepAudioSource = GetComponent<AudioSource>();

            if (footstepAudioSource == null)
            {
                footstepAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    void Update()
    {
        // Press Left Shift to run
        isRunning = Input.GetKey(KeyCode.LeftShift);

        // Press Left Control to crouch
        isCrouching = Input.GetKey(KeyCode.LeftControl);
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        float height = characterController.height;
        float speed = isCrouching ? crouchSpeed : standingSpeed;
        height = Mathf.Lerp(height, targetHeight, Time.deltaTime * speed);
        characterController.height = height;

        // Get raw input
        float inputX = canMove ? Input.GetAxis("Horizontal") : 0;
        float inputY = canMove ? Input.GetAxis("Vertical") : 0;

        // Create input vector
        Vector3 input = new Vector3(inputX, 0, inputY);

        // Normalize input if it exceeds 1 to prevent faster diagonal movement
        if (input.magnitude > 1)
        {
            input.Normalize();
        }

        // Preserve vertical movement
        float movementDirectionY = moveDirection.y;

        // Adjust speed based on whether the player is crouching or running
        float movementSpeed = isCrouching
            ? crouchWalkingSpeed
            : (isRunning ? runningSpeed : walkingSpeed);

        // Calculate movement direction
        moveDirection = transform.TransformDirection(input) * movementSpeed;

        // Restore vertical movement
        moveDirection.y = movementDirectionY;

        // Handle jumping and gravity
        if (characterController.isGrounded)
        {
            if (Input.GetButtonDown("Jump") && canMove)
            {
                moveDirection.y = jumpSpeed;
                PlayJumpStartSound();
                hasJumped = true; // Player has jumped
            }
            else if (moveDirection.y < -1f)
            {
                moveDirection.y = -1f; // Keep grounded
            }
        }

        // Apply gravity
        moveDirection.y -= gravity * Time.deltaTime;

        // Move the controller
        characterController.Move(moveDirection * Time.deltaTime);

        // Footstep sound logic
        float currentSpeed = new Vector3(
            characterController.velocity.x,
            0,
            characterController.velocity.z
        ).magnitude;

        if (characterController.isGrounded && currentSpeed > 0.1f && canMove && footstepTimer <= 0f)
        {
            if (CurrentMovementState == MovementState.Running)
            {
                footstepInterval = baseStepIntervalRunning;
            }
            else
            {
                footstepInterval = baseStepIntervalWalking;
            }

            PlayFootstepSound();
            footstepTimer = footstepInterval;
        }
        else
        {
            footstepTimer -= Time.deltaTime;
        }

        // Player and Camera rotation
        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }

        // Landing sound logic
        bool isGrounded = characterController.isGrounded;

        if (!isGrounded && previouslyGrounded)
        {
            // Player just left the ground
            fallStartY = transform.position.y;
            isFalling = true;
        }
        else if (isGrounded && !previouslyGrounded)
        {
            // Player just landed
            if (hasJumped)
            {
                // Play landing sound after jump
                PlayJumpLandSound();
                hasJumped = false; // Reset jump flag
            }
            else if (isFalling)
            {
                // Calculate fall distance and play landing sound only if fall distance is significant
                float fallDistance = fallStartY - transform.position.y;
                if (fallDistance > 1.0f)
                {
                    PlayJumpLandSound();
                }
                isFalling = false; // Reset falling flag
            }
        }

        previouslyGrounded = isGrounded;
    }

    void PlayFootstepSound()
    {
        RaycastHit hit;

        if (
            Physics.Raycast(
                transform.position,
                Vector3.down,
                out hit,
                characterController.height / 2 + 0.4f
            )
        )
        {
            string tag = hit.collider.tag;

            FootstepSound fs;
            if (!footstepSoundsDict.TryGetValue(tag, out fs))
            {
                // Use DirtyGround as default
                footstepSoundsDict.TryGetValue("DirtyGround", out fs);
            }

            if (fs != null)
            {
                List<AudioClip> clips = null;

                if (CurrentMovementState == MovementState.Running && fs.runClips.Count > 0)
                {
                    clips = fs.runClips;
                }
                else if (
                    (
                        CurrentMovementState == MovementState.Walking
                        || CurrentMovementState == MovementState.Crouching
                    )
                    && fs.walkClips.Count > 0
                )
                {
                    clips = fs.walkClips;
                }

                if (clips != null && clips.Count > 0)
                {
                    AudioClip clip = clips[Random.Range(0, clips.Count)];
                    footstepAudioSource.PlayOneShot(clip);
                }
            }
        }
    }

    void PlayJumpStartSound()
    {
        RaycastHit hit;

        if (
            Physics.Raycast(
                transform.position,
                Vector3.down,
                out hit,
                characterController.height / 2 + 0.4f
            )
        )
        {
            string tag = hit.collider.tag;

            FootstepSound fs;
            if (!footstepSoundsDict.TryGetValue(tag, out fs))
            {
                // Use DirtyGround as default
                footstepSoundsDict.TryGetValue("DirtyGround", out fs);
            }

            if (fs != null && fs.jumpStartClips.Count > 0)
            {
                AudioClip clip = fs.jumpStartClips[Random.Range(0, fs.jumpStartClips.Count)];
                footstepAudioSource.PlayOneShot(clip, 0.5f); // Play at half volume
            }
        }
    }

    void PlayJumpLandSound()
    {
        RaycastHit hit;

        if (
            Physics.Raycast(
                transform.position,
                Vector3.down,
                out hit,
                characterController.height / 2 + 0.4f
            )
        )
        {
            string tag = hit.collider.tag;

            FootstepSound fs;
            if (!footstepSoundsDict.TryGetValue(tag, out fs))
            {
                // Use DirtyGround as default
                footstepSoundsDict.TryGetValue("DirtyGround", out fs);
            }

            if (fs != null && fs.jumpLandClips.Count > 0)
            {
                AudioClip clip = fs.jumpLandClips[Random.Range(0, fs.jumpLandClips.Count)];
                footstepAudioSource.PlayOneShot(clip, 0.5f); // Play at half volume
            }
        }
    }

    public float CurrentSpeed
    {
        get
        {
            if (isCrouching) // Crouching
            {
                return crouchWalkingSpeed;
            }
            else if (isRunning) // Running
            {
                return runningSpeed;
            }
            else // Walking
            {
                return walkingSpeed;
            }
        }
    }

    // Movement state enum
    public enum MovementState
    {
        Standing,
        Walking,
        Running,
        Crouching
    }

    public MovementState CurrentMovementState
    {
        get
        {
            if (isCrouching)
            {
                return MovementState.Crouching;
            }
            if (isRunning && canMove && characterController.velocity.magnitude >= 0.1f)
            {
                return MovementState.Running;
            }
            if (canMove && characterController.velocity.magnitude >= 0.1f)
            {
                return MovementState.Walking;
            }
            return MovementState.Standing;
        }
    }
}
