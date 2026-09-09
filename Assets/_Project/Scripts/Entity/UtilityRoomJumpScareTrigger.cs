using System.Collections;
using UnityEngine;
using EndlessHallway.Player;
using EndlessHallway.Audio;
using EndlessHallway.UI;
using EndlessHallway.Core;

namespace EndlessHallway.Entity
{
    /// <summary>
    /// Executes the single scripted jump-scare beat reserved for the hotel expansion
    /// inside the enclosed Utility/Maintenance Room (Room 216).
    /// Fires exactly once per playthrough using a persistent hasFired flag.
    /// </summary>
    public class UtilityRoomJumpScareTrigger : MonoBehaviour
    {
        private const string PREF_KEY = "JumpScare_Room216_Fired";

        [Header("Scene References")]
        [SerializeField] private Light utilityBulbLight;
        [SerializeField] private GameObject utilityBulbFixture;
        [SerializeField] private Transform observerDoorwaySpot;
        [SerializeField] private AudioClip jumpScareClip;
        [SerializeField] private AudioClip bulbPopClip;

        private bool hasTriggeredThisSession = false;

        private void Start()
        {
            if (PlayerPrefs.GetInt(PREF_KEY, 0) == 1)
            {
                hasTriggeredThisSession = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggeredThisSession) return;
            if (!other.CompareTag("Player") && other.GetComponent<PlayerController>() == null) return;

            hasTriggeredThisSession = true;
            PlayerPrefs.SetInt(PREF_KEY, 1);
            PlayerPrefs.Save();

            StartCoroutine(ExecuteJumpScareRoutine(other.transform));
        }

        private IEnumerator ExecuteJumpScareRoutine(Transform player)
        {
            // Give player a brief moment of exploration inside the utility room
            yield return new WaitForSeconds(1.2f);

            // 1. Cut the light abruptly
            if (utilityBulbLight != null)
            {
                utilityBulbLight.enabled = false;
            }

            // 2. Play sudden pop & violent jump scare stinger
            if (jumpScareClip != null && OneShotPool.Instance != null)
            {
                OneShotPool.Instance.Play2D(jumpScareClip, 1.0f, 0.95f);
            }

            if (SubtitleUI.Instance != null)
            {
                SubtitleUI.Instance.ShowSubtitle("[Sudden violent impact & electrical snap]", 2.5f);
            }

            // 3. Spike player physiological stress & camera trauma
            if (PlayerStressSystem.Instance != null)
            {
                PlayerStressSystem.Instance.AddTrauma(0.85f);
            }

            // 4. Temporarily show Observer standing right in the doorway
            if (ObserverController.Instance != null && observerDoorwaySpot != null)
            {
                ObserverController.Instance.transform.SetPositionAndRotation(observerDoorwaySpot.position, observerDoorwaySpot.rotation);
                ObserverController.Instance.SetVisible(true);
            }

            // 5. Brief red emergency flash if photosensitivity allows
            if (FadeController.Instance != null)
            {
                bool soften = SettingsManager.Instance != null && SettingsManager.Instance.PhotosensitivitySoftening;
                if (!soften)
                {
                    FadeController.Instance.SetColor(new Color(0.85f, 0.1f, 0.1f, 0.5f));
                    yield return new WaitForSeconds(0.1f);
                    FadeController.Instance.SetColor(Color.black);
                    FadeController.Instance.SetAlpha(0f);
                }
            }

            yield return new WaitForSeconds(1.1f);

            // 6. Observer vanishes in the dark
            if (ObserverController.Instance != null)
            {
                ObserverController.Instance.Vanish();
            }

            // 7. Light sputters back on dimly
            yield return new WaitForSeconds(0.4f);
            if (utilityBulbLight != null)
            {
                utilityBulbLight.enabled = true;
                utilityBulbLight.intensity = 0.6f;
            }
        }

        public static void ResetJumpScareFlag()
        {
            PlayerPrefs.DeleteKey(PREF_KEY);
            PlayerPrefs.Save();
        }
    }
}
