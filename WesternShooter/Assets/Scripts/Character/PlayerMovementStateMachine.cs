using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Audio;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementStateMachine : MonoBehaviour, IDamageable
{
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

    private PlayerMotor playerMotor;
    private PlayerAnimationController playerAnimationController;
    private IPlayerMovementState currentState;
    private PlayerInputReader playerInputReader;
    private Transform lastConfiguredCameraTransform;
    private Animator lastConfiguredAnimator;
    private PlayerHealthModel playerHealth;
    private float nextStepTime;
    private AudioSource footstepSource;
    private int lastFootstepClipIndex = -1;
    private bool deathEffectsApplied;

    public PlayerIdleState IdleState { get; } = new PlayerIdleState();
    public PlayerWalkState WalkState { get; } = new PlayerWalkState();
    public PlayerRunState RunState { get; } = new PlayerRunState();

    public float WalkSpeed => walkSpeed;
    public float RunSpeed => runSpeed;
    public float MaxHealth => playerHealth != null ? playerHealth.MaxHealth : maxHealth;
    public float CurrentHealth => playerHealth != null ? playerHealth.CurrentHealth : maxHealth;

    public Vector2 MoveInput { get; private set; }
    public bool IsMovePressed => MoveInput.sqrMagnitude > 0.01f;
    public bool IsRunPressed { get; private set; }
    public bool IsAimPressed { get; private set; }
    public bool IsShootPressed { get; private set; }
    public bool IsShootPressedThisFrame { get; private set; }
    public bool IsDead => playerHealth != null && playerHealth.IsDead;

    private void Awake()
    {
        playerMotor = GetComponent<PlayerMotor>();
        playerAnimationController = GetComponent<PlayerAnimationController>();
        playerInputReader = GetComponent<PlayerInputReader>();

        playerHealth = new PlayerHealthModel(maxHealth);
        playerHealth.Died += OnPlayerDied;
        footstepSource = CreateOrGetAudioSource("FootstepAudioSource");

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<Animator>();
        }

        ConfigureMotor();
        ConfigureAnimationController();
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= OnPlayerDied;
        }
    }

    private void Start()
    {
        SwitchState(IdleState);
    }

    private void Update()
    {
        SyncMotorCameraTransform();
        SyncAnimationControllerAnimator();
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

        bool isMovingOnGround = playerMotor != null && playerMotor.IsGrounded && IsMovePressed;
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
        if (playerMotor == null)
        {
            return;
        }

        playerMotor.Move(MoveInput, speed, deltaTime);
    }

    public void ApplyGravity(float deltaTime)
    {
        if (playerMotor == null)
        {
            return;
        }

        playerMotor.ApplyGravity(IsMovePressed, deltaTime);
    }

    private void ReadInput()
    {
        if (playerInputReader == null)
        {
            MoveInput = Vector2.zero;
            IsRunPressed = false;
            IsAimPressed = false;
            IsShootPressed = false;
            IsShootPressedThisFrame = false;
            return;
        }

        playerInputReader.EnsurePolledThisFrame();
        MoveInput = playerInputReader.MoveInput;
        IsRunPressed = playerInputReader.IsRunPressed;
        IsAimPressed = playerInputReader.IsAimPressed;
        IsShootPressed = playerInputReader.IsShootPressed;
        IsShootPressedThisFrame = playerInputReader.IsShootPressedThisFrame;
    }

    private void HandleDeathInput()
    {
        if (IsDead)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            playerHealth.MarkDead();
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (IsDead)
        {
            return;
        }

        playerHealth.ApplyDamage(damageAmount);
    }

    private void OnPlayerDied()
    {
        if (deathEffectsApplied)
        {
            return;
        }

        deathEffectsApplied = true;

        playerAnimationController?.PlayDeath();

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
        if (playerAnimationController == null)
        {
            return;
        }

        playerAnimationController.SyncGameplayAnimationState(IsDead, IsMovePressed, IsRunPressed, IsAimPressed, IsShootPressed);
    }

    private void RotateWhileAiming(float deltaTime)
    {
        if (playerMotor == null)
        {
            return;
        }

        playerMotor.RotateWhileAiming(deltaTime);
    }

    private void ConfigureMotor()
    {
        if (playerMotor == null)
        {
            Debug.LogError("[PlayerMovementStateMachine] Missing PlayerMotor component on player root.", this);
            return;
        }

        playerMotor.Configure(cameraTransform, rotationSpeed, aimRotationSpeed, aimAlignSpeed, gravity, groundedStickForce);
        lastConfiguredCameraTransform = cameraTransform;
    }

    private void SyncMotorCameraTransform()
    {
        if (playerMotor == null)
        {
            return;
        }

        if (lastConfiguredCameraTransform == cameraTransform)
        {
            return;
        }

        playerMotor.SetCameraTransform(cameraTransform);
        lastConfiguredCameraTransform = cameraTransform;
    }

    private void ConfigureAnimationController()
    {
        if (playerAnimationController == null)
        {
            Debug.LogError("[PlayerMovementStateMachine] Missing PlayerAnimationController component on player root.", this);
            return;
        }

        playerAnimationController.Configure(characterAnimator);
        lastConfiguredAnimator = characterAnimator;
    }

    private void SyncAnimationControllerAnimator()
    {
        if (playerAnimationController == null)
        {
            return;
        }

        if (lastConfiguredAnimator == characterAnimator)
        {
            return;
        }

        playerAnimationController.Configure(characterAnimator);
        lastConfiguredAnimator = characterAnimator;
    }
}
