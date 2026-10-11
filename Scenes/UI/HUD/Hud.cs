using Godot;

public partial class Hud : CanvasLayer
{
    private ProgressBar _healthBar;
    private HealthController _healthController;
    private Control _deathScreen;

    public override void _Ready()
    {
        _healthBar = GetNode<ProgressBar>("MarginContainer/HealthBar");
        _deathScreen = GetNode<Control>("DeathScreen");
        _deathScreen.Hide();

        var restartButton = _deathScreen.GetNodeOrNull<Button>("VBoxContainer/RestartButton");

        if (restartButton != null)
            restartButton.Pressed += OnRestartPressed;
    }

    public override void _Process(double delta)
    {
        if (_healthController != null && GodotObject.IsInstanceValid(_healthController))
            return;

        _healthController = null;

        var player = GetTree().GetFirstNodeInGroup("player");
        var controller = player?.GetNodeOrNull<HealthController>("HealthController");

        if (controller == null)
            return;

        _healthController = controller;
        _healthBar.MaxValue = controller.MaxHealth;
        _healthBar.Value = controller.CurrentHealth;

        controller.HealthChanged += OnHealthChanged;
        controller.Died += OnPlayerDied;

        GD.Print("[HUD] HealthController connected.");
    }

    private void OnHealthChanged(float newHealth)
    {
        _healthBar.Value = newHealth;
    }

    private void OnPlayerDied()
    {
        _deathScreen.Show();
        Input.MouseMode = Input.MouseModeEnum.Visible;

        var player = GetTree().GetFirstNodeInGroup("player");

        if (player != null)
        {
            player.SetProcess(false);
            player.SetPhysicsProcess(false);
            player.SetProcessInput(false);
        }
    }

    private void OnRestartPressed()
    {
        _deathScreen.Hide();

        if (_healthController != null && GodotObject.IsInstanceValid(_healthController))
        {
            _healthController.HealthChanged -= OnHealthChanged;
            _healthController.Died -= OnPlayerDied;
        }

        _healthController = null;

        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();

        Input.MouseMode = Input.MouseModeEnum.Captured;

        CallDeferred(nameof(RefreshHealthController));
    }

    private void RefreshHealthController()
    {
        _healthController = null;
    }

    public override void _ExitTree()
    {
        if (_healthController != null && GodotObject.IsInstanceValid(_healthController))
        {
            _healthController.HealthChanged -= OnHealthChanged;
            _healthController.Died -= OnPlayerDied;
        }
    }
}