using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("Energy")]
    public float maxEnergy = 100f;
    public float currentEnergy = 100f;

    [Header("Energy Settings")]
    [Tooltip("Số giây phải chờ sau lần tiêu hao Energy cuối cùng.")]
    [SerializeField]
    private float energyRegenDelay = 3f;

    [Tooltip("Số Energy hồi mỗi giây sau khi hết thời gian chờ.")]
    [SerializeField]
    private float energyRegenPerSecond = 5f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;

    [Header("Stamina Settings")]
    [SerializeField]
    private float staminaDrainPerSecond = 8f;

    [SerializeField]
    private float staminaRegenPerSecond = 2f;

    [Header("Money")]
    public int Money = 600;

    [Header("XP")]
    public int Level = 1;
    public int CurrentXP = 0;
    public int RequiredXP = 100;

    private float energyRegenTimer;

    private void Awake()
    {
        currentHealth = maxHealth;
        currentEnergy = maxEnergy;
        currentStamina = maxStamina;

        energyRegenTimer = 0f;
    }

    private void Update()
    {
        HandleEnergyRegen();
    }

    #region Energy

    public bool HasEnergy(float amount)
    {
        return currentEnergy >= amount;
    }

    public void UseEnergy(float amount)
    {
        if (amount <= 0f)
            return;

        currentEnergy -= amount;
        currentEnergy = Mathf.Clamp(
            currentEnergy,
            0f,
            maxEnergy
        );

        // Mỗi lần làm việc sẽ tính lại thời gian chờ hồi Energy.
        energyRegenTimer = energyRegenDelay;
    }

    public void RestoreEnergy(float amount)
    {
        if (amount <= 0f)
            return;

        currentEnergy = Mathf.Clamp(
            currentEnergy + amount,
            0f,
            maxEnergy
        );

        if (currentEnergy >= maxEnergy)
            energyRegenTimer = 0f;
    }

    private void HandleEnergyRegen()
    {
        if (currentEnergy >= maxEnergy)
        {
            currentEnergy = maxEnergy;
            energyRegenTimer = 0f;
            return;
        }

        if (energyRegenTimer > 0f)
        {
            energyRegenTimer -= Time.deltaTime;
            return;
        }

        currentEnergy = Mathf.MoveTowards(
            currentEnergy,
            maxEnergy,
            energyRegenPerSecond * Time.deltaTime
        );
    }

    public float CurrentEnergy => currentEnergy;

    public float EnergyRegenTimeRemaining =>
        Mathf.Max(0f, energyRegenTimer);

    #endregion

    #region Stamina

    public bool HasStamina(float amount)
    {
        return currentStamina >= amount;
    }

    public bool HasStamina()
    {
        return currentStamina > 1f;
    }

    public void UseStamina(float amount)
    {
        currentStamina = Mathf.Clamp(
            currentStamina - amount,
            0f,
            maxStamina
        );
    }

    public void DrainStamina()
    {
        currentStamina -=
            staminaDrainPerSecond *
            Time.deltaTime;

        currentStamina = Mathf.Clamp(
            currentStamina,
            0f,
            maxStamina
        );
    }

    public void RegenStamina()
    {
        currentStamina +=
            staminaRegenPerSecond *
            Time.deltaTime;

        currentStamina = Mathf.Clamp(
            currentStamina,
            0f,
            maxStamina
        );
    }

    public void AddMaxStamina(float amount)
    {
        maxStamina += amount;
        currentStamina = maxStamina;
    }

    public float CurrentStamina =>
        currentStamina;

    #endregion

    #region Health

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Clamp(
            currentHealth - amount,
            0f,
            maxHealth
        );
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(
            currentHealth + amount,
            0f,
            maxHealth
        );
    }

    #endregion

    #region Money

    public void AddMoney(int amount)
    {
        if (amount <= 0)
            return;

        float multiplier = 1f;

        if (CharacterPassiveManager.Instance != null)
        {
            multiplier =
                CharacterPassiveManager
                    .Instance
                    .MoneyMultiplier;
        }

        int finalAmount =
            Mathf.RoundToInt(
                amount * multiplier
            );

        Money += finalAmount;

        Debug.Log(
            "Nhận tiền: $" +
            finalAmount +
            " | Tổng tiền: $" +
            Money
        );
    }

    public bool SpendMoney(int amount)
    {
        if (amount <= 0)
            return true;

        if (Money < amount)
        {
            Debug.Log("Không đủ tiền.");
            return false;
        }

        Money -= amount;

        Debug.Log(
            "Đã tiêu: $" +
            amount +
            " | Còn lại: $" +
            Money
        );

        return true;
    }

    public void SetMoney(int value)
    {
        Money = Mathf.Max(0, value);
    }

    #endregion

    #region XP

    public void AddXP(int amount)
    {
        if (amount <= 0)
            return;

        float multiplier = 1f;

        if (CharacterPassiveManager.Instance != null)
        {
            multiplier =
                CharacterPassiveManager
                    .Instance
                    .XpMultiplier;
        }

        CurrentXP += Mathf.RoundToInt(
            amount * multiplier
        );

        while (CurrentXP >= RequiredXP)
        {
            CurrentXP -= RequiredXP;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        Level++;

        RequiredXP += 50;

        maxHealth += 5f;
        maxEnergy += 5f;
        maxStamina += 5f;

        currentHealth = maxHealth;
        currentEnergy = maxEnergy;
        currentStamina = maxStamina;

        energyRegenTimer = 0f;

        Debug.Log(
            "Level Up! Level: " +
            Level
        );
    }

    public void SetLevel(int value)
    {
        Level = Mathf.Max(1, value);
    }

    #endregion

    #region Save / Load

    public void SetStamina(float value)
    {
        currentStamina = Mathf.Clamp(
            value,
            0f,
            maxStamina
        );
    }

    public void SetEnergy(float value)
    {
        currentEnergy = Mathf.Clamp(
            value,
            0f,
            maxEnergy
        );

        energyRegenTimer = 0f;
    }

    #endregion
}
