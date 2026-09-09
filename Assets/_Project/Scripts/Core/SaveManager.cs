using System.Collections.Generic;
using UnityEngine;

namespace EndlessHallway.Core
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string KEY_SAVED_LOOP = "EndlessHallway_SavedLoop";
        private const string KEY_HAS_CHECKPOINT = "EndlessHallway_HasCheckpoint";
        private const string KEY_CLUES_READ = "EndlessHallway_CluesRead";

        private HashSet<string> readClues = new HashSet<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadCluesList();
        }

        public void SaveCheckpoint(int loopIndex)
        {
            PlayerPrefs.SetInt(KEY_SAVED_LOOP, loopIndex);
            PlayerPrefs.SetInt(KEY_HAS_CHECKPOINT, 1);
            PlayerPrefs.Save();
            Debug.Log($"[SaveManager] Checkpoint saved at Loop {loopIndex}");
        }

        public int GetSavedLoop()
        {
            return PlayerPrefs.GetInt(KEY_SAVED_LOOP, 0);
        }

        public bool HasCheckpoint()
        {
            return PlayerPrefs.GetInt(KEY_HAS_CHECKPOINT, 0) == 1;
        }

        public void ClearCheckpoint()
        {
            PlayerPrefs.DeleteKey(KEY_SAVED_LOOP);
            PlayerPrefs.DeleteKey(KEY_HAS_CHECKPOINT);
            PlayerPrefs.DeleteKey(KEY_CLUES_READ);
            PlayerPrefs.Save();
            readClues.Clear();
            Debug.Log("[SaveManager] Save checkpoint cleared.");
        }

        public void RecordClue(string clueId)
        {
            if (string.IsNullOrEmpty(clueId)) return;
            if (!readClues.Contains(clueId))
            {
                readClues.Add(clueId);
                SaveCluesList();
            }
        }

        public bool IsClueRead(string clueId)
        {
            return readClues.Contains(clueId);
        }

        private void SaveCluesList()
        {
            string serialized = string.Join(";", readClues);
            PlayerPrefs.SetString(KEY_CLUES_READ, serialized);
            PlayerPrefs.Save();
        }

        private void LoadCluesList()
        {
            readClues.Clear();
            string data = PlayerPrefs.GetString(KEY_CLUES_READ, "");
            if (!string.IsNullOrEmpty(data))
            {
                string[] split = data.Split(';');
                foreach (var s in split)
                {
                    if (!string.IsNullOrEmpty(s)) readClues.Add(s);
                }
            }
        }
    }
}
