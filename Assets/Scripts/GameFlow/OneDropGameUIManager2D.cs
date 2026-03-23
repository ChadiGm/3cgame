using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.UI;
#endif

namespace WaterBlob
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(OneDropGameManager2D))]
    public class OneDropGameUIManager2D : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private OneDropGameManager2D gameManager;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private EventSystem targetEventSystem;

        [Header("HUD")]
        [SerializeField] private TMP_Text timerLabel;
        [SerializeField] private Vector2 timerOffset = new(0f, -26f);
        [SerializeField] private Color timerColor = new(0.78f, 0.94f, 1f, 1f);
        [SerializeField, Min(100f)] private float timerSizePercent = 122f;
        [SerializeField] private bool timerUseMonospaceEffect = true;
        [SerializeField, Min(0.1f)] private float timerMonospaceEm = 0.62f;
        [SerializeField, Min(0f)] private float timerPulseAmplitude = 0.02f;
        [SerializeField, Min(0f)] private float timerPulseFrequency = 1.65f;
        [SerializeField, Min(0f)] private float timerWobbleDegrees = 0.7f;
        [SerializeField, Min(0f)] private float timerWobbleFrequency = 0.95f;

        [Header("Pause Menu")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button pauseResumeButton;
        [SerializeField] private Button pauseRestartButton;
        [SerializeField] private Button pauseExitButton;

        [Header("Game Over")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text gameOverTimeLabel;
        [SerializeField] private Button gameOverRestartButton;
        [SerializeField] private Button gameOverExitButton;

        [Header("Audio Hooks")]
        [SerializeField] private AudioSource pauseMenuMusicSource;
        [SerializeField] private AudioSource gameOverMusicSource;
        [SerializeField] private AudioSource buttonClickSoundSource;
        [SerializeField] private AudioSource navigationSoundSource;

        private Font defaultFont;
        private bool buttonsWired;
        private RectTransform timerRect;
        private Vector3 timerBaseScale = Vector3.one;
        private int lastTimerCentiseconds = -1;
        private float timerAnimTime;
#if UNITY_EDITOR
        private bool editorBuildQueued;
#endif

        private void Reset()
        {
            ResolveGameManager();
            EnsureCanvas();
            EnsureEventSystem();
            EnsureRuntimeUi();
            CacheTimerRect();
        }

        private void OnValidate()
        {
            timerSizePercent = Mathf.Max(100f, timerSizePercent);
            timerMonospaceEm = Mathf.Max(0.1f, timerMonospaceEm);
            timerPulseAmplitude = Mathf.Max(0f, timerPulseAmplitude);
            timerPulseFrequency = Mathf.Max(0f, timerPulseFrequency);
            timerWobbleDegrees = Mathf.Max(0f, timerWobbleDegrees);
            timerWobbleFrequency = Mathf.Max(0f, timerWobbleFrequency);

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                return;
            }

            QueueEditorUiBuild();
#endif
        }

        private void Awake()
        {
            ResolveGameManager();
            EnsureCanvas();
            EnsureEventSystem();
            EnsureRuntimeUi();
            CacheTimerRect();
            WireButtons();

            SetPausePanelVisible(false, selectDefault: false);
            SetGameOverPanelVisible(false, -1f);
            RefreshTimerLabel(gameManager != null ? gameManager.ElapsedTime : 0f);
            ApplyTimerVisibility();
        }

        private void OnEnable()
        {
            ResolveGameManager();
            if (gameManager == null)
            {
                return;
            }

            gameManager.TimerUpdated += HandleTimerUpdated;
            gameManager.PauseStateChanged += HandlePauseStateChanged;
            gameManager.GameOverTriggered += HandleGameOverTriggered;

            HandleTimerUpdated(gameManager.ElapsedTime);
            HandlePauseStateChanged(gameManager.IsPaused);
            if (gameManager.IsGameOver)
            {
                HandleGameOverTriggered(gameManager.ElapsedTime);
            }
        }

        private void OnDisable()
        {
            if (gameManager == null)
            {
                ResetTimerVisualTransform();
                return;
            }

            gameManager.TimerUpdated -= HandleTimerUpdated;
            gameManager.PauseStateChanged -= HandlePauseStateChanged;
            gameManager.GameOverTriggered -= HandleGameOverTriggered;
            ResetTimerVisualTransform();
        }

        private void Update()
        {
            UpdateTimerVisualMotion();
        }

#if UNITY_EDITOR
        [ContextMenu("Build Editable UI Now")]
        private void BuildEditableUiNow()
        {
            if (Application.isPlaying)
            {
                return;
            }

            BuildEditorUiImmediate();
        }

        private void QueueEditorUiBuild()
        {
            if (editorBuildQueued)
            {
                return;
            }

            editorBuildQueued = true;
            UnityEditor.EditorApplication.delayCall += BuildEditorUiImmediate;
        }

        private void BuildEditorUiImmediate()
        {
            editorBuildQueued = false;
            if (this == null || Application.isPlaying)
            {
                return;
            }

            ResolveGameManager();
            EnsureCanvas();
            EnsureEventSystem();
            EnsureRuntimeUi();
            CacheTimerRect();
            RefreshTimerLabel(gameManager != null ? gameManager.ElapsedTime : 0f);
            ApplyTimerVisibility();

            UnityEditor.EditorUtility.SetDirty(this);
            if (gameObject.scene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
        }
#endif

        private void ResolveGameManager()
        {
            if (gameManager != null)
            {
                return;
            }

            gameManager = GetComponent<OneDropGameManager2D>();
            if (gameManager != null)
            {
                return;
            }

            gameManager = FindFirstObjectByType<OneDropGameManager2D>();
        }

        private void EnsureCanvas()
        {
            if (targetCanvas != null)
            {
                return;
            }

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
                {
                    continue;
                }

                targetCanvas = canvas;
                return;
            }

            GameObject canvasGo = new("GameFlowCanvas_Auto", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            targetCanvas = canvasGo.GetComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        private void EnsureEventSystem()
        {
            if (targetEventSystem != null)
            {
                return;
            }

            if (EventSystem.current != null)
            {
                targetEventSystem = EventSystem.current;
                return;
            }

            GameObject eventSystemGo = new("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemGo.AddComponent<StandaloneInputModule>();
#endif
            targetEventSystem = eventSystemGo.GetComponent<EventSystem>();
        }

        private void EnsureRuntimeUi()
        {
            ResolveTimerReference();
            if (timerLabel == null)
            {
                timerLabel = CreateTimerLabel();
            }

            ResolvePauseUiReferences();
            if (pausePanel == null)
            {
                CreatePauseMenu();
            }

            ResolveGameOverUiReferences();
            if (gameOverPanel == null)
            {
                CreateGameOverMenu();
            }
        }

        private void ResolveTimerReference()
        {
            if (timerLabel != null)
            {
                return;
            }

            if (targetCanvas == null)
            {
                return;
            }

            TMP_Text[] labels = targetCanvas.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                TMP_Text label = labels[i];
                if (label == null || string.IsNullOrEmpty(label.name))
                {
                    continue;
                }

                if (label.name.IndexOf("timer", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    timerLabel = label;
                    return;
                }
            }
        }

        private void ResolvePauseUiReferences()
        {
            if (pausePanel == null)
            {
                return;
            }

            Button[] buttons = pausePanel.GetComponentsInChildren<Button>(true);
            if (pauseResumeButton == null && buttons.Length > 0)
            {
                pauseResumeButton = buttons[0];
            }

            if (pauseRestartButton == null && buttons.Length > 1)
            {
                pauseRestartButton = buttons[1];
            }

            if (pauseExitButton == null && buttons.Length > 2)
            {
                pauseExitButton = buttons[2];
            }
        }

        private void ResolveGameOverUiReferences()
        {
            if (gameOverPanel == null)
            {
                return;
            }

            if (gameOverTimeLabel == null)
            {
                Text[] texts = gameOverPanel.GetComponentsInChildren<Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    Text text = texts[i];
                    if (text == null || string.IsNullOrEmpty(text.name))
                    {
                        continue;
                    }

                    if (text.name.IndexOf("time", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        gameOverTimeLabel = text;
                        break;
                    }
                }

                if (gameOverTimeLabel == null)
                {
                    for (int i = 0; i < texts.Length; i++)
                    {
                        Text text = texts[i];
                        if (text != null && text.GetComponentInParent<Button>() == null)
                        {
                            gameOverTimeLabel = text;
                            break;
                        }
                    }
                }
            }

            Button[] buttons = gameOverPanel.GetComponentsInChildren<Button>(true);
            if (gameOverRestartButton == null && buttons.Length > 0)
            {
                gameOverRestartButton = buttons[0];
            }

            if (gameOverExitButton == null && buttons.Length > 1)
            {
                gameOverExitButton = buttons[1];
            }
        }

        private TMP_Text CreateTimerLabel()
        {
            GameObject timerGo = new("GameTimer", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rect = timerGo.GetComponent<RectTransform>();
            rect.SetParent(targetCanvas.transform, false);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = timerOffset;
            rect.sizeDelta = new Vector2(360f, 70f);

            TextMeshProUGUI label = timerGo.GetComponent<TextMeshProUGUI>();
            label.fontSize = 36f;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.richText = true;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = BuildStyledTimerText("00:00:00");

            return label;
        }

        private void CreatePauseMenu()
        {
            pausePanel = CreateFullscreenPanel("PauseMenuPanel", new Color(0f, 0f, 0f, 0.66f));
            RectTransform card = CreateMenuCard(pausePanel.transform, new Vector2(480f, 400f));

            CreateLabel(card, "PauseTitle", "Paused", 50, FontStyle.Bold, TextAnchor.MiddleCenter);
            pauseResumeButton = CreateButton(card, "ResumeButton", "Resume");
            pauseRestartButton = CreateButton(card, "RestartButton", "Restart");
            pauseExitButton = CreateButton(card, "ExitButton", "Exit");
        }

        private void CreateGameOverMenu()
        {
            gameOverPanel = CreateFullscreenPanel("GameOverPanel", new Color(0f, 0f, 0f, 0.74f));
            RectTransform card = CreateMenuCard(gameOverPanel.transform, new Vector2(540f, 430f));

            CreateLabel(card, "GameOverTitle", "Game Over", 52, FontStyle.Bold, TextAnchor.MiddleCenter);
            gameOverTimeLabel = CreateLabel(card, "FinalTimeText", "Time Survived: 00:00:00", 31, FontStyle.Normal, TextAnchor.MiddleCenter);
            gameOverRestartButton = CreateButton(card, "GameOverRestartButton", "Restart");
            gameOverExitButton = CreateButton(card, "GameOverExitButton", "Exit");
        }

        private GameObject CreateFullscreenPanel(string panelName, Color panelColor)
        {
            GameObject panelGo = new(panelName, typeof(RectTransform), typeof(Image));
            RectTransform rect = panelGo.GetComponent<RectTransform>();
            rect.SetParent(targetCanvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            Image image = panelGo.GetComponent<Image>();
            image.color = panelColor;
            panelGo.SetActive(false);
            return panelGo;
        }

        private RectTransform CreateMenuCard(Transform parent, Vector2 size)
        {
            GameObject cardGo = new("MenuCard", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            RectTransform rect = cardGo.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            Image image = cardGo.GetComponent<Image>();
            image.color = new Color(0.1f, 0.16f, 0.21f, 0.95f);

            VerticalLayoutGroup layout = cardGo.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 36, 36);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 16f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = cardGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return rect;
        }

        private Text CreateLabel(Transform parent, string objectName, string value, int fontSize, FontStyle style, TextAnchor alignment)
        {
            GameObject labelGo = new(objectName, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            RectTransform rect = labelGo.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(0f, fontSize + 20f);

            Text label = labelGo.GetComponent<Text>();
            label.text = value;
            label.font = GetDefaultFont();
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = Color.white;

            LayoutElement layout = labelGo.GetComponent<LayoutElement>();
            layout.preferredHeight = fontSize + 20f;

            return label;
        }

        private Button CreateButton(Transform parent, string objectName, string label)
        {
            GameObject buttonGo = new(objectName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            RectTransform rect = buttonGo.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(0f, 58f);

            Image image = buttonGo.GetComponent<Image>();
            image.color = new Color(0.17f, 0.31f, 0.39f, 1f);

            Button button = buttonGo.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.17f, 0.31f, 0.39f, 1f);
            colors.highlightedColor = new Color(0.22f, 0.41f, 0.52f, 1f);
            colors.selectedColor = new Color(0.22f, 0.41f, 0.52f, 1f);
            colors.pressedColor = new Color(0.12f, 0.23f, 0.28f, 1f);
            colors.disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.7f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            LayoutElement layout = buttonGo.GetComponent<LayoutElement>();
            layout.preferredHeight = 58f;

            GameObject textGo = new("Label", typeof(RectTransform), typeof(Text));
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.SetParent(buttonGo.transform, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text buttonText = textGo.GetComponent<Text>();
            buttonText.text = label;
            buttonText.font = GetDefaultFont();
            buttonText.fontSize = 28;
            buttonText.fontStyle = FontStyle.Bold;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;

            return button;
        }

        private Font GetDefaultFont()
        {
            if (defaultFont != null)
            {
                return defaultFont;
            }

            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return defaultFont;
        }

        private void WireButtons()
        {
            if (buttonsWired)
            {
                return;
            }

            BindButton(pauseResumeButton, OnPauseResumeClicked);
            BindButton(pauseRestartButton, OnPauseRestartClicked);
            BindButton(pauseExitButton, OnPauseExitClicked);
            BindButton(gameOverRestartButton, OnGameOverRestartClicked);
            BindButton(gameOverExitButton, OnGameOverExitClicked);

            AddNavigationSoundHooks(pauseResumeButton);
            AddNavigationSoundHooks(pauseRestartButton);
            AddNavigationSoundHooks(pauseExitButton);
            AddNavigationSoundHooks(gameOverRestartButton);
            AddNavigationSoundHooks(gameOverExitButton);

            buttonsWired = true;
        }

        private void BindButton(Button button, UnityAction callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(callback);
            button.onClick.AddListener(callback);
        }

        private void AddNavigationSoundHooks(Button button)
        {
            if (button == null)
            {
                return;
            }

            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }

            if (trigger.triggers == null)
            {
                trigger.triggers = new List<EventTrigger.Entry>();
            }

            AddEventTrigger(trigger, EventTriggerType.PointerEnter, _ => PlayNavigationSound());
            AddEventTrigger(trigger, EventTriggerType.Select, _ => PlayNavigationSound());
        }

        private static void AddEventTrigger(EventTrigger trigger, EventTriggerType type, UnityAction<BaseEventData> callback)
        {
            EventTrigger.Entry entry = new()
            {
                eventID = type
            };
            entry.callback.AddListener(callback);
            trigger.triggers.Add(entry);
        }

        private void HandleTimerUpdated(float elapsedSeconds)
        {
            RefreshTimerLabel(elapsedSeconds);
        }

        private void HandlePauseStateChanged(bool isPaused)
        {
            SetPausePanelVisible(isPaused, selectDefault: true);
            ApplyTimerVisibility();

            if (isPaused)
            {
                PlayMenuMusic(pauseMenuMusicSource);
                StopMenuMusic(gameOverMusicSource);
            }
            else
            {
                StopMenuMusic(pauseMenuMusicSource);
            }
        }

        private void HandleGameOverTriggered(float finalTime)
        {
            SetPausePanelVisible(false, selectDefault: false);
            SetGameOverPanelVisible(true, finalTime);
            ApplyTimerVisibility();

            StopMenuMusic(pauseMenuMusicSource);
            PlayMenuMusic(gameOverMusicSource);
        }

        private void SetPausePanelVisible(bool visible, bool selectDefault)
        {
            if (pausePanel == null)
            {
                return;
            }

            pausePanel.SetActive(visible);
            if (visible && selectDefault)
            {
                SelectButton(pauseResumeButton);
            }
        }

        private void SetGameOverPanelVisible(bool visible, float finalTime)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            if (gameOverTimeLabel != null)
            {
                gameOverTimeLabel.text = $"Time Survived: {FormatElapsedTime(finalTime)}";
            }

            SelectButton(gameOverRestartButton);
        }

        private void ApplyTimerVisibility()
        {
            if (timerLabel == null)
            {
                return;
            }

            bool showTimer = gameManager != null && !gameManager.IsPaused && !gameManager.IsGameOver;
            timerLabel.gameObject.SetActive(showTimer);
            if (!showTimer)
            {
                ResetTimerVisualTransform();
            }
        }

        private void RefreshTimerLabel(float elapsedSeconds)
        {
            if (timerLabel == null)
            {
                return;
            }

            int centiseconds = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds * 100f));
            if (centiseconds == lastTimerCentiseconds)
            {
                return;
            }

            lastTimerCentiseconds = centiseconds;
            timerLabel.text = BuildStyledTimerText(FormatElapsedTime(centiseconds));
        }

        private string BuildStyledTimerText(string timeText)
        {
            int sizePercent = Mathf.Max(100, Mathf.RoundToInt(timerSizePercent));
            string colorHex = ColorUtility.ToHtmlStringRGBA(timerColor);
            if (!timerUseMonospaceEffect)
            {
                return $"<size={sizePercent}%><b><color=#{colorHex}>{timeText}</color></b></size>";
            }

            string monoEm = timerMonospaceEm.ToString("0.###", CultureInfo.InvariantCulture);
            return $"<size={sizePercent}%><b><color=#{colorHex}><mspace={monoEm}em>{timeText}</mspace></color></b></size>";
        }

        private void CacheTimerRect()
        {
            if (timerLabel == null)
            {
                timerRect = null;
                timerBaseScale = Vector3.one;
                return;
            }

            timerLabel.richText = true;
            timerLabel.enableWordWrapping = false;
            timerLabel.overflowMode = TextOverflowModes.Overflow;
            timerRect = timerLabel.rectTransform;
            timerBaseScale = timerRect.localScale;
        }

        private void UpdateTimerVisualMotion()
        {
            if (timerLabel == null || !timerLabel.gameObject.activeInHierarchy)
            {
                return;
            }

            if (timerRect == null)
            {
                CacheTimerRect();
            }

            if (timerRect == null)
            {
                return;
            }

            timerAnimTime += Time.unscaledDeltaTime;
            float pulseWave = Mathf.Sin(timerAnimTime * timerPulseFrequency * Mathf.PI * 2f);
            float wobbleWave = Mathf.Sin(timerAnimTime * timerWobbleFrequency * Mathf.PI * 2f);

            float pulseScale = 1f + pulseWave * timerPulseAmplitude;
            timerRect.localScale = timerBaseScale * pulseScale;
            timerRect.localRotation = Quaternion.Euler(0f, 0f, wobbleWave * timerWobbleDegrees);
        }

        private void ResetTimerVisualTransform()
        {
            if (timerRect == null)
            {
                return;
            }

            timerRect.localScale = timerBaseScale;
            timerRect.localRotation = Quaternion.identity;
        }

        private void SelectButton(Button button)
        {
            if (button == null)
            {
                return;
            }

            if (targetEventSystem == null)
            {
                EnsureEventSystem();
            }

            if (targetEventSystem == null)
            {
                return;
            }

            targetEventSystem.SetSelectedGameObject(null);
            targetEventSystem.SetSelectedGameObject(button.gameObject);
        }

        private static string FormatElapsedTime(float seconds)
        {
            int centiseconds = Mathf.Max(0, Mathf.FloorToInt(seconds * 100f));
            return FormatElapsedTime(centiseconds);
        }

        private static string FormatElapsedTime(int centiseconds)
        {
            int safe = Mathf.Max(0, centiseconds);
            int minutes = safe / 6000;
            int seconds = (safe / 100) % 60;
            int milliseconds = safe % 100;
            return $"{minutes:00}:{seconds:00}:{milliseconds:00}";
        }

        private void OnPauseResumeClicked()
        {
            PlayButtonClickSound();
            gameManager?.ResumeGame();
        }

        private void OnPauseRestartClicked()
        {
            PlayButtonClickSound();
            gameManager?.RestartFromBeginning();
        }

        private void OnPauseExitClicked()
        {
            PlayButtonClickSound();
            gameManager?.ExitGame();
        }

        private void OnGameOverRestartClicked()
        {
            PlayButtonClickSound();
            gameManager?.RestartFromBeginning();
        }

        private void OnGameOverExitClicked()
        {
            PlayButtonClickSound();
            gameManager?.ExitGame();
        }

        private void PlayNavigationSound()
        {
            PlayOneShotFromSource(navigationSoundSource);
        }

        private void PlayButtonClickSound()
        {
            PlayOneShotFromSource(buttonClickSoundSource);
        }

        private static void PlayOneShotFromSource(AudioSource source)
        {
            if (source == null || source.clip == null)
            {
                return;
            }

            source.PlayOneShot(source.clip);
        }

        private static void PlayMenuMusic(AudioSource source)
        {
            if (source == null || source.clip == null || source.isPlaying)
            {
                return;
            }

            source.Play();
        }

        private static void StopMenuMusic(AudioSource source)
        {
            if (source == null || !source.isPlaying)
            {
                return;
            }

            source.Stop();
        }
    }
}
