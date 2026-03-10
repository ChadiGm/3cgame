using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace WaterBlob
{
    /// <summary>
    /// Global manager that handles the game lifecycle, specifically the player's death and respawn loop.
    /// This component is a singleton and persists across scene loads.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager instance;
        public static GameManager Instance => instance;

        [Header("Respawn Settings")]
        [SerializeField] private float respawnDelay = 1.0f;
        
        private bool isRespawning = false;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            SetupPlayerDeathListeners();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            isRespawning = false;
            SetupPlayerDeathListeners();
        }

        private void SetupPlayerDeathListeners()
        {
            // Find player - using tag is most reliable for auto-spawners
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                // Fallback to name search if tag is not yet set up
                player = GameObject.Find("WaterBlobPlayer");
            }

            if (player != null)
            {
                // Subscribe to WaterResource if present
                OneDropWaterResource2D water = player.GetComponent<OneDropWaterResource2D>();
                if (water != null)
                {
                    water.OnDied += TriggerRespawn;
                }
            }
        }

        public void TriggerRespawn()
        {
            if (isRespawning) return;
            
            isRespawning = true;
            Debug.Log("[GameManager] Player Died. Triggering respawn loop...");
            StartCoroutine(RespawnSequence());
        }

        private IEnumerator RespawnSequence()
        {
            yield return new WaitForSeconds(respawnDelay);
            
            // For now, simply reload the current scene
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.buildIndex);
        }
    }
}
