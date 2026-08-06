using System;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine;
using WesternShooter.Player.StateMachine.States;
using WesternShooter.Player.Health;
using WesternShooter.Player.Input;
using WesternShooter.Player.Movement;
using WesternShooter.Player.Presentation;
using WesternShooter.Shared.Combat;

namespace WesternShooter.Player.StateMachine
{
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
        private PlayerFootstepAudio playerFootstepAudio;
        private IPlayerMovementState currentState;
        private PlayerInputReader playerInputReader;
        private Transform lastConfiguredCameraTransform;
        private Animator lastConfiguredAnimator;
        private PlayerHealthModel playerHealth;
        private bool deathEffectsApplied;

        public PlayerIdleState IdleState { get; } = new PlayerIdleState();
        public PlayerWalkState WalkState { get; } = new PlayerWalkState();
        public PlayerRunState RunState { get; } = new PlayerRunState();

        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        public float MaxHealth => playerHealth != null ? playerHealth.MaxHealth : maxHealth;
        public float CurrentHealth => playerHealth != null ? playerHealth.CurrentHealth : maxHealth;

        public event Action<float, float> HealthChanged;
        public event Action Died;
        public event Action<bool> AimChanged;

        public Vector2 MoveInput { get; private set; }
        public bool IsMovePressed => MoveInput.sqrMagnitude > 0.01f;
        public bool IsRunPressed { get; private set; }
        public bool IsAimPressed { get; private set; }
        public bool IsShootPressed { get; private set; }
        public bool IsDead => playerHealth != null && playerHealth.IsDead;

        private void Awake()
        {
            playerMotor = GetComponent<PlayerMotor>();
            playerAnimationController = GetComponent<PlayerAnimationController>();
            playerFootstepAudio = GetComponent<PlayerFootstepAudio>();
            playerInputReader = GetComponent<PlayerInputReader>();

            playerHealth = new PlayerHealthModel(maxHealth);
            playerHealth.HealthChanged += OnHealthChanged;
            playerHealth.Died += OnPlayerDied;

            if (cameraTransform == null && UnityEngine.Camera.main != null)
                cameraTransform = UnityEngine.Camera.main.transform;

            if (characterAnimator == null)
                characterAnimator = GetComponentInChildren<Animator>();

            ConfigureMotor();
            ConfigureAnimationController();
            ConfigureFootstepAudio();
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= OnHealthChanged;
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
                SetAimPressed(false);
                IsShootPressed = false;

                SwitchState(IdleState);
                ApplyGravity(Time.deltaTime);
                HandleFootsteps();
                UpdateAnimatorParameters();
                return;
            }

            ReadInput();

            if (IsAimPressed)
            {
                SwitchState(IdleState);
                ApplyGravity(Time.deltaTime);
                RotateWhileAiming(Time.deltaTime);
                HandleFootsteps();
                UpdateAnimatorParameters();
                return;
            }

            currentState.Tick(this, Time.deltaTime);
            HandleFootsteps();
            UpdateAnimatorParameters();
        }

        private void HandleFootsteps()
        {
            if (playerFootstepAudio == null)
                return;

            var isGrounded = playerMotor != null && playerMotor.IsGrounded;
            playerFootstepAudio.Tick(isGrounded, IsMovePressed, IsRunPressed, IsAimPressed, IsDead, Time.time);
        }

        public void SwitchState(IPlayerMovementState newState)
        {
            if (currentState == newState)
                return;

            currentState?.Exit(this);
            currentState = newState;
            currentState.Enter(this);
        }

        public void Move(float speed, float deltaTime)
        {
            if (playerMotor == null)
                return;

            playerMotor.Move(MoveInput, speed, deltaTime);
        }

        public void ApplyGravity(float deltaTime)
        {
            if (playerMotor == null)
                return;

            playerMotor.ApplyGravity(IsMovePressed, deltaTime);
        }

        private void ReadInput()
        {
            if (playerInputReader == null)
            {
                MoveInput = Vector2.zero;
                IsRunPressed = false;
                SetAimPressed(false);
                IsShootPressed = false;
                return;
            }

            playerInputReader.EnsurePolledThisFrame();
            MoveInput = playerInputReader.MoveInput;
            IsRunPressed = playerInputReader.IsRunPressed;
            SetAimPressed(playerInputReader.IsAimPressed);
            IsShootPressed = playerInputReader.IsShootPressed;
        }

        private void SetAimPressed(bool isAimPressed)
        {
            if (IsAimPressed == isAimPressed)
                return;

            IsAimPressed = isAimPressed;
            AimChanged?.Invoke(IsAimPressed);
        }

        private void OnHealthChanged(float currentHealth, float maxHealthValue)
        {
            HealthChanged?.Invoke(currentHealth, maxHealthValue);
        }

        private void HandleDeathInput()
        {
            if (IsDead)
                return;

            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                playerHealth.MarkDead();
        }

        public void TakeDamage(float damageAmount)
        {
            if (IsDead)
                return;

            playerHealth.ApplyDamage(damageAmount);
        }

        private void OnPlayerDied()
        {
            if (deathEffectsApplied)
                return;

            deathEffectsApplied = true;

            playerAnimationController?.PlayDeath();
            Died?.Invoke();
        }

        private void UpdateAnimatorParameters()
        {
            if (playerAnimationController == null)
                return;

            playerAnimationController.SyncGameplayAnimationState(IsDead, IsMovePressed, IsRunPressed, IsAimPressed, IsShootPressed);
        }

        private void RotateWhileAiming(float deltaTime)
        {
            if (playerMotor == null)
                return;

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
                return;

            if (lastConfiguredCameraTransform == cameraTransform)
                return;

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
                return;

            if (lastConfiguredAnimator == characterAnimator)
                return;

            playerAnimationController.Configure(characterAnimator);
            lastConfiguredAnimator = characterAnimator;
        }

        private void ConfigureFootstepAudio()
        {
            if (playerFootstepAudio == null)
            {
                Debug.LogError("[PlayerMovementStateMachine] Missing PlayerFootstepAudio component on player root.", this);
                return;
            }

            playerFootstepAudio.Configure(
                footstepClips,
                walkFootstepClips,
                runFootstepClips,
                walkStepInterval,
                runStepInterval,
                footstepPitchRange,
                footstepVolume,
                sfxOutputGroup);
        }
    }

}

