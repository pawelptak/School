using Godot;
using System;

public partial class HealthController : Node
{
    [Export] public float MaxHealth { get; set; } = 100.0f;
    [Export] public float RegenerationRate { get; set; } = 10.0f;
    [Export] public double RegenDelay { get; set; } = 30.0f;

    public float CurrentHealth { get; private set; }
    private double _timeSinceLastDamage = 0.0f;
    private bool _isRegenerating = false;

    public event Action<float> HealthChanged;
    public event Action<float> TookDamage;
    public event Action Died;

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
        HealthChanged?.Invoke(CurrentHealth);
    }

    public override void _Process(double delta)
    {
        ProcessRegeneration(delta);
    }

    private void ProcessRegeneration(double delta)
    {
        if (CurrentHealth >= MaxHealth || CurrentHealth <= 0.0f)
            return;

        _timeSinceLastDamage += delta;

        if (_timeSinceLastDamage >= RegenDelay)
        {
            float healAmount = RegenerationRate * (float)delta;
            CurrentHealth = Mathf.Clamp(CurrentHealth + healAmount, 0.0f, MaxHealth);

            HealthChanged?.Invoke(CurrentHealth);
        }
    }

    public void TakeDamage(float amount)
    {
        if (CurrentHealth <= 0.0f)
            return;

        CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0.0f, MaxHealth);
        _timeSinceLastDamage = 0.0;

        GD.Print($"[PLAYER HEALTH] {amount} damage taken! Remaining HP: {CurrentHealth}/{MaxHealth}");
        HealthChanged?.Invoke(CurrentHealth);
        TookDamage?.Invoke(amount);

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
