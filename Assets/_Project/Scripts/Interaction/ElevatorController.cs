using System.Collections;
using UnityEngine;
using EndlessHallway.Core;
using EndlessHallway.Player;
using EndlessHallway.UI;
using EndlessHallway.Audio;

namespace EndlessHallway.Interaction
{
    public class ElevatorController : MonoBehaviour, IInteractable
    {
        [Header("Door Transforms")]
        [SerializeField] private Transform leftDoor;
        [SerializeField] private Transform rightDoor;
        [SerializeField] private float doorOpenDistance = 0.9f;
        [SerializeField] private float doorSpeed = 2.0f;

        [Header("Elevator Settings")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private bool startDoorsOpen = true;

        [Header("Audio")]
        [SerializeField] private AudioClip bellDingClip;
        [SerializeField] private AudioClip doorsMovingClip;
        [SerializeField] private AudioClip elevatorRideHumClip;

        private Vector3 leftDoorClosedPos;
        private Vector3 rightDoorClosedPos;
        private Vector3 leftDoorOpenPos;
        private Vector3 rightDoorOpenPos;

        private bool areDoorsOpen = false;
        private bool isTransitioning = false;

        public bool AreDoorsOpen => areDoorsOpen;
        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            if (leftDoor != null)
            {
                leftDoorClosedPos = new Vector3(-0.55f, leftDoor.localPosition.y, leftDoor.localPosition.z);
                leftDoorOpenPos = leftDoorClosedPos + Vector3.left * doorOpenDistance;
            }

            if (rightDoor != null)
            {
                rightDoorClosedPos = new Vector3(0.55f, rightDoor.localPosition.y, rightDoor.localPosition.z);
                rightDoorOpenPos = rightDoorClosedPos + Vector3.right * doorOpenDistance;
            }

            areDoorsOpen = startDoorsOpen;
            SetDoorsImmediate(areDoorsOpen);
        }

        public bool CanInteract()
        {
            return !isTransitioning;
        }

        public string GetInteractionPrompt()
        {
            if (isTransitioning) return "";
            return areDoorsOpen ? "[E] Close Elevator & Ride" : "[E] Call Elevator";
        }

        public void Interact(PlayerInteraction source)
        {
            if (isTransitioning) return;

            if (areDoorsOpen)
            {
                StartCoroutine(ElevatorRideRoutine(source));
            }
            else
            {
                StartCoroutine(OpenDoorsRoutine());
            }
        }

        private IEnumerator OpenDoorsRoutine()
        {
            isTransitioning = true;
            PlayBellDing();
            yield return StartCoroutine(AnimateDoors(true));
            isTransitioning = false;
        }

        private IEnumerator ElevatorRideRoutine(PlayerInteraction player)
        {
            isTransitioning = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.InElevator);
            }

            // 1. Close doors
            yield return StartCoroutine(AnimateDoors(false));

            // Check if final loop choice made (Erosion Ending: player returned to elevator)
            if (LoopManager.Instance != null && LoopManager.Instance.CurrentLoop >= 7)
            {
                if (Entity.ObserverController.Instance != null)
                {
                    Entity.ObserverController.Instance.TriggerFinalChoice(false);
                }
                else if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerEnding(false);
                }
                isTransitioning = false;
                yield break;
            }

            // 2. Play hum / ride sound
            if (elevatorRideHumClip != null && OneShotPool.Instance != null)
            {
                OneShotPool.Instance.Play2D(elevatorRideHumClip, 0.7f);
            }

            yield return new WaitForSeconds(0.6f);

            // 3. Fade out
            if (FadeController.Instance != null)
            {
                yield return StartCoroutine(FadeController.Instance.FadeOutRoutine(0.8f));
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            // 4. Advance Loop & Reset Scene
            if (LoopManager.Instance != null)
            {
                LoopManager.Instance.AdvanceLoop();
            }

            // Reposition player to spawn point inside or outside elevator if desired
            if (playerSpawnPoint != null && player != null)
            {
                PlayerController controller = player.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.Teleport(playerSpawnPoint.position, playerSpawnPoint.rotation);
                }
            }

            yield return new WaitForSeconds(0.5f);

            // 5. Fade In
            if (FadeController.Instance != null)
            {
                yield return StartCoroutine(FadeController.Instance.FadeInRoutine(0.8f));
            }

            PlayBellDing();

            // 6. Open doors on the new loop
            yield return StartCoroutine(AnimateDoors(true));

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Exploring);
            }

            isTransitioning = false;
        }

        private IEnumerator AnimateDoors(bool open)
        {
            areDoorsOpen = open;
            Vector3 targetLeft = open ? leftDoorOpenPos : leftDoorClosedPos;
            Vector3 targetRight = open ? rightDoorOpenPos : rightDoorClosedPos;

            if (doorsMovingClip != null && OneShotPool.Instance != null)
            {
                OneShotPool.Instance.PlayOneShot(doorsMovingClip, transform.position, 0.6f);
            }

            float elapsed = 0f;
            Vector3 startLeft = leftDoor != null ? leftDoor.localPosition : Vector3.zero;
            Vector3 startRight = rightDoor != null ? rightDoor.localPosition : Vector3.zero;

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * doorSpeed;
                float t = Mathf.SmoothStep(0f, 1f, elapsed);

                if (leftDoor != null) leftDoor.localPosition = Vector3.Lerp(startLeft, targetLeft, t);
                if (rightDoor != null) rightDoor.localPosition = Vector3.Lerp(startRight, targetRight, t);

                yield return null;
            }

            if (leftDoor != null) leftDoor.localPosition = targetLeft;
            if (rightDoor != null) rightDoor.localPosition = targetRight;
        }

        private void SetDoorsImmediate(bool open)
        {
            if (leftDoor != null) leftDoor.localPosition = open ? leftDoorOpenPos : leftDoorClosedPos;
            if (rightDoor != null) rightDoor.localPosition = open ? rightDoorOpenPos : rightDoorClosedPos;
        }

        private void PlayBellDing()
        {
            if (bellDingClip != null && OneShotPool.Instance != null)
            {
                OneShotPool.Instance.PlayOneShot(bellDingClip, transform.position, 0.8f);
            }
        }
    }
}
