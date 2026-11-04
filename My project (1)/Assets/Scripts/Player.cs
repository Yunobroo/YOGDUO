using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2.5f;
    public float sneakSpeed = 1.3f;
    public float sprintSpeed = 4.5f;
    public bool Hidden { get; private set; }
    public bool Sneaking { get; private set; }
    public float NoiseRadius { get; private set; }
    public float Stamina { get; private set; } = 1f;
    public int FootstepId { get; private set; }
    public Vector3 FootstepPosition { get; private set; }

    public static float StaminaDrain => .27f / (1 + DataManager.Data.stamina * .25f);
    public float FootstepRadius { get; private set; }
    CharacterController controller;
    Transform body;
    Transform movementCamera;
    bool crouchToggled, sprintToggled, exhausted;
    float stepDistance;
    Vector3 standingScale = Vector3.one;
    Animator animator;
    static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
    static readonly int GaitRate = Animator.StringToHash("GaitRate");

    void Start()
    {
        controller = GetComponent<CharacterController>();
        body = transform.Find("Body");
        if (body != null) standingScale = body.localScale;
        if (body != null) animator = body.GetComponent<Animator>();
        Hidden = false;
        if (Camera.main != null) movementCamera = Camera.main.transform;
    }

    void Update()
    {
        var game = GoblinDungeon.Instance;

        if (game == null || !game.Playing)
        {
            NoiseRadius = 0;
            stepDistance = 0;
            sprintToggled = false;
            if (animator != null && animator.isActiveAndEnabled) animator.SetFloat(MoveSpeed, 0);
            return;
        }

        // Read stance before moving so leaving cover never takes an extra frame.
        Vector2 input = ReadMovementInput();
        bool running = UpdateMovementMode(input, game.CarryingCrown);
        Hidden = Sneaking && game.InHidingSpot(transform.position);
        Move(input, running, Time.deltaTime);

        //Sneaking
        Hidden = Sneaking && game.InHidingSpot(transform.position);
        if (body != null)
        {
            body.localScale = Vector3.Scale(standingScale, new Vector3(1, Sneaking ? .65f : 1, 1));
            body.gameObject.SetActive(!Hidden);                          //If completely hidden turn the body invisible
        }
    }

    Vector2 ReadMovementInput()
    {
        var keys = Keyboard.current;
        var pad = Gamepad.current;
        Vector2 input = Vector2.zero;
        if (keys != null)
            input = Vector2.ClampMagnitude(new Vector2(
                (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0),
                (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0) - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0)), 1);
        if (pad != null)
        {
            // ReadValue applies the Input System's stick deadzone and preserves analog speed.
            Vector2 stick = pad.leftStick.ReadValue();
            if (stick.sqrMagnitude > input.sqrMagnitude) input = stick;
            if (pad.leftStickButton.wasPressedThisFrame) sprintToggled = !sprintToggled;
        }
        else sprintToggled = false;
        return input;
    }

    bool UpdateMovementMode(Vector2 input, bool carryingCrown)
    {
        var keys = Keyboard.current;
        var pad = Gamepad.current;
        if ((keys != null && keys.cKey.wasPressedThisFrame) || (pad != null && pad.rightStickButton.wasPressedThisFrame))
            crouchToggled = !crouchToggled;
        Sneaking = crouchToggled || (keys != null && (keys.leftCtrlKey.isPressed || keys.rightCtrlKey.isPressed));
        // Click L3 once to run; stopping or crouching cancels controller sprint.
        if (input.sqrMagnitude == 0 || Sneaking || carryingCrown) sprintToggled = false;
        bool sprintHeld = sprintToggled || (keys != null && keys.spaceKey.isPressed);
        if (exhausted && (!sprintHeld || Stamina >= .3f)) exhausted = false;
        bool running = sprintHeld && !carryingCrown && !Sneaking && !exhausted && Stamina > 0 && input.sqrMagnitude > 0;
        Stamina = Mathf.Clamp01(Stamina + (running ? -StaminaDrain : .2f) * Time.deltaTime);
        if (Stamina <= 0) exhausted = true;
        return running;
    }

    void Move(Vector2 input, bool running, float deltaTime)
    {
            // Project camera axes onto the floor: W moves up the screen, D moves right.
            Vector3 right = movementCamera != null ? movementCamera.right : Vector3.right;
            Vector3 forward = movementCamera != null ? movementCamera.forward : Vector3.forward;
            right.y = forward.y = 0;
            Vector3 direction = (right.normalized * input.x + forward.normalized * input.y);
            direction = Vector3.ClampMagnitude(direction, 1);
            float speed = Sneaking ? sneakSpeed : running ? sprintSpeed + DataManager.Data.boots * .5f : walkSpeed;
            Vector3 before = transform.position;
            controller.Move((direction * speed + Vector3.down * 4) * deltaTime);
            if (direction.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), deltaTime * 15);
            Vector3 displacement = transform.position - before;
            displacement.y = 0;
            if (animator != null && animator.isActiveAndEnabled)
            {
                float actualSpeed = deltaTime > 0 ? displacement.magnitude / deltaTime : 0;
                animator.SetFloat(MoveSpeed, actualSpeed, .1f, deltaTime);
                animator.SetFloat(GaitRate, Mathf.Clamp(actualSpeed / 3.5f, .4f, 1.5f));
            }

        
            UpdateFootsteps(displacement, running, DataManager.Data.softShoes);
    }

    void UpdateFootsteps(Vector3 displacement, bool running, bool softShoes)
    {
        NoiseRadius = displacement.sqrMagnitude < .000001f || Hidden ? 0 : Sneaking ? .5f : running ? 7f : softShoes ? 1.5f : 3f;
        if (NoiseRadius > 0)
        {
            stepDistance += displacement.magnitude;
            if (stepDistance >= (Sneaking ? .65f : running ? 1.1f : .85f))
            {
                stepDistance = 0;
                FootstepPosition = transform.position;
                FootstepRadius = NoiseRadius;
                FootstepId++;
            }
        }
        else stepDistance = 0;

    }

}

