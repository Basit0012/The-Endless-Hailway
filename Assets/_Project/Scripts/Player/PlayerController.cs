using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EndlessHallway.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Speeds")]
        [SerializeField] private float walkSpeed = 3.0f;
        [SerializeField] private float sprintSpeed = 5.5f;
        [SerializeField] private float crouchSpeed = 1.8f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Sprint")]
        [SerializeField] private bool canSprint = true;
        [SerializeField] private float sprintBobFrequencyMultiplier = 1.3f;
        [SerializeField] private float sprintStepInterval = 0.35f;

        [Header("Crouch")]
        [SerializeField] private bool canCrouch = true;
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchHeight = 1.1f;
        [SerializeField] private float crouchTransitionSpeed = 10f;
        [SerializeField] private float crouchStepInterval = 0.75f;

        [Header("Head Bobbing")]
        [SerializeField] private bool enableHeadBob = true;
        [SerializeField] private Transform cameraHolder;
        [SerializeField] private float bobFrequency = 1.6f;
        [SerializeField] private float bobHorizontalAmplitude = 0.03f;
        [SerializeField] private float bobVerticalAmplitude = 0.04f;

        [Header("Footstep Audio")]
        [SerializeField] private float stepInterval = 0.55f;
        [SerializeField] private AudioClip[] footstepClips;

        [Header("Character Rig")]
        [SerializeField] private GameObject characterModel;
        [SerializeField] private Animator characterAnimator;

        private CharacterController characterController;
        private Vector3 velocity;
        private float bobTimer = 0f;
        private Vector3 defaultCameraLocalPos;
        private Vector3 currentCameraBasePos;
        private float stepTimer = 0f;
        private bool canMove = true;

        private bool isSprinting = false;
        private bool isCrouching = false;
        private float currentSpeed = 3.0f;
        private float currentControllerHeight = 1.8f;

        private static readonly int AnimSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimIsCrouching = Animator.StringToHash("IsCrouching");
        private static readonly int AnimTurn = Animator.StringToHash("Turn");
        private static readonly int AnimMoveX = Animator.StringToHash("MoveX");
        private static readonly int AnimMoveZ = Animator.StringToHash("MoveZ");

        private float smoothedSpeed = 0f;
        private float smoothedTurn = 0f;
        private float smoothedMoveX = 0f;
        private float smoothedMoveZ = 0f;

        public bool CanMove
        {
            get => canMove;
            set
            {
                canMove = value;
                if (!canMove)
                {
                    velocity = Vector3.zero;
                    isSprinting = false;
                    UpdateAnimations(Vector2.zero);
                }
            }
        }

        public bool IsSprinting => isSprinting;
        public bool IsCrouching => isCrouching;
        public float CurrentSpeed => currentSpeed;
        public Transform CameraHolder => cameraHolder;
        public GameObject CharacterModel => characterModel;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (characterController != null)
            {
                standingHeight = characterController.height;
                currentControllerHeight = standingHeight;
            }

            if (cameraHolder == null)
            {
                cameraHolder = transform.Find("CameraPivot") ?? transform.Find("CameraHolder");
            }

            if (cameraHolder != null)
            {
                defaultCameraLocalPos = cameraHolder.localPosition;
                currentCameraBasePos = defaultCameraLocalPos;
            }

            if (characterModel == null)
            {
                // Auto-detect if child exists
                var manChild = transform.Find("Adventure_Character") ?? transform.Find("Man_03") ?? transform.Find("Character");
                if (manChild != null) characterModel = manChild.gameObject;
            }

            if (characterAnimator == null && characterModel != null)
            {
                characterAnimator = characterModel.GetComponent<Animator>();
            }
            if (characterAnimator == null)
            {
                characterAnimator = GetComponentInChildren<Animator>(true);
            }
            if (characterAnimator != null)
            {
                characterAnimator.applyRootMotion = false;
            }

            if (GetComponent<PlayerStressSystem>() == null)
            {
                gameObject.AddComponent<PlayerStressSystem>();
            }
        }

        private void Update()
        {
            if (Core.GameManager.Instance != null && Core.GameManager.Instance.CurrentState != Core.GameState.Exploring)
            {
                velocity = Vector3.zero;
                isSprinting = false;
                UpdateAnimations(Vector2.zero);
                return;
            }

            ApplyGravity();

            if (!canMove)
            {
                UpdateAnimations(Vector2.zero);
                return;
            }

            Vector2 moveInput = ReadMovementInput();
            HandleCrouch();
            HandleMovementSpeed(moveInput);

            Vector3 moveDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
            characterController.Move(moveDirection * (currentSpeed * Time.deltaTime) + velocity * Time.deltaTime);

            HandleHeadBob(moveInput);
            HandleFootsteps(moveInput);
            UpdateAnimations(moveInput);
        }

        private void UpdateAnimations(Vector2 moveInput)
        {
            if (characterAnimator == null) return;

            bool isMoving = moveInput.sqrMagnitude > 0.01f && canMove;
            float targetSpeed = isMoving ? currentSpeed : 0f;

            smoothedSpeed = Mathf.MoveTowards(smoothedSpeed, targetSpeed, Time.deltaTime * 12f);
            smoothedTurn = Mathf.MoveTowards(smoothedTurn, isMoving ? moveInput.x : 0f, Time.deltaTime * 10f);
            smoothedMoveX = Mathf.MoveTowards(smoothedMoveX, isMoving ? moveInput.x : 0f, Time.deltaTime * 10f);
            smoothedMoveZ = Mathf.MoveTowards(smoothedMoveZ, isMoving ? moveInput.y : 0f, Time.deltaTime * 10f);

            characterAnimator.SetFloat(AnimSpeed, smoothedSpeed);
            characterAnimator.SetBool(AnimIsCrouching, isCrouching);
            characterAnimator.SetFloat(AnimTurn, smoothedTurn);
            characterAnimator.SetFloat(AnimMoveX, smoothedMoveX);
            characterAnimator.SetFloat(AnimMoveZ, smoothedMoveZ);
        }

        private Vector2 ReadMovementInput()
        {
            Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
                return input.sqrMagnitude > 1f ? input.normalized : input;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) input.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) input.y -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input.x += 1f;
#endif

            return input.sqrMagnitude > 1f ? input.normalized : input;
        }

        private void HandleMovementSpeed(Vector2 moveInput)
        {
            bool sprintRequested = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                sprintRequested = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!sprintRequested)
            {
                sprintRequested = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            }
#endif

            // Can only sprint if moving forward, not crouching, and grounded
            if (canSprint && !isCrouching && sprintRequested && moveInput.y > 0.1f && characterController.isGrounded)
            {
                isSprinting = true;
                currentSpeed = sprintSpeed;
            }
            else if (isCrouching)
            {
                isSprinting = false;
                currentSpeed = crouchSpeed;
            }
            else
            {
                isSprinting = false;
                currentSpeed = walkSpeed;
            }
        }

        private void HandleCrouch()
        {
            if (!canCrouch) return;

            bool crouchRequested = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                crouchRequested = Keyboard.current.leftCtrlKey.isPressed ||
                                  Keyboard.current.rightCtrlKey.isPressed ||
                                  Keyboard.current.cKey.isPressed;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!crouchRequested)
            {
                crouchRequested = Input.GetKey(KeyCode.LeftControl) ||
                                  Input.GetKey(KeyCode.RightControl) ||
                                  Input.GetKey(KeyCode.C);
            }
#endif

            // If releasing crouch, check for ceiling obstruction before standing up
            if (!crouchRequested && isCrouching)
            {
                if (HasCeilingObstruction())
                {
                    crouchRequested = true; // Stay crouched under obstacles
                }
            }

            isCrouching = crouchRequested;

            float targetHeight = isCrouching ? crouchHeight : standingHeight;
            currentControllerHeight = Mathf.Lerp(currentControllerHeight, targetHeight, Time.deltaTime * crouchTransitionSpeed);

            if (characterController != null)
            {
                characterController.height = currentControllerHeight;
                // Adjust center so capsule base stays anchored to ground
                float centerY = (currentControllerHeight - standingHeight) * 0.5f;
                characterController.center = new Vector3(0f, centerY, 0f);
            }

            // Adjust camera height smoothly
            float heightDelta = standingHeight - currentControllerHeight;
            currentCameraBasePos = defaultCameraLocalPos - new Vector3(0f, heightDelta, 0f);
        }

        private bool HasCeilingObstruction()
        {
            if (characterController == null) return false;
            float radius = characterController.radius * 0.85f;
            Vector3 start = transform.position + Vector3.up * (crouchHeight * 0.5f);
            float checkDistance = standingHeight - (crouchHeight * 0.5f);
            return Physics.SphereCast(start, radius, Vector3.up, out _, checkDistance, ~0, QueryTriggerInteraction.Ignore);
        }

        private void ApplyGravity()
        {
            if (characterController.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            velocity.y += gravity * Time.deltaTime;
        }

        private void HandleHeadBob(Vector2 moveInput)
        {
            if (!enableHeadBob || cameraHolder == null) return;

            float shakeScale = Core.SettingsManager.Instance != null ? Core.SettingsManager.Instance.CameraShake : 1.0f;
            if (shakeScale <= 0.001f)
            {
                cameraHolder.localPosition = Vector3.Lerp(cameraHolder.localPosition, currentCameraBasePos, Time.deltaTime * 6f);
                return;
            }

            if (moveInput.sqrMagnitude > 0.01f && characterController.isGrounded)
            {
                float freqMultiplier = isSprinting ? sprintBobFrequencyMultiplier : (isCrouching ? 0.8f : 1.0f);
                float speedFactor = currentSpeed / walkSpeed;
                bobTimer += Time.deltaTime * (bobFrequency * speedFactor * freqMultiplier);

                float hOffset = Mathf.Cos(bobTimer) * bobHorizontalAmplitude * shakeScale;
                float vOffset = Mathf.Abs(Mathf.Sin(bobTimer)) * bobVerticalAmplitude * shakeScale;

                cameraHolder.localPosition = currentCameraBasePos + new Vector3(hOffset, vOffset, 0f);
            }
            else
            {
                bobTimer = 0f;
                cameraHolder.localPosition = Vector3.Lerp(cameraHolder.localPosition, currentCameraBasePos, Time.deltaTime * 6f);
            }
        }

        private void HandleFootsteps(Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude > 0.01f && characterController.isGrounded)
            {
                float currentInterval = isSprinting ? sprintStepInterval : (isCrouching ? crouchStepInterval : stepInterval);
                stepTimer += Time.deltaTime;
                if (stepTimer >= currentInterval)
                {
                    stepTimer = 0f;
                    PlayFootstepSound();
                }
            }
            else
            {
                stepTimer = stepInterval * 0.8f;
            }
        }

        private void PlayFootstepSound()
        {
            if (footstepClips != null && footstepClips.Length > 0 && Audio.OneShotPool.Instance != null)
            {
                AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
                if (clip != null)
                {
                    float volume = isCrouching ? 0.2f : (isSprinting ? 0.55f : 0.4f);
                    Audio.OneShotPool.Instance.PlayOneShot(clip, transform.position, volume, Random.Range(0.92f, 1.08f));
                }
            }
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            characterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            characterController.enabled = true;
        }
    }
}

