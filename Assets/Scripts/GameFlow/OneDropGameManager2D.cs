using System;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class OneDropGameManager2D : MonoBehaviour
    {
        private static OneDropGameManager2D instance;

        [Header("References")]
        [SerializeField] private OneDropWaterResource2D playerHealth;

        [Header("Pause")]
        [SerializeField] private bool enablePauseToggle = true;
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

        private float elapsedTime;
        private int lastReportedTimerCentiseconds = -1;
        private bool isPaused;
        private bool isGameOver;
        private bool subscribedToHealth;

        public static OneDropGameManager2D Instance => instance;
        public static bool IsGameplayInputBlocked => instance != null && (instance.isPaused || instance.isGameOver);

        public float ElapsedTime => elapsedTime;
        public bool IsPaused => isPaused;
        public bool IsGameOver => isGameOver;

        public event Action<float> TimerUpdated;
        public event Action<bool> PauseStateChanged;
        public event Action<float> GameOverTriggered;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
            ResolveHealthReference();
            SubscribeToHealth();
        }

        private void OnEnable()
        {
            if (instance != this)
            {
                return;
            }

            ResolveHealthReference();
            SubscribeToHealth();
        }

        private void OnDisable()
        {
            if (instance != this)
            {
                return;
            }

            UnsubscribeFromHealth();
            if (isPaused && !isGameOver)
            {
                Time.timeScale = 1f;
                isPaused = false;
                PauseStateChanged?.Invoke(false);
            }
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            UnsubscribeFromHealth();
            instance = null;
        }

        private void Update()
        {
            if (enablePauseToggle && !isGameOver && ReadPausePressedThisFrame())
            {
                TogglePause();
            }

            if (isPaused || isGameOver)
            {
                return;
            }

            elapsedTime += Time.deltaTime;
            int centiseconds = Mathf.Max(0, Mathf.FloorToInt(elapsedTime * 100f));
            if (centiseconds == lastReportedTimerCentiseconds)
            {
                return;
            }

            lastReportedTimerCentiseconds = centiseconds;
            TimerUpdated?.Invoke(elapsedTime);
        }

        private void ResolveHealthReference()
        {
            if (playerHealth != null)
            {
                return;
            }

            playerHealth = GetComponent<OneDropWaterResource2D>();
            if (playerHealth != null)
            {
                return;
            }

            playerHealth = FindFirstObjectByType<OneDropWaterResource2D>();
        }

        private void SubscribeToHealth()
        {
            if (subscribedToHealth || playerHealth == null)
            {
                return;
            }

            playerHealth.FinalHealthDepleted += HandleFinalHealthDepleted;
            subscribedToHealth = true;
        }

        private void UnsubscribeFromHealth()
        {
            if (!subscribedToHealth || playerHealth == null)
            {
                subscribedToHealth = false;
                return;
            }

            playerHealth.FinalHealthDepleted -= HandleFinalHealthDepleted;
            subscribedToHealth = false;
        }

        private void HandleFinalHealthDepleted()
        {
            if (isGameOver)
            {
                return;
            }

            isGameOver = true;
            if (isPaused)
            {
                isPaused = false;
                PauseStateChanged?.Invoke(false);
            }

            Time.timeScale = 0f;
            GameOverTriggered?.Invoke(elapsedTime);
        }

        public void TogglePause()
        {
            if (isGameOver)
            {
                return;
            }

            if (isPaused)
            {
                ResumeGame();
                return;
            }

            PauseGame();
        }

        public void PauseGame()
        {
            if (isGameOver || isPaused)
            {
                return;
            }

            isPaused = true;
            Time.timeScale = 0f;
            PauseStateChanged?.Invoke(true);
        }

        public void ResumeGame()
        {
            if (isGameOver || !isPaused)
            {
                return;
            }

            isPaused = false;
            Time.timeScale = 1f;
            PauseStateChanged?.Invoke(false);
        }

        public void RestartFromBeginning()
        {
            Time.timeScale = 1f;
            isPaused = false;
            isGameOver = false;

            if (SceneManager.sceneCountInBuildSettings > 0)
            {
                SceneManager.LoadScene(0);
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
                return;
            }

            SceneManager.LoadScene(activeScene.name);
        }

        public void ExitGame()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private bool ReadPausePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                return true;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null && gamepad.startButton.wasPressedThisFrame)
            {
                return true;
            }

            return false;
#else
            return Input.GetKeyDown(pauseKey);
#endif
        }
    }
}
