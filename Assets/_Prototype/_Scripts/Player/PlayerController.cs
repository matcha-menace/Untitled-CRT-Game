using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public enum CameraDirection
{ North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest, Count };

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform mainCam;
    // input
    private PrototypePlayerInput input;

    // direction
    private Vector3 targetDirection;
    // cam based direction
    [Header("Camera Switch")]
    [SerializeField] private CameraSwitcher cameraSwitcher;
    [SerializeField] private float camLerpDuration;
    [SerializeField] public CameraDirection currentCameraDirection;
    [HideInInspector] public bool isInvertedControls;
    private bool playerLeft; // for changing animation of running left & right
    private Coroutine camSwitchRoutineInstance;

    // movement
    private Vector2 currentMovementInput;
    public static bool isMoving;
    private bool isSprinting;
    private float moveSpeed;
    private bool lastFacing; // false = right
    
    [Header("Movement")]
    [SerializeField] public float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [Range(0.0f, 0.5f)]
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float speedChangeRate;

    public static float PlayerCurrentSpeed { get; private set; }

    // dash
    private bool isDashing;
    private bool canDash;
    [Header("Dash")]
    [SerializeField] private float dashDistance;
    [Range(0, .5f)]
    [SerializeField] private float dashDuration;
    [SerializeField] private float dashCooldown;
    
    // intersection
    [SerializeField] private CameraDirection previousCameraDirection;
    public bool isInIntersection { get; set; }
    public IntersectionManager activeIntersectionManager { get; set; }
    
    // test
    [Header("Test")]
    [SerializeField] private bool isLookingBack;

    // enabling input
    void OnEnable()
    {
        input.Player.Enable();
    }
    void OnDisable()
    {
        input.Player.Disable();
    }

    void SubscribeInputEvents()
    {
        input = new PrototypePlayerInput();
        // input events
        // walking & running
        input.Player.Move.performed += OnMovementInput;
        input.Player.Move.canceled += OnMovementInput;
        input.Player.Sprint.performed += context => isSprinting = context.ReadValueAsButton();
        // dashing
        canDash = true;
        input.Player.Dash.performed += Dash;
        // camera change
        input.Player.ChangeCameraLeft.performed += ctx => OnCameraInput("Left");
        input.Player.ChangeCameraRight.performed += ctx => OnCameraInput("Right");
        input.Player.LookBack.performed += ctx => OnLookBack(true);
        input.Player.LookBack.canceled += ctx => OnLookBack(false);
        // pausing
        input.Player.Pause.performed += ctx => Services.PauseMenuManager.TogglePause();
        // console
        input.Player.Console.performed += ctx => Services.ConsoleMenuManager.ToggleConsole();
    }

    void UnsubscribeInputEvents()
    {
        // walking & running
        input.Player.Move.performed -= OnMovementInput;
        input.Player.Move.canceled -= OnMovementInput;
        input.Player.Sprint.performed -= context => isSprinting = context.ReadValueAsButton();
        // dashing
        canDash = true;
        input.Player.Dash.performed -= Dash;
        // camera change
        input.Player.ChangeCameraLeft.performed -= ctx => OnCameraInput("Left");
        input.Player.ChangeCameraRight.performed -= ctx => OnCameraInput("Right");
        input.Player.LookBack.performed -= ctx => OnLookBack(true);
        input.Player.LookBack.canceled -= ctx => OnLookBack(false);
        // pausing
        input.Player.Pause.performed -= ctx => Services.PauseMenuManager.TogglePause();
        // console
        input.Player.Console.performed -= ctx => Services.ConsoleMenuManager.ToggleConsole();
    }

    void LoadControlSettings()
    {
        isInvertedControls = PlayerPrefs.GetInt("InvertCam") == 1;
    }

    void Awake()
    {
        Services.PlayerController = this;
        LoadControlSettings();
        SubscribeInputEvents();
        currentCameraDirection = CameraDirection.North;
    }
    
    public void TogglePlayerInput(bool isActive)
    {
        if (isActive) input.Player.Enable();
        else input.Player.Disable();
    }

    // change animation state
    void HandleAnimation()
    {
        if ((GameManager.IsGamePaused && !RewindManager.isRewinding) || GameManager.IsGameEnded) return; // do not run on pause
        var currentState = animator.GetCurrentAnimatorStateInfo(0).ToString();
        if (isMoving || RewindManager.isRewindMoving)
        {
            AnimationChange.ChangeAnimationState(animator, currentState, "NewRun", true,
                !lastFacing);
            animator.speed = characterController.velocity.magnitude * .2f;
        }
        else { 
            AnimationChange.ChangeAnimationState(animator, currentState, "NewIdle", false, currentMovementInput.x < 0); 
            animator.speed = RewindManager.isRewinding ? RewindManager.RewindSpeed : 1;
        }
    }

    void Update()
    {
        HandleAnimation();
        PlayerCurrentSpeed = characterController.velocity.magnitude;
    }

    void FixedUpdate()
    {
        Move();
    }

    // ACTION METHODS
    // movement
    private void OnMovementInput(InputAction.CallbackContext context)                                    
    {                                                                                            
        currentMovementInput = context.ReadValue<Vector2>();                                     
        isMoving = currentMovementInput.x != 0 || currentMovementInput.y != 0;
        if (currentMovementInput.x < 0)
        {
            lastFacing = false;
        }
        else if (currentMovementInput.x > 0)
        {
            lastFacing = true;
        }
    }
    // movement (called per frame)
    private void Move()
    {
        // get target speed based on sprint or walk
        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
        if (!isMoving) targetSpeed = 0;
        // get input magnitude, cap at 1
        float inputMagnitude = currentMovementInput.magnitude;
        if (inputMagnitude > 1f) inputMagnitude = 1f;

        // get current horizontal speed
        float currentHorizontalSpeed = new Vector3(characterController.velocity.x, 0.0f, characterController.velocity.z).magnitude;
        // accelerate or decelerate to target speed
        if (currentHorizontalSpeed < targetSpeed - .1f || currentHorizontalSpeed > targetSpeed + .1f)
        {
            moveSpeed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, speedChangeRate * Time.deltaTime);
            // round speed to 3 decimal places
            moveSpeed = Mathf.Round(moveSpeed * 1000f) / 1000f;
        }
        else
        {
            moveSpeed = targetSpeed;
        }
        
        // create move vector
        Vector3 direct = new Vector3(currentMovementInput.x, 0f, currentMovementInput.y).normalized;
        float targetAngle = Mathf.Atan2(direct.x, direct.z) * Mathf.Rad2Deg + Camera.main.transform.eulerAngles.y - (isLookingBack ? 180 : 0);
        Vector3 move = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

        // gravity
        var gravityVector = new Vector3(0, characterController.isGrounded ? 0 : -1, 0);

        // rotate player when moving
        if (isMoving)
        {
            // set player direction to move direction, ignore Y
            Rotate(new Vector3(move.x, 0, move.z), rotationSpeed);
        }
        // move player
        if (isDashing) characterController.Move(transform.forward * (Time.deltaTime * (dashDistance / dashDuration)));
        else characterController.Move((move.normalized + gravityVector) * (Time.deltaTime * moveSpeed));
    }
    // movement rotation
    private void Rotate(Vector3 rotateTo, float rate)
    {
        // parse direction to quaternion
        Quaternion targetRotation = Quaternion.LookRotation(rotateTo);
        // rotate player
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rate);
    }

    // dash
    private void Dash(InputAction.CallbackContext context)
    {
        if (!isDashing && canDash)
        {
            StartCoroutine(Dashing());
        }
    }
    private IEnumerator Dashing()
    {
        isDashing = true;
        yield return new WaitForSeconds(dashDuration);
        StartCoroutine(DashCooling());
        canDash = false;
        isDashing = false;
    }
    private IEnumerator DashCooling()
    {
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // change camera
    private void OnCameraInput(string direction)
    {
        if (isLookingBack || GameManager.IsGamePaused || GameManager.IsGameEnded || !cameraSwitcher.isDefault) return; // prevent camera change when looking back
        var camIndex = (int)currentCameraDirection;
        previousCameraDirection = currentCameraDirection;
        switch (direction)
        {
            case "Right":
                camIndex += isInvertedControls ? -2 : 2;
                lastFacing = !isInvertedControls;
                break;
            case "Left":
                camIndex += isInvertedControls ? 2 : -2;
                lastFacing = isInvertedControls;
                break;
        }
        camIndex = Helpers.Wrap(camIndex, (int)CameraDirection.Count - 1);
        // log camera position
        // RewindManager.LogCamera(currentCameraDirection);
        SwitchCamera((CameraDirection)camIndex, camLerpDuration);
    }

    /// <summary>
    /// Switch camera to direction
    /// </summary>
    /// <param name="direction">camDirection to switch to</param>
    public void SwitchCamera(CameraDirection direction, float lerpDuration)
    {
        currentCameraDirection = direction;
        if (camSwitchRoutineInstance != null) StopCoroutine(camSwitchRoutineInstance);
        camSwitchRoutineInstance = StartCoroutine(CameraSwitchRoutine(GetTargetAngle(), lerpDuration));

        if (isInIntersection)
        {
            var indexChecker = (int)direction - 2;
            if (indexChecker < 0) indexChecker = (int)CameraDirection.Count - 2;
            activeIntersectionManager.OnToggleContact(direction, (int)previousCameraDirection == indexChecker);
        }
    }
    
    private IEnumerator CameraSwitchRoutine(float angle, float duration)
    {
        var lerpElapsed = 0f;
        while (lerpElapsed < duration)
        {
            mainCam.eulerAngles = 
                new Vector3(
                    mainCam.eulerAngles.x,
                    Mathf.LerpAngle(mainCam.eulerAngles.y, 
                        angle, lerpElapsed / duration),
                    0);
            lerpElapsed += Time.deltaTime;
            yield return null;
        }
        mainCam.eulerAngles = new Vector3(mainCam.eulerAngles.x, angle, 0);
    }
    
    float GetTargetAngle()
    {
        float angle = currentCameraDirection switch
        {
            CameraDirection.North => 0,
            CameraDirection.NorthEast => 45,
            CameraDirection.East => 90,
            CameraDirection.SouthEast => 135,
            CameraDirection.South => 180,
            CameraDirection.SouthWest => -135,
            CameraDirection.West => -90,
            CameraDirection.NorthWest => -45,
        };

        angle += isLookingBack ? 180 : 0;
        return angle;
    }

    // look back logic
    void OnLookBack(bool isActive)
    {
        if (!cameraSwitcher.isDefault) return;
        isLookingBack = isActive;
        if (camSwitchRoutineInstance != null) StopCoroutine(camSwitchRoutineInstance);
        mainCam.eulerAngles = new Vector3(mainCam.eulerAngles.x, GetTargetAngle(), 0);
    }
}
