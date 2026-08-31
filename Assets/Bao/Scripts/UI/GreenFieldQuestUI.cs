using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GreenField.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-8500)]
    public sealed class GreenFieldQuestUI : MonoBehaviour
    {
        private const string GameplayScene = "MAPMAIN";
        private static readonly Color Ink = Hex("#4B3527");
        private static readonly Color Cream = Hex("#FFF4D6");
        private static readonly Color Paper = Hex("#F8E6B7");
        private static readonly Color Green = Hex("#5F9E55");
        private static readonly Color GreenDark = Hex("#376F43");
        private static readonly Color Gold = Hex("#F2B84B");
        private static readonly Color Coral = Hex("#E77755");
        private static readonly Color Blue = Hex("#4D9BC2");

        private readonly Quest[] quests =
        {
            new Quest("Gieo mầm ngày mới", "Trồng 5 cây trên ruộng", 5, 60, QuestKind.Plant, Green),
            new Quest("Mùa màng tươi tốt", "Thu hoạch 5 nông sản", 5, 80, QuestKind.Harvest, Coral),
            new Quest("Bữa cá bên hồ", "Câu được 3 chú cá", 3, 120, QuestKind.CatchFish, Blue),
            new Quest("Phiên chợ ven sông", "Bán 3 con cá", 3, 100, QuestKind.SellFish, Gold)
        };

        private RectTransform safeArea;
        private GameObject modalRoot;
        private GameObject window;
        private TMP_Text summaryText;
        private TMP_Text[] progressTexts;
        private Image[] progressFills;
        private Button[] claimButtons;
        private TMP_Text[] claimLabels;
        private PlayerStats stats;
        private TMP_FontAsset font;
        private int plantedCount;
        private int harvestedCount;
        private int caughtFishCount;
        private int soldFishCount;
        private int lastWidth;
        private int lastHeight;
        private Rect lastSafeArea;

        private enum QuestKind { Plant, Harvest, CatchFish, SellFish }

        [Serializable]
        private sealed class Quest
        {
            public readonly string title;
            public readonly string description;
            public readonly int target;
            public readonly int reward;
            public readonly QuestKind kind;
            public readonly Color color;
            public int progress;
            public bool claimed;

            public Quest(string title, string description, int target, int reward, QuestKind kind, Color color)
            {
                this.title = title;
                this.description = description;
                this.target = target;
                this.reward = reward;
                this.kind = kind;
                this.color = color;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != GameplayScene || FindFirstObjectByType<GreenFieldQuestUI>() != null)
                return;

            GameObject root = new GameObject("GreenField_QuestUI");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<GreenFieldQuestUI>();
        }

        private void Start()
        {
            font = TMP_Settings.defaultFontAsset;
            if (font == null)
                font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            Build();
            FindSources();
            ApplySafeArea(true);
            Refresh(true);
        }

        private void OnEnable()
        {
            GreenFieldQuestEvents.CropsPlanted += OnCropsPlanted;
            GreenFieldQuestEvents.CropsHarvested += OnCropsHarvested;
            GreenFieldQuestEvents.FishCaught += OnFishCaught;
            GreenFieldQuestEvents.FishSold += OnFishSold;
        }

        private void OnDisable()
        {
            GreenFieldQuestEvents.CropsPlanted -= OnCropsPlanted;
            GreenFieldQuestEvents.CropsHarvested -= OnCropsHarvested;
            GreenFieldQuestEvents.FishCaught -= OnFishCaught;
            GreenFieldQuestEvents.FishSold -= OnFishSold;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Q))
                SetOpen(!modalRoot.activeSelf);
            if (modalRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                SetOpen(false);

            if (stats == null)
                FindSources();

            ApplySafeArea(false);
            Refresh(false);
        }

        private void FindSources()
        {
            stats = FindFirstObjectByType<PlayerStats>();
        }

        private void Build()
        {
            GameObject canvasObject = UI("Quest Canvas", transform, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 80;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            safeArea = UI("Safe Area", canvasObject.transform).GetComponent<RectTransform>();
            Stretch(safeArea);

            Button tab = MakeButton("Quest Tab", safeArea, GreenDark, new Vector2(194f, 74f));
            Anchor(tab.GetComponent<RectTransform>(), new Vector2(1f, 0.56f), new Vector2(-26f, 0f), new Vector2(194f, 74f));
            AddOutline(tab.gameObject, new Color(.10f, .22f, .12f, 1f), new Vector2(3f, -3f));
            TMP_Text tabLabel = MakeText("Label", tab.transform, "VIỆC HÔM NAY", 23, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(tabLabel.rectTransform);
            tab.onClick.AddListener(() => SetOpen(!modalRoot.activeSelf));

            GameObject hint = UI("Shortcut", safeArea);
            Anchor(hint.GetComponent<RectTransform>(), new Vector2(1f, 0.56f), new Vector2(-210f, 0f), new Vector2(46f, 46f));
            AddImage(hint, Color.white);
            AddOutline(hint, Ink, new Vector2(2f, -2f));
            TMP_Text hintLabel = MakeText("Q", hint.transform, "Q", 22, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(hintLabel.rectTransform);

            modalRoot = UI("Quest Modal", safeArea).gameObject;
            Stretch(modalRoot.GetComponent<RectTransform>());
            AddImage(modalRoot, new Color(.08f, .06f, .04f, .58f));

            window = UI("Quest Book", modalRoot.transform).gameObject;
            RectTransform windowRect = window.GetComponent<RectTransform>();
            Anchor(windowRect, new Vector2(1f, 0.5f), new Vector2(-48f, 0f), new Vector2(690f, 850f));
            windowRect.pivot = new Vector2(1f, 0.5f);
            AddImage(window, Hex("#FFF0C4"));
            AddOutline(window, new Color(.20f, .12f, .07f, 1f), new Vector2(7f, -7f));

            GameObject header = UI("Header", window.transform);
            RectTransform headerRect = header.GetComponent<RectTransform>();
            SetRect(headerRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -126f), Vector2.zero);
            AddImage(header, GreenDark);
            TMP_Text title = MakeText("Title", header.transform, "SỔ VIỆC TRONG NGÀY", 38, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            SetRect(title.rectTransform, Vector2.zero, Vector2.one, new Vector2(36f, 0f), new Vector2(-112f, 0f));
            Button close = MakeButton("Close", header.transform, Coral, new Vector2(62f, 62f));
            Anchor(close.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(62f, 62f));
            TMP_Text closeLabel = MakeText("X", close.transform, "×", 38, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(closeLabel.rectTransform);
            close.onClick.AddListener(() => SetOpen(false));

            summaryText = MakeText("Summary", window.transform, "0/4 việc đã xong", 23, Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            SetRect(summaryText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(34f, -168f), new Vector2(-34f, -126f));

            progressTexts = new TMP_Text[quests.Length];
            progressFills = new Image[quests.Length];
            claimButtons = new Button[quests.Length];
            claimLabels = new TMP_Text[quests.Length];
            for (int i = 0; i < quests.Length; i++) BuildCard(i, -190f - i * 128f);

            TMP_Text footer = MakeText("Footer", window.transform, "Nhiệm vụ làm mới vào mỗi buổi sáng  •  Q để đóng", 19, Ink, TextAlignmentOptions.Center, FontStyles.Italic);
            SetRect(footer.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(22f, 22f), new Vector2(-22f, 66f));

            modalRoot.SetActive(false);
        }

        private void BuildCard(int index, float top)
        {
            Quest quest = quests[index];
            GameObject card = UI("Quest " + (index + 1), window.transform);
            RectTransform rect = card.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(26f, top - 116f), new Vector2(-26f, top));
            AddImage(card, new Color(1f, .985f, .93f, 1f));
            AddOutline(card, new Color(Ink.r, Ink.g, Ink.b, .48f), new Vector2(3f, -3f));

            GameObject icon = UI("Icon", card.transform);
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            Anchor(iconRect, new Vector2(0f, .5f), new Vector2(62f, 0f), new Vector2(74f, 74f));
            iconRect.pivot = new Vector2(.5f, .5f);
            AddImage(icon, quest.color);
            string symbol = quest.kind == QuestKind.Plant
                ? "GIEO"
                : quest.kind == QuestKind.Harvest
                    ? "GẶT"
                    : quest.kind == QuestKind.CatchFish
                        ? "CÂU"
                        : "BÁN";
            TMP_Text iconText = MakeText("Symbol", icon.transform, symbol, 18, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(iconText.rectTransform);

            TMP_Text title = MakeText("Title", card.transform, quest.title, 27, Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(112f, -46f), new Vector2(-150f, -8f));
            TMP_Text desc = MakeText("Description", card.transform, quest.description, 20, Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            SetRect(desc.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(112f, -76f), new Vector2(-150f, -42f));

            GameObject bar = UI("Progress", card.transform);
            SetRect(bar.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(112f, 14f), new Vector2(-150f, 32f));
            AddImage(bar, new Color(Ink.r, Ink.g, Ink.b, .28f));
            GameObject fill = UI("Fill", bar.transform);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = new Vector2(0f, 1f); fillRect.pivot = Vector2.zero;
            fillRect.offsetMin = Vector2.zero; fillRect.offsetMax = Vector2.zero;
            progressFills[index] = AddImage(fill, quest.color);

            progressTexts[index] = MakeText("Count", card.transform, "0/" + quest.target, 19, Ink, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
            SetRect(progressTexts[index].rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(112f, 33f), new Vector2(-150f, 57f));

            Button claim = MakeButton("Claim", card.transform, Gold, new Vector2(116f, 76f));
            Anchor(claim.GetComponent<RectTransform>(), new Vector2(1f, .5f), new Vector2(-18f, 0f), new Vector2(116f, 76f));
            claimLabels[index] = MakeText("Label", claim.transform, "+" + quest.reward + " G", 20, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(claimLabels[index].rectTransform);
            int captured = index;
            claim.onClick.AddListener(() => Claim(captured));
            claimButtons[index] = claim;
        }

        private void Refresh(bool force)
        {
            if (progressTexts == null || progressFills == null || claimButtons == null)
                return;

            int complete = 0;

            for (int i = 0; i < quests.Length; i++)
            {
                Quest q = quests[i];
                q.progress = q.kind == QuestKind.Plant
                    ? plantedCount
                    : q.kind == QuestKind.Harvest
                        ? harvestedCount
                        : q.kind == QuestKind.CatchFish
                            ? caughtFishCount
                            : soldFishCount;
                bool done = q.progress >= q.target;
                if (done) complete++;
                float ratio = Mathf.Clamp01((float)q.progress / q.target);
                RectTransform fillRect = progressFills[i].rectTransform;
                fillRect.anchorMax = new Vector2(ratio, 1f);
                progressTexts[i].text = Mathf.Min(q.progress, q.target) + "/" + q.target;
                claimButtons[i].interactable = done && !q.claimed;
                Image buttonImage = claimButtons[i].GetComponent<Image>();
                buttonImage.color = q.claimed ? Green : done ? Gold : new Color(.72f, .68f, .58f, 1f);
                claimLabels[i].text = q.claimed ? "ĐÃ NHẬN" : "+" + q.reward + " G";
            }
            summaryText.text = complete + "/" + quests.Length + " việc đã xong  •  Hoàn thành để nhận tiền thưởng";
        }

        private void Claim(int index)
        {
            Quest q = quests[index];
            if (q.claimed || q.progress < q.target || stats == null) return;
            q.claimed = true;
            stats.AddMoney(q.reward);
            Refresh(true);
        }

        private void OnCropsPlanted(int amount)
        {
            plantedCount += Mathf.Max(0, amount);
            Refresh(true);
        }

        private void OnCropsHarvested(int amount)
        {
            harvestedCount += Mathf.Max(0, amount);
            Refresh(true);
        }

        private void OnFishCaught(int amount)
        {
            caughtFishCount += Mathf.Max(0, amount);
            Refresh(true);
        }

        private void OnFishSold(int amount)
        {
            soldFishCount += Mathf.Max(0, amount);
            Refresh(true);
        }

        private void SetOpen(bool open)
        {
            modalRoot.SetActive(open);
            if (open && EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private void ApplySafeArea(bool force)
        {
            if (safeArea == null) return;
            Rect area = Screen.safeArea;
            if (!force && lastWidth == Screen.width && lastHeight == Screen.height && area == lastSafeArea) return;
            lastWidth = Screen.width; lastHeight = Screen.height; lastSafeArea = area;
            Vector2 min = area.position;
            Vector2 max = area.position + area.size;
            min.x /= Screen.width; min.y /= Screen.height; max.x /= Screen.width; max.y /= Screen.height;
            safeArea.anchorMin = min; safeArea.anchorMax = max;
            safeArea.offsetMin = Vector2.zero; safeArea.offsetMax = Vector2.zero;
        }

        private static GameObject UI(string name, Transform parent, params Type[] extra)
        {
            Type[] types = new Type[extra.Length + 1];
            types[0] = typeof(RectTransform);
            Array.Copy(extra, 0, types, 1, extra.Length);
            GameObject go = new GameObject(name, types);
            go.transform.SetParent(parent, false);
            return go;
        }

        private TMP_Text MakeText(string name, Transform parent, string value, int size, Color color, TextAlignmentOptions alignment, FontStyles style)
        {
            TextMeshProUGUI text = UI(name, parent, typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.enableAutoSizing = false;
            text.overflowMode = TextOverflowModes.Truncate;
            text.extraPadding = true;
            text.raycastTarget = false;
            return text;
        }

        private static Button MakeButton(string name, Transform parent, Color color, Vector2 size)
        {
            GameObject go = UI(name, parent, typeof(Image), typeof(Button));
            go.GetComponent<Image>().color = color;
            go.GetComponent<RectTransform>().sizeDelta = size;
            return go.GetComponent<Button>();
        }

        private static Image AddImage(GameObject go, Color color)
        {
            Image image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static void AddOutline(GameObject go, Color color, Vector2 distance)
        {
            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = color; outline.effectDistance = distance;
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = anchor;
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out Color color);
            return color;
        }
    }
}
