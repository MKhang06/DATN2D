using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GreenField.UI
{
    [DisallowMultipleComponent]
    public sealed class GreenFieldIntroCutscene : MonoBehaviour
    {
        private const float SkipInputDelay = 0.45f;

        private static readonly string[] Titles =
        {
            "MỘT KHỞI ĐẦU MỚI",
            "MẢNH ĐẤT ĐANG CHỜ",
            "CUỘC SỐNG Ở GREEN FIELD"
        };

        private static readonly string[] Stories =
        {
            "Rời xa những ngày ồn ào, bạn trở về mảnh vườn cũ " +
            "mà gia đình đã để lại.",

            "Đất đã nghỉ qua nhiều mùa. Hãy gieo những hạt giống đầu tiên, " +
            "chăm sóc cây trồng và thu hoạch thành quả.",

            "Khi việc đồng áng tạm xong, mặt hồ yên bình luôn chờ bạn. " +
            "Câu cá, bán sản vật và vun đắp một cuộc sống của riêng mình."
        };

        private CanvasGroup rootCanvasGroup;
        private CanvasGroup storyCanvasGroup;
        private RectTransform backgroundRect;
        private Image fadeToBlack;
        private Text titleText;
        private Text storyText;
        private Action finished;
        private float skipUnlockedAt;
        private bool ending;

        public static GreenFieldIntroCutscene Create(Transform menuRoot)
        {
            if (menuRoot == null)
                return null;

            Sprite backgroundSprite = FindMenuBackground(menuRoot);
            Font font = FindMenuFont(menuRoot);

            GameObject root = new GameObject(
                "IntroCutscene",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image),
                typeof(GreenFieldIntroCutscene)
            );

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.SetParent(menuRoot, false);
            Stretch(rootRect);
            rootRect.SetAsLastSibling();

            Image inputBlocker = root.GetComponent<Image>();
            inputBlocker.color = Color.clear;
            inputBlocker.raycastTarget = true;

            GreenFieldIntroCutscene cutscene =
                root.GetComponent<GreenFieldIntroCutscene>();

            cutscene.Build(backgroundSprite, font);
            return cutscene;
        }

        public void Play(Action onFinished)
        {
            if (ending)
                return;

            finished = onFinished;
            skipUnlockedAt = Time.unscaledTime + SkipInputDelay;
            StartCoroutine(PlayRoutine());
        }

        private void Build(Sprite backgroundSprite, Font font)
        {
            rootCanvasGroup = GetComponent<CanvasGroup>();
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = true;
            rootCanvasGroup.blocksRaycasts = true;

            Image background = CreateImage(
                "FarmBackground",
                transform,
                Color.white
            );
            background.sprite = backgroundSprite;
            background.preserveAspect = false;
            backgroundRect = background.rectTransform;
            Stretch(backgroundRect);
            backgroundRect.localScale = Vector3.one * 1.04f;

            Image shade = CreateImage(
                "CinematicShade",
                transform,
                new Color(0.01f, 0.045f, 0.025f, 0.58f)
            );
            Stretch(shade.rectTransform);

            GameObject panel = new GameObject(
                "StoryPanel",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image)
            );
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.SetParent(transform, false);
            panelRect.anchorMin = new Vector2(0.11f, 0.10f);
            panelRect.anchorMax = new Vector2(0.89f, 0.43f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.025f, 0.10f, 0.065f, 0.94f);

            storyCanvasGroup = panel.GetComponent<CanvasGroup>();

            Image accent = CreateImage(
                "AccentLine",
                panel.transform,
                new Color(0.93f, 0.74f, 0.30f, 1f)
            );
            RectTransform accentRect = accent.rectTransform;
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.pivot = new Vector2(0.5f, 1f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(0f, 5f);

            titleText = CreateText(
                "Title",
                panel.transform,
                font,
                34,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(1f, 0.83f, 0.42f, 1f)
            );
            SetAnchors(
                titleText.rectTransform,
                new Vector2(0.07f, 0.63f),
                new Vector2(0.93f, 0.91f)
            );

            storyText = CreateText(
                "Story",
                panel.transform,
                font,
                27,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                new Color(0.96f, 0.94f, 0.83f, 1f)
            );
            storyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            storyText.verticalOverflow = VerticalWrapMode.Truncate;
            storyText.lineSpacing = 1.12f;
            SetAnchors(
                storyText.rectTransform,
                new Vector2(0.07f, 0.18f),
                new Vector2(0.93f, 0.66f)
            );

            Text skipText = CreateText(
                "SkipHint",
                transform,
                font,
                19,
                FontStyle.Italic,
                TextAnchor.MiddleRight,
                new Color(0.92f, 0.91f, 0.80f, 0.88f)
            );
            SetAnchors(
                skipText.rectTransform,
                new Vector2(0.55f, 0.035f),
                new Vector2(0.95f, 0.09f)
            );
            skipText.text = "SPACE / ESC để bỏ qua";

            fadeToBlack = CreateImage(
                "FadeToBlack",
                transform,
                new Color(0f, 0f, 0f, 0f)
            );
            Stretch(fadeToBlack.rectTransform);
            fadeToBlack.raycastTarget = false;
        }

        private IEnumerator PlayRoutine()
        {
            yield return FadeCanvas(rootCanvasGroup, 0f, 1f, 0.65f);

            bool skipped = false;

            for (int index = 0; index < Stories.Length; index++)
            {
                titleText.text = Titles[index];
                storyText.text = Stories[index];

                yield return FadeCanvas(storyCanvasGroup, 0f, 1f, 0.35f);

                float endAt = Time.unscaledTime + 2.35f;

                while (Time.unscaledTime < endAt)
                {
                    AnimateBackground();

                    if (WasSkipPressed())
                    {
                        skipped = true;
                        break;
                    }

                    yield return null;
                }

                if (skipped)
                    break;

                yield return FadeCanvas(storyCanvasGroup, 1f, 0f, 0.28f);
            }

            ending = true;
            yield return FadeImage(fadeToBlack, 0f, 1f, skipped ? 0.22f : 0.55f);

            Action callback = finished;
            finished = null;
            gameObject.SetActive(false);
            callback?.Invoke();
        }

        private void AnimateBackground()
        {
            if (backgroundRect == null)
                return;

            float scale = Mathf.Lerp(
                1.04f,
                1.10f,
                Mathf.PingPong(Time.unscaledTime * 0.045f, 1f)
            );
            backgroundRect.localScale = Vector3.one * scale;
        }

        private bool WasSkipPressed()
        {
            if (Time.unscaledTime < skipUnlockedAt)
                return false;

            return Input.GetKeyDown(KeyCode.Space) ||
                   Input.GetKeyDown(KeyCode.Return) ||
                   Input.GetKeyDown(KeyCode.Escape) ||
                   Input.GetMouseButtonDown(0);
        }

        private static IEnumerator FadeCanvas(
            CanvasGroup canvasGroup,
            float from,
            float to,
            float duration)
        {
            float elapsed = 0f;
            canvasGroup.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(
                    from,
                    to,
                    Mathf.Clamp01(elapsed / duration)
                );
                yield return null;
            }

            canvasGroup.alpha = to;
        }

        private static IEnumerator FadeImage(
            Image image,
            float from,
            float to,
            float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                Color color = image.color;
                color.a = Mathf.Lerp(
                    from,
                    to,
                    Mathf.Clamp01(elapsed / duration)
                );
                image.color = color;
                yield return null;
            }

            Color finalColor = image.color;
            finalColor.a = to;
            image.color = finalColor;
        }

        private static Sprite FindMenuBackground(Transform menuRoot)
        {
            Image[] images = menuRoot.GetComponentsInChildren<Image>(true);

            foreach (Image image in images)
            {
                if (image != null &&
                    image.sprite != null &&
                    image.name == "Background")
                {
                    return image.sprite;
                }
            }

            return null;
        }

        private static Font FindMenuFont(Transform menuRoot)
        {
            Text text = menuRoot.GetComponentInChildren<Text>(true);

            return text != null && text.font != null
                ? text.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static Image CreateImage(
            string objectName,
            Transform parent,
            Color color)
        {
            GameObject imageObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image)
            );
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(
            string objectName,
            Transform parent,
            Font font,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Color color)
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Text)
            );
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.supportRichText = true;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(14, fontSize - 8);
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            SetAnchors(rectTransform, Vector2.zero, Vector2.one);
        }

        private static void SetAnchors(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
