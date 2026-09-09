using System;
using System.Collections.Generic;
using UnityEngine;
using EndlessHallway.Core;
using EndlessHallway.UI;

namespace EndlessHallway.Entity
{
    public enum ObserverState
    {
        Hidden,
        GlimpseFar,
        GlimpseMirror,
        Present,
        Aggressive
    }

    /// <summary>
    /// Controls the shadowy silhouette of Elias Voss.
    /// Can vanish on look, stand motionless, or in late loops enter an aggressive pursuit state.
    /// </summary>
    public class ObserverController : MonoBehaviour, IResettable
    {
        public static ObserverController Instance { get; private set; }

        [Header("State")]
        [SerializeField] private ObserverState currentState = ObserverState.Hidden;
        public ObserverState CurrentState => currentState;

        [Header("Visual Mesh / Silhouette")]
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Collider observerCollider;

        [Header("Detection Settings")]
        [SerializeField] private float vanishDistance = 6.0f;
        [SerializeField] private float directLookAngleThreshold = 25.0f;
        [SerializeField] private LayerMask occlusionLayers = ~0;

        [Header("Aggressive State Settings")]
        [SerializeField] private float aggressiveMoveSpeed = 3.2f;
        [SerializeField] private float catchDistance = 1.25f;
        [SerializeField] private float hesitationLingerLimit = 2.5f;
        [SerializeField] private AudioClip jumpScareStingerClip;

        [Header("Audio Stinger")]
        [SerializeField] private AudioClip vanishStingerClip;

        public event Action<bool> OnFinalChoiceMade;

        private readonly Dictionary<string, Transform> spawnPoints = new Dictionary<string, Transform>();
        private Camera playerCamera;
        private Transform playerTransform;
        private float lingerTimer = 0f;
        private bool isCatchingPlayer = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (GetComponent<Audio.ProximityWhispers>() == null)
            {
                gameObject.AddComponent<Audio.ProximityWhispers>();
            }

            RefreshSpawnPoints();
            SetVisible(false);
        }

        private void Start()
        {
            if (WorldStateResetter.Instance != null)
            {
                WorldStateResetter.Instance.Register(this);
            }

            FindPlayer();
        }

        private void OnDestroy()
        {
            if (WorldStateResetter.Instance != null)
            {
                WorldStateResetter.Instance.Unregister(this);
            }
        }

        private void FindPlayer()
        {
            if (Camera.main != null)
            {
                playerCamera = Camera.main;
                playerTransform = playerCamera.transform.parent != null ? playerCamera.transform.parent : playerCamera.transform;
            }
        }

        public void RefreshSpawnPoints()
        {
            spawnPoints.Clear();
            ObserverSpawnPoint[] points = FindObjectsByType<ObserverSpawnPoint>(FindObjectsSortMode.None);
            foreach (var pt in points)
            {
                if (!string.IsNullOrEmpty(pt.PointId) && !spawnPoints.ContainsKey(pt.PointId))
                {
                    spawnPoints.Add(pt.PointId, pt.transform);
                }
            }
        }

        public void SpawnAtPoint(string pointId, ObserverState state)
        {
            if (spawnPoints.TryGetValue(pointId, out Transform targetPt))
            {
                transform.SetPositionAndRotation(targetPt.position, targetPt.rotation);
                currentState = state;
                lingerTimer = 0f;
                isCatchingPlayer = false;
                SetVisible(true);
            }
            else
            {
                Debug.LogWarning($"[ObserverController] Spawn point '{pointId}' not found.");
            }
        }

        public void SetVisible(bool visible)
        {
            if (visualRoot != null) visualRoot.SetActive(visible);
            if (observerCollider != null) observerCollider.enabled = visible;
            if (!visible) currentState = ObserverState.Hidden;
        }

        private void Update()
        {
            if (currentState == ObserverState.Hidden || currentState == ObserverState.Present) return;

            if (playerCamera == null || playerTransform == null)
            {
                FindPlayer();
                if (playerCamera == null || playerTransform == null) return;
            }

            if (currentState == ObserverState.Aggressive)
            {
                HandleAggressiveBehavior();
            }
            else
            {
                CheckPlayerGazeAndDistance();
            }
        }

        private void CheckPlayerGazeAndDistance()
        {
            if (currentState == ObserverState.Hidden || currentState == ObserverState.Present) return;
            if (playerCamera == null) return;

            Vector3 toObserver = (transform.position + Vector3.up * 1f) - playerCamera.transform.position;
            float distance = toObserver.magnitude;

            // 1. Proximity check
            if (distance < vanishDistance)
            {
                Vanish();
                return;
            }

            // 2. Direct gaze check
            float angle = Vector3.Angle(playerCamera.transform.forward, toObserver);
            if (angle < directLookAngleThreshold)
            {
                // Raycast check to confirm line of sight
                if (Physics.Raycast(playerCamera.transform.position, toObserver.normalized, out RaycastHit hit, distance + 1f, occlusionLayers))
                {
                    if (hit.collider == observerCollider || hit.transform.IsChildOf(transform))
                    {
                        Vanish();
                    }
                }
            }
        }

        private void HandleAggressiveBehavior()
        {
            if (isCatchingPlayer || playerCamera == null || playerTransform == null) return;

            Vector3 toObserver = (transform.position + Vector3.up * 1f) - playerCamera.transform.position;
            float distance = toObserver.magnitude;

            // Look orientation towards player
            Vector3 lookTarget = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
            transform.LookAt(lookTarget);

            float gazeAngle = Vector3.Angle(playerCamera.transform.forward, toObserver);
            bool isLookingAt = gazeAngle < directLookAngleThreshold;

            if (isLookingAt)
            {
                lingerTimer += Time.deltaTime;
            }
            else
            {
                // Player looked away! Advance immediately
                lingerTimer += Time.deltaTime * 2.5f;
            }

            // Advance towards player if hesitation limit reached
            if (lingerTimer > hesitationLingerLimit)
            {
                float step = aggressiveMoveSpeed * Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, lookTarget, step);

                if (distance < catchDistance)
                {
                    StartCoroutine(AggressiveJumpScareRoutine());
                }
            }
        }

        private System.Collections.IEnumerator AggressiveJumpScareRoutine()
        {
            isCatchingPlayer = true;
            Debug.Log("[ObserverController] Observer caught player! Triggering jump scare fail state.");

            // 1. Freeze player controls
            var pController = FindAnyObjectByType<Player.PlayerController>();
            var pLook = FindAnyObjectByType<Player.PlayerCameraLook>();
            if (pController != null) pController.CanMove = false;
            if (pLook != null) pLook.CanLook = false;

            // 2. Violent camera snap directly onto Observer's face
            if (playerCamera != null)
            {
                Vector3 toHead = (transform.position + Vector3.up * 1.6f) - playerCamera.transform.position;
                Quaternion targetRot = Quaternion.LookRotation(toHead);
                if (pLook != null)
                {
                    pLook.ResetRotation(targetRot.eulerAngles.y, targetRot.eulerAngles.x);
                }
            }

            // 3. Jump scare stinger audio & stress spike
            AudioClip stinger = jumpScareStingerClip != null ? jumpScareStingerClip : vanishStingerClip;
            if (stinger != null && Audio.OneShotPool.Instance != null)
            {
                Audio.OneShotPool.Instance.Play2D(stinger, 1.0f, 0.95f);
            }

            if (UI.SubtitleUI.Instance != null)
            {
                UI.SubtitleUI.Instance.ShowSubtitle("[Violent audio stinger & impact]", 2.5f);
            }

            if (Player.PlayerStressSystem.Instance != null)
            {
                Player.PlayerStressSystem.Instance.AddTrauma(1.0f);
            }

            // 4. Screen flash & rapid fade to black
            if (UI.FadeController.Instance != null)
            {
                // Softening check for photosensitivity
                bool soften = SettingsManager.Instance != null && SettingsManager.Instance.PhotosensitivitySoftening;
                if (!soften)
                {
                    UI.FadeController.Instance.SetColor(new Color(0.9f, 0.1f, 0.1f, 0.7f));
                    yield return new WaitForSeconds(0.12f);
                }

                UI.FadeController.Instance.SetColor(Color.black);
                yield return StartCoroutine(UI.FadeController.Instance.FadeOutRoutine(0.4f));
            }
            else
            {
                yield return new WaitForSeconds(0.4f);
            }

            yield return new WaitForSeconds(1.0f);

            // 5. Present Game Over screen or reset loop
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameOver(
                    onRetry: () =>
                    {
                        if (LoopManager.Instance != null)
                        {
                            int curLoop = LoopManager.Instance.CurrentLoop;
                            LoopManager.Instance.ResetLoop(curLoop);

                            var elevator = FindAnyObjectByType<Interaction.ElevatorController>();
                            var spawnField = typeof(Interaction.ElevatorController).GetField("playerSpawnPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            Transform spawnPt = null;
                            if (spawnField != null && elevator != null)
                            {
                                spawnPt = spawnField.GetValue(elevator) as Transform;
                            }

                            if (spawnPt != null && pController != null)
                            {
                                pController.Teleport(spawnPt.position, spawnPt.rotation);
                            }
                            else if (pController != null)
                            {
                                pController.Teleport(new Vector3(0f, 1.0f, -1.4f), Quaternion.identity);
                            }
                        }

                        if (UI.FadeController.Instance != null)
                        {
                            UI.FadeController.Instance.FadeIn(1.2f);
                        }

                        if (pController != null) pController.CanMove = true;
                        if (pLook != null) pLook.CanLook = true;
                        isCatchingPlayer = false;
                    },
                    onExit: () =>
                    {
                        if (UIManager.Instance != null)
                        {
                            UIManager.Instance.ShowMainMenu();
                        }
                        isCatchingPlayer = false;
                    }
                );
            }
            else
            {
                // Fallback soft fail state
                if (LoopManager.Instance != null)
                {
                    int curLoop = LoopManager.Instance.CurrentLoop;
                    LoopManager.Instance.ResetLoop(curLoop);

                    var elevator = FindAnyObjectByType<Interaction.ElevatorController>();
                    var spawnField = typeof(Interaction.ElevatorController).GetField("playerSpawnPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Transform spawnPt = null;
                    if (spawnField != null && elevator != null)
                    {
                        spawnPt = spawnField.GetValue(elevator) as Transform;
                    }

                    if (spawnPt != null && pController != null)
                    {
                        pController.Teleport(spawnPt.position, spawnPt.rotation);
                    }
                    else if (pController != null)
                    {
                        pController.Teleport(new Vector3(0f, 1.0f, -1.4f), Quaternion.identity);
                    }
                }

                if (UI.FadeController.Instance != null)
                {
                    yield return StartCoroutine(UI.FadeController.Instance.FadeInRoutine(1.2f));
                }

                if (pController != null) pController.CanMove = true;
                if (pLook != null) pLook.CanLook = true;
                isCatchingPlayer = false;
            }
        }

        public void Vanish()
        {
            if (currentState == ObserverState.Hidden) return;

            Debug.Log("[ObserverController] Player looked directly or approached. Observer vanished.");
            SetVisible(false);

            if (vanishStingerClip != null && Audio.OneShotPool.Instance != null)
            {
                Audio.OneShotPool.Instance.PlayOneShot(vanishStingerClip, transform.position, 0.5f);
            }

            if (UI.SubtitleUI.Instance != null)
            {
                UI.SubtitleUI.Instance.ShowSubtitle("[Cold metallic displacement whoosh]", 2.0f);
            }
        }

        public void TriggerFinalChoice(bool enteredRoom)
        {
            OnFinalChoiceMade?.Invoke(enteredRoom);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerEnding(enteredRoom);
            }
        }

        public void ResetToDefaultState()
        {
            SetVisible(false);
            currentState = ObserverState.Hidden;
            lingerTimer = 0f;
            isCatchingPlayer = false;
        }
    }
}
