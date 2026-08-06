using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GreenField.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10000)]
    public sealed class GreenFieldPauseMenuController : MonoBehaviour
    {
        private const string GameplaySceneName = "BaoDemo";
        private const string MainMenuSceneName = "MenuGame";
        private const string MasterVolumeKey = "GreenField.Audio.MasterVolume";
        private const string MutedKey = "GreenField.Audio.Muted";
        private const string QualityKey = "GreenField.Graphics.Quality";
        private const string FullscreenKey = "GreenField.Graphics.Fullscreen";
        private const string VSyncKey = "GreenField.Graphics.VSync";
        private const float DefaultMasterVolume = 0.8f;

        private GameObject menuRoot;
        private GameObject pausePage;
        private GameObject settingsPage;
        private Text pauseStatusText;
        private Text volumeValueText;
        private Text qualityValueText;
        private Slider volumeSlider;
        private Toggle muteToggle;
        private Toggle fullscreenToggle;
        private Toggle vSyncToggle;

        private float previousTimeScale = 1f;
        private bool previousAudioPause;
        private bool previousCursorVisible;
        private CursorLockMode previousCursorLockMode;
        private bool isPaused;
        private bool lockedPlayerWithPause;

        public static bool IsGamePaused { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneHook()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != GameplaySceneName ||
                Object.FindFirstObjectByType<GreenFieldPauseMenuController>() != null)
            {
                return;
            }

            GameObject pauseMenuObject = new GameObject("GreenField_PauseMenu");
            SceneManager.MoveGameObjectToScene(pauseMenuObject, scene);
            pauseMenuObject.AddComponent<GreenFieldPauseMenuController>();
        }

        private void Start()
        {
            EnsureEventSystem();
            BuildUI();
            ShowPausePage();
            menuRoot.SetActive(false);
        }

        private void Update()
        {
            if (!WasEscapePressed())
            {
                return;
            }

            if (isPaused)
            {
                if (settingsPage != null && settingsPage.activeSelf)
                {
                    ShowPausePage();
                }
                else
                {
                    ResumeGame();
                }

                return;
            }

            if (!IsAnotherGameplayWindowOpen())
            {
                PauseGame();
            }
        }

        private void OnDestroy()
        {
            if (isPaused)
            {
                RestoreGameState();
            }

            IsGamePaused = false;
        }

        public void PauseGame()
        {
            if (isPaused || menuRoot == null)
            {
                return;
            }

            previousTimeScale = Time.timeScale;
            previousAudioPause = AudioListener.pause;
            previousCursorVisible = Cursor.visible;
            previousCursorLockMode = Cursor.lockState;

            isPaused = true;
            IsGamePaused = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (GameLockManager.Instance != null)
            {
                GameLockManager.Instance.LockPlayer();
                lockedPlayerWithPause = true;
            }

            SyncSettingsUI();
            ShowPausePage();
            menuRoot.SetActive(true);
            SetPauseStatus(string.Empty, true);
            EventSystem.current?.SetSelectedGameObject(null);
        }

        public void ResumeGame()
        {
            if (!isPaused)
            {
                return;
            }

            menuRoot?.SetActive(false);
            RestoreGameState();
        }

        public void OpenSettings()
        {
            SyncSettingsUI();
            pausePage?.SetActive(false);
            settingsPage?.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(null);
        }

        public void ShowPausePage()
        {
            settingsPage?.SetActive(false);
            pausePage?.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(null);
        }

        public void ReturnToMainMenu()
        {
            if (!Application.CanStreamedLevelBeLoaded(MainMenuSceneName))
            {
                Debug.LogError(
                    "[Green Field] Không thể mở scene '" +
                    MainMenuSceneName + "'. Hãy thêm scene vào Build Settings.",
                    this
                );
                return;
            }

            if (SaveManager.Instance != null &&
                !SaveManager.Instance.TrySaveGame(out string saveMessage))
            {
                SetPauseStatus(saveMessage, false);
                return;
            }

            menuRoot?.SetActive(false);
            RestoreGameState();
            SceneManager.LoadScene(MainMenuSceneName, LoadSceneMode.Single);
        }

        private void RestoreGameState()
        {
            isPaused = false;
            IsGamePaused = false;
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisible;

            if (lockedPlayerWithPause && GameLockManager.Instance != null)
            {
                GameLockManager.Instance.UnlockPlayer();
            }

            lockedPlayerWithPause = false;
        }

        private void SetPauseStatus(string message, bool success)
        {
            if (pauseStatusText == null)
                return;

            pauseStatusText.text = message;
            pauseStatusText.color = success
                ? new Color(0.58f, 1f, 0.38f, 1f)
                : new Color(1f, 0.42f, 0.32f, 1f);
        }

        private static bool WasEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null &&
                   Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private static bool IsAnotherGameplayWindowOpen()
        {
            FishMarketUI fishMarket = Object.FindFirstObjectByType<FishMarketUI>();
            if (fishMarket != null && fishMarket.IsOpen)
            {
                return true;
            }

            FishingSupplyShopUI fishingShop =
                Object.FindFirstObjectByType<FishingSupplyShopUI>();
            if (fishingShop != null && fishingShop.IsOpen)
            {
                return true;
            }

            FishingShopTabsUI fishingTabs =
                Object.FindFirstObjectByType<FishingShopTabsUI>();
            if (fishingTabs != null && fishingTabs.IsOpen)
            {
                return true;
            }

            FishingPurchasePopupUI purchasePopup =
                Object.FindFirstObjectByType<FishingPurchasePopupUI>();
            if (purchasePopup != null && purchasePopup.IsOpen)
            {
                return true;
            }

            FishingCustomizationUI customization =
                Object.FindFirstObjectByType<FishingCustomizationUI>();
            if (customization != null && customization.IsOpen)
            {
                return true;
            }

            ToolSupplyShopUI toolShop =
                Object.FindFirstObjectByType<ToolSupplyShopUI>();
            if (toolShop != null && toolShop.IsOpen)
            {
                return true;
            }

            ToolSupplyShopStandalone standaloneToolShop =
                Object.FindFirstObjectByType<ToolSupplyShopStandalone>();
            if (standaloneToolShop != null && standaloneToolShop.IsOpen)
            {
                return true;
            }

            return WoodChopMinigameUI.Instance != null &&
                   WoodChopMinigameUI.Instance.IsPlaying;
        }

        private void SetMasterVolume(float volume)
        {
            float clampedVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MasterVolumeKey, clampedVolume);
            ApplyAudioSettings();
            UpdateVolumeText();
            PlayerPrefs.Save();
        }

        private void SetMuted(bool muted)
        {
            PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
            ApplyAudioSettings();
            UpdateVolumeText();
            PlayerPrefs.Save();
        }

        private void SetFullscreen(bool fullscreen)
        {
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            Screen.fullScreen = fullscreen;
            PlayerPrefs.Save();
        }

        private void SetVSync(bool enabled)
        {
            PlayerPrefs.SetInt(VSyncKey, enabled ? 1 : 0);
            QualitySettings.vSyncCount = enabled ? 1 : 0;
            PlayerPrefs.Save();
        }

        private void CycleQuality()
        {
            string[] qualityNames = QualitySettings.names;
            if (qualityNames.Length == 0)
            {
                return;
            }

            int nextQuality = (QualitySettings.GetQualityLevel() + 1) %
                              qualityNames.Length;
            QualitySettings.SetQualityLevel(nextQuality, true);
            PlayerPrefs.SetInt(QualityKey, nextQuality);
            PlayerPrefs.Save();
            UpdateQualityText();
        }

        private void ApplyAudioSettings()
        {
            float volume = Mathf.Clamp01(
                PlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume)
            );
            bool muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
            AudioListener.volume = muted ? 0f : volume;
        }

        private void SyncSettingsUI()
        {
            float volume = Mathf.Clamp01(
                PlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume)
            );
            bool muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
            bool fullscreen = PlayerPrefs.GetInt(
                FullscreenKey,
                Screen.fullScreen ? 1 : 0
            ) == 1;
            bool vSync = PlayerPrefs.GetInt(
                VSyncKey,
                QualitySettings.vSyncCount > 0 ? 1 : 0
            ) == 1;

            volumeSlider?.SetValueWithoutNotify(volume);
            muteToggle?.SetIsOnWithoutNotify(muted);
            fullscreenToggle?.SetIsOnWithoutNotify(fullscreen);
            vSyncToggle?.SetIsOnWithoutNotify(vSync);
            UpdateVolumeText();
            UpdateQualityText();
        }

        private void UpdateVolumeText()
        {
            if (volumeValueText == null)
            {
                return;
            }

            bool muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
            float volume = PlayerPrefs.GetFloat(
                MasterVolumeKey,
                DefaultMasterVolume
            );
            volumeValueText.text = muted
                ? "TẮT"
                : Mathf.RoundToInt(Mathf.Clamp01(volume) * 100f) + "%";
        }

        private void UpdateQualityText()
        {
            if (qualityValueText == null)
            {
                return;
            }

            string[] names = QualitySettings.names;
            qualityValueText.text = names.Length == 0
                ? "KHÔNG CÓ"
                : names[Mathf.Clamp(
                    QualitySettings.GetQualityLevel(),
                    0,
                    names.Length - 1
                )];
        }

        private void BuildUI()
        {
            GameObject canvasObject = new GameObject(
                "PauseCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            menuRoot = CreateUIObject("PauseOverlay", canvasObject.transform);
            Stretch(menuRoot.GetComponent<RectTransform>());
            Image overlay = menuRoot.AddComponent<Image>();
            overlay.color = new Color(0.01f, 0.025f, 0.018f, 0.92f);

            GameObject window = CreatePanel(
                menuRoot.transform,
                "PauseWindow",
                new Vector2(760f, 760f)
            );

            pausePage = CreateUIObject("PausePage", window.transform);
            Stretch(pausePage.GetComponent<RectTransform>());
            BuildPausePage(pausePage.transform);

            settingsPage = CreateUIObject("SettingsPage", window.transform);
            Stretch(settingsPage.GetComponent<RectTransform>());
            BuildSettingsPage(settingsPage.transform);
        }

        private void BuildPausePage(Transform parent)
        {
            CreateText(
                parent,
                "Title",
                "TẠM DỪNG",
                new Vector2(0f, 290f),
                new Vector2(650f, 80f),
                52,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.82f, 0.35f, 1f)
            );

            CreateText(
                parent,
                "Hint",
                "NHẤN ESC ĐỂ TIẾP TỤC",
                new Vector2(0f, 230f),
                new Vector2(600f, 42f),
                20,
                FontStyle.Normal,
                TextAnchor.MiddleCenter,
                new Color(0.78f, 0.78f, 0.72f, 1f)
            );

            CreateButton(
                parent,
                "ResumeButton",
                "TIẾP TỤC",
                new Vector2(0f, 75f),
                new Color(0.27f, 0.62f, 0.14f, 1f),
                ResumeGame
            );
            CreateButton(
                parent,
                "SettingsButton",
                "CÀI ĐẶT",
                new Vector2(0f, -25f),
                new Color(0.18f, 0.43f, 0.68f, 1f),
                OpenSettings
            );
            CreateButton(
                parent,
                "MainMenuButton",
                "LƯU & VỀ MENU",
                new Vector2(0f, -125f),
                new Color(0.58f, 0.25f, 0.12f, 1f),
                ReturnToMainMenu
            );

            pauseStatusText = CreateText(
                parent,
                "SaveStatus",
                string.Empty,
                new Vector2(0f, -225f),
                new Vector2(640f, 52f),
                21,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(0.58f, 1f, 0.38f, 1f)
            );
        }

        private void BuildSettingsPage(Transform parent)
        {
            CreateText(
                parent,
                "Title",
                "CÀI ĐẶT",
                new Vector2(0f, 285f),
                new Vector2(650f, 70f),
                44,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.82f, 0.35f, 1f)
            );

            CreateText(
                parent,
                "VolumeLabel",
                "ÂM LƯỢNG",
                new Vector2(-210f, 175f),
                new Vector2(220f, 45f),
                23,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                Color.white
            );
            volumeSlider = CreateSlider(
                parent,
                new Vector2(70f, 175f),
                new Vector2(300f, 42f)
            );
            volumeSlider.onValueChanged.AddListener(SetMasterVolume);

            volumeValueText = CreateText(
                parent,
                "VolumeValue",
                "80%",
                new Vector2(275f, 175f),
                new Vector2(100f, 45f),
                23,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(0.65f, 1f, 0.38f, 1f)
            );

            muteToggle = CreateToggle(
                parent,
                "MuteToggle",
                "TẮT TIẾNG",
                new Vector2(-95f, 100f)
            );
            muteToggle.onValueChanged.AddListener(SetMuted);

            fullscreenToggle = CreateToggle(
                parent,
                "FullscreenToggle",
                "TOÀN MÀN HÌNH",
                new Vector2(-95f, 30f)
            );
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

            vSyncToggle = CreateToggle(
                parent,
                "VSyncToggle",
                "ĐỒNG BỘ KHUNG HÌNH",
                new Vector2(-95f, -40f)
            );
            vSyncToggle.onValueChanged.AddListener(SetVSync);

            CreateText(
                parent,
                "QualityLabel",
                "CHẤT LƯỢNG",
                new Vector2(-205f, -120f),
                new Vector2(230f, 45f),
                22,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                Color.white
            );
            Button qualityButton = CreateButton(
                parent,
                "QualityButton",
                string.Empty,
                new Vector2(145f, -120f),
                new Color(0.24f, 0.38f, 0.55f, 1f),
                CycleQuality,
                new Vector2(300f, 58f)
            );
            qualityValueText = qualityButton.GetComponentInChildren<Text>();

            CreateButton(
                parent,
                "BackButton",
                "QUAY LẠI",
                new Vector2(0f, -245f),
                new Color(0.44f, 0.31f, 0.16f, 1f),
                ShowPausePage
            );
        }

        private static GameObject CreatePanel(
            Transform parent,
            string objectName,
            Vector2 size
        )
        {
            GameObject panel = CreateUIObject(objectName, parent);
            RectTransform rect = panel.GetComponent<RectTransform>();
            SetCentered(rect, Vector2.zero, size);

            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.13f, 0.09f, 0.045f, 0.99f);

            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.94f, 0.66f, 0.2f, 0.95f);
            outline.effectDistance = new Vector2(4f, -4f);

            Shadow shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(12f, -12f);
            return panel;
        }

        private static Button CreateButton(
            Transform parent,
            string objectName,
            string label,
            Vector2 position,
            Color color,
            UnityEngine.Events.UnityAction onClick,
            Vector2? customSize = null
        )
        {
            Vector2 size = customSize ?? new Vector2(430f, 72f);
            GameObject buttonObject = CreateUIObject(objectName, parent);
            SetCentered(
                buttonObject.GetComponent<RectTransform>(),
                position,
                size
            );

            Image image = buttonObject.AddComponent<Image>();
            image.color = color;

            Outline outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.03f, 0.015f, 0.005f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            button.colors = colors;
            button.onClick.AddListener(onClick);

            Text text = CreateText(
                buttonObject.transform,
                "Label",
                label,
                Vector2.zero,
                size,
                26,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.white
            );
            Stretch(text.rectTransform);
            return button;
        }

        private static Slider CreateSlider(
            Transform parent,
            Vector2 position,
            Vector2 size
        )
        {
            GameObject sliderObject = CreateUIObject("VolumeSlider", parent);
            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            SetCentered(sliderRect, position, size);

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            Image background = CreateImage(
                sliderObject.transform,
                "Background",
                new Color(0.04f, 0.035f, 0.025f, 1f)
            );
            Stretch(background.rectTransform);
            background.rectTransform.offsetMin = new Vector2(0f, 13f);
            background.rectTransform.offsetMax = new Vector2(0f, -13f);

            GameObject fillArea = CreateUIObject("Fill Area", sliderObject.transform);
            Stretch(fillArea.GetComponent<RectTransform>());
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.offsetMin = new Vector2(8f, 13f);
            fillAreaRect.offsetMax = new Vector2(-8f, -13f);

            Image fill = CreateImage(
                fillArea.transform,
                "Fill",
                new Color(0.35f, 0.82f, 0.17f, 1f)
            );
            Stretch(fill.rectTransform);

            GameObject handleArea = CreateUIObject(
                "Handle Slide Area",
                sliderObject.transform
            );
            Stretch(handleArea.GetComponent<RectTransform>());
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);

            Image handle = CreateImage(
                handleArea.transform,
                "Handle",
                new Color(1f, 0.82f, 0.32f, 1f)
            );
            handle.rectTransform.sizeDelta = new Vector2(24f, 42f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            return slider;
        }

        private static Toggle CreateToggle(
            Transform parent,
            string objectName,
            string label,
            Vector2 position
        )
        {
            GameObject toggleObject = CreateUIObject(objectName, parent);
            SetCentered(
                toggleObject.GetComponent<RectTransform>(),
                position,
                new Vector2(480f, 54f)
            );

            Toggle toggle = toggleObject.AddComponent<Toggle>();
            Image box = CreateImage(
                toggleObject.transform,
                "Background",
                new Color(0.04f, 0.035f, 0.025f, 1f)
            );
            RectTransform boxRect = box.rectTransform;
            boxRect.anchorMin = new Vector2(0f, 0.5f);
            boxRect.anchorMax = new Vector2(0f, 0.5f);
            boxRect.anchoredPosition = new Vector2(22f, 0f);
            boxRect.sizeDelta = new Vector2(40f, 40f);

            Image checkmark = CreateImage(
                box.transform,
                "Checkmark",
                new Color(0.35f, 0.86f, 0.15f, 1f)
            );
            Stretch(checkmark.rectTransform);
            checkmark.rectTransform.offsetMin = new Vector2(7f, 7f);
            checkmark.rectTransform.offsetMax = new Vector2(-7f, -7f);

            CreateText(
                toggleObject.transform,
                "Label",
                label,
                new Vector2(85f, 0f),
                new Vector2(370f, 50f),
                22,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                Color.white
            );

            toggle.targetGraphic = box;
            toggle.graphic = checkmark;
            return toggle;
        }

        private static Image CreateImage(
            Transform parent,
            string objectName,
            Color color
        )
        {
            GameObject imageObject = CreateUIObject(objectName, parent);
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(
            Transform parent,
            string objectName,
            string value,
            Vector2 position,
            Vector2 size,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Color color
        )
        {
            GameObject textObject = CreateUIObject(objectName, parent);
            SetCentered(
                textObject.GetComponent<RectTransform>(),
                position,
                size
            );

            Text text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = LoadBuiltInFont();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static Font LoadBuiltInFont()
        {
#if UNITY_2022_2_OR_NEWER
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }

        private static GameObject CreateUIObject(
            string objectName,
            Transform parent
        )
        {
            GameObject uiObject = new GameObject(
                objectName,
                typeof(RectTransform)
            );
            uiObject.layer = LayerMask.NameToLayer("UI");
            uiObject.transform.SetParent(parent, false);
            return uiObject;
        }

        private static void SetCentered(
            RectTransform rect,
            Vector2 position,
            Vector2 size
        )
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
            eventSystemObject.AddComponent<
                UnityEngine.InputSystem.UI.InputSystemUIInputModule
            >();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
