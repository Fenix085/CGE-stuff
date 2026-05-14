namespace MainEngine;

public class Health
{
    public int MaxHealth {get; private set;}
    public int CurrentHealth {get; private set;}

    public bool IsDead => CurrentHealth <= 0;

    public Health(int maxHealth)
    {
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        CurrentHealth -= damage;

        if (CurrentHealth < 0)
            CurrentHealth = 0;
    }
}