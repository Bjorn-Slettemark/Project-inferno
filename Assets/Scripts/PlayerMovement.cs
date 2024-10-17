using UnityEngine;

// First, let's define our possible states
public enum PlayerState
{
    Idle,
    Walking,
    Running,
    Crouching,
    Jumping
}

// The state handler class
public class PlayerStateHandler
{
    private PlayerState currentState;

    public void Initialize(PlayerState startState)
    {
        currentState = startState;
        Debug.Log($"Starting in state: {currentState}");
    }

    public void TransitionTo(PlayerState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        OnStateEnter(newState);
    }

    public PlayerState GetCurrentState()
    {
        return currentState;
    }

    private void OnStateEnter(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Idle:
                Debug.Log("Entering Idle state");
                break;
            case PlayerState.Walking:
                Debug.Log("Entering Walking state");
                break;
            case PlayerState.Running:
                Debug.Log("Entering Running state");
                break;
            case PlayerState.Crouching:
                Debug.Log("Entering Crouching state");
                break;
            case PlayerState.Jumping:
                Debug.Log("Entering Jumping state");
                break;
        }
    }
}

// Main PlayerMovement class
public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    private PlayerStateHandler stateHandler;

    [Header("Required References")]
    [SerializeField] private Transform capsuleModel;
    [SerializeField] private Transform playerCamera;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravityValue = -20f;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("Crouch Settings")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 10f;

    private Vector3 playerVelocity;
    private bool isGrounded;
    private float cameraPitch = 0f;
    private float currentSpeed;
    private Vector3 originalCapsuleScale;
    private Vector3 originalCameraPosition;
    private float originalControllerHeight;
    private Vector3 originalControllerCenter;
    private bool isRunning;
    private bool isCrouching;

    private void Start()
    {
        // Get components
        controller = GetComponent<CharacterController>();
        stateHandler = new PlayerStateHandler();
        stateHandler.Initialize(PlayerState.Idle);

        // Find references if not set
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        if (capsuleModel == null)
        {
            capsuleModel = transform.Find("Capsule");
        }

        // Store original transforms
        if (capsuleModel != null)
        {
            originalCapsuleScale = capsuleModel.localScale;
        }

        if (playerCamera != null)
        {
            originalCameraPosition = playerCamera.localPosition;
        }

        // Store original controller properties
        originalControllerHeight = controller.height;
        originalControllerCenter = controller.center;

        currentSpeed = walkSpeed;

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMouseLook();
        HandleStates();
        HandleMovement();
        UpdateState();
    }

    private void HandleStates()
    {
        if (Input.GetKey(KeyCode.LeftShift) && !isCrouching)
        {
            isRunning = true;
            currentSpeed = runSpeed;
        }
        else if (!isCrouching)
        {
            isRunning = false;
            currentSpeed = walkSpeed;
        }

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            ToggleCrouch();
        }

        UpdateCrouch();
    }

    private void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        currentSpeed = isCrouching ? crouchSpeed : walkSpeed;
    }

    private void UpdateCrouch()
    {
        if (capsuleModel == null) return;

        // Calculate the target heights
        float targetHeight = isCrouching ? crouchHeight : originalControllerHeight;
        float targetCapsuleScale = isCrouching ? crouchHeight / standingHeight : 1f;

        // Smoothly adjust the character controller's height
        float newHeight = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        controller.height = newHeight;

        // Adjust the character controller's center
        float heightDifference = originalControllerHeight - newHeight;
        Vector3 newCenter = originalControllerCenter;
        newCenter.y -= heightDifference * 0.5f;
        controller.center = newCenter;

        // Adjust the visual capsule
        Vector3 newScale = capsuleModel.localScale;
        newScale.y = Mathf.Lerp(newScale.y, originalCapsuleScale.y * targetCapsuleScale, Time.deltaTime * crouchTransitionSpeed);
        capsuleModel.localScale = newScale;

        // Adjust visual capsule position
        Vector3 newCapsulePos = Vector3.zero;
        newCapsulePos.y = -heightDifference * 0.5f;
        capsuleModel.localPosition = newCapsulePos;

        // Adjust camera position
        if (playerCamera != null)
        {
            float targetCameraY = isCrouching ?
                originalCameraPosition.y * (crouchHeight / standingHeight) :
                originalCameraPosition.y;

            Vector3 newCameraPos = playerCamera.localPosition;
            newCameraPos.y = Mathf.Lerp(newCameraPos.y, targetCameraY, Time.deltaTime * crouchTransitionSpeed);
            playerCamera.localPosition = newCameraPos;
        }
    }

    private void HandleMouseLook()
    {
        if (playerCamera == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        cameraPitch = Mathf.Clamp(cameraPitch - mouseY, -maxLookAngle, maxLookAngle);
        playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0, 0);

        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovement()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        move = move.normalized;

        controller.Move(move * Time.deltaTime * currentSpeed);

        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            playerVelocity.y = jumpForce;
        }

        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    private void UpdateState()
    {
        if (!isGrounded)
        {
            stateHandler.TransitionTo(PlayerState.Jumping);
        }
        else if (isCrouching)
        {
            stateHandler.TransitionTo(PlayerState.Crouching);
        }
        else if (isRunning && new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).magnitude > 0.1f)
        {
            stateHandler.TransitionTo(PlayerState.Running);
        }
        else if (new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).magnitude > 0.1f)
        {
            stateHandler.TransitionTo(PlayerState.Walking);
        }
        else
        {
            stateHandler.TransitionTo(PlayerState.Idle);
        }
    }

    public void SetCursorState(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}