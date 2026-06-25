using UnityEngine;

public class CharacterPassiveManager : MonoBehaviour
{
    public static CharacterPassiveManager Instance;

    public enum CharacterType
    {
        Rin,
        May,
        Kai,
        Max,
        Hana,
        Leon
    }

    [Header("Selected Character")]
    [SerializeField] private CharacterType selectedCharacter = CharacterType.Rin;

    [Header("Passive Values")]
    [SerializeField] private float rinMoveSpeedMultiplier = 1.2f;
    [SerializeField] private float mayEnergyCostMultiplier = 0.8f;
    [SerializeField] private float kaiXpMultiplier = 1.25f;
    [SerializeField] private float maxMoneyMultiplier = 1.2f;
    [SerializeField] private float hanaDailyEnergyRestore = 20f;
    [SerializeField] private float leonMaxStaminaBonus = 20f;

    public float MoveSpeedMultiplier { get; private set; } = 1f;
    public float EnergyCostMultiplier { get; private set; } = 1f;
    public float XpMultiplier { get; private set; } = 1f;
    public float MoneyMultiplier { get; private set; } = 1f;

    private PlayerStats playerStats;
    private bool leonApplied;

    private void Awake()
    {
        Instance = this;
        playerStats = GetComponent<PlayerStats>();

        ApplyPassive();
    }

    private void OnEnable()
    {
        if (GameTimeManager.Instance != null)
            GameTimeManager.Instance.OnDayChanged += HandleNewDay;
    }

    private void OnDisable()
    {
        if (GameTimeManager.Instance != null)
            GameTimeManager.Instance.OnDayChanged -= HandleNewDay;
    }

    private void ApplyPassive()
    {
        MoveSpeedMultiplier = 1f;
        EnergyCostMultiplier = 1f;
        XpMultiplier = 1f;
        MoneyMultiplier = 1f;

        switch (selectedCharacter)
        {
            case CharacterType.Rin:
                MoveSpeedMultiplier = rinMoveSpeedMultiplier;
                Debug.Log("Rin Passive: +20% tốc độ di chuyển");
                break;

            case CharacterType.May:
                EnergyCostMultiplier = mayEnergyCostMultiplier;
                Debug.Log("May Passive: -20% Energy khi dùng công cụ");
                break;

            case CharacterType.Kai:
                XpMultiplier = kaiXpMultiplier;
                Debug.Log("Kai Passive: +25% XP");
                break;

            case CharacterType.Max:
                MoneyMultiplier = maxMoneyMultiplier;
                Debug.Log("Max Passive: +20% vàng");
                break;

            case CharacterType.Hana:
                Debug.Log("Hana Passive: hồi 20 Energy mỗi ngày");
                break;

            case CharacterType.Leon:
                ApplyLeonPassive();
                Debug.Log("Leon Passive: +20 Stamina tối đa");
                break;
        }
    }

    private void ApplyLeonPassive()
    {
        if (leonApplied) return;
        if (playerStats == null) return;

        playerStats.AddMaxStamina(leonMaxStaminaBonus);
        leonApplied = true;
    }

    private void HandleNewDay()
    {
        if (selectedCharacter != CharacterType.Hana) return;
        if (playerStats == null) return;

        playerStats.RestoreEnergy(hanaDailyEnergyRestore);
        Debug.Log("Hana Passive: đã hồi " + hanaDailyEnergyRestore + " Energy");
    }
}