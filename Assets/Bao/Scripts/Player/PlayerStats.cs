using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("Energy")]
    public float maxEnergy = 100f;
    public float currentEnergy = 100f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;

    [Header("Stamina Settings")]
    [SerializeField] private float staminaDrainPerSecond = 8f;
    [SerializeField] private float staminaRegenPerSecond = 2f;

    [Header("Money")]
    public int Money = 0;

    [Header("XP")]
    public int Level = 1;
    public int CurrentXP = 0;
    public int RequiredXP = 100;

    private void Awake()
    {
        currentHealth = maxHealth;
        currentEnergy = maxEnergy;
        currentStamina = maxStamina;
    }

    #region Energy

    public bool HasEnergy(float amount)
    {
        return currentEnergy >= amount;
    }

    public void UseEnergy(float amount)
    {
        currentEnergy -= amount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
    }

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
        currentStamina =
            Mathf.Clamp(currentStamina - amount, 0, maxStamina);
    }

    public void DrainStamina()
    {
        currentStamina -=
            staminaDrainPerSecond * Time.deltaTime;

        currentStamina =
            Mathf.Clamp(currentStamina, 0, maxStamina);
    }

    public void RegenStamina()
    {
        currentStamina +=
            staminaRegenPerSecond * Time.deltaTime;

        currentStamina =
            Mathf.Clamp(currentStamina, 0, maxStamina);
    }

    #endregion

    #region Health

    public void TakeDamage(float amount)
    {
        currentHealth =
            Mathf.Clamp(currentHealth - amount, 0, maxHealth);
    }

    public void Heal(float amount)
    {
        currentHealth =
            Mathf.Clamp(currentHealth + amount, 0, maxHealth);
    }

    #endregion

    #region Money

    public void AddMoney(int amount)
    {
        float multiplier = 1f;

        if (CharacterPassiveManager.Instance != null)
            multiplier = CharacterPassiveManager.Instance.MoneyMultiplier;

        Money += Mathf.RoundToInt(amount * multiplier);
    }

    public bool SpendMoney(int amount)
    {
        if (Money < amount)
            return false;

        Money -= amount;
        return true;
    }

    #endregion

    #region XP

    public void AddXP(int amount)
    {
        float multiplier = 1f;

        if (CharacterPassiveManager.Instance != null)
            multiplier = CharacterPassiveManager.Instance.XpMultiplier;

        CurrentXP += Mathf.RoundToInt(amount * multiplier);

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

        maxHealth += 5;
        maxEnergy += 5;
        maxStamina += 5;

        currentHealth = maxHealth;
        currentEnergy = maxEnergy;
        currentStamina = maxStamina;

        Debug.Log("Level Up! Level: " + Level);
    }

    #endregion

    #region Save / Load

    public float CurrentStamina => currentStamina;

    public void SetMoney(int value)
    {
        Money = value;
    }

    public void SetStamina(float value)
    {
        currentStamina =
            Mathf.Clamp(value, 0, maxStamina);
    }

    public void SetLevel(int value)
    {
        Level = value;
    }

    #endregion
    public void RestoreEnergy(float amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0, maxEnergy);
    }

    public void AddMaxStamina(float amount)
    {
        maxStamina += amount;
        currentStamina = maxStamina;
    }
}