using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float sneakSpeed = 1.6f;
    public float sprintSpeed = 5.5f;
    public bool Hidden { get; private set; }
    public bool Sneaking { get; private set; }
    public float NoiseRadius { get; private set; }
    public float Stamina { get; private set; } = 1f;
    public int FootstepId { get; private set; }
    public Vector3 FootstepPosition { get; private set; }
    public float FootstepRadius { get; private set; }
    CharacterController controller;
    Transform body;
    Transform movementCamera;
    bool crouchToggled, exhausted;
    float stepDistance;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        body = transform.Find("Body");
        if (Camera.main != null) movementCamera = Camera.main.transform;
    }

    void Update()
    {
        var game = GoblinDungeon.Instance;
        var keys = Keyboard.current;
        if (keys == null || game == null || !game.Playing) { NoiseRadius = 0; return; }
        Vector2 input = new Vector2(
            (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0),
            (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0) - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0));
        input = Vector2.ClampMagnitude(input, 1);
        if (keys.cKey.wasPressedThisFrame) crouchToggled = !crouchToggled;
        Sneaking = crouchToggled || keys.leftCtrlKey.isPressed || keys.rightCtrlKey.isPressed;
        bool sprintHeld = keys.leftShiftKey.isPressed || keys.rightShiftKey.isPressed;
        if (exhausted && (!sprintHeld || Stamina >= .3f)) exhausted = false;
        bool running = sprintHeld && !Sneaking && !exhausted && Stamina > 0 && input.sqrMagnitude > 0;
        Stamina = Mathf.Clamp01(Stamina + (running ? -.27f : .2f) * Time.deltaTime);
        if (Stamina <= 0) exhausted = true;
        // Project camera axes onto the floor: W moves up the screen, D moves right.
        Vector3 right = movementCamera != null ? movementCamera.right : Vector3.right;
        Vector3 forward = movementCamera != null ? movementCamera.forward : Vector3.forward;
        right.y = forward.y = 0;
        Vector3 direction = (right.normalized * input.x + forward.normalized * input.y);
        direction = Vector3.ClampMagnitude(direction, 1);
        float speed = Sneaking ? sneakSpeed : running ? sprintSpeed + game.Boots * .5f : walkSpeed;
        Vector3 before = transform.position;
        controller.Move((direction * speed + Vector3.down * 4) * Time.deltaTime);
        if (direction.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 15);
        Hidden = Sneaking && game.InHidingSpot(transform.position);
        Vector3 displacement = transform.position - before;
        displacement.y = 0;
        NoiseRadius = displacement.sqrMagnitude < .000001f || Hidden ? 0 : Sneaking ? .5f : running ? 7f : game.SoftShoes ? 1.5f : 3f;
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
        if (body != null) body.localScale = new Vector3(1, Sneaking ? .65f : 1, 1);
    }
}
