using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FishingMiniGameUI : MonoBehaviour
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

    [Header("Lock Player")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerToolAnimation toolAnimation;
    [SerializeField] private PlayerToolController toolController;

    [Header("Settings")]
    [SerializeField] private float timeLimit = 6f;
    [SerializeField, Range(2, 10)] private int sequenceLength = 5;

    [Header("Random Inputs")]
    [SerializeField] private MiniGameInput[] possibleInputs;

    private readonly List<MiniGameInput> currentSequence = new List<MiniGameInput>();
    private int currentIndex;
    private float timer;
    private bool isPlaying;
    private bool finished;

    private Action<bool> onFinish;

    private void Awake()
    {
        BuildDefaultInputsIfEmpty();
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

        CheckOnlyPressedKeys();
    }

    public void StartMiniGame(Action<bool> callback)
    {
        CancelInvoke();

        onFinish = callback;
        finished = false;
        isPlaying = true;
        LockPlayer();

        currentIndex = 0;
        timer = timeLimit;

        GenerateSequence();

        if (root != null)
            root.SetActive(true);

        if (resultText != null)
            resultText.text = "";

        RefreshUI();
    }

    private void GenerateSequence()
    {
        currentSequence.Clear();

        int count = Mathf.Clamp(sequenceLength, 2, 10);

        if (slotTexts != null && slotTexts.Length > 0)
            count = Mathf.Min(count, slotTexts.Length);

        for (int i = 0; i < count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, possibleInputs.Length);
            currentSequence.Add(possibleInputs[randomIndex]);
        }
    }

    private void CheckOnlyPressedKeys()
    {
        if (currentIndex < 0 || currentIndex >= currentSequence.Count)
            return;

        MiniGameInput need = currentSequence[currentIndex];

        bool pressedSomething = false;

        foreach (MiniGameInput input in possibleInputs)
        {
            if (Input.GetKeyDown(input.keyCode))
            {
                pressedSomething = true;

                if (input.keyCode == need.keyCode)
                {
                    CorrectInput();
                    return;
                }
            }
        }

        if (pressedSomething)
        {
            Finish(false);
        }
    }

    private void CorrectInput()
    {
        currentIndex++;

        if (currentIndex >= currentSequence.Count)
        {
            Finish(true);
            return;
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (titleText != null)
        {
            int showIndex = Mathf.Clamp(currentIndex + 1, 1, currentSequence.Count);
            titleText.text = "CHECKING FISH " + showIndex + "/" + currentSequence.Count;
        }

        for (int i = 0; i < slotTexts.Length; i++)
        {
            if (slotTexts[i] == null)
                continue;

            if (i >= currentSequence.Count)
            {
                slotTexts[i].text = "";
                continue;
            }

            slotTexts[i].text = currentSequence[i].displayText;

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
            resultText.text = success ? "THÀNH CÔNG!" : "THẤT BẠI!";

        UnlockPlayer();
        onFinish?.Invoke(success);

        Invoke(nameof(Hide), 0.4f);
    }

    public void Hide()
    {
        isPlaying = false;
        finished = false;

        if (root != null)
            root.SetActive(false);

        if (Application.isPlaying)
            UnlockPlayer();
    }

    private void BuildDefaultInputsIfEmpty()
    {
        if (possibleInputs != null && possibleInputs.Length > 0)
            return;

        possibleInputs = new MiniGameInput[]
        {
            new MiniGameInput { displayText = "A", keyCode = KeyCode.A },
            new MiniGameInput { displayText = "B", keyCode = KeyCode.B },
            new MiniGameInput { displayText = "C", keyCode = KeyCode.C },
            new MiniGameInput { displayText = "S", keyCode = KeyCode.S },

            new MiniGameInput { displayText = "1", keyCode = KeyCode.Alpha1 },
            new MiniGameInput { displayText = "2", keyCode = KeyCode.Alpha2 },
            new MiniGameInput { displayText = "3", keyCode = KeyCode.Alpha3 },
            new MiniGameInput { displayText = "4", keyCode = KeyCode.Alpha4 },

            new MiniGameInput { displayText = "↑", keyCode = KeyCode.UpArrow },
            new MiniGameInput { displayText = "↓", keyCode = KeyCode.DownArrow },
            new MiniGameInput { displayText = "←", keyCode = KeyCode.LeftArrow },
            new MiniGameInput { displayText = "→", keyCode = KeyCode.RightArrow },
        };
    }
    private void LockPlayer()
    {
        if (playerController != null)
            playerController.SetMovementLocked(true);

        if (toolAnimation != null)
            toolAnimation.SetFishingLock(true);

        if (toolController != null)
            toolController.enabled = false;
    }

    private void UnlockPlayer()
    {
        if (playerController != null)
            playerController.SetMovementLocked(false);

        if (toolAnimation != null)
            toolAnimation.SetFishingLock(false);

        if (toolController != null)
            toolController.enabled = true;
    }
}