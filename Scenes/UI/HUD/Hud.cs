using Godot;

public partial class Hud : CanvasLayer
{
    private ProgressBar _healthBar;

    public override async void _Ready()
    {
        _healthBar = GetNode<ProgressBar>("MarginContainer/HealthBar");

        // Wait one frame to ensure the player and its components are fully initialized
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        var player = GetTree().GetFirstNodeInGroup("player");
        var healthController = player?.GetNodeOrNull<HealthController>("HealthController");

        if (healthController != null)
        {
            _healthBar.MaxValue = healthController.MaxHealth;
            _healthBar.Value = healthController.CurrentHealth;

            GD.Print($"[HUD] Initialized health bar: {healthController.CurrentHealth} / {healthController.MaxHealth}");

            healthController.HealthChanged += OnHealthChanged;
        }
        else
        {
            GD.PushWarning("[HUD] Player or HealthController not found!");
        }
    }

    private void OnHealthChanged(float newHealth)
    {
        if (_healthBar != null)
            _healthBar.Value = newHealth;
    }

    public override void _ExitTree()
    {
        var player = GetTree().GetFirstNodeInGroup("Player");
        var healthController = player?.GetNodeOrNull<HealthController>("HealthController");
        
        if (healthController != null)
        {
            healthController.HealthChanged -= OnHealthChanged;
        }
    }
}
