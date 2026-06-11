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

    public bool HasEnergy(float amount)
{
    return currentEnergy >= amount;
}

public void UseEnergy(float amount)
{
    currentEnergy -= amount;
    currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
}

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
        currentStamina = Mathf.Clamp(currentStamina - amount, 0, maxStamina);
    }

    public void DrainStamina()
    {
        currentStamina -= staminaDrainPerSecond * Time.deltaTime;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
    }

    public void RegenStamina()
    {
        currentStamina += staminaRegenPerSecond * Time.deltaTime;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
    }

    public void AddMoney(int amount)
    {
        Money += amount;
    }

    public bool SpendMoney(int amount)
    {
        if (Money < amount)
            return false;

        Money -= amount;
        return true;
    }

    public void AddXP(int amount)
    {
        CurrentXP += amount;

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
}