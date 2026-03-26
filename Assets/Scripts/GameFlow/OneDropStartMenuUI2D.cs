using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.UI;
#endif

namespace WaterBlob
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class OneDropStartMenuUI2D : MonoBehaviour
    {
        private enum SubPanel
        {
            Main,
            Options,
            Credits
        }

        [System.Serializable]
        private struct MenuButtonStyle
        {
            public string label;
            public Vector2 anchoredPosition;
            public Vector2 size;
            public Color boxColor;
            public Color textColor;
        }

        [Header("Core")]
        [SerializeField] private OneDropGameManager2D gameManager;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private EventSystem targetEventSystem;
        [SerializeField] private bool showOnStart = true;

        [Header("Background")]
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private string backgroundSpriteName = "One Drop Concept ui bg";
        [SerializeField] private Vector2 backgroundSize = new(1920f, 1080f);
        [SerializeField] private Vector2 backgroundPosition = Vector2.zero;
        [SerializeField] private Color backgroundTint = new(1f, 1f, 1f, 0.92f);
        [SerializeField] private Sprite optionsBackgroundSprite;
        [SerializeField] private Color optionsBackgroundTint = new(0.78f, 0.9f, 1f, 0.92f);
        [SerializeField] private Sprite creditsBackgroundSprite;
        [SerializeField] private Color creditsBackgroundTint = new(1f, 0.88f, 0.76f, 0.92f);
        [SerializeField] private Color overlayColor = new(0f, 0f, 0f, 0.38f);

        [Header("Title")]
        [SerializeField] private string titleText = "ONE DROP";
        [SerializeField] private int titleFontSize = 56;
        [SerializeField] private Color titleColor = new(0.94f, 0.98f, 1f, 1f);

        [Header("Main Card")]
        [SerializeField] private Vector2 mainCardSize = new(560f, 710f);
        [SerializeField] private Vector2 mainCardPosition = Vector2.zero;
        [SerializeField] private Color mainCardColor = new(0.08f, 0.11f, 0.16f, 0.62f);
        [SerializeField] private Font buttonFont;
        [SerializeField] private int buttonFontSize = 34;
        [SerializeField] private MenuButtonStyle newGameButtonStyle = new()
        {
            label = "NEW GAME",
            anchoredPosition = new Vector2(0f, 145f),
            size = new Vector2(380f, 82f),
            boxColor = new Color(0.2f, 0.52f, 0.9f, 0.85f),
            textColor = Color.white
        };
        [SerializeField] private MenuButtonStyle optionsButtonStyle = new()
        {
            label = "OPTIONS",
            anchoredPosition = new Vector2(0f, 35f),
            size = new Vector2(380f, 82f),
            boxColor = new Color(0.2f, 0.72f, 0.48f, 0.85f),
            textColor = Color.white
        };
        [SerializeField] private MenuButtonStyle creditsButtonStyle = new()
        {
            label = "CREDIT",
            anchoredPosition = new Vector2(0f, -75f),
            size = new Vector2(380f, 82f),
            boxColor = new Color(0.89f, 0.58f, 0.21f, 0.85f),
            textColor = Color.white
        };
        [SerializeField] private MenuButtonStyle exitButtonStyle = new()
        {
            label = "EXIT",
            anchoredPosition = new Vector2(0f, -185f),
            size = new Vector2(380f, 82f),
            boxColor = new Color(0.82f, 0.26f, 0.26f, 0.88f),
            textColor = Color.white
        };

        [Header("Options Card")]
        [SerializeField] private Vector2 optionsCardSize = new(560f, 520f);
        [SerializeField] private Vector2 optionsCardPosition = Vector2.zero;
        [SerializeField] private Color optionsCardColor = new(0.09f, 0.13f, 0.2f, 0.7f);
        [SerializeField] private Color optionsTitleColor = new(0.93f, 0.98f, 1f, 1f);
        [SerializeField] private Color sliderBackgroundColor = new(0.15f, 0.17f, 0.22f, 0.85f);
        [SerializeField] private Color sliderFillColor = new(0.25f, 0.65f, 1f, 0.95f);
        [SerializeField] private Color sliderHandleColor = new(0.92f, 0.97f, 1f, 1f);
        [SerializeField] private Color optionsTextColor = new(0.94f, 0.97f, 1f, 1f);
        [SerializeField] private MenuButtonStyle optionsBackButtonStyle = new()
        {
            label = "BACK",
            anchoredPosition = new Vector2(0f, -185f),
            size = new Vector2(300f, 72f),
            boxColor = new Color(0.3f, 0.34f, 0.55f, 0.9f),
            textColor = Color.white
        };

        [Header("Credits Card")]
        [SerializeField] private Vector2 creditsCardSize = new(560f, 520f);
        [SerializeField] private Vector2 creditsCardPosition = Vector2.zero;
        [SerializeField] private Color creditsCardColor = new(0.09f, 0.13f, 0.2f, 0.7f);
        [SerializeField] private Color creditsTitleColor = new(0.93f, 0.98f, 1f, 1f);
        [SerializeField] private MenuButtonStyle creditsBackButtonStyle = new()
        {
            label = "BACK",
            anchoredPosition = new Vector2(0f, -185f),
            size = new Vector2(300f, 72f),
            boxColor = new Color(0.3f, 0.34f, 0.55f, 0.9f),
            textColor = Color.white
        };

        [Header("Menu SFX")]
        [SerializeField] private AudioSource uiSfxSource;
        [SerializeField] private AudioClip buttonClickSfx;
        [SerializeField] private AudioClip buttonHoverSfx;
        [SerializeField, Range(0f, 1f)] private float uiSfxVolume = 1f;
        [SerializeField, Min(0f)] private float hoverSfxInterval = 0.08f;

        private const string RootName = "OneDropStartMenu_Root";
        private const string BackgroundName = "Background";
        private const string MainCardName = "MainCard";
        private const string OptionsCardName = "OptionsCard";
        private const string CreditsCardName = "CreditsCard";
        private const string NewGameButtonName = "NewGameButton";
        private const string OptionsButtonName = "OptionsButton";
        private const string CreditsButtonName = "CreditsButton";
        private const string ExitButtonName = "ExitButton";
        private const string OptionsBackButtonName = "OptionsBackButton";
        private const string CreditsBackButtonName = "CreditsBackButton";
        private const string SfxSliderName = "SfxSlider";
        private const string MusicSliderName = "MusicSlider";

        private GameObject root;
        private Image backgroundImage;
        private GameObject mainCard;
        private GameObject optionsCard;
        private GameObject creditsCard;
        private Button newGameButton;
        private Button optionsButton;
        private Button creditsButton;
        private Button exitButton;
        private Button optionsBackButton;
        private Button creditsBackButton;
        private Slider sfxSlider;
        private Slider musicSlider;
        private bool menuOpen;
        private bool gameplayBlocked;
        private float lastHoverSfxTime = -99f;
        private Sprite mainPanelBackgroundSprite;
        private Color mainPanelBackgroundTint;

        private void Reset()
        {
            ResolveReferences();
            TryResolveBackgroundSpriteByName();
        }

        private void Awake()
        {
            ResolveReferences();
            EnsureBackgroundMusicController();
            TryResolveBackgroundSpriteByName();
            BuildUi();
            if (Application.isPlaying)
            {
                if (showOnStart)
                {
                    OpenMenu();
                }
                else
                {
                    SetMenuVisible(false);
                }
            }
            else
            {
                SetMenuVisible(showOnStart);
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                ResolveReferences();
                TryResolveBackgroundSpriteByName();
                BuildUi();
                SetMenuVisible(showOnStart);
                return;
            }

            ResolveReferences();
            EnsureBackgroundMusicController();
            TryResolveBackgroundSpriteByName();
            BuildUi();
            OneDropAudioSettings2D.VolumesChanged += HandleVolumeSettingsChanged;
            RefreshVolumeSliders();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            OneDropAudioSettings2D.VolumesChanged -= HandleVolumeSettingsChanged;
            ReleaseGameplayBlock();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            ResolveReferences();
            TryResolveBackgroundSpriteByName();
            BuildUi();
            SetMenuVisible(showOnStart);
        }

        private void ResolveReferences()
        {
            if (gameManager == null)
            {
                gameManager = GetComponent<OneDropGameManager2D>();
                if (gameManager == null)
                {
                    gameManager = FindFirstObjectByType<OneDropGameManager2D>();
                }
            }

            if (targetCanvas == null)
            {
                Transform existingRoot = FindExistingRootTransform();
                if (existingRoot != null)
                {
                    Canvas existingCanvas = existingRoot.GetComponentInParent<Canvas>();
                    if (existingCanvas != null && existingCanvas.renderMode != RenderMode.WorldSpace)
                    {
                        targetCanvas = existingCanvas;
                    }
                }
            }

            if (targetCanvas == null)
            {
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < canvases.Length; i++)
                {
                    if (canvases[i] != null && canvases[i].renderMode != RenderMode.WorldSpace)
                    {
                        targetCanvas = canvases[i];
                        break;
                    }
                }

                if (targetCanvas == null)
                {
                    GameObject canvasGo = new("StartMenuCanvas_Auto", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    targetCanvas = canvasGo.GetComponent<Canvas>();
                    targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920f, 1080f);
                    scaler.matchWidthOrHeight = 0.5f;
                }
            }

            if (targetEventSystem == null)
            {
                targetEventSystem = EventSystem.current;
                if (targetEventSystem == null)
                {
                    GameObject eventSystemGo = new("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                    eventSystemGo.AddComponent<InputSystemUIInputModule>();
#else
                    eventSystemGo.AddComponent<StandaloneInputModule>();
#endif
                    targetEventSystem = eventSystemGo.GetComponent<EventSystem>();
                }
            }
        }

        private void EnsureBackgroundMusicController()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (FindFirstObjectByType<OneDropBackgroundMusic2D>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            if (GetComponent<OneDropBackgroundMusic2D>() == null)
            {
                gameObject.AddComponent<OneDropBackgroundMusic2D>();
            }
        }

        private void BuildUi()
        {
            if (targetCanvas == null)
            {
                return;
            }

            Transform existing = FindExistingRootTransform();
            if (existing != null)
            {
                Canvas existingCanvas = existing.GetComponentInParent<Canvas>();
                if (existingCanvas != null && existingCanvas.renderMode != RenderMode.WorldSpace)
                {
                    targetCanvas = existingCanvas;
                }

                root = existing.gameObject;
                CacheExistingUiReferences();
                if (HasRequiredUiReferences())
                {
                    SetupInteractionBindings();
                    ApplyMenuInteractionSafety();
                    EnsureUiAudioSource();
                    SetSubPanel(SubPanel.Main);
                    return;
                }

                if (Application.isPlaying)
                {
                    Destroy(existing.gameObject);
                }
                else
                {
                    DestroyImmediate(existing.gameObject);
                }

                ClearUiReferences();
            }

            root = CreatePanel(RootName, targetCanvas.transform, Vector2.zero, new Vector2(9999f, 9999f), overlayColor);
            backgroundImage = CreateImage(BackgroundName, root.transform, backgroundPosition, backgroundSize, backgroundTint);
            backgroundImage.sprite = backgroundSprite;
            backgroundImage.preserveAspect = true;
            mainPanelBackgroundSprite = backgroundImage.sprite;
            mainPanelBackgroundTint = backgroundImage.color;

            mainCard = CreatePanel(MainCardName, root.transform, mainCardPosition, mainCardSize, mainCardColor);
            CreateText("Title", mainCard.transform, new Vector2(0f, 275f), new Vector2(420f, 72f), titleText, titleFontSize, titleColor);
            newGameButton = CreateStyledButton("NewGameButton", mainCard.transform, newGameButtonStyle, HandleNewGameClicked);
            optionsButton = CreateStyledButton("OptionsButton", mainCard.transform, optionsButtonStyle, HandleOptionsClicked);
            creditsButton = CreateStyledButton("CreditsButton", mainCard.transform, creditsButtonStyle, HandleCreditsClicked);
            exitButton = CreateStyledButton("ExitButton", mainCard.transform, exitButtonStyle, HandleExitClicked);

            optionsCard = CreatePanel(OptionsCardName, root.transform, optionsCardPosition, optionsCardSize, optionsCardColor);
            CreateText("OptionsTitle", optionsCard.transform, new Vector2(0f, 190f), new Vector2(360f, 56f), "OPTIONS", 42, optionsTitleColor);
            CreateText("SfxLabel", optionsCard.transform, new Vector2(0f, 92f), new Vector2(360f, 36f), "SFX", 28, optionsTextColor);
            sfxSlider = CreateSlider(SfxSliderName, optionsCard.transform, new Vector2(0f, 48f), new Vector2(380f, 32f));
            CreateText("MusicLabel", optionsCard.transform, new Vector2(0f, -22f), new Vector2(360f, 36f), "MUSIC", 28, optionsTextColor);
            musicSlider = CreateSlider(MusicSliderName, optionsCard.transform, new Vector2(0f, -66f), new Vector2(380f, 32f));
            optionsBackButton = CreateStyledButton("OptionsBackButton", optionsCard.transform, optionsBackButtonStyle, HandleBackToMainClicked);

            creditsCard = CreatePanel(CreditsCardName, root.transform, creditsCardPosition, creditsCardSize, creditsCardColor);
            CreateText("CreditsTitle", creditsCard.transform, new Vector2(0f, 190f), new Vector2(360f, 56f), "CREDIT", 42, creditsTitleColor);
            creditsBackButton = CreateStyledButton("CreditsBackButton", creditsCard.transform, creditsBackButtonStyle, HandleBackToMainClicked);

            SetupInteractionBindings();
            ApplyMenuInteractionSafety();
            EnsureUiAudioSource();
            SetSubPanel(SubPanel.Main);
        }

        private void ClearUiReferences()
        {
            root = null;
            backgroundImage = null;
            mainCard = null;
            optionsCard = null;
            creditsCard = null;
            newGameButton = null;
            optionsButton = null;
            creditsButton = null;
            exitButton = null;
            optionsBackButton = null;
            creditsBackButton = null;
            sfxSlider = null;
            musicSlider = null;
        }

        private Transform FindExistingRootTransform()
        {
            if (targetCanvas != null)
            {
                Transform child = targetCanvas.transform.Find(RootName);
                if (child != null)
                {
                    return child;
                }
            }

            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate != null && candidate.name == RootName)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void CacheExistingUiReferences()
        {
            if (root == null)
            {
                ClearUiReferences();
                return;
            }

            mainCard = FindChildObject(root.transform, MainCardName);
            optionsCard = FindChildObject(root.transform, OptionsCardName);
            creditsCard = FindChildObject(root.transform, CreditsCardName);
            backgroundImage = FindImage(root.transform, BackgroundName);

            newGameButton = FindButton(mainCard != null ? mainCard.transform : null, NewGameButtonName);
            optionsButton = FindButton(mainCard != null ? mainCard.transform : null, OptionsButtonName);
            creditsButton = FindButton(mainCard != null ? mainCard.transform : null, CreditsButtonName);
            exitButton = FindButton(mainCard != null ? mainCard.transform : null, ExitButtonName);
            optionsBackButton = FindButton(optionsCard != null ? optionsCard.transform : null, OptionsBackButtonName);
            creditsBackButton = FindButton(creditsCard != null ? creditsCard.transform : null, CreditsBackButtonName);

            sfxSlider = FindSlider(optionsCard != null ? optionsCard.transform : null, SfxSliderName);
            musicSlider = FindSlider(optionsCard != null ? optionsCard.transform : null, MusicSliderName);

            if (backgroundImage != null)
            {
                mainPanelBackgroundSprite = backgroundImage.sprite;
                mainPanelBackgroundTint = backgroundImage.color;
            }
            else
            {
                mainPanelBackgroundSprite = backgroundSprite;
                mainPanelBackgroundTint = backgroundTint;
            }
        }

        private bool HasRequiredUiReferences()
        {
            return root != null
                && mainCard != null
                && optionsCard != null
                && creditsCard != null
                && newGameButton != null
                && optionsButton != null
                && creditsButton != null
                && exitButton != null
                && optionsBackButton != null
                && creditsBackButton != null
                && sfxSlider != null
                && musicSlider != null
                && backgroundImage != null;
        }

        private void SetupInteractionBindings()
        {
            BindButton(newGameButton, HandleNewGameClicked);
            BindButton(optionsButton, HandleOptionsClicked);
            BindButton(creditsButton, HandleCreditsClicked);
            BindButton(exitButton, HandleExitClicked);
            BindButton(optionsBackButton, HandleBackToMainClicked);
            BindButton(creditsBackButton, HandleBackToMainClicked);
            BindFallbackNewGameButtonsInScene();
            SetupSliderCallbacks();
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(callback);
        }

        private static GameObject FindChildObject(Transform parent, string childName)
        {
            Transform child = FindChildTransform(parent, childName);
            return child != null ? child.gameObject : null;
        }

        private static Button FindButton(Transform parent, string childName)
        {
            Transform child = FindChildTransform(parent, childName);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private static Slider FindSlider(Transform parent, string childName)
        {
            Transform child = FindChildTransform(parent, childName);
            return child != null ? child.GetComponent<Slider>() : null;
        }

        private static Image FindImage(Transform parent, string childName)
        {
            Transform child = FindChildTransform(parent, childName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private static Transform FindChildTransform(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            Transform direct = parent.Find(childName);
            if (direct != null)
            {
                return direct;
            }

            Transform[] descendants = parent.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                Transform descendant = descendants[i];
                if (descendant != null && descendant.name == childName)
                {
                    return descendant;
                }
            }

            return null;
        }

        private void SetupSliderCallbacks()
        {
            if (sfxSlider != null)
            {
                sfxSlider.minValue = 0f;
                sfxSlider.maxValue = 1f;
                sfxSlider.wholeNumbers = false;
                sfxSlider.onValueChanged.RemoveAllListeners();
                sfxSlider.onValueChanged.AddListener(value =>
                {
                    OneDropAudioSettings2D.SetSfxVolume(value);
                    PlayHoverSfx();
                });
            }

            if (musicSlider != null)
            {
                musicSlider.minValue = 0f;
                musicSlider.maxValue = 1f;
                musicSlider.wholeNumbers = false;
                musicSlider.onValueChanged.RemoveAllListeners();
                musicSlider.onValueChanged.AddListener(OneDropAudioSettings2D.SetMusicVolume);
            }

            RefreshVolumeSliders();
        }

        private void RefreshVolumeSliders()
        {
            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(OneDropAudioSettings2D.SfxVolume);
            }

            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(OneDropAudioSettings2D.MusicVolume);
            }
        }

        private void HandleVolumeSettingsChanged()
        {
            RefreshVolumeSliders();
        }

        public void OpenMenu()
        {
            ApplyMenuInteractionSafety();
            SetMenuVisible(true);
            SetSubPanel(SubPanel.Main);
            if (targetEventSystem != null && newGameButton != null)
            {
                targetEventSystem.SetSelectedGameObject(newGameButton.gameObject);
            }
        }

        public void CloseMenu()
        {
            SetMenuVisible(false);
        }

        private void SetMenuVisible(bool visible)
        {
            menuOpen = visible;
            if (root != null)
            {
                root.SetActive(visible);
            }
            else if (!visible)
            {
                HideLooseMenuPanels();
            }

            if (!Application.isPlaying)
            {
                return;
            }

            if (visible)
            {
                ApplyGameplayBlock();
            }
            else
            {
                ReleaseGameplayBlock();
                HideLooseMenuPanels();
            }
        }

        private void SetSubPanel(SubPanel panel)
        {
            if (mainCard != null)
            {
                mainCard.SetActive(panel == SubPanel.Main);
            }

            if (optionsCard != null)
            {
                optionsCard.SetActive(panel == SubPanel.Options);
            }

            if (creditsCard != null)
            {
                creditsCard.SetActive(panel == SubPanel.Credits);
            }

            ApplyBackgroundForPanel(panel);
            ApplyMenuInteractionSafety();
        }

        private void ApplyBackgroundForPanel(SubPanel panel)
        {
            if (backgroundImage == null)
            {
                return;
            }

            switch (panel)
            {
                case SubPanel.Options:
                    backgroundImage.sprite = optionsBackgroundSprite != null ? optionsBackgroundSprite : mainPanelBackgroundSprite;
                    backgroundImage.color = optionsBackgroundTint;
                    break;
                case SubPanel.Credits:
                    backgroundImage.sprite = creditsBackgroundSprite != null ? creditsBackgroundSprite : mainPanelBackgroundSprite;
                    backgroundImage.color = creditsBackgroundTint;
                    break;
                default:
                    backgroundImage.sprite = mainPanelBackgroundSprite;
                    backgroundImage.color = mainPanelBackgroundTint;
                    break;
            }

            backgroundImage.preserveAspect = true;
        }

        private void ApplyGameplayBlock()
        {
            if (!gameplayBlocked)
            {
                gameManager?.SetExternalGameplayInputBlocked(true);
                gameplayBlocked = true;
            }

            Time.timeScale = 0f;
        }

        private void ReleaseGameplayBlock()
        {
            if (gameplayBlocked)
            {
                gameManager?.SetExternalGameplayInputBlocked(false);
                gameplayBlocked = false;
            }

            if (!menuOpen)
            {
                Time.timeScale = 1f;
            }
        }

        private void HandleNewGameClicked()
        {
            PlayClickSfx();
            CloseAllStartMenusInScene();
            gameManager?.ClearExternalGameplayInputBlocks();
            gameManager?.ResumeGame();
            Time.timeScale = 1f;
        }

        private static void HideLooseMenuPanels()
        {
            SetActiveForAllNamedObjects(RootName, false);
            SetActiveForAllNamedObjects(MainCardName, false);
            SetActiveForAllNamedObjects(OptionsCardName, false);
            SetActiveForAllNamedObjects(CreditsCardName, false);
        }

        private void CloseAllStartMenusInScene()
        {
            OneDropStartMenuUI2D[] menus = FindObjectsByType<OneDropStartMenuUI2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < menus.Length; i++)
            {
                OneDropStartMenuUI2D menu = menus[i];
                if (menu == null)
                {
                    continue;
                }

                menu.SetMenuVisible(false);
            }

            HideLooseMenuPanels();
        }

        private static void SetActiveForAllNamedObjects(string objectName, bool active)
        {
            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform t = transforms[i];
                if (t == null || !t.name.Equals(objectName, System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (t.gameObject.activeSelf != active)
                {
                    t.gameObject.SetActive(active);
                }
            }
        }

        private void ApplyMenuInteractionSafety()
        {
            if (root == null)
            {
                return;
            }

            Image rootImage = root.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.raycastTarget = false;
            }

            Transform background = FindChildTransform(root.transform, BackgroundName);
            if (background != null)
            {
                background.SetAsFirstSibling();
                Image backgroundImage = background.GetComponent<Image>();
                if (backgroundImage != null)
                {
                    backgroundImage.raycastTarget = false;
                }
            }

            if (mainCard != null && mainCard.activeSelf)
            {
                mainCard.transform.SetAsLastSibling();
            }

            if (optionsCard != null && optionsCard.activeSelf)
            {
                optionsCard.transform.SetAsLastSibling();
            }

            if (creditsCard != null && creditsCard.activeSelf)
            {
                creditsCard.transform.SetAsLastSibling();
            }
        }

        private void BindFallbackNewGameButtonsInScene()
        {
            if (targetCanvas == null)
            {
                return;
            }

            Button[] buttons = targetCanvas.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button candidate = buttons[i];
                if (!IsLikelyNewGameButton(candidate))
                {
                    continue;
                }

                candidate.onClick.RemoveListener(HandleNewGameClicked);
                candidate.onClick.AddListener(HandleNewGameClicked);
                candidate.interactable = true;
                if (newGameButton == null)
                {
                    newGameButton = candidate;
                }
            }
        }

        private static bool IsLikelyNewGameButton(Button button)
        {
            if (button == null)
            {
                return false;
            }

            string name = button.name ?? string.Empty;
            if (name.IndexOf("newgame", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (name.IndexOf("new", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                name.IndexOf("game", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            string labelText = GetButtonLabelText(button);
            if (labelText.IndexOf("new game", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return labelText.IndexOf("start", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetButtonLabelText(Button button)
        {
            if (button == null)
            {
                return string.Empty;
            }

            TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
            if (tmpText != null && !string.IsNullOrWhiteSpace(tmpText.text))
            {
                return tmpText.text;
            }

            Text legacyText = button.GetComponentInChildren<Text>(true);
            if (legacyText != null && !string.IsNullOrWhiteSpace(legacyText.text))
            {
                return legacyText.text;
            }

            return string.Empty;
        }

        private void HandleOptionsClicked()
        {
            PlayClickSfx();
            SetSubPanel(SubPanel.Options);
            if (targetEventSystem != null && optionsBackButton != null)
            {
                targetEventSystem.SetSelectedGameObject(optionsBackButton.gameObject);
            }
        }

        private void HandleCreditsClicked()
        {
            PlayClickSfx();
            SetSubPanel(SubPanel.Credits);
            if (targetEventSystem != null && creditsBackButton != null)
            {
                targetEventSystem.SetSelectedGameObject(creditsBackButton.gameObject);
            }
        }

        private void HandleBackToMainClicked()
        {
            PlayClickSfx();
            SetSubPanel(SubPanel.Main);
            if (targetEventSystem != null && optionsButton != null)
            {
                targetEventSystem.SetSelectedGameObject(optionsButton.gameObject);
            }
        }

        private void HandleExitClicked()
        {
            PlayClickSfx();
            gameManager?.ExitGame();
            if (gameManager == null)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        private void EnsureUiAudioSource()
        {
            if (uiSfxSource == null)
            {
                uiSfxSource = root != null ? root.GetComponent<AudioSource>() : null;
            }

            if (uiSfxSource == null && root != null)
            {
                uiSfxSource = root.AddComponent<AudioSource>();
            }

            if (uiSfxSource == null)
            {
                return;
            }

            uiSfxSource.playOnAwake = false;
            uiSfxSource.loop = false;
            uiSfxSource.spatialBlend = 0f;
        }

        private void PlayClickSfx()
        {
            if (buttonClickSfx != null && uiSfxSource != null)
            {
                uiSfxSource.PlayOneShot(buttonClickSfx, OneDropAudioSettings2D.ApplySfx(uiSfxVolume));
            }
        }

        private void PlayHoverSfx()
        {
            if (buttonHoverSfx == null || uiSfxSource == null)
            {
                return;
            }

            if (Time.unscaledTime - lastHoverSfxTime < hoverSfxInterval)
            {
                return;
            }

            lastHoverSfxTime = Time.unscaledTime;
            uiSfxSource.PlayOneShot(buttonHoverSfx, OneDropAudioSettings2D.ApplySfx(uiSfxVolume));
        }

        private Button CreateStyledButton(string name, Transform parent, MenuButtonStyle style, UnityEngine.Events.UnityAction callback)
        {
            GameObject buttonGo = CreatePanel(name, parent, style.anchoredPosition, style.size, style.boxColor);
            Button button = buttonGo.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = style.boxColor;
            colors.highlightedColor = style.boxColor * 1.08f;
            colors.pressedColor = style.boxColor * 0.92f;
            colors.selectedColor = style.boxColor * 1.05f;
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.onClick.AddListener(callback);
            CreateText("Label", buttonGo.transform, Vector2.zero, style.size, style.label, buttonFontSize, style.textColor);

            EventTrigger trigger = buttonGo.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new() { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener(_ => PlayHoverSfx());
            trigger.triggers.Add(entry);
            EventTrigger.Entry selectEntry = new() { eventID = EventTriggerType.Select };
            selectEntry.callback.AddListener(_ => PlayHoverSfx());
            trigger.triggers.Add(selectEntry);

            return button;
        }

        private Slider CreateSlider(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject sliderGo = new(name, typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(parent, false);
            RectTransform sliderRect = sliderGo.GetComponent<RectTransform>();
            sliderRect.sizeDelta = size;
            sliderRect.anchoredPosition = anchoredPosition;

            Image bg = CreateImage("Background", sliderGo.transform, Vector2.zero, size, sliderBackgroundColor);
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.offsetMin = Vector2.zero;
            bg.rectTransform.offsetMax = Vector2.zero;

            Image fill = CreateImage("Fill", sliderGo.transform, Vector2.zero, size, sliderFillColor);
            fill.rectTransform.anchorMin = new Vector2(0f, 0f);
            fill.rectTransform.anchorMax = new Vector2(1f, 1f);
            fill.rectTransform.offsetMin = new Vector2(4f, 4f);
            fill.rectTransform.offsetMax = new Vector2(-4f, -4f);

            Image handle = CreateImage("Handle", sliderGo.transform, Vector2.zero, new Vector2(24f, size.y + 12f), sliderHandleColor);

            Slider slider = sliderGo.GetComponent<Slider>();
            slider.targetGraphic = handle;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.direction = Slider.Direction.LeftToRight;

            return slider;
        }

        private GameObject CreatePanel(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = go.GetComponent<Image>();
            image.color = color;
            return go;
        }

        private Image CreateImage(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, string content, int fontSize, Color color)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            if (buttonFont != null)
            {
                TMP_FontAsset tmp = TMP_FontAsset.CreateFontAsset(buttonFont);
                if (tmp != null)
                {
                    text.font = tmp;
                }
            }

            return text;
        }

        private void TryResolveBackgroundSpriteByName()
        {
            if (backgroundSprite != null || string.IsNullOrWhiteSpace(backgroundSpriteName))
            {
                return;
            }

            Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null && sprites[i].name.Equals(backgroundSpriteName, System.StringComparison.OrdinalIgnoreCase))
                {
                    backgroundSprite = sprites[i];
                    break;
                }
            }
        }
    }
}
