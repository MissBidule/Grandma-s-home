using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class SoloPlayerController : MonoBehaviour
{
    [SerializeField]
    private float _moveSpeed = 5f; // how fast our player will move
    [SerializeField]
    private float _gravity = -20f; // how strong is gravity when we jump
    [SerializeField]
    private float _rotationSpeed = 90f; // how quickly our player will turn
    [SerializeField]
    private float _jumpSpeed = 15f; // how fast our player will jump
    private float verticalInput; // our vertical axis input
    Vector3 moveVelocity; // forward velocity
    private CharacterController _controller; // variable of type CharacterController
    private CinemachineCamera m_playerCamera; // reference to the child camera
    private Rigidbody m_rigidbody; // reference to the Rigidbody component
    public Vector2 m_lookInputVector { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        m_playerCamera = GetComponentInChildren<CinemachineCamera>(); // assign the child camera to a variable
        m_rigidbody = GetComponent<Rigidbody>(); // assign the Rigidbody to a variable
        _controller = GetComponent<CharacterController>(); // assign the CharacterController to a variable
    }

    // Update is called once per frame
    void Update()
    {
        verticalInput = Input.GetAxis("Vertical"); 
        if (_controller.isGrounded) 
        {
            Transform cam = m_playerCamera.transform;

            Vector3 forward = cam.forward;
            Vector3 right = cam.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            Vector3 wishDir;
            wishDir = (forward * verticalInput).normalized;
            // must be on a surface to jump so no double-jumping and no movement changes while airborne but make sure Min Move
            // Distance on the CharacterController is set to e
            moveVelocity = forward *_moveSpeed * verticalInput; // forward/backward - Z axis
            if (Input.GetKeyDown(KeyCode.Space)) // space bar
            {
                moveVelocity.y = _jumpSpeed; // set upward velocity burst
            }
            if (wishDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(wishDir, Vector3.up);

                m_rigidbody.MoveRotation(
                    Quaternion.Slerp(
                        m_rigidbody.rotation,
                        targetRotation,
                        _rotationSpeed * Time.fixedDeltaTime
                    )
                );
            }
        }

        moveVelocity.y += _gravity * Time.deltaTime; // account for gravity
        _controller.Move(moveVelocity * Time.deltaTime); // move the player regulated by constant time
    }

    public void OnLook(InputAction.CallbackContext _context)
    {
        m_lookInputVector = _context.ReadValue<Vector2>();
    }
}