using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EndlessHallway.Player
{
    public class PlayerCameraLook : MonoBehaviour
    {
        [Header("Sensitivity & Limits")]
        [SerializeField] private float mouseSensitivity = 1.5f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;
        [SerializeField] private bool smoothRotation = true;
        [SerializeField] private float smoothingFactor = 25f;

        [Header("References")]
        [SerializeField] private Transform playerBody;
        [SerializeField] private Transform cameraTransform;

        private float currentPitch = 0f;
        private float targetPitch = 0f;
        private float targetYaw = 0f;
        private bool canLook = true;

        public bool CanLook
        {
            get => canLook;
            set => canLook = value;
        }

        private Camera targetCamera;

        public Camera TargetCamera => targetCamera;

        private void Start()
        {
            if (playerBody == null && transform.parent != null)
            {
                playerBody = transform.parent;
            }

            if (cameraTransform == null)
            {
                targetCamera = GetComponentInChildren<Camera>();
                if (targetCamera != null) cameraTransform = targetCamera.transform;
                else cameraTransform = transform;
            }
            else
            {
                targetCamera = cameraTransform.GetComponent<Camera>();
            }

            if (playerBody != null)
            {
                targetYaw = playerBody.eulerAngles.y;
            }

            if (Core.SettingsManager.Instance != null)
            {
                Core.SettingsManager.Instance.OnSettingsChanged += HandleSettingsChanged;
                ApplySettings();
            }

            LockCursor();
        }

        private void OnDestroy()
        {
            if (Core.SettingsManager.Instance != null)
            {
                Core.SettingsManager.Instance.OnSettingsChanged -= HandleSettingsChanged;
            }
        }

        private void HandleSettingsChanged()
        {
            ApplySettings();
        }

        public void ApplySettings()
        {
            if (Core.SettingsManager.Instance != null && targetCamera != null)
            {
                targetCamera.fieldOfView = Core.SettingsManager.Instance.FieldOfView;
            }
        }

        private void Update()
        {
            if (!canLook) return;

            Vector2 lookInput = ReadLookInput();

            bool invert = Core.SettingsManager.Instance != null && Core.SettingsManager.Instance.InvertY;
            float pitchSign = invert ? 1f : -1f;

            targetYaw += lookInput.x * mouseSensitivity;
            targetPitch += pitchSign * lookInput.y * mouseSensitivity;
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

            if (smoothRotation)
            {
                currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * smoothingFactor);
                if (playerBody != null)
                {
                    playerBody.rotation = Quaternion.Euler(0f, targetYaw, 0f);
                }
                cameraTransform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
            }
            else
            {
                if (playerBody != null)
                {
                    playerBody.rotation = Quaternion.Euler(0f, targetYaw, 0f);
                }
                cameraTransform.localRotation = Quaternion.Euler(targetPitch, 0f, 0f);
            }
        }

        private Vector2 ReadLookInput()
        {
            Vector2 delta = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.delta.ReadValue() * 0.1f;
            }
#endif

            return delta;
        }

        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ResetRotation(float yaw, float pitch = 0f)
        {
            targetYaw = yaw;
            targetPitch = pitch;
            currentPitch = pitch;

            if (playerBody != null)
            {
                playerBody.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }
    }
}
