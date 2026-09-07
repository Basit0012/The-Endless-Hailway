using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EndlessHallway.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3.0f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Head Bobbing")]
        [SerializeField] private bool enableHeadBob = true;
        [SerializeField] private Transform cameraHolder;
        [SerializeField] private float bobFrequency = 1.6f;
        [SerializeField] private float bobHorizontalAmplitude = 0.03f;
        [SerializeField] private float bobVerticalAmplitude = 0.04f;

        [Header("Footstep Audio")]
        [SerializeField] private float stepInterval = 0.55f;
        [SerializeField] private AudioClip[] footstepClips;

        private CharacterController characterController;
        private Vector3 velocity;
        private float bobTimer = 0f;
        private Vector3 defaultCameraLocalPos;
        private float stepTimer = 0f;
        private bool canMove = true;

        public bool CanMove
        {
            get => canMove;
            set
            {
                canMove = value;
                if (!canMove)
                {
                    velocity = Vector3.zero;
                }
            }
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (cameraHolder != null)
            {
                defaultCameraLocalPos = cameraHolder.localPosition;
            }
        }

        private void Update()
        {
            ApplyGravity();

            if (!canMove) return;

            Vector2 moveInput = ReadMovementInput();
            Vector3 moveDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;

            characterController.Move(moveDirection * (walkSpeed * Time.deltaTime) + velocity * Time.deltaTime);

            HandleHeadBob(moveInput);
            HandleFootsteps(moveInput);
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

            return input.sqrMagnitude > 1f ? input.normalized : input;
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

            if (moveInput.sqrMagnitude > 0.01f && characterController.isGrounded)
            {
                bobTimer += Time.deltaTime * (bobFrequency * (walkSpeed / 2.5f));
                float hOffset = Mathf.Cos(bobTimer) * bobHorizontalAmplitude;
                float vOffset = Mathf.Abs(Mathf.Sin(bobTimer)) * bobVerticalAmplitude;

                cameraHolder.localPosition = defaultCameraLocalPos + new Vector3(hOffset, vOffset, 0f);
            }
            else
            {
                bobTimer = 0f;
                cameraHolder.localPosition = Vector3.Lerp(cameraHolder.localPosition, defaultCameraLocalPos, Time.deltaTime * 6f);
            }
        }

        private void HandleFootsteps(Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude > 0.01f && characterController.isGrounded)
            {
                stepTimer += Time.deltaTime;
                if (stepTimer >= stepInterval)
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
                    Audio.OneShotPool.Instance.PlayOneShot(clip, transform.position, 0.4f, Random.Range(0.92f, 1.08f));
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
