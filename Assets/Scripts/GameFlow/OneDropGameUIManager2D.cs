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
        [SerializeField] private bool rebuildPauseMenuFromScratchAtRuntime = true;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button pauseResumeButton;
        [SerializeField] private Button pauseRestartButton;
        [SerializeField] private Button pauseSoundButton;
        [SerializeField] private Button pauseExitButton;
        [SerializeField] private GameObject pauseSoundPanel;
        [SerializeField] private Button pauseSoundBackButton;

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

        [Header("Audio Settings")]
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField, Min(0f)] private float sfxSliderPreviewInterval = 0.08f;

        private Font defaultFont;
        private bool buttonsWired;
        private RectTransform timerRect;
        private Vector3 timerBaseScale = Vector3.one;
        private int lastTimerCentiseconds = -1;
        private float timerAnimTime;
        private float pauseMenuMusicBaseVolume = 1f;
        private float gameOverMusicBaseVolume = 1f;
        private float buttonClickBaseVolume = 1f;
        private float navigationBaseVolume = 1f;
        private Text sfxVolumeLabelText;
        private Text musicVolumeLabelText;
        private float lastSfxSliderPreviewTime = -99f;
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
            CacheAudioBaseVolumes();
            WireVolumeControls();
            ApplyMenuAudioVolumes();

            SetPausePanelVisible(false, selectDefault: false);
            SetGameOverPanelVisible(false, -1f);
            RefreshTimerLabel(gameManager != null ? gameManager.ElapsedTime : 0f);
            ApplyTimerVisibility();
        }

        private void OnEnable()
        {
            ResolveGameManager();
            WireVolumeControls();
            OneDropAudioSettings2D.VolumesChanged += HandleGlobalAudioVolumesChanged;
            ApplyMenuAudioVolumes();
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
            OneDropAudioSettings2D.VolumesChanged -= HandleGlobalAudioVolumesChanged;
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

            if (Application.isPlaying)
            {
                RebuildPauseMenuFromScratchIfRequested();
            }

            PreparePauseMenuLayout();
            ResolvePauseUiReferences();
            if (pausePanel == null)
            {
                CreatePauseMenu();
            }
            else
            {
                EnsurePauseVolumeControlsUi();
                ResolvePauseUiReferences();
            }

            ResolveGameOverUiReferences();
            if (gameOverPanel == null)
            {
                CreateGameOverMenu();
            }
        }

        private void RebuildPauseMenuFromScratchIfRequested()
        {
            if (!rebuildPauseMenuFromScratchAtRuntime)
            {
                return;
            }

            if (targetCanvas != null)
            {
                List<GameObject> toRemove = new();
                for (int i = 0; i < targetCanvas.transform.childCount; i++)
                {
                    Transform child = targetCanvas.transform.GetChild(i);
                    if (child == null)
                    {
                        continue;
                    }

                    string name = child.gameObject.name;
                    if (name.Equals("PauseMenuPanel", StringComparison.OrdinalIgnoreCase) || name.Equals("PauseSoundPanel", StringComparison.OrdinalIgnoreCase))
                    {
                        toRemove.Add(child.gameObject);
                    }
                }

                for (int i = 0; i < toRemove.Count; i++)
                {
                    if (toRemove[i] != null)
                    {
                        Destroy(toRemove[i]);
                    }
                }
            }

            pausePanel = null;
            pauseResumeButton = null;
            pauseRestartButton = null;
            pauseSoundButton = null;
            pauseExitButton = null;
            pauseSoundPanel = null;
            pauseSoundBackButton = null;
            sfxVolumeSlider = null;
            musicVolumeSlider = null;
            sfxVolumeLabelText = null;
            musicVolumeLabelText = null;
            buttonsWired = false;
            rebuildPauseMenuFromScratchAtRuntime = false;
        }

        private void PreparePauseMenuLayout()
        {
            if (pausePanel == null)
            {
                return;
            }

            ResolvePauseUiReferences();
            bool hasSoundButton = pauseSoundButton != null && (ButtonContainsToken(pauseSoundButton, "sound") || ButtonContainsToken(pauseSoundButton, "audio"));
            if (hasSoundButton)
            {
                return;
            }

            // Legacy pause panel detected (Resume/Restart/Exit + sliders). Hide it and rebuild a new one with a Sound entry.
            pausePanel.SetActive(false);
            pausePanel = null;
            pauseResumeButton = null;
            pauseRestartButton = null;
            pauseSoundButton = null;
            pauseExitButton = null;
            pauseSoundPanel = null;
            pauseSoundBackButton = null;
            sfxVolumeSlider = null;
            musicVolumeSlider = null;
            sfxVolumeLabelText = null;
            musicVolumeLabelText = null;
            buttonsWired = false;
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
            if (pauseSoundButton != null)
            {
                bool looksLikeSound = ButtonContainsToken(pauseSoundButton, "sound") || ButtonContainsToken(pauseSoundButton, "audio");
                bool conflicts = pauseSoundButton == pauseResumeButton || pauseSoundButton == pauseRestartButton || pauseSoundButton == pauseExitButton;
                if (!looksLikeSound || conflicts)
                {
                    pauseSoundButton = null;
                }
            }

            if (pauseSoundBackButton != null)
            {
                bool looksLikeSoundBack = ButtonContainsToken(pauseSoundBackButton, "back") && (ButtonContainsToken(pauseSoundBackButton, "sound") || ButtonContainsToken(pauseSoundBackButton, "audio"));
                if (!looksLikeSoundBack)
                {
                    pauseSoundBackButton = null;
                }
            }

            if (pauseResumeButton == null)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    Button button = buttons[i];
                    if (button == null)
                    {
                        continue;
                    }

                    if (ButtonContainsToken(button, "resume"))
                    {
                        pauseResumeButton = button;
                        break;
                    }
                }
            }

            if (pauseRestartButton == null)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    Button button = buttons[i];
                    if (button == null)
                    {
                        continue;
                    }

                    if (ButtonContainsToken(button, "restart") && !ButtonContainsToken(button, "gameover"))
                    {
                        pauseRestartButton = button;
                        break;
                    }
                }
            }

            if (pauseSoundButton == null)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    Button button = buttons[i];
                    if (button == null)
                    {
                        continue;
                    }

                    if (ButtonContainsToken(button, "sound") || ButtonContainsToken(button, "audio"))
                    {
                        pauseSoundButton = button;
                        break;
                    }
                }
            }

            if (pauseExitButton == null)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    Button button = buttons[i];
                    if (button == null)
                    {
                        continue;
                    }

                    if (ButtonContainsToken(button, "exit") && !ButtonContainsToken(button, "gameover"))
                    {
                        pauseExitButton = button;
                        break;
                    }
                }
            }

            if (pauseSoundBackButton == null)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    Button button = buttons[i];
                    if (button == null)
                    {
                        continue;
                    }

                    if (ButtonContainsToken(button, "back") && (ButtonContainsToken(button, "sound") || ButtonContainsToken(button, "audio")))
                    {
                        pauseSoundBackButton = button;
                        break;
                    }
                }
            }

            if (pauseResumeButton == null && buttons.Length > 0)
            {
                pauseResumeButton = buttons[0];
            }

            if (pauseRestartButton == null && buttons.Length > 1)
            {
                pauseRestartButton = buttons[1];
            }

            if (pauseExitButton == null)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    Button button = buttons[i];
                    if (button == null || button == pauseResumeButton || button == pauseRestartButton || button == pauseSoundButton || button == pauseSoundBackButton)
                    {
                        continue;
                    }

                    pauseExitButton = button;
                    break;
                }
            }

            if (pauseSoundPanel == null)
            {
                Transform foundSoundPanel = pausePanel.transform.Find("PauseSoundPanel");
                if (foundSoundPanel != null)
                {
                    pauseSoundPanel = foundSoundPanel.gameObject;
                }
            }

            Slider[] sliders = pausePanel.GetComponentsInChildren<Slider>(true);
            for (int i = 0; i < sliders.Length; i++)
            {
                Slider slider = sliders[i];
                if (slider == null)
                {
                    continue;
                }

                if (sfxVolumeSlider == null && SliderContainsToken(slider, "sfx"))
                {
                    sfxVolumeSlider = slider;
                }
                else if (musicVolumeSlider == null && SliderContainsToken(slider, "music"))
                {
                    musicVolumeSlider = slider;
                }
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeLabelText = ResolveSliderRowLabel(sfxVolumeSlider);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeLabelText = ResolveSliderRowLabel(musicVolumeSlider);
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
            RectTransform card = CreateMenuCard(pausePanel.transform, new Vector2(540f, 430f));

            CreateLabel(card, "PauseTitle", "Paused", 50, FontStyle.Bold, TextAnchor.MiddleCenter);
            pauseResumeButton = CreateButton(card, "ResumeButton", "Resume");
            pauseRestartButton = CreateButton(card, "RestartButton", "Restart");
            pauseSoundButton = CreateButton(card, "SoundButton", "Sound");
            pauseExitButton = CreateButton(card, "ExitButton", "Exit");

            CreatePauseSoundPanel();
        }

        private void CreatePauseSoundPanel()
        {
            if (pausePanel == null)
            {
                return;
            }

            GameObject panelGo = new("PauseSoundPanel", typeof(RectTransform), typeof(Image));
            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.SetParent(pausePanel.transform, false);
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = panelGo.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.52f);

            RectTransform card = CreateMenuCard(panelGo.transform, new Vector2(540f, 380f));
            CreateLabel(card, "PauseSoundTitle", "Sound", 46, FontStyle.Bold, TextAnchor.MiddleCenter);
            sfxVolumeSlider = CreateLabeledSlider(card, "SfxVolume", "SFX Volume");
            musicVolumeSlider = CreateLabeledSlider(card, "MusicVolume", "Music Volume");
            pauseSoundBackButton = CreateButton(card, "PauseSoundBackButton", "Back");

            pauseSoundPanel = panelGo;
            pauseSoundPanel.SetActive(false);
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

        private Slider CreateLabeledSlider(Transform parent, string objectName, string labelText)
        {
            GameObject rowGo = new($"{objectName}Row", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            RectTransform rowRect = rowGo.GetComponent<RectTransform>();
            rowRect.SetParent(parent, false);
            rowRect.sizeDelta = new Vector2(0f, 82f);

            VerticalLayoutGroup rowLayout = rowGo.GetComponent<VerticalLayoutGroup>();
            rowLayout.padding = new RectOffset(0, 0, 0, 0);
            rowLayout.spacing = 6f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;

            LayoutElement rowElement = rowGo.GetComponent<LayoutElement>();
            rowElement.preferredHeight = 82f;

            Text label = CreateLabel(rowRect, $"{objectName}Label", labelText, 24, FontStyle.Normal, TextAnchor.MiddleLeft);
            LayoutElement labelLayout = label.GetComponent<LayoutElement>();
            if (labelLayout != null)
            {
                labelLayout.preferredHeight = 30f;
            }

            GameObject sliderGo = new($"{objectName}Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
            RectTransform sliderRect = sliderGo.GetComponent<RectTransform>();
            sliderRect.SetParent(rowRect, false);
            sliderRect.sizeDelta = new Vector2(0f, 34f);

            LayoutElement sliderLayout = sliderGo.GetComponent<LayoutElement>();
            sliderLayout.preferredHeight = 34f;

            Slider slider = sliderGo.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;

            GameObject backgroundGo = new("Background", typeof(RectTransform), typeof(Image));
            RectTransform backgroundRect = backgroundGo.GetComponent<RectTransform>();
            backgroundRect.SetParent(sliderRect, false);
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(0f, 12f);
            backgroundRect.anchoredPosition = Vector2.zero;

            Image backgroundImage = backgroundGo.GetComponent<Image>();
            backgroundImage.color = new Color(0.14f, 0.22f, 0.28f, 0.95f);

            GameObject fillAreaGo = new("Fill Area", typeof(RectTransform));
            RectTransform fillAreaRect = fillAreaGo.GetComponent<RectTransform>();
            fillAreaRect.SetParent(sliderRect, false);
            fillAreaRect.anchorMin = new Vector2(0f, 0f);
            fillAreaRect.anchorMax = new Vector2(1f, 1f);
            fillAreaRect.offsetMin = new Vector2(10f, 11f);
            fillAreaRect.offsetMax = new Vector2(-10f, -11f);

            GameObject fillGo = new("Fill", typeof(RectTransform), typeof(Image));
            RectTransform fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.SetParent(fillAreaRect, false);
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fillImage = fillGo.GetComponent<Image>();
            fillImage.color = new Color(0.36f, 0.73f, 0.92f, 1f);

            GameObject handleAreaGo = new("Handle Slide Area", typeof(RectTransform));
            RectTransform handleAreaRect = handleAreaGo.GetComponent<RectTransform>();
            handleAreaRect.SetParent(sliderRect, false);
            handleAreaRect.anchorMin = new Vector2(0f, 0f);
            handleAreaRect.anchorMax = new Vector2(1f, 1f);
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);

            GameObject handleGo = new("Handle", typeof(RectTransform), typeof(Image));
            RectTransform handleRect = handleGo.GetComponent<RectTransform>();
            handleRect.SetParent(handleAreaRect, false);
            handleRect.sizeDelta = new Vector2(20f, 30f);

            Image handleImage = handleGo.GetComponent<Image>();
            handleImage.color = new Color(0.93f, 0.99f, 1f, 1f);

            slider.targetGraphic = handleImage;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;

            return slider;
        }

        private void EnsurePauseVolumeControlsUi()
        {
            if (pausePanel == null)
            {
                return;
            }

            RectTransform mainCard = ResolvePauseMenuCard();
            if (mainCard == null)
            {
                return;
            }

            if (pauseSoundButton == null)
            {
                pauseSoundButton = CreateButton(mainCard, "SoundButton", "Sound");
                if (pauseExitButton != null)
                {
                    pauseSoundButton.transform.SetSiblingIndex(Mathf.Max(0, pauseExitButton.transform.GetSiblingIndex()));
                }
            }
            else if (!pauseSoundButton.transform.IsChildOf(mainCard))
            {
                pauseSoundButton.transform.SetParent(mainCard, false);
            }

            if (pauseSoundPanel == null)
            {
                CreatePauseSoundPanel();
            }

            RectTransform soundCard = ResolvePauseSoundCard();
            if (soundCard == null)
            {
                return;
            }

            Slider[] allPauseSliders = pausePanel.GetComponentsInChildren<Slider>(true);
            for (int i = 0; i < allPauseSliders.Length; i++)
            {
                Slider slider = allPauseSliders[i];
                if (slider == null || slider.transform.parent == null)
                {
                    continue;
                }

                if (slider.transform.IsChildOf(soundCard) || !slider.transform.IsChildOf(mainCard))
                {
                    continue;
                }

                // Move any legacy pause sliders into the sound sub-panel so old layouts migrate automatically.
                slider.transform.parent.SetParent(soundCard, false);
            }

            if (sfxVolumeSlider != null && sfxVolumeSlider.transform.parent != null && !sfxVolumeSlider.transform.IsChildOf(soundCard))
            {
                sfxVolumeSlider.transform.parent.SetParent(soundCard, false);
            }

            if (musicVolumeSlider != null && musicVolumeSlider.transform.parent != null && !musicVolumeSlider.transform.IsChildOf(soundCard))
            {
                musicVolumeSlider.transform.parent.SetParent(soundCard, false);
            }

            if (sfxVolumeSlider == null || !sfxVolumeSlider.transform.IsChildOf(soundCard))
            {
                Slider existingSfx = FindSliderByToken(soundCard, "sfx");
                sfxVolumeSlider = existingSfx != null ? existingSfx : CreateLabeledSlider(soundCard, "SfxVolume", "SFX Volume");
            }

            if (musicVolumeSlider == null || !musicVolumeSlider.transform.IsChildOf(soundCard))
            {
                Slider existingMusic = FindSliderByToken(soundCard, "music");
                musicVolumeSlider = existingMusic != null ? existingMusic : CreateLabeledSlider(soundCard, "MusicVolume", "Music Volume");
            }

            if (pauseSoundBackButton == null)
            {
                pauseSoundBackButton = CreateButton(soundCard, "PauseSoundBackButton", "Back");
            }
            else if (!pauseSoundBackButton.transform.IsChildOf(soundCard))
            {
                pauseSoundBackButton.transform.SetParent(soundCard, false);
            }

            if (pauseSoundPanel != null && pauseSoundPanel.activeSelf)
            {
                pauseSoundPanel.SetActive(false);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeLabelText = ResolveSliderRowLabel(sfxVolumeSlider);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeLabelText = ResolveSliderRowLabel(musicVolumeSlider);
            }
        }

        private static Slider FindSliderByToken(Transform root, string nameToken)
        {
            if (root == null)
            {
                return null;
            }

            Slider[] sliders = root.GetComponentsInChildren<Slider>(true);
            for (int i = 0; i < sliders.Length; i++)
            {
                Slider slider = sliders[i];
                if (slider == null)
                {
                    continue;
                }

                if (SliderContainsToken(slider, nameToken))
                {
                    return slider;
                }
            }

            return null;
        }

        private RectTransform ResolvePauseMenuCard()
        {
            if (pausePanel == null)
            {
                return null;
            }

            Transform named = pausePanel.transform.Find("MenuCard");
            if (named is RectTransform namedRect)
            {
                return namedRect;
            }

            if (pausePanel.transform.childCount > 0 && pausePanel.transform.GetChild(0) is RectTransform childRect)
            {
                return childRect;
            }

            return null;
        }

        private RectTransform ResolvePauseSoundCard()
        {
            if (pauseSoundPanel == null)
            {
                return null;
            }

            Transform named = pauseSoundPanel.transform.Find("MenuCard");
            if (named is RectTransform namedRect)
            {
                return namedRect;
            }

            if (pauseSoundPanel.transform.childCount > 0 && pauseSoundPanel.transform.GetChild(0) is RectTransform childRect)
            {
                return childRect;
            }

            return null;
        }

        private static Text ResolveSliderRowLabel(Slider slider)
        {
            if (slider == null || slider.transform.parent == null)
            {
                return null;
            }

            string labelName = slider.name.Replace("Slider", "Label");
            Transform found = slider.transform.parent.Find(labelName);
            if (found != null)
            {
                return found.GetComponent<Text>();
            }

            return slider.transform.parent.GetComponentInChildren<Text>(true);
        }

        private static bool ButtonContainsToken(Button button, string token)
        {
            if (button == null || string.IsNullOrEmpty(token))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(button.name) && button.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            Text legacyText = button.GetComponentInChildren<Text>(true);
            if (legacyText != null && !string.IsNullOrEmpty(legacyText.text) && legacyText.text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
            return tmpText != null && !string.IsNullOrEmpty(tmpText.text) && tmpText.text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool SliderContainsToken(Slider slider, string token)
        {
            if (slider == null || string.IsNullOrEmpty(token))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(slider.name) && slider.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            Text rowLabel = ResolveSliderRowLabel(slider);
            if (rowLabel != null && !string.IsNullOrEmpty(rowLabel.text) && rowLabel.text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            TMP_Text rowTmp = slider.transform.parent != null ? slider.transform.parent.GetComponentInChildren<TMP_Text>(true) : null;
            return rowTmp != null && !string.IsNullOrEmpty(rowTmp.text) && rowTmp.text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void UpdateVolumeLabelTexts()
        {
            if (sfxVolumeLabelText != null)
            {
                int pct = Mathf.RoundToInt(OneDropAudioSettings2D.SfxVolume * 100f);
                sfxVolumeLabelText.text = $"SFX Volume ({pct}%)";
            }

            if (musicVolumeLabelText != null)
            {
                int pct = Mathf.RoundToInt(OneDropAudioSettings2D.MusicVolume * 100f);
                musicVolumeLabelText.text = $"Music Volume ({pct}%)";
            }
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
            BindButton(pauseSoundButton, OnPauseSoundClicked);
            BindButton(pauseExitButton, OnPauseExitClicked);
            BindButton(pauseSoundBackButton, OnPauseSoundBackClicked);
            BindButton(gameOverRestartButton, OnGameOverRestartClicked);
            BindButton(gameOverExitButton, OnGameOverExitClicked);

            AddNavigationSoundHooks(pauseResumeButton);
            AddNavigationSoundHooks(pauseRestartButton);
            AddNavigationSoundHooks(pauseSoundButton);
            AddNavigationSoundHooks(pauseExitButton);
            AddNavigationSoundHooks(pauseSoundBackButton);
            AddNavigationSoundHooks(gameOverRestartButton);
            AddNavigationSoundHooks(gameOverExitButton);

            buttonsWired = true;
        }

        private void WireVolumeControls()
        {
            ResolvePauseUiReferences();
            EnsurePauseVolumeControlsUi();
            ResolvePauseUiReferences();

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.minValue = 0f;
                sfxVolumeSlider.maxValue = 1f;
                sfxVolumeSlider.wholeNumbers = false;
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeSliderChanged);
                sfxVolumeSlider.SetValueWithoutNotify(OneDropAudioSettings2D.SfxVolume);
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeSliderChanged);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.minValue = 0f;
                musicVolumeSlider.maxValue = 1f;
                musicVolumeSlider.wholeNumbers = false;
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeSliderChanged);
                musicVolumeSlider.SetValueWithoutNotify(OneDropAudioSettings2D.MusicVolume);
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeSliderChanged);
            }

            UpdateVolumeLabelTexts();
        }

        private void OnSfxVolumeSliderChanged(float value)
        {
            OneDropAudioSettings2D.SetSfxVolume(value);
            UpdateVolumeLabelTexts();

            if (Time.unscaledTime - lastSfxSliderPreviewTime >= sfxSliderPreviewInterval)
            {
                lastSfxSliderPreviewTime = Time.unscaledTime;
                PlayNavigationSound();
            }
        }

        private void OnMusicVolumeSliderChanged(float value)
        {
            OneDropAudioSettings2D.SetMusicVolume(value);
            UpdateVolumeLabelTexts();
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
            ApplyMenuAudioVolumes();

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
            ApplyMenuAudioVolumes();

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
            SetPauseSoundPanelVisible(false, selectBackButton: false, selectSoundButton: false);
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

        private void OnPauseSoundClicked()
        {
            PlayButtonClickSound();
            SetPauseSoundPanelVisible(true, selectBackButton: true, selectSoundButton: false);
        }

        private void OnPauseSoundBackClicked()
        {
            PlayButtonClickSound();
            SetPauseSoundPanelVisible(false, selectBackButton: false, selectSoundButton: true);
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

        private void PlayOneShotFromSource(AudioSource source)
        {
            if (source == null || source.clip == null)
            {
                return;
            }

            ApplyMenuAudioVolumes();
            source.PlayOneShot(source.clip);
        }

        private void PlayMenuMusic(AudioSource source)
        {
            if (source == null || source.clip == null || source.isPlaying)
            {
                return;
            }

            ApplyMenuAudioVolumes();
            source.Play();
        }

        private void StopMenuMusic(AudioSource source)
        {
            if (source == null || !source.isPlaying)
            {
                return;
            }

            source.Stop();
        }

        private void SetPauseSoundPanelVisible(bool visible, bool selectBackButton, bool selectSoundButton)
        {
            if (pauseSoundPanel == null)
            {
                return;
            }

            pauseSoundPanel.SetActive(visible);
            if (visible && selectBackButton)
            {
                SelectButton(pauseSoundBackButton);
                return;
            }

            if (!visible && selectSoundButton)
            {
                SelectButton(pauseSoundButton);
            }
        }

        private void CacheAudioBaseVolumes()
        {
            pauseMenuMusicBaseVolume = pauseMenuMusicSource != null ? Mathf.Clamp01(pauseMenuMusicSource.volume) : 1f;
            gameOverMusicBaseVolume = gameOverMusicSource != null ? Mathf.Clamp01(gameOverMusicSource.volume) : 1f;
            buttonClickBaseVolume = buttonClickSoundSource != null ? Mathf.Clamp01(buttonClickSoundSource.volume) : 1f;
            navigationBaseVolume = navigationSoundSource != null ? Mathf.Clamp01(navigationSoundSource.volume) : 1f;
        }

        private void ApplyMenuAudioVolumes()
        {
            if (pauseMenuMusicSource != null)
            {
                pauseMenuMusicSource.volume = OneDropAudioSettings2D.ApplyMusic(pauseMenuMusicBaseVolume);
            }

            if (gameOverMusicSource != null)
            {
                gameOverMusicSource.volume = OneDropAudioSettings2D.ApplyMusic(gameOverMusicBaseVolume);
            }

            if (buttonClickSoundSource != null)
            {
                buttonClickSoundSource.volume = OneDropAudioSettings2D.ApplySfx(buttonClickBaseVolume);
            }

            if (navigationSoundSource != null)
            {
                navigationSoundSource.volume = OneDropAudioSettings2D.ApplySfx(navigationBaseVolume);
            }
        }

        private void HandleGlobalAudioVolumesChanged()
        {
            ApplyMenuAudioVolumes();

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(OneDropAudioSettings2D.SfxVolume);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(OneDropAudioSettings2D.MusicVolume);
            }

            UpdateVolumeLabelTexts();
        }
    }
}
