using Godot;
using System;

public partial class HealthController : Node
{
    [Export] public float MaxHealth { get; set; } = 100.0f;
    public float CurrentHealth { get; private set; }

    public event Action<float> HealthChanged;
    public event Action Died;

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
        HealthChanged?.Invoke(CurrentHealth);
    }

    public void TakeDamage(float amount)
    {
        if (CurrentHealth <= 0.0f)
            return;

        CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0.0f, MaxHealth);

        GD.Print($"[PLAYER HEALTH] {amount} damage taken! Remaining HP: {CurrentHealth}/{MaxHealth}");
        HealthChanged?.Invoke(CurrentHealth);

        if (CurrentHealth <= 0.0f)
        {
            Die();
        }
    }

    private void Die()
    {
        GD.Print("[COMBAT] Player died.");
        Died?.Invoke();
    }
}
