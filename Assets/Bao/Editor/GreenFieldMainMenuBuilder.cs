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
