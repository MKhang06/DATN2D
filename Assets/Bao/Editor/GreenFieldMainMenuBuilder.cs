using System;
using System.Collections.Generic;
using System.IO;
using GreenField.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GreenField.UI.Editor
{
    public static class GreenFieldMainMenuBuilder
    {
        private sealed class SettingsUI
        {
            public SettingsUI(
                GameObject root,
                GameObject generalPage,
                GameObject audioPage,
                GameObject graphicsPage,
                GameObject controlsPage,
                Button generalTabButton,
                Button audioTabButton,
                Button graphicsTabButton,
                Button controlsTabButton,
                Dropdown languageDropdown,
                Slider masterVolumeSlider,
                Text masterVolumeValueText,
                Toggle muteToggle,
                Dropdown resolutionDropdown,
                Dropdown qualityDropdown,
                Toggle fullscreenToggle,
                Toggle vSyncToggle,
                Button resetButton,
                Button closeButton
            )
            {
                Root = root;
                GeneralPage = generalPage;
                AudioPage = audioPage;
                GraphicsPage = graphicsPage;
                ControlsPage = controlsPage;
                GeneralTabButton = generalTabButton;
                AudioTabButton = audioTabButton;
                GraphicsTabButton = graphicsTabButton;
                ControlsTabButton = controlsTabButton;
                LanguageDropdown = languageDropdown;
                MasterVolumeSlider = masterVolumeSlider;
                MasterVolumeValueText = masterVolumeValueText;
                MuteToggle = muteToggle;
                ResolutionDropdown = resolutionDropdown;
                QualityDropdown = qualityDropdown;
                FullscreenToggle = fullscreenToggle;
                VSyncToggle = vSyncToggle;
                ResetButton = resetButton;
                CloseButton = closeButton;
            }

            public GameObject Root { get; }
            public GameObject GeneralPage { get; }
            public GameObject AudioPage { get; }
            public GameObject GraphicsPage { get; }
            public GameObject ControlsPage { get; }
            public Button GeneralTabButton { get; }
            public Button AudioTabButton { get; }
            public Button GraphicsTabButton { get; }
            public Button ControlsTabButton { get; }
            public Dropdown LanguageDropdown { get; }
            public Slider MasterVolumeSlider { get; }
            public Text MasterVolumeValueText { get; }
            public Toggle MuteToggle { get; }
            public Dropdown ResolutionDropdown { get; }
            public Dropdown QualityDropdown { get; }
            public Toggle FullscreenToggle { get; }
            public Toggle VSyncToggle { get; }
            public Button ResetButton { get; }
            public Button CloseButton { get; }
        }

        private const string RootName = "GreenField_MainMenu";
        private const string TargetSceneName = "BaoDemo";
        private const string TargetScenePath = "Assets/Bao/Scenes/BaoDemo.unity";

        private const string BackgroundPath =
            "Assets/Bao/Art/Backgrounds/main_menu_background.png";
        private const string LogoPath =
            "Assets/Bao/Art/Logo/logo_green_field.png";
        private const string ButtonStartPath =
            "Assets/Bao/Art/Buttons/button_start.png";
        private const string ButtonContinuePath =
            "Assets/Bao/Art/Buttons/button_continue.png";
        private const string ButtonLoadPath =
            "Assets/Bao/Art/Buttons/button_load.png";
        private const string ButtonSettingsPath =
            "Assets/Bao/Art/Buttons/button_settings.png";
        private const string ButtonExitPath =
            "Assets/Bao/Art/Buttons/button_exit.png";

        private const string PrefabPath =
            "Assets/Bao/Prefabs/GreenField_MainMenu.prefab";

        [MenuItem("Tools/Green Field/Build Main Menu", priority = 1)]
        public static void BuildMainMenu()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ValidateAndConfigureAssets();
                EnsureBaoDemoInBuildSettings();

                GameObject existing = GameObject.Find(RootName);
                if (existing != null)
                {
                    bool replace = EditorUtility.DisplayDialog(
                        "Green Field UI Builder",
                        "Scene đã có GreenField_MainMenu. Bạn muốn thay bằng bản mới?",
                        "Thay thế",
                        "Hủy"
                    );

                    if (!replace)
                    {
                        return;
                    }

                    Undo.DestroyObjectImmediate(existing);
                }

                GameObject root = CreateCanvasRoot();
                GreenFieldMainMenuController controller =
                    root.AddComponent<GreenFieldMainMenuController>();
                SetControllerScene(controller, TargetSceneName);

                CreateBackground(root.transform, LoadSprite(BackgroundPath));
                CreateLogo(root.transform, LoadSprite(LogoPath));

                Transform buttonGroup = CreateButtonGroup(root.transform);
                Button startButton = CreateMenuButton(
                    buttonGroup,
                    "Button_Start",
                    LoadSprite(ButtonStartPath),
                    268f
                );
                Button continueButton = CreateMenuButton(
                    buttonGroup,
                    "Button_Continue",
                    LoadSprite(ButtonContinuePath),
                    134f
                );
                Button loadButton = CreateMenuButton(
                    buttonGroup,
                    "Button_Load",
                    LoadSprite(ButtonLoadPath),
                    0f
                );
                Button settingsButton = CreateMenuButton(
                    buttonGroup,
                    "Button_Settings",
                    LoadSprite(ButtonSettingsPath),
                    -134f
                );
                Button exitButton = CreateMenuButton(
                    buttonGroup,
                    "Button_Exit",
                    LoadSprite(ButtonExitPath),
                    -268f
                );

                UnityEventTools.AddPersistentListener(
                    startButton.onClick,
                    controller.StartNewGame
                );
                UnityEventTools.AddPersistentListener(
                    continueButton.onClick,
                    controller.ContinueGame
                );
                UnityEventTools.AddPersistentListener(
                    loadButton.onClick,
                    controller.LoadGame
                );
                UnityEventTools.AddPersistentListener(
                    settingsButton.onClick,
                    controller.OpenSettings
                );
                UnityEventTools.AddPersistentListener(
                    exitButton.onClick,
                    controller.ExitGame
                );

                CreateVersionLabel(root.transform);

                SettingsUI settings = CreateSettingsPanel(root.transform);
                SetControllerSettings(controller, settings);

                UnityEventTools.AddPersistentListener(
                    settings.GeneralTabButton.onClick,
                    controller.ShowGeneralTab
                );
                UnityEventTools.AddPersistentListener(
                    settings.AudioTabButton.onClick,
                    controller.ShowAudioTab
                );
                UnityEventTools.AddPersistentListener(
                    settings.GraphicsTabButton.onClick,
                    controller.ShowGraphicsTab
                );
                UnityEventTools.AddPersistentListener(
                    settings.ControlsTabButton.onClick,
                    controller.ShowControlsTab
                );
                UnityEventTools.AddPersistentListener(
                    settings.LanguageDropdown.onValueChanged,
                    controller.SetLanguage
                );
                UnityEventTools.AddPersistentListener(
                    settings.MasterVolumeSlider.onValueChanged,
                    controller.SetMasterVolume
                );
                UnityEventTools.AddPersistentListener(
                    settings.MuteToggle.onValueChanged,
                    controller.SetMuted
                );
                UnityEventTools.AddPersistentListener(
                    settings.ResolutionDropdown.onValueChanged,
                    controller.SetResolution
                );
                UnityEventTools.AddPersistentListener(
                    settings.QualityDropdown.onValueChanged,
                    controller.SetQuality
                );
                UnityEventTools.AddPersistentListener(
                    settings.FullscreenToggle.onValueChanged,
                    controller.SetFullscreen
                );
                UnityEventTools.AddPersistentListener(
                    settings.VSyncToggle.onValueChanged,
                    controller.SetVSync
                );
                UnityEventTools.AddPersistentListener(
                    settings.ResetButton.onClick,
                    controller.ResetAllSettings
                );
                UnityEventTools.AddPersistentListener(
                    settings.CloseButton.onClick,
                    controller.CloseSettings
                );

                settings.GeneralPage.SetActive(true);
                settings.AudioPage.SetActive(false);
                settings.GraphicsPage.SetActive(false);
                settings.ControlsPage.SetActive(false);
                settings.Root.SetActive(false);

                EnsureEventSystem();
                SavePrefab(root);

                Selection.activeGameObject = root;
                EditorSceneManager.MarkSceneDirty(root.scene);
                AssetDatabase.SaveAssets();

                EditorUtility.DisplayDialog(
                    "Green Field UI Builder",
                    "Đã tạo menu thành công.\n\n" +
                    "Các nút chơi sẽ tải scene: " + TargetSceneName + "\n" +
                    "Prefab: " + PrefabPath,
                    "Xong"
                );
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Green Field UI Builder",
                    "Không thể tạo menu.\n\n" + exception.Message +
                    "\n\nXem Console để biết chi tiết.",
                    "Đóng"
                );
            }
        }

        private static void ValidateAndConfigureAssets()
        {
            ConfigureSprite(BackgroundPath, false);
            ConfigureSprite(LogoPath, true);
            ConfigureSprite(ButtonStartPath, true);
            ConfigureSprite(ButtonContinuePath, true);
            ConfigureSprite(ButtonLoadPath, true);
            ConfigureSprite(ButtonSettingsPath, true);
            ConfigureSprite(ButtonExitPath, true);
        }

        private static void ConfigureSprite(string assetPath, bool hasAlpha)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException(
                    "Không tìm thấy ảnh menu tại: " + assetPath
                );
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = hasAlpha;
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static void EnsureBaoDemoInBuildSettings()
        {
            SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath);
            if (scene == null)
            {
                throw new FileNotFoundException(
                    "Không tìm thấy scene BaoDemo tại: " + TargetScenePath
                );
            }

            List<EditorBuildSettingsScene> scenes =
                new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;

            for (int i = 0; i < scenes.Count; i++)
            {
                if (!string.Equals(
                        scenes[i].path,
                        TargetScenePath,
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    continue;
                }

                scenes[i] = new EditorBuildSettingsScene(TargetScenePath, true);
                found = true;
                break;
            }

            if (!found)
            {
                scenes.Add(new EditorBuildSettingsScene(TargetScenePath, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[Green Field] BaoDemo đã sẵn sàng trong Build Settings.");
        }

        private static void SetControllerScene(
            GreenFieldMainMenuController controller,
            string sceneName
        )
        {
            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty sceneProperty =
                serializedController.FindProperty("farmSceneName");

            if (sceneProperty == null)
            {
                throw new MissingFieldException(
                    "GreenFieldMainMenuController thiếu field farmSceneName."
                );
            }

            sceneProperty.stringValue = sceneName;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static void SetControllerSettings(
            GreenFieldMainMenuController controller,
            SettingsUI settings
        )
        {
            SerializedObject serializedController = new SerializedObject(controller);

            SetObjectReference(serializedController, "settingsPanel", settings.Root);
            SetObjectReference(serializedController, "generalSettingsPage", settings.GeneralPage);
            SetObjectReference(serializedController, "audioSettingsPage", settings.AudioPage);
            SetObjectReference(serializedController, "graphicsSettingsPage", settings.GraphicsPage);
            SetObjectReference(serializedController, "controlsSettingsPage", settings.ControlsPage);
            SetObjectReference(serializedController, "generalTabButton", settings.GeneralTabButton);
            SetObjectReference(serializedController, "audioTabButton", settings.AudioTabButton);
            SetObjectReference(serializedController, "graphicsTabButton", settings.GraphicsTabButton);
            SetObjectReference(serializedController, "controlsTabButton", settings.ControlsTabButton);
            SetObjectReference(serializedController, "languageDropdown", settings.LanguageDropdown);
            SetObjectReference(serializedController, "masterVolumeSlider", settings.MasterVolumeSlider);
            SetObjectReference(serializedController, "masterVolumeValueText", settings.MasterVolumeValueText);
            SetObjectReference(serializedController, "muteToggle", settings.MuteToggle);
            SetObjectReference(serializedController, "resolutionDropdown", settings.ResolutionDropdown);
            SetObjectReference(serializedController, "qualityDropdown", settings.QualityDropdown);
            SetObjectReference(serializedController, "fullscreenToggle", settings.FullscreenToggle);
            SetObjectReference(serializedController, "vSyncToggle", settings.VSyncToggle);

            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static void SetObjectReference(
            SerializedObject serializedObject,
            string propertyName,
            UnityEngine.Object value
        )
        {
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new MissingFieldException(
                    "GreenFieldMainMenuController thiếu field " +
                    propertyName + "."
                );
            }

            property.objectReferenceValue = value;
        }

        private static GameObject CreateCanvasRoot()
        {
            GameObject root = new GameObject(
                RootName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            Undo.RegisterCreatedObjectUndo(root, "Build Green Field Main Menu");
            SetUiLayer(root);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            return root;
        }

        private static void CreateBackground(Transform parent, Sprite sprite)
        {
            Image image = CreateImage("Background", parent, sprite);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            image.preserveAspect = false;
            image.raycastTarget = false;
        }

        private static void CreateLogo(Transform parent, Sprite sprite)
        {
            Image image = CreateImage("Logo_GreenField", parent, sprite);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -24f);
            rect.sizeDelta = new Vector2(820f, 363f);
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private static Transform CreateButtonGroup(Transform parent)
        {
            GameObject group = CreateUiObject("ButtonGroup", parent);
            RectTransform rect = group.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -164f);
            rect.sizeDelta = new Vector2(650f, 660f);
            return group.transform;
        }

        private static Button CreateMenuButton(
            Transform parent,
            string objectName,
            Sprite sprite,
            float y
        )
        {
            GameObject buttonObject = CreateUiObject(objectName, parent);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(610f, 122f);

            Image image = buttonObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            return button;
        }

        private static SettingsUI CreateSettingsPanel(Transform parent)
        {
            GameObject overlay = CreateUiObject("SettingsPanel", parent);
            StretchToParent(overlay.GetComponent<RectTransform>());

            Image overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0.015f, 0.035f, 0.025f, 0.9f);
            overlayImage.raycastTarget = true;

            GameObject window = CreateUiObject("SettingsWindow", overlay.transform);
            SetCenteredRect(
                window.GetComponent<RectTransform>(),
                Vector2.zero,
                new Vector2(1120f, 720f)
            );

            Image windowImage = window.AddComponent<Image>();
            windowImage.color = new Color(0.16f, 0.105f, 0.055f, 0.99f);
            windowImage.raycastTarget = true;

            Outline outline = window.AddComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.68f, 0.22f, 0.95f);
            outline.effectDistance = new Vector2(4f, -4f);

            Shadow shadow = window.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.82f);
            shadow.effectDistance = new Vector2(12f, -12f);

            CreatePanelText(
                window.transform,
                "Title",
                "CÀI ĐẶT",
                new Vector2(0f, 310f),
                new Vector2(1000f, 60f),
                38,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.84f, 0.42f, 1f)
            );

            Image titleDivider = CreateSolidImage(
                "TitleDivider",
                window.transform,
                new Color(0.95f, 0.68f, 0.22f, 0.7f)
            );
            SetCenteredRect(
                titleDivider.rectTransform,
                new Vector2(0f, 275f),
                new Vector2(1010f, 3f)
            );

            Image sideDivider = CreateSolidImage(
                "SideDivider",
                window.transform,
                new Color(0.95f, 0.68f, 0.22f, 0.38f)
            );
            SetCenteredRect(
                sideDivider.rectTransform,
                new Vector2(-305f, -5f),
                new Vector2(3f, 525f)
            );

            Button generalTab = CreateSettingsTabButton(
                window.transform,
                "Tab_General",
                "CHUNG",
                new Vector2(-425f, 190f)
            );
            Button audioTab = CreateSettingsTabButton(
                window.transform,
                "Tab_Audio",
                "ÂM THANH",
                new Vector2(-425f, 105f)
            );
            Button graphicsTab = CreateSettingsTabButton(
                window.transform,
                "Tab_Graphics",
                "ĐỒ HỌA",
                new Vector2(-425f, 20f)
            );
            Button controlsTab = CreateSettingsTabButton(
                window.transform,
                "Tab_Controls",
                "ĐIỀU KHIỂN",
                new Vector2(-425f, -65f)
            );

            GameObject generalPage = CreateSettingsPage(
                window.transform,
                "GeneralPage"
            );
            GameObject audioPage = CreateSettingsPage(
                window.transform,
                "AudioPage"
            );
            GameObject graphicsPage = CreateSettingsPage(
                window.transform,
                "GraphicsPage"
            );
            GameObject controlsPage = CreateSettingsPage(
                window.transform,
                "ControlsPage"
            );

            Dropdown languageDropdown = BuildGeneralPage(generalPage.transform);
            Slider masterVolumeSlider;
            Text masterVolumeValueText;
            Toggle muteToggle;
            BuildAudioPage(
                audioPage.transform,
                out masterVolumeSlider,
                out masterVolumeValueText,
                out muteToggle
            );

            Dropdown resolutionDropdown;
            Dropdown qualityDropdown;
            Toggle fullscreenToggle;
            Toggle vSyncToggle;
            BuildGraphicsPage(
                graphicsPage.transform,
                out resolutionDropdown,
                out qualityDropdown,
                out fullscreenToggle,
                out vSyncToggle
            );
            BuildControlsPage(controlsPage.transform);

            Button resetButton = CreatePanelButton(
                window.transform,
                "Button_ResetSettings",
                "MẶC ĐỊNH",
                new Vector2(170f, -315f),
                new Vector2(240f, 62f),
                new Color(0.16f, 0.39f, 0.62f, 1f)
            );
            Button closeButton = CreatePanelButton(
                window.transform,
                "Button_CloseSettings",
                "LƯU & ĐÓNG",
                new Vector2(430f, -315f),
                new Vector2(240f, 62f),
                new Color(0.28f, 0.62f, 0.12f, 1f)
            );

            return new SettingsUI(
                overlay,
                generalPage,
                audioPage,
                graphicsPage,
                controlsPage,
                generalTab,
                audioTab,
                graphicsTab,
                controlsTab,
                languageDropdown,
                masterVolumeSlider,
                masterVolumeValueText,
                muteToggle,
                resolutionDropdown,
                qualityDropdown,
                fullscreenToggle,
                vSyncToggle,
                resetButton,
                closeButton
            );
        }

        private static GameObject CreateSettingsPage(
            Transform parent,
            string objectName
        )
        {
            GameObject page = CreateUiObject(objectName, parent);
            SetCenteredRect(
                page.GetComponent<RectTransform>(),
                new Vector2(125f, -5f),
                new Vector2(770f, 500f)
            );
            return page;
        }

        private static Button CreateSettingsTabButton(
            Transform parent,
            string objectName,
            string label,
            Vector2 position
        )
        {
            Button button = CreatePanelButton(
                parent,
                objectName,
                label,
                position,
                new Vector2(220f, 64f),
                new Color(0.24f, 0.19f, 0.12f, 1f)
            );

            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(0.95f, 0.58f, 0.1f, 1f);
            button.colors = colors;
            return button;
        }

        private static Dropdown BuildGeneralPage(Transform page)
        {
            CreatePageTitle(page, "CÀI ĐẶT CHUNG");

            CreatePanelText(
                page,
                "LanguageLabel",
                "NGÔN NGỮ",
                new Vector2(-225f, 115f),
                new Vector2(250f, 45f),
                24,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                Color.white
            );

            Dropdown languageDropdown = CreateDropdown(
                page,
                "LanguageDropdown",
                new Vector2(175f, 115f),
                new Vector2(330f, 52f),
                new List<string> { "Tiếng Việt", "English" }
            );

            CreateInfoBox(
                page,
                "GeneralInfo",
                "Nông Trại Green Field\n\n" +
                "Ngôn ngữ được lưu cho tài khoản trên máy này. " +
                "Nội dung tiếng Anh sẽ được áp dụng khi hệ thống bản địa hóa hoàn tất.",
                new Vector2(0f, -45f),
                new Vector2(660f, 190f)
            );

            return languageDropdown;
        }

        private static void BuildAudioPage(
            Transform page,
            out Slider masterVolumeSlider,
            out Text masterVolumeValueText,
            out Toggle muteToggle
        )
        {
            CreatePageTitle(page, "ÂM THANH");

            CreatePanelText(
                page,
                "MasterVolumeLabel",
                "ÂM LƯỢNG GAME",
                new Vector2(-230f, 100f),
                new Vector2(240f, 42f),
                24,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                Color.white
            );
            masterVolumeSlider = CreateAudioSlider(
                page,
                new Vector2(90f, 100f),
                new Vector2(330f, 48f)
            );
            masterVolumeValueText = CreatePanelText(
                page,
                "MasterVolumeValue",
                "80%",
                new Vector2(310f, 100f),
                new Vector2(90f, 42f),
                24,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(0.72f, 1f, 0.42f, 1f)
            );

            muteToggle = CreateLabeledToggle(
                page,
                "MuteToggle",
                "TẮT TIẾNG",
                new Vector2(-145f, 25f)
            );

            CreateInfoBox(
                page,
                "AudioInfo",
                "Âm lượng được lưu tự động và áp dụng cho toàn bộ game, " +
                "kể cả khi mở trực tiếp scene BaoDemo.",
                new Vector2(0f, -105f),
                new Vector2(660f, 115f)
            );
        }

        private static void BuildGraphicsPage(
            Transform page,
            out Dropdown resolutionDropdown,
            out Dropdown qualityDropdown,
            out Toggle fullscreenToggle,
            out Toggle vSyncToggle
        )
        {
            CreatePageTitle(page, "ĐỒ HỌA");

            CreatePanelText(
                page,
                "ResolutionLabel",
                "ĐỘ PHÂN GIẢI",
                new Vector2(-220f, 120f),
                new Vector2(260f, 42f),
                23,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                Color.white
            );
            resolutionDropdown = CreateDropdown(
                page,
                "ResolutionDropdown",
                new Vector2(175f, 120f),
                new Vector2(330f, 50f),
                new List<string> { "1920 x 1080" }
            );

            CreatePanelText(
                page,
                "QualityLabel",
                "CHẤT LƯỢNG",
                new Vector2(-220f, 50f),
                new Vector2(260f, 42f),
                23,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                Color.white
            );
            qualityDropdown = CreateDropdown(
                page,
                "QualityDropdown",
                new Vector2(175f, 50f),
                new Vector2(330f, 50f),
                new List<string> { "Trung bình" }
            );

            fullscreenToggle = CreateLabeledToggle(
                page,
                "FullscreenToggle",
                "TOÀN MÀN HÌNH",
                new Vector2(-145f, -35f)
            );
            vSyncToggle = CreateLabeledToggle(
                page,
                "VSyncToggle",
                "ĐỒNG BỘ KHUNG HÌNH (VSYNC)",
                new Vector2(-80f, -100f)
            );
        }

        private static void BuildControlsPage(Transform page)
        {
            CreatePageTitle(page, "ĐIỀU KHIỂN");
            CreateControlRow(page, "W A S D", "Di chuyển", 125f);
            CreateControlRow(page, "E", "Tương tác", 65f);
            CreateControlRow(page, "I", "Mở túi đồ", 5f);
            CreateControlRow(page, "ESC", "Đóng cửa sổ / Tạm dừng", -55f);

            CreateInfoBox(
                page,
                "ControlsInfo",
                "Các phím này phản ánh hệ thống điều khiển hiện tại. " +
                "Tính năng đổi phím sẽ được nối khi project có Input Actions dùng chung.",
                new Vector2(0f, -155f),
                new Vector2(660f, 95f)
            );
        }

        private static void CreatePageTitle(Transform page, string title)
        {
            CreatePanelText(
                page,
                "PageTitle",
                title,
                new Vector2(0f, 205f),
                new Vector2(680f, 48f),
                30,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(1f, 0.8f, 0.32f, 1f)
            );
        }

        private static void CreateControlRow(
            Transform page,
            string key,
            string description,
            float y
        )
        {
            CreatePanelText(
                page,
                "Key_" + key.Replace(" ", string.Empty),
                key,
                new Vector2(-225f, y),
                new Vector2(190f, 46f),
                22,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.84f, 0.42f, 1f)
            );
            CreatePanelText(
                page,
                "Action_" + key.Replace(" ", string.Empty),
                description,
                new Vector2(120f, y),
                new Vector2(430f, 46f),
                22,
                FontStyle.Normal,
                TextAnchor.MiddleLeft,
                Color.white
            );
        }

        private static void CreateInfoBox(
            Transform parent,
            string objectName,
            string text,
            Vector2 position,
            Vector2 size
        )
        {
            Image background = CreateSolidImage(
                objectName,
                parent,
                new Color(0.07f, 0.055f, 0.035f, 0.72f)
            );
            SetCenteredRect(background.rectTransform, position, size);

            Text label = CreatePanelText(
                background.transform,
                "Text",
                text,
                Vector2.zero,
                size,
                19,
                FontStyle.Normal,
                TextAnchor.MiddleCenter,
                new Color(0.88f, 0.84f, 0.74f, 1f)
            );
            StretchToParent(label.rectTransform);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }

#if false
        private static AudioSettingsUI CreateAudioSettingsPanel(
            Transform parent
        )
        {
            GameObject overlay = CreateUiObject("AudioSettingsPanel", parent);
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            StretchToParent(overlayRect);

            Image overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0.015f, 0.035f, 0.025f, 0.86f);
            overlayImage.raycastTarget = true;

            GameObject window = CreateUiObject("SettingsWindow", overlay.transform);
            RectTransform windowRect = window.GetComponent<RectTransform>();
            SetCenteredRect(windowRect, Vector2.zero, new Vector2(780f, 500f));

            Image windowImage = window.AddComponent<Image>();
            windowImage.color = new Color(0.16f, 0.105f, 0.055f, 0.98f);
            windowImage.raycastTarget = true;

            Outline windowOutline = window.AddComponent<Outline>();
            windowOutline.effectColor = new Color(0.95f, 0.68f, 0.22f, 0.9f);
            windowOutline.effectDistance = new Vector2(4f, -4f);

            Shadow windowShadow = window.AddComponent<Shadow>();
            windowShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            windowShadow.effectDistance = new Vector2(12f, -12f);

            CreatePanelText(
                window.transform,
                "Title",
                "CÀI ĐẶT ÂM THANH",
                new Vector2(0f, 195f),
                new Vector2(690f, 62f),
                36,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.84f, 0.42f, 1f)
            );

            Image divider = CreateSolidImage(
                "Divider",
                window.transform,
                new Color(0.95f, 0.68f, 0.22f, 0.7f)
            );
            SetCenteredRect(
                divider.rectTransform,
                new Vector2(0f, 155f),
                new Vector2(660f, 3f)
            );

            CreatePanelText(
                window.transform,
                "MasterVolumeLabel",
                "ÂM LƯỢNG GAME",
                new Vector2(-245f, 82f),
                new Vector2(230f, 42f),
                24,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                Color.white
            );

            Slider masterVolumeSlider = CreateAudioSlider(
                window.transform,
                new Vector2(35f, 82f),
                new Vector2(320f, 48f)
            );

            Text masterVolumeValueText = CreatePanelText(
                window.transform,
                "MasterVolumeValue",
                "80%",
                new Vector2(260f, 82f),
                new Vector2(110f, 42f),
                24,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(0.72f, 1f, 0.42f, 1f)
            );

            Toggle muteToggle = CreateMuteToggle(
                window.transform,
                new Vector2(-120f, 15f)
            );

            CreatePanelText(
                window.transform,
                "AudioHint",
                "Âm lượng được lưu tự động và áp dụng cho toàn bộ game.",
                new Vector2(0f, -55f),
                new Vector2(650f, 42f),
                20,
                FontStyle.Italic,
                TextAnchor.MiddleCenter,
                new Color(0.86f, 0.82f, 0.72f, 1f)
            );

            Button resetButton = CreatePanelButton(
                window.transform,
                "Button_ResetAudio",
                "MẶC ĐỊNH",
                new Vector2(-160f, -160f),
                new Vector2(250f, 70f),
                new Color(0.16f, 0.39f, 0.62f, 1f)
            );

            Button closeButton = CreatePanelButton(
                window.transform,
                "Button_CloseSettings",
                "ĐÓNG",
                new Vector2(160f, -160f),
                new Vector2(250f, 70f),
                new Color(0.28f, 0.62f, 0.12f, 1f)
            );

            return new AudioSettingsUI(
                overlay,
                masterVolumeSlider,
                masterVolumeValueText,
                muteToggle,
                resetButton,
                closeButton
            );
        }

#endif

        private static Slider CreateAudioSlider(
            Transform parent,
            Vector2 position,
            Vector2 size
        )
        {
            GameObject sliderObject = CreateUiObject("MasterVolumeSlider", parent);
            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            SetCenteredRect(sliderRect, position, size);

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;

            Image background = CreateSolidImage(
                "Background",
                sliderObject.transform,
                new Color(0.055f, 0.045f, 0.035f, 1f)
            );
            RectTransform backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.offsetMin = new Vector2(0f, -10f);
            backgroundRect.offsetMax = new Vector2(0f, 10f);

            Outline backgroundOutline =
                background.gameObject.AddComponent<Outline>();
            backgroundOutline.effectColor = new Color(0f, 0f, 0f, 0.75f);
            backgroundOutline.effectDistance = new Vector2(2f, -2f);

            GameObject fillAreaObject =
                CreateUiObject("Fill Area", sliderObject.transform);
            RectTransform fillArea =
                fillAreaObject.GetComponent<RectTransform>();
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.offsetMin = new Vector2(10f, -7f);
            fillArea.offsetMax = new Vector2(-10f, 7f);

            Image fill = CreateSolidImage(
                "Fill",
                fillAreaObject.transform,
                new Color(0.32f, 0.78f, 0.12f, 1f)
            );
            StretchToParent(fill.rectTransform);

            GameObject handleAreaObject =
                CreateUiObject("Handle Slide Area", sliderObject.transform);
            RectTransform handleArea =
                handleAreaObject.GetComponent<RectTransform>();
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(12f, 0f);
            handleArea.offsetMax = new Vector2(-12f, 0f);

            Image handle = CreateSolidImage(
                "Handle",
                handleAreaObject.transform,
                new Color(1f, 0.82f, 0.32f, 1f)
            );
            RectTransform handleRect = handle.rectTransform;
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.sizeDelta = new Vector2(30f, 38f);

            Outline handleOutline = handle.gameObject.AddComponent<Outline>();
            handleOutline.effectColor = new Color(0.16f, 0.08f, 0.02f, 1f);
            handleOutline.effectDistance = new Vector2(2f, -2f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle;

            ColorBlock colors = slider.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            slider.colors = colors;

            return slider;
        }

        private static Dropdown CreateDropdown(
            Transform parent,
            string objectName,
            Vector2 position,
            Vector2 size,
            List<string> options
        )
        {
            GameObject dropdownObject = CreateUiObject(objectName, parent);
            RectTransform dropdownRect =
                dropdownObject.GetComponent<RectTransform>();
            SetCenteredRect(dropdownRect, position, size);

            Image dropdownImage = dropdownObject.AddComponent<Image>();
            dropdownImage.color = new Color(0.94f, 0.82f, 0.59f, 1f);

            Outline dropdownOutline = dropdownObject.AddComponent<Outline>();
            dropdownOutline.effectColor = new Color(0.13f, 0.075f, 0.025f, 1f);
            dropdownOutline.effectDistance = new Vector2(2f, -2f);

            Dropdown dropdown = dropdownObject.AddComponent<Dropdown>();
            dropdown.targetGraphic = dropdownImage;

            Text captionText = CreatePanelText(
                dropdownObject.transform,
                "Label",
                options != null && options.Count > 0 ? options[0] : string.Empty,
                new Vector2(-12f, 0f),
                new Vector2(size.x - 68f, size.y),
                20,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.15f, 0.09f, 0.035f, 1f)
            );

            CreatePanelText(
                dropdownObject.transform,
                "Arrow",
                "▼",
                new Vector2((size.x * 0.5f) - 28f, 0f),
                new Vector2(42f, size.y),
                18,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(0.15f, 0.09f, 0.035f, 1f)
            );

            GameObject templateObject = CreateUiObject(
                "Template",
                dropdownObject.transform
            );
            RectTransform templateRect =
                templateObject.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, -4f);
            templateRect.sizeDelta = new Vector2(0f, 188f);

            Image templateImage = templateObject.AddComponent<Image>();
            templateImage.color = new Color(0.16f, 0.105f, 0.055f, 0.99f);

            Outline templateOutline = templateObject.AddComponent<Outline>();
            templateOutline.effectColor = new Color(0.95f, 0.68f, 0.22f, 0.8f);
            templateOutline.effectDistance = new Vector2(2f, -2f);

            ScrollRect scrollRect = templateObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            GameObject viewportObject = CreateUiObject(
                "Viewport",
                templateObject.transform
            );
            RectTransform viewportRect =
                viewportObject.GetComponent<RectTransform>();
            StretchToParent(viewportRect);
            viewportRect.offsetMin = new Vector2(4f, 4f);
            viewportRect.offsetMax = new Vector2(-4f, -4f);

            Image viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
            Mask viewportMask = viewportObject.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            GameObject contentObject = CreateUiObject(
                "Content",
                viewportObject.transform
            );
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 44f);

            GameObject itemObject = CreateUiObject("Item", contentObject.transform);
            RectTransform itemRect = itemObject.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.pivot = new Vector2(0.5f, 0.5f);
            itemRect.anchoredPosition = Vector2.zero;
            itemRect.sizeDelta = new Vector2(0f, 44f);

            Toggle itemToggle = itemObject.AddComponent<Toggle>();
            itemToggle.isOn = true;

            Image itemBackground = CreateSolidImage(
                "Item Background",
                itemObject.transform,
                new Color(0.31f, 0.22f, 0.12f, 0.92f)
            );
            StretchToParent(itemBackground.rectTransform);

            Image itemCheckmark = CreateSolidImage(
                "Item Checkmark",
                itemObject.transform,
                new Color(0.38f, 0.84f, 0.15f, 1f)
            );
            RectTransform checkmarkRect = itemCheckmark.rectTransform;
            checkmarkRect.anchorMin = new Vector2(0f, 0.5f);
            checkmarkRect.anchorMax = new Vector2(0f, 0.5f);
            checkmarkRect.anchoredPosition = new Vector2(19f, 0f);
            checkmarkRect.sizeDelta = new Vector2(20f, 20f);

            Text itemLabel = CreatePanelText(
                itemObject.transform,
                "Item Label",
                options != null && options.Count > 0 ? options[0] : string.Empty,
                new Vector2(24f, 0f),
                new Vector2(size.x - 58f, 44f),
                19,
                FontStyle.Normal,
                TextAnchor.MiddleLeft,
                Color.white
            );

            itemToggle.targetGraphic = itemBackground;
            itemToggle.graphic = itemCheckmark;
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            dropdown.template = templateRect;
            dropdown.captionText = captionText;
            dropdown.itemText = itemLabel;
            dropdown.options.Clear();
            if (options != null)
            {
                dropdown.AddOptions(options);
            }
            dropdown.value = 0;
            dropdown.RefreshShownValue();

            templateObject.SetActive(false);
            return dropdown;
        }

        private static Toggle CreateLabeledToggle(
            Transform parent,
            string objectName,
            string label,
            Vector2 position
        )
        {
            GameObject toggleObject = CreateUiObject(objectName, parent);
            RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
            SetCenteredRect(toggleRect, position, new Vector2(430f, 52f));

            Toggle toggle = toggleObject.AddComponent<Toggle>();
            toggle.isOn = false;

            Image box = CreateSolidImage(
                "Background",
                toggleObject.transform,
                new Color(0.055f, 0.045f, 0.035f, 1f)
            );
            RectTransform boxRect = box.rectTransform;
            boxRect.anchorMin = new Vector2(0f, 0.5f);
            boxRect.anchorMax = new Vector2(0f, 0.5f);
            boxRect.anchoredPosition = new Vector2(22f, 0f);
            boxRect.sizeDelta = new Vector2(38f, 38f);

            Outline boxOutline = box.gameObject.AddComponent<Outline>();
            boxOutline.effectColor = new Color(0f, 0f, 0f, 0.75f);
            boxOutline.effectDistance = new Vector2(2f, -2f);

            Image checkmark = CreateSolidImage(
                "Checkmark",
                box.transform,
                new Color(0.36f, 0.86f, 0.14f, 1f)
            );
            RectTransform checkmarkRect = checkmark.rectTransform;
            checkmarkRect.anchorMin = Vector2.zero;
            checkmarkRect.anchorMax = Vector2.one;
            checkmarkRect.offsetMin = new Vector2(7f, 7f);
            checkmarkRect.offsetMax = new Vector2(-7f, -7f);

            CreatePanelText(
                toggleObject.transform,
                "Label",
                label,
                new Vector2(75f, 0f),
                new Vector2(330f, 48f),
                22,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                Color.white
            );

            toggle.targetGraphic = box;
            toggle.graphic = checkmark;
            return toggle;
        }

        private static Toggle CreateMuteToggle(
            Transform parent,
            Vector2 position
        )
        {
            GameObject toggleObject = CreateUiObject("MuteToggle", parent);
            RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
            SetCenteredRect(
                toggleRect,
                position,
                new Vector2(300f, 52f)
            );

            Toggle toggle = toggleObject.AddComponent<Toggle>();
            toggle.isOn = false;

            Image box = CreateSolidImage(
                "Background",
                toggleObject.transform,
                new Color(0.055f, 0.045f, 0.035f, 1f)
            );
            RectTransform boxRect = box.rectTransform;
            boxRect.anchorMin = new Vector2(0f, 0.5f);
            boxRect.anchorMax = new Vector2(0f, 0.5f);
            boxRect.anchoredPosition = new Vector2(22f, 0f);
            boxRect.sizeDelta = new Vector2(38f, 38f);

            Image checkmark = CreateSolidImage(
                "Checkmark",
                box.transform,
                new Color(0.36f, 0.86f, 0.14f, 1f)
            );
            RectTransform checkmarkRect = checkmark.rectTransform;
            checkmarkRect.anchorMin = Vector2.zero;
            checkmarkRect.anchorMax = Vector2.one;
            checkmarkRect.offsetMin = new Vector2(7f, 7f);
            checkmarkRect.offsetMax = new Vector2(-7f, -7f);

            CreatePanelText(
                toggleObject.transform,
                "Label",
                "TẮT TIẾNG",
                new Vector2(105f, 0f),
                new Vector2(190f, 48f),
                23,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                Color.white
            );

            toggle.targetGraphic = box;
            toggle.graphic = checkmark;

            return toggle;
        }

        private static Button CreatePanelButton(
            Transform parent,
            string objectName,
            string text,
            Vector2 position,
            Vector2 size,
            Color color
        )
        {
            GameObject buttonObject = CreateUiObject(objectName, parent);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            SetCenteredRect(buttonRect, position, size);

            Image image = buttonObject.AddComponent<Image>();
            image.color = color;

            Outline outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.05f, 0.025f, 0.01f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            button.colors = colors;

            Text label = CreatePanelText(
                buttonObject.transform,
                "Label",
                text,
                Vector2.zero,
                size,
                25,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.white
            );
            StretchToParent(label.rectTransform);

            return button;
        }

        private static Text CreatePanelText(
            Transform parent,
            string objectName,
            string text,
            Vector2 position,
            Vector2 size,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Color color
        )
        {
            GameObject textObject = CreateUiObject(objectName, parent);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            SetCenteredRect(textRect, position, size);

            Text label = textObject.AddComponent<Text>();
            label.text = text;
            label.font = LoadBuiltInFont();
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;

            return label;
        }

        private static Image CreateSolidImage(
            string objectName,
            Transform parent,
            Color color
        )
        {
            GameObject imageObject = CreateUiObject(objectName, parent);
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static void SetCenteredRect(
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

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void CreateVersionLabel(Transform parent)
        {
            GameObject labelObject = CreateUiObject("Version", parent);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(26f, 20f);
            rect.sizeDelta = new Vector2(220f, 50f);

            Text label = labelObject.AddComponent<Text>();
            label.text = "v1.0.0";
            label.font = LoadBuiltInFont();
            label.fontSize = 28;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.LowerLeft;
            label.color = Color.white;
            label.raycastTarget = false;

            Shadow shadow = labelObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }

        private static Font LoadBuiltInFont()
        {
#if UNITY_2022_2_OR_NEWER
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }

        private static Image CreateImage(
            string objectName,
            Transform parent,
            Sprite sprite
        )
        {
            GameObject imageObject = CreateUiObject(objectName, parent);
            Image image = imageObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            return image;
        }

        private static GameObject CreateUiObject(string objectName, Transform parent)
        {
            GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(uiObject, "Build Green Field Main Menu");
            uiObject.transform.SetParent(parent, false);
            SetUiLayer(uiObject);
            return uiObject;
        }

        private static Sprite LoadSprite(string assetPath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                throw new FileNotFoundException(
                    "Không thể load Sprite tại: " + assetPath
                );
            }

            return sprite;
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            Type inputSystemModuleType = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem"
            );

            GameObject eventSystem = inputSystemModuleType != null
                ? new GameObject("EventSystem", typeof(EventSystem), inputSystemModuleType)
                : new GameObject(
                    "EventSystem",
                    typeof(EventSystem),
                    typeof(StandaloneInputModule)
                );

            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }

        private static void SavePrefab(GameObject root)
        {
            EnsureAssetFolder("Assets/Bao/Prefabs");
            PrefabUtility.SaveAsPrefabAssetAndConnect(
                root,
                PrefabPath,
                InteractionMode.UserAction
            );
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
            string[] parts = assetFolder.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void SetUiLayer(GameObject target)
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                target.layer = uiLayer;
            }
        }
    }
}
