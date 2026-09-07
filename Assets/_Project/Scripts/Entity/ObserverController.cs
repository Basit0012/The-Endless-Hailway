using System;
using System.Collections.Generic;
using UnityEngine;
using EndlessHallway.Core;

namespace EndlessHallway.Entity
{
    public enum ObserverState
    {
        Hidden,
        GlimpseFar,
        GlimpseMirror,
        Present
    }

    /// <summary>
    /// Controls the shadowy silhouette of Elias Voss.
    /// Never chases; appears at fixed points and vanishes when looked at or approached.
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

        [Header("Audio Stinger")]
        [SerializeField] private AudioClip vanishStingerClip;

        public event Action<bool> OnFinalChoiceMade;

        private readonly Dictionary<string, Transform> spawnPoints = new Dictionary<string, Transform>();
        private Camera playerCamera;
        private Transform playerTransform;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

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

            if (playerCamera == null)
            {
                FindPlayer();
                if (playerCamera == null) return;
            }

            CheckPlayerGazeAndDistance();
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

        public void Vanish()
        {
            if (currentState == ObserverState.Hidden) return;

            Debug.Log("[ObserverController] Player looked directly or approached. Observer vanished.");
            SetVisible(false);

            if (vanishStingerClip != null && Audio.OneShotPool.Instance != null)
            {
                Audio.OneShotPool.Instance.PlayOneShot(vanishStingerClip, transform.position, 0.5f);
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
        }
    }
}
