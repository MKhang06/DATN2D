using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GreenField.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-9000)]
    public sealed class GreenFieldGameplayHUDController : MonoBehaviour
    {
        private const string GameplaySceneName = "BaoDemo";
        private const string PanelTexturePath = "GreenFieldHUD/hud_panel_frame_v2";
        private const string IconTexturePath = "GreenFieldHUD/hud_icons_v2";

        private static readonly Color HealthColor = new Color(0.93f, 0.20f, 0.16f, 1f);
        private static readonly Color EnergyColor = new Color(1f, 0.70f, 0.12f, 1f);
        private static readonly Color StaminaColor = new Color(0.34f, 0.82f, 0.18f, 1f);
        private static readonly Color XpColor = new Color(0.48f, 0.38f, 1f, 1f);
        private static readonly Color TextColor = new Color(1f, 0.96f, 0.84f, 1f);
        private static readonly Color MutedTextColor = new Color(0.82f, 0.75f, 0.62f, 1f);
        private static readonly Color GoldTextColor = new Color(1f, 0.79f, 0.28f, 1f);
        private static readonly Color BarBackgroundColor = new Color(0.055f, 0.035f, 0.025f, 0.94f);

        private readonly Sprite[] iconSprites = new Sprite[6];

        private RectTransform safeAreaRect;
        private RectTransform statsPanelRect;
        private RectTransform timePanelRect;
        private Sprite panelSprite;
        private Sprite solidSprite;
        private Texture2D solidTexture;
        private Font hudFont;
        private PlayerStats playerStats;
        private bool ownsIconSprites;

        private Image healthFill;
        private Image energyFill;
        private Image staminaFill;
        private Image xpFill;
        private Image dayProgressFill;

        private Text healthValueText;
        private Text energyValueText;
        private Text staminaValueText;
        private Text moneyText;
        private Text levelText;
        private Text xpValueText;
        private Text timeText;
        private Text dayText;
        private Text seasonText;
        private Text weatherText;

        private float displayedHealth = 1f;
        private float displayedEnergy = 1f;
        private float displayedStamina = 1f;
        private float displayedXp;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private Rect lastSafeArea;
        private float nextStatsSearchTime;
        private int lastHealth = int.MinValue;
        private int lastMaxHealth = int.MinValue;
        private int lastEnergy = int.MinValue;
        private int lastMaxEnergy = int.MinValue;
        private int lastStamina = int.MinValue;
        private int lastMaxStamina = int.MinValue;
        private int lastMoney = int.MinValue;
        private int lastLevel = int.MinValue;
        private int lastXp = int.MinValue;
        private int lastRequiredXp = int.MinValue;
        private int lastDay = int.MinValue;
        private int lastHour = int.MinValue;
        private int lastMinute = int.MinValue;
        private GameTimeManager.Season lastSeason = (GameTimeManager.Season)(-1);
        private WeatherManager.WeatherType lastWeather = (WeatherManager.WeatherType)(-1);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneHook()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != GameplaySceneName ||
                Object.FindFirstObjectByType<GreenFieldGameplayHUDController>() != null)
            {
                return;
            }

            GameObject hudObject = new GameObject("GreenField_GameplayHUD");
            SceneManager.MoveGameObjectToScene(hudObject, scene);
            hudObject.AddComponent<GreenFieldGameplayHUDController>();
        }

        private void Start()
        {
            DisableLegacyHud(SceneManager.GetActiveScene());
            LoadArtAssets();
            BuildHud();
            FindPlayerStats();
            SnapDisplayedValues();
            RefreshHud(true);
            ApplySafeArea(true);
        }

        private void Update()
        {
            if (playerStats == null && Time.unscaledTime >= nextStatsSearchTime)
            {
                FindPlayerStats();
                nextStatsSearchTime = Time.unscaledTime + 1f;
            }

            ApplySafeArea(false);
            RefreshHud(false);
        }

        private void OnDestroy()
        {
            if (panelSprite != null)
                Destroy(panelSprite);

            if (solidSprite != null)
                Destroy(solidSprite);

            if (solidTexture != null)
                Destroy(solidTexture);

            if (!ownsIconSprites)
                return;

            foreach (Sprite iconSprite in iconSprites)
            {
                if (iconSprite != null)
                    Destroy(iconSprite);
            }
        }

        private void LoadArtAssets()
        {
            solidTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "GreenField HUD Solid Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            solidTexture.SetPixel(0, 0, Color.white);
            solidTexture.Apply(false, false);
            solidSprite = Sprite.Create(
                solidTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                100f
            );
            solidSprite.name = "GreenField HUD Solid Sprite";

            Sprite[] importedPanelSprites = Resources.LoadAll<Sprite>(PanelTexturePath);
            if (importedPanelSprites.Length > 0)
            {
                Sprite importedPanel = importedPanelSprites[0];
                Texture2D panelTexture = importedPanel.texture;
                panelTexture.filterMode = FilterMode.Bilinear;
                panelSprite = Sprite.Create(
                    panelTexture,
                    importedPanel.rect,
                    new Vector2(0.5f, 0.5f),
                    300f,
                    0,
                    SpriteMeshType.FullRect,
                    new Vector4(150f, 150f, 150f, 150f)
                );
                panelSprite.name = "GreenField HUD Panel Runtime Sprite";
            }

            Sprite[] importedIcons = Resources.LoadAll<Sprite>(IconTexturePath);
            if (importedIcons.Length >= iconSprites.Length)
            {
                System.Array.Sort(
                    importedIcons,
                    (left, right) => string.CompareOrdinal(left.name, right.name)
                );
                for (int i = 0; i < iconSprites.Length; i++)
                    iconSprites[i] = importedIcons[i];

                return;
            }

            Texture2D iconTexture = Resources.Load<Texture2D>(IconTexturePath);
            if (iconTexture == null)
                return;

            ownsIconSprites = true;
            iconTexture.filterMode = FilterMode.Bilinear;
            float cellWidth = Mathf.Floor(iconTexture.width / 3f);
            float iconSize = Mathf.Floor(Mathf.Min(cellWidth, iconTexture.height * 0.335f));
            float bottomRowY = Mathf.Round(iconTexture.height * 0.15f);
            float topRowY = Mathf.Round(iconTexture.height * 0.55f);

            for (int row = 0; row < 2; row++)
            {
                float y = row == 0 ? topRowY : bottomRowY;
                for (int column = 0; column < 3; column++)
                {
                    int index = row * 3 + column;
                    Rect iconRect = new Rect(column * cellWidth, y, iconSize, iconSize);
                    iconSprites[index] = Sprite.Create(
                        iconTexture,
                        iconRect,
                        new Vector2(0.5f, 0.5f),
                        100f,
                        0,
                        SpriteMeshType.FullRect
                    );
                    iconSprites[index].name = "GreenField HUD Icon " + index;
                }
            }
        }

        private void BuildHud()
        {
            hudFont = LoadBuiltInFont();

            GameObject canvasObject = new GameObject(
                "HUD Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler)
            );
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            safeAreaRect = CreateUIObject("Safe Area", canvasObject.transform)
                .GetComponent<RectTransform>();
            Stretch(safeAreaRect);

            CanvasGroup canvasGroup = safeAreaRect.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            BuildStatsPanel();
            BuildTimePanel();
        }

        private void BuildStatsPanel()
        {
            GameObject panel = CreatePanel(safeAreaRect, "Status Panel", new Vector2(600f, 350f));
            statsPanelRect = panel.GetComponent<RectTransform>();
            AnchorTopLeft(statsPanelRect, new Vector2(22f, -22f));

            CreateStatRow(panel.transform, "Health", 0, "M\u00C1U", 54f, HealthColor,
                out healthFill, out healthValueText);
            CreateStatRow(panel.transform, "Energy", 1, "N\u0102NG L\u01AF\u1EE2NG", 120f,
                EnergyColor, out energyFill, out energyValueText);
            CreateStatRow(panel.transform, "Stamina", 2, "TH\u1EC2 L\u1EF0C", 186f,
                StaminaColor, out staminaFill, out staminaValueText);

            Image divider = CreateImage(
                panel.transform,
                "Divider",
                new Color(0.76f, 0.55f, 0.24f, 0.52f)
            );
            SetTopLeft(divider.rectTransform, 54f, -245f, 492f, 2f);

            CreateIcon(panel.transform, "Money Icon", 3, new Vector2(76f, -287f), 48f);
            moneyText = CreateText(
                panel.transform,
                "Money",
                "0 G",
                new Vector2(105f, -264f),
                new Vector2(170f, 48f),
                25,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                GoldTextColor
            );

            CreateIcon(panel.transform, "Level Icon", 4, new Vector2(300f, -286f), 50f);
            levelText = CreateText(
                panel.transform,
                "Level",
                "C\u1EA4P 1",
                new Vector2(330f, -257f),
                new Vector2(210f, 34f),
                22,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                TextColor
            );

            xpFill = CreateBar(
                panel.transform,
                "XP Bar",
                new Vector2(330f, -300f),
                new Vector2(210f, 20f),
                XpColor
            );
            xpValueText = CreateText(
                panel.transform,
                "XP Value",
                "0 / 100",
                new Vector2(330f, -304f),
                new Vector2(210f, 26f),
                14,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.white
            );
        }

        private void BuildTimePanel()
        {
            GameObject panel = CreatePanel(
                safeAreaRect,
                "Time and Day Panel",
                new Vector2(455f, 260f)
            );
            timePanelRect = panel.GetComponent<RectTransform>();
            AnchorTopRight(timePanelRect, new Vector2(-22f, -22f));

            CreateIcon(panel.transform, "Time Icon", 5, new Vector2(88f, -119f), 108f);
            timeText = CreateText(
                panel.transform,
                "Time",
                "06:00",
                new Vector2(155f, -52f),
                new Vector2(235f, 64f),
                43,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                GoldTextColor
            );
            dayText = CreateText(
                panel.transform,
                "Day",
                "NG\u00C0Y 01",
                new Vector2(157f, -112f),
                new Vector2(230f, 38f),
                25,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                TextColor
            );
            seasonText = CreateText(
                panel.transform,
                "Season",
                "M\u00D9A XU\u00C2N",
                new Vector2(157f, -151f),
                new Vector2(230f, 34f),
                20,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.54f, 0.92f, 0.34f, 1f)
            );
            weatherText = CreateText(
                panel.transform,
                "Weather",
                "N\u1EAENG",
                new Vector2(157f, -185f),
                new Vector2(230f, 34f),
                19,
                FontStyle.Normal,
                TextAnchor.MiddleLeft,
                MutedTextColor
            );
            dayProgressFill = CreateBar(
                panel.transform,
                "Day Progress",
                new Vector2(62f, -224f),
                new Vector2(330f, 11f),
                new Color(1f, 0.63f, 0.12f, 1f)
            );
        }

        private void CreateStatRow(
            Transform parent,
            string objectName,
            int iconIndex,
            string label,
            float top,
            Color fillColor,
            out Image fill,
            out Text valueText
        )
        {
            CreateIcon(parent, objectName + " Icon", iconIndex,
                new Vector2(76f, -(top + 27f)), 52f);
            CreateText(
                parent,
                objectName + " Label",
                label,
                new Vector2(112f, -top),
                new Vector2(215f, 25f),
                17,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                MutedTextColor
            );
            fill = CreateBar(
                parent,
                objectName + " Bar",
                new Vector2(112f, -(top + 29f)),
                new Vector2(430f, 25f),
                fillColor
            );
            valueText = CreateText(
                parent,
                objectName + " Value",
                "100 / 100",
                new Vector2(112f, -(top + 34f)),
                new Vector2(430f, 34f),
                15,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.white
            );
        }

        private void RefreshHud(bool immediate)
        {
            if (playerStats != null)
            {
                float healthTarget = SafeRatio(playerStats.currentHealth, playerStats.maxHealth);
                float energyTarget = SafeRatio(playerStats.currentEnergy, playerStats.maxEnergy);
                float staminaTarget = SafeRatio(playerStats.currentStamina, playerStats.maxStamina);
                float xpTarget = SafeRatio(playerStats.CurrentXP, playerStats.RequiredXP);

                float speed = immediate ? 1000f : 3.6f * Time.unscaledDeltaTime;
                displayedHealth = Mathf.MoveTowards(displayedHealth, healthTarget, speed);
                displayedEnergy = Mathf.MoveTowards(displayedEnergy, energyTarget, speed);
                displayedStamina = Mathf.MoveTowards(displayedStamina, staminaTarget, speed);
                displayedXp = Mathf.MoveTowards(displayedXp, xpTarget, speed);

                healthFill.fillAmount = displayedHealth;
                energyFill.fillAmount = displayedEnergy;
                staminaFill.fillAmount = displayedStamina;
                xpFill.fillAmount = displayedXp;

                RefreshPlayerText(immediate);
            }

            GameTimeManager gameTime = GameTimeManager.Instance;
            if (gameTime != null)
            {
                if (immediate || gameTime.CurrentHour != lastHour || gameTime.CurrentMinute != lastMinute)
                {
                    lastHour = gameTime.CurrentHour;
                    lastMinute = gameTime.CurrentMinute;
                    timeText.text = lastHour.ToString("00") + ":" + lastMinute.ToString("00");
                }

                if (immediate || gameTime.CurrentDay != lastDay)
                {
                    lastDay = gameTime.CurrentDay;
                    dayText.text = "NG\u00C0Y " + lastDay.ToString("00");
                }

                if (immediate || gameTime.CurrentSeason != lastSeason)
                {
                    lastSeason = gameTime.CurrentSeason;
                    seasonText.text = GetSeasonLabel(lastSeason);
                }

                float currentMinute = gameTime.CurrentHour * 60f + gameTime.CurrentMinute;
                dayProgressFill.fillAmount = Mathf.Clamp01(currentMinute / 1440f);
            }

            WeatherManager weather = WeatherManager.Instance;
            if (weather != null && (immediate || weather.CurrentWeather != lastWeather))
            {
                lastWeather = weather.CurrentWeather;
                weatherText.text = GetWeatherLabel(lastWeather);
            }
        }

        private void RefreshPlayerText(bool force)
        {
            int health = Mathf.CeilToInt(Mathf.Max(0f, playerStats.currentHealth));
            int maxHealth = Mathf.CeilToInt(Mathf.Max(0f, playerStats.maxHealth));
            int energy = Mathf.CeilToInt(Mathf.Max(0f, playerStats.currentEnergy));
            int maxEnergy = Mathf.CeilToInt(Mathf.Max(0f, playerStats.maxEnergy));
            int stamina = Mathf.CeilToInt(Mathf.Max(0f, playerStats.currentStamina));
            int maxStamina = Mathf.CeilToInt(Mathf.Max(0f, playerStats.maxStamina));
            int money = Mathf.Max(0, playerStats.Money);
            int level = Mathf.Max(1, playerStats.Level);
            int xp = Mathf.Max(0, playerStats.CurrentXP);
            int requiredXp = Mathf.Max(1, playerStats.RequiredXP);

            if (force || health != lastHealth || maxHealth != lastMaxHealth)
            {
                lastHealth = health;
                lastMaxHealth = maxHealth;
                healthValueText.text = FormatStat(health, maxHealth);
            }

            if (force || energy != lastEnergy || maxEnergy != lastMaxEnergy)
            {
                lastEnergy = energy;
                lastMaxEnergy = maxEnergy;
                energyValueText.text = FormatStat(energy, maxEnergy);
            }

            if (force || stamina != lastStamina || maxStamina != lastMaxStamina)
            {
                lastStamina = stamina;
                lastMaxStamina = maxStamina;
                staminaValueText.text = FormatStat(stamina, maxStamina);
            }

            if (force || money != lastMoney)
            {
                lastMoney = money;
                moneyText.text = money.ToString("N0", CultureInfo.InvariantCulture) + " G";
            }

            if (force || level != lastLevel)
            {
                lastLevel = level;
                levelText.text = "C\u1EA4P " + level;
            }

            if (force || xp != lastXp || requiredXp != lastRequiredXp)
            {
                lastXp = xp;
                lastRequiredXp = requiredXp;
                xpValueText.text = FormatStat(xp, requiredXp);
            }
        }

        private void FindPlayerStats()
        {
            playerStats = Object.FindFirstObjectByType<PlayerStats>();
            if (playerStats != null)
                SnapDisplayedValues();
        }

        private void SnapDisplayedValues()
        {
            if (playerStats == null)
                return;

            displayedHealth = SafeRatio(playerStats.currentHealth, playerStats.maxHealth);
            displayedEnergy = SafeRatio(playerStats.currentEnergy, playerStats.maxEnergy);
            displayedStamina = SafeRatio(playerStats.currentStamina, playerStats.maxStamina);
            displayedXp = SafeRatio(playerStats.CurrentXP, playerStats.RequiredXP);
        }

        private void ApplySafeArea(bool force)
        {
            if (safeAreaRect == null)
                return;

            Rect safeArea = Screen.safeArea;
            if (!force && Screen.width == lastScreenWidth && Screen.height == lastScreenHeight &&
                safeArea == lastSafeArea)
            {
                return;
            }

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastSafeArea = safeArea;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Mathf.Max(1f, Screen.width);
            anchorMin.y /= Mathf.Max(1f, Screen.height);
            anchorMax.x /= Mathf.Max(1f, Screen.width);
            anchorMax.y /= Mathf.Max(1f, Screen.height);
            safeAreaRect.anchorMin = anchorMin;
            safeAreaRect.anchorMax = anchorMax;
            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;
        }

        private static void DisableLegacyHud(Scene scene)
        {
            string[] legacyRootNames =
            {
                "Health,Stamina,Energy Panel",
                "Money,Level Panel",
                "Weather/Time",
                "TimeWeatherPanel"
            };

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform candidate in transforms)
                {
                    foreach (string legacyName in legacyRootNames)
                    {
                        if (candidate.name == legacyName)
                        {
                            candidate.gameObject.SetActive(false);
                            break;
                        }
                    }
                }
            }

            DisableLegacyComponents<PlayerStatsUI>(scene);
            DisableLegacyComponents<MoneyUI>(scene);
            DisableLegacyComponents<XPUI>(scene);
            DisableLegacyComponents<TimeSeasonWeatherUI>(scene);
        }

        private static void DisableLegacyComponents<T>(Scene scene) where T : Behaviour
        {
            T[] components = Resources.FindObjectsOfTypeAll<T>();
            foreach (T component in components)
            {
                if (component != null && component.gameObject.scene == scene)
                    component.enabled = false;
            }
        }

        private GameObject CreatePanel(Transform parent, string objectName, Vector2 size)
        {
            GameObject panel = CreateUIObject(objectName, parent);
            panel.GetComponent<RectTransform>().sizeDelta = size;

            Image image = panel.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = panelSprite != null
                ? new Color(1f, 1f, 1f, 0.98f)
                : new Color(0.13f, 0.075f, 0.035f, 0.97f);
            if (panelSprite != null)
            {
                image.sprite = panelSprite;
                image.type = Image.Type.Sliced;
            }

            Shadow shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.64f);
            shadow.effectDistance = new Vector2(6f, -7f);
            shadow.useGraphicAlpha = true;
            return panel;
        }

        private Image CreateBar(
            Transform parent,
            string objectName,
            Vector2 position,
            Vector2 size,
            Color fillColor
        )
        {
            Image background = CreateImage(parent, objectName + " Background", BarBackgroundColor);
            SetTopLeft(background.rectTransform, position.x, position.y, size.x, size.y);

            Outline outline = background.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.76f, 0.55f, 0.25f, 0.62f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;

            Image fill = CreateImage(background.transform, objectName + " Fill", fillColor);
            fill.sprite = solidSprite;
            Stretch(fill.rectTransform);
            fill.rectTransform.offsetMin = new Vector2(3f, 3f);
            fill.rectTransform.offsetMax = new Vector2(-3f, -3f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            return fill;
        }

        private void CreateIcon(
            Transform parent,
            string objectName,
            int iconIndex,
            Vector2 center,
            float size
        )
        {
            if (iconIndex < 0 || iconIndex >= iconSprites.Length || iconSprites[iconIndex] == null)
                return;

            Image icon = CreateImage(parent, objectName, Color.white);
            RectTransform rect = icon.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            rect.sizeDelta = new Vector2(size, size);
            icon.sprite = iconSprites[iconIndex];
            icon.preserveAspect = true;
        }

        private Text CreateText(
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
            Text text = textObject.AddComponent<Text>();
            text.raycastTarget = false;
            text.text = value;
            text.font = hudFont;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            SetTopLeft(text.rectTransform, position.x, position.y, size.x, size.y);

            Shadow shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.74f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            shadow.useGraphicAlpha = true;
            return text;
        }

        private static Image CreateImage(Transform parent, string objectName, Color color)
        {
            GameObject imageObject = CreateUIObject(objectName, parent);
            Image image = imageObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = color;
            return image;
        }

        private static GameObject CreateUIObject(string objectName, Transform parent)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.layer = 5;
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void AnchorTopLeft(RectTransform rect, Vector2 position)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
        }

        private static void AnchorTopRight(RectTransform rect, Vector2 position)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = position;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Font LoadBuiltInFont()
        {
#if UNITY_2022_2_OR_NEWER
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }

        private static float SafeRatio(float current, float maximum)
        {
            return maximum <= 0f ? 0f : Mathf.Clamp01(current / maximum);
        }

        private static string FormatStat(int current, int maximum)
        {
            return current + " / " + maximum;
        }

        private static string GetSeasonLabel(GameTimeManager.Season season)
        {
            switch (season)
            {
                case GameTimeManager.Season.Spring:
                    return "M\u00D9A XU\u00C2N";
                case GameTimeManager.Season.Summer:
                    return "M\u00D9A H\u00C8";
                case GameTimeManager.Season.Autumn:
                    return "M\u00D9A THU";
                case GameTimeManager.Season.Winter:
                    return "M\u00D9A \u0110\u00D4NG";
                default:
                    return string.Empty;
            }
        }

        private static string GetWeatherLabel(WeatherManager.WeatherType weather)
        {
            switch (weather)
            {
                case WeatherManager.WeatherType.Sunny:
                    return "N\u1EAENG";
                case WeatherManager.WeatherType.Cloudy:
                    return "NHI\u1EC0U M\u00C2Y";
                case WeatherManager.WeatherType.Rainy:
                    return "M\u01AFA";
                case WeatherManager.WeatherType.Stormy:
                    return "B\u00C3O";
                case WeatherManager.WeatherType.Snowy:
                    return "TUY\u1EBET";
                default:
                    return string.Empty;
            }
        }
    }
}
