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

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    void Update()
    {
        // We are grounded, so recalculate move direction based on axes
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        // Press Left Shift to run
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        // Press Left Control to crouch
        bool isCrouching = Input.GetKey(KeyCode.LeftControl);
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        float height = characterController.height;
        float speed = isCrouching ? crouchSpeed : standingSpeed;
        height = Mathf.Lerp(height, targetHeight, Time.deltaTime * speed);
        characterController.height = height;

        // Adjust speed based on whether the player is crouching or running
        float movementSpeed = isCrouching ? crouchWalkingSpeed : (isRunning ? runningSpeed : walkingSpeed);
        float curSpeedX = canMove ? (movementSpeed * Input.GetAxis("Vertical")) : 0;
        float curSpeedY = canMove ? (movementSpeed * Input.GetAxis("Horizontal")) : 0;

        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpSpeed;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        // Apply gravity
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        // Move the controller
        characterController.Move(moveDirection * Time.deltaTime);

        // Player and Camera rotation
        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }
    }

    public float CurrentSpeed
    {
        get
        {
            if (Input.GetKey(KeyCode.LeftControl)) // Crouching
            {
                return crouchWalkingSpeed;
            }
            else if (Input.GetKey(KeyCode.LeftShift)) // Running
            {
                return runningSpeed;
            }
            else // Walking
            {
                return walkingSpeed;
            }
        }
    }
}
