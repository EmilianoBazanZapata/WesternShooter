using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Audio;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementStateMachine : MonoBehaviour, IDamageable
{
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
    private static readonly int IsShootingHash = Animator.StringToHash("IsShooting");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int DieTriggerHash = Animator.StringToHash("DieTrigger");

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float runSpeed = 5.5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Aim")]
    [SerializeField] private float aimRotationSpeed = 140f;
    [SerializeField] private float aimAlignSpeed = 18f;

    [Header("Grounding")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedStickForce = -2f;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator characterAnimator;

    [Header("Audio - Footsteps")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip[] walkFootstepClips;
    [SerializeField] private AudioClip[] runFootstepClips;
    [SerializeField] private float walkStepInterval = 0.45f;
    [SerializeField] private float runStepInterval = 0.3f;
    [SerializeField] private Vector2 footstepPitchRange = new Vector2(0.95f, 1.08f);
    [SerializeField] private float footstepVolume = 0.75f;
    [SerializeField] private AudioMixerGroup sfxOutputGroup;

    private CharacterController characterController;
    private IPlayerMovementState currentState;
    private float verticalVelocity;
    private float currentHealth;
    private float nextStepTime;
    private AudioSource footstepSource;
    private int lastFootstepClipIndex = -1;

    public PlayerIdleState IdleState { get; } = new PlayerIdleState();
    public PlayerWalkState WalkState { get; } = new PlayerWalkState();
    public PlayerRunState RunState { get; } = new PlayerRunState();

    public float WalkSpeed => walkSpeed;
    public float RunSpeed => runSpeed;
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;

    public Vector2 MoveInput { get; private set; }
    public bool IsMovePressed => MoveInput.sqrMagnitude > 0.01f;
    public bool IsRunPressed { get; private set; }
    public bool IsAimPressed { get; private set; }
    public bool IsShootPressed { get; private set; }
    public bool IsShootPressedThisFrame { get; private set; }
    public bool IsDead { get; private set; }

    private bool wasShootPressedLastFrame;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        currentHealth = maxHealth;
        footstepSource = CreateOrGetAudioSource("FootstepAudioSource");

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        SwitchState(IdleState);
    }

    private void Update()
    {
        HandleDeathInput();

        if (IsDead)
        {
            MoveInput = Vector2.zero;
            IsRunPressed = false;
            IsAimPressed = false;
            IsShootPressed = false;
            IsShootPressedThisFrame = false;

            SwitchState(IdleState);
            ApplyGravity(Time.deltaTime);
            UpdateAnimatorParameters();
            nextStepTime = 0f;
            return;
        }

        ReadInput();

        if (IsAimPressed)
        {
            SwitchState(IdleState);
            ApplyGravity(Time.deltaTime);
            RotateWhileAiming(Time.deltaTime);
            UpdateAnimatorParameters();
            nextStepTime = 0f;
            return;
        }

        currentState.Tick(this, Time.deltaTime);
        HandleFootsteps();
        UpdateAnimatorParameters();
    }

    private void HandleFootsteps()
    {
        if (footstepSource == null)
        {
            return;
        }

        bool isMovingOnGround = characterController.isGrounded && IsMovePressed;
        if (!isMovingOnGround)
        {
            nextStepTime = 0f;
            return;
        }

        if (Time.time < nextStepTime)
        {
            return;
        }

        AudioClip stepClip = GetRandomFootstepClip();
        if (stepClip == null)
        {
            return;
        }

        footstepSource.pitch = Random.Range(footstepPitchRange.x, footstepPitchRange.y);
        footstepSource.PlayOneShot(stepClip, footstepVolume);

        float interval = IsRunPressed ? runStepInterval : walkStepInterval;
        nextStepTime = Time.time + Mathf.Max(0.05f, interval);
    }

    private AudioClip GetRandomFootstepClip()
    {
        AudioClip[] source = GetFootstepSourcePool();

        if (source == null || source.Length == 0)
        {
            return null;
        }

        int index;
        if (source.Length == 1)
        {
            index = 0;
        }
        else
        {
            index = Random.Range(0, source.Length);
            if (index == lastFootstepClipIndex)
            {
                index = (index + 1) % source.Length;
            }
        }

        lastFootstepClipIndex = index;
        return source[index];
    }

    private AudioClip[] GetFootstepSourcePool()
    {
        if (footstepClips != null && footstepClips.Length > 0)
        {
            return footstepClips;
        }

        if (IsRunPressed && runFootstepClips != null && runFootstepClips.Length > 0)
        {
            return runFootstepClips;
        }

        return walkFootstepClips;
    }

    private AudioSource CreateOrGetAudioSource(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            GameObject childObject = new GameObject(childName);
            childObject.transform.SetParent(transform);
            childObject.transform.localPosition = Vector3.zero;
            childObject.transform.localRotation = Quaternion.identity;
            childObject.transform.localScale = Vector3.one;
            child = childObject.transform;
        }

        AudioSource source = child.GetComponent<AudioSource>();
        if (source == null)
        {
            source = child.gameObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 1.5f;
        source.maxDistance = 18f;
        source.outputAudioMixerGroup = sfxOutputGroup;
        return source;
    }

    public void SwitchState(IPlayerMovementState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState?.Exit(this);
        currentState = newState;
        currentState.Enter(this);
    }

    public void Move(float speed, float deltaTime)
    {
        Vector3 moveDirection = GetMoveDirection();

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * deltaTime);
        }

        Vector3 horizontalVelocity = moveDirection * speed;
        Vector3 finalVelocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
        characterController.Move(finalVelocity * deltaTime);
    }

    public void ApplyGravity(float deltaTime)
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedStickForce;
        }

        verticalVelocity += gravity * deltaTime;

        if (!IsMovePressed)
        {
            Vector3 verticalOnlyVelocity = new Vector3(0f, verticalVelocity, 0f);
            characterController.Move(verticalOnlyVelocity * deltaTime);
        }
    }

    private void ReadInput()
    {
        Vector2 keyboardInput = Vector2.zero;
        Vector2 gamepadInput = Vector2.zero;
        bool mouseAimPressed = false;
        bool mouseShootPressed = false;
        bool gamepadAimPressed = false;
        bool gamepadShootPressed = false;

        if (Keyboard.current != null)
        {
            float x = (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f);
            float y = (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f);
            keyboardInput = new Vector2(x, y);

            IsRunPressed = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
        }
        else
        {
            IsRunPressed = false;
        }

        if (Gamepad.current != null)
        {
            gamepadInput = Gamepad.current.leftStick.ReadValue();
            IsRunPressed = IsRunPressed || Gamepad.current.leftStickButton.isPressed;
            gamepadAimPressed = Gamepad.current.leftTrigger.ReadValue() > 0.2f;
            gamepadShootPressed = Gamepad.current.rightTrigger.ReadValue() > 0.2f;
        }

        if (Mouse.current != null)
        {
            mouseAimPressed = Mouse.current.rightButton.isPressed;
            mouseShootPressed = Mouse.current.leftButton.isPressed;
        }

        IsAimPressed = mouseAimPressed || gamepadAimPressed;
        IsShootPressed = (mouseAimPressed && mouseShootPressed) || (gamepadAimPressed && gamepadShootPressed);
        IsShootPressedThisFrame = IsShootPressed && !wasShootPressedLastFrame;
        wasShootPressedLastFrame = IsShootPressed;

        if (IsAimPressed)
        {
            MoveInput = Vector2.zero;
            IsRunPressed = false;
            return;
        }

        MoveInput = Vector2.ClampMagnitude(keyboardInput + gamepadInput, 1f);
    }

    private void HandleDeathInput()
    {
        if (IsDead)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            TriggerDeath();
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (IsDead)
        {
            return;
        }

        currentHealth -= damageAmount;

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            TriggerDeath();
        }
    }

    private void TriggerDeath()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;

        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(DieTriggerHash);
        }

        GameAudioManager manager = GameAudioManager.Instance
            ?? FindFirstObjectByType<GameAudioManager>();

        if (manager == null)
        {
            Debug.LogWarning("[PlayerMovementStateMachine] No GameAudioManager found for defeat music.", this);
            return;
        }

        manager.OnPlayerDefeatTriggered();
        FindFirstObjectByType<GameManager>()?.ShowDefeat();
    }

    private void UpdateAnimatorParameters()
    {
        if (characterAnimator == null)
        {
            return;
        }

        bool isWalking = !IsDead && IsMovePressed && !IsRunPressed && !IsAimPressed;
        bool isRunning = !IsDead && IsMovePressed && IsRunPressed && !IsAimPressed;
        bool isAiming = !IsDead && IsAimPressed;
        bool isShooting = !IsDead && (IsAimPressed || IsShootPressed);

        characterAnimator.SetBool(IsWalkingHash, isWalking);
        characterAnimator.SetBool(IsRunningHash, isRunning);
        characterAnimator.SetBool(IsAimingHash, isAiming);
        characterAnimator.SetBool(IsShootingHash, isShooting);
        characterAnimator.SetBool(IsDeadHash, IsDead);
    }

    private void RotateWhileAiming(float deltaTime)
    {
        if (cameraTransform != null)
        {
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0f;

            if (cameraForward.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(cameraForward.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, aimAlignSpeed * deltaTime);
                return;
            }
        }

        float yawInput = 0f;

        if (Mouse.current != null)
        {
            yawInput += Mouse.current.delta.ReadValue().x;
        }

        if (Gamepad.current != null)
        {
            yawInput += Gamepad.current.rightStick.ReadValue().x * 10f;
        }

        if (Mathf.Abs(yawInput) <= 0.001f)
        {
            return;
        }

        float yawDelta = yawInput * aimRotationSpeed * deltaTime;
        transform.Rotate(0f, yawDelta, 0f, Space.World);
    }

    private Vector3 GetMoveDirection()
    {
        Vector3 rawDirection = new Vector3(MoveInput.x, 0f, MoveInput.y);

        if (rawDirection.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        if (cameraTransform == null)
        {
            return rawDirection.normalized;
        }

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraForward * rawDirection.z + cameraRight * rawDirection.x;
        return moveDirection.normalized;
    }
}
