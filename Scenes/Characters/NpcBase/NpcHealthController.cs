using Godot;
using System;

public partial class NpcHealthController : Node
{
    [Export] public float MaxHealth { get; set; } = 100.0f;
    [Export] public bool IsImmortal { get; set; } = false;

    public float CurrentHealth { get; private set; }

    public event Action<float> HealthChanged;
    public event Action Died;

    private SpriteAnimator _spriteAnimator;

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
        GD.Print($"[NPC HEALTH] {GetParent().Name}: {CurrentHealth}/{MaxHealth} HP");

        _spriteAnimator = GetParent().GetNodeOrNull<SpriteAnimator>("SpriteAnimator");
    }

    public void TakeDamage(float amount)
    {
        if (IsImmortal || CurrentHealth <= 0.0f || amount <= 0.0f)
            return;

        CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0.0f, MaxHealth);

        _spriteAnimator?.PlayHitFlash(new Color(1.0f, 0.0f, 0.0f));

        GD.Print($"[NPC HEALTH] {GetParent().Name} took {amount} damage. Remaining HP: {CurrentHealth}/{MaxHealth}");

        HealthChanged?.Invoke(CurrentHealth);

        if (CurrentHealth <= 0.0f)
            Die();
    }

    private void Die()
    {
        GD.Print($"[NPC HEALTH] {GetParent().Name} was defeated.");
        Died?.Invoke();
    }

    public void RestoreHealth()
    {
        CurrentHealth = MaxHealth;
        HealthChanged?.Invoke(CurrentHealth);

        GD.Print(
            $"[NPC HEALTH] {GetParent().Name} recovered. " +
            $"HP: {CurrentHealth}/{MaxHealth}"
        );
    }
}
