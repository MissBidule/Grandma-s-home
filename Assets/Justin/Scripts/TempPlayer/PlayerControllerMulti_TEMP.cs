using System;
using System.Collections.Generic;
using PurrNet;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerControllerMulti_TEMP : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpForce = 1f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundCheckDistance = 0.2f;

    [Header("Look Settings")]
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private NetworkAnimator playerAnimator;
    [SerializeField] private List<Renderer> renderersToHide = new();
    
    private CharacterController characterController;
    private Vector3 velocity;
    private float verticalRotation = 0f;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        enabled = isOwner;

        if (!isOwner) {
            Destroy(playerCamera.gameObject);
            
            // Change color of non-owned players for better visibility
            foreach (var renderer in renderersToHide)
            {
                renderer.material.color = Color.HSVToRGB(UnityEngine.Random.Range(0f, 1f), 0.8f, 0.9f);
            }
        }

        if (isOwner)
        {
            foreach (var renderer in renderersToHide)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        characterController = GetComponent<CharacterController>();

        if (playerCamera == null)
        {
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement()
    {
        bool isGrounded = IsGrounded();
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
        characterController.Move(moveDirection * (currentSpeed * Time.deltaTime));

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);

        // Update animator parameters
        if (playerAnimator != null)
        {
            // Debug.Log(playerAnimator.GetFloat("Forward"));
            playerAnimator.SetFloat("Forward", vertical);
            // Debug.Log(playerAnimator.GetFloat("Sideways"));
            playerAnimator.SetFloat("Sideways", horizontal);
        }
    }

    private void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.03f, Vector3.down, groundCheckDistance);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.03f, Vector3.down * groundCheckDistance);
    }
#endif
}