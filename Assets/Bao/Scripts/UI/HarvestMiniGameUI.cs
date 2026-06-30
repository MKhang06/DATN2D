using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HarvestMiniGameUI : MonoBehaviour
{
    [System.Serializable]
    public class MiniGameInput
    {
        public string displayText;
        public KeyCode keyCode;
    }

    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text[] slotTexts;

    [Header("Settings")]
    [SerializeField] private float timeLimit = 5f;
    [SerializeField, Range(2, 10)] private int sequenceLength = 4;

    [Header("Inputs")]
    [SerializeField] private MiniGameInput[] possibleInputs;

    [Header("Lock Player")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerToolAnimation toolAnimation;
    [SerializeField] private PlayerToolController toolController;

    private readonly List<MiniGameInput> sequence = new List<MiniGameInput>();
    private int currentIndex;
    private float timer;
    private bool isPlaying;
    private bool finished;

    private Action<bool> onFinish;

    private void Awake()
    {
        BuildDefaultInputs();

        if (root != null)
            root.SetActive(false);
    }

    private void Update()
    {
        if (!isPlaying || finished)
            return;

        timer -= Time.deltaTime;

        if (timeText != null)
            timeText.text = Mathf.CeilToInt(timer).ToString();

        if (timer <= 0f)
        {
            Finish(false);
            return;
        }

        CheckInput();
    }

    public void StartMiniGame(Action<bool> callback)
    {
        CancelInvoke();

        onFinish = callback;
        finished = false;
        isPlaying = true;
        currentIndex = 0;
        timer = timeLimit;

        GenerateSequence();

        if (root != null)
            root.SetActive(true);

        if (resultText != null)
            resultText.text = "";

        LockPlayer();
        RefreshUI();
    }

    private void GenerateSequence()
    {
        sequence.Clear();

        int count = Mathf.Clamp(sequenceLength, 2, 10);

        if (slotTexts != null && slotTexts.Length > 0)
            count = Mathf.Min(count, slotTexts.Length);

        for (int i = 0; i < count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, possibleInputs.Length);
            sequence.Add(possibleInputs[randomIndex]);
        }
    }

    private void CheckInput()
    {
        if (currentIndex < 0 || currentIndex >= sequence.Count)
            return;

        MiniGameInput need = sequence[currentIndex];
        bool pressedSomething = false;

        foreach (MiniGameInput input in possibleInputs)
        {
            if (Input.GetKeyDown(input.keyCode))
            {
                pressedSomething = true;

                if (input.keyCode == need.keyCode)
                {
                    currentIndex++;

                    if (currentIndex >= sequence.Count)
                    {
                        Finish(true);
                        return;
                    }

                    RefreshUI();
                    return;
                }
            }
        }

        if (pressedSomething)
            Finish(false);
    }

    private void RefreshUI()
    {
        if (titleText != null)
        {
            int showIndex = Mathf.Clamp(currentIndex + 1, 1, sequence.Count);
            titleText.text = "HARVESTING " + showIndex + "/" + sequence.Count;
        }

        for (int i = 0; i < slotTexts.Length; i++)
        {
            if (slotTexts[i] == null)
                continue;

            if (i >= sequence.Count)
            {
                slotTexts[i].text = "";
                continue;
            }

            slotTexts[i].text = sequence[i].displayText;

            if (i < currentIndex)
                slotTexts[i].color = Color.green;
            else if (i == currentIndex)
                slotTexts[i].color = Color.red;
            else
                slotTexts[i].color = Color.white;
        }
    }

    private void Finish(bool success)
    {
        if (finished)
            return;

        finished = true;
        isPlaying = false;

        if (resultText != null)
            resultText.text = success ? "THU HOẠCH!" : "THẤT BẠI!";

        UnlockPlayer();

        onFinish?.Invoke(success);

        Invoke(nameof(Hide), 0.4f);
    }

    private void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    private void LockPlayer()
{
    GameLockManager.Instance?.LockPlayer();
}

    private void UnlockPlayer()
{
    GameLockManager.Instance?.UnlockPlayer();
}

    private void BuildDefaultInputs()
    {
        if (possibleInputs != null && possibleInputs.Length > 0)
            return;

        possibleInputs = new MiniGameInput[]
        {
            new MiniGameInput { displayText = "A", keyCode = KeyCode.A },
            new MiniGameInput { displayText = "S", keyCode = KeyCode.S },
            new MiniGameInput { displayText = "D", keyCode = KeyCode.D },
            new MiniGameInput { displayText = "W", keyCode = KeyCode.W },

            new MiniGameInput { displayText = "1", keyCode = KeyCode.Alpha1 },
            new MiniGameInput { displayText = "2", keyCode = KeyCode.Alpha2 },
            new MiniGameInput { displayText = "3", keyCode = KeyCode.Alpha3 },

            new MiniGameInput { displayText = "←", keyCode = KeyCode.LeftArrow },
            new MiniGameInput { displayText = "↑", keyCode = KeyCode.UpArrow },
            new MiniGameInput { displayText = "↓", keyCode = KeyCode.DownArrow },
            new MiniGameInput { displayText = "→", keyCode = KeyCode.RightArrow },
        };
    }
}