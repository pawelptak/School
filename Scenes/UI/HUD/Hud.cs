using Godot;

public partial class Hud : CanvasLayer
{
    private ProgressBar _healthBar;
    private HealthController _healthController;
    private Control _deathScreen;

    [Export] private double hideDelay = 3.0;
    private double _timeSinceLastHealthChange = 0.0;
    private bool _isBarVisible = false;

    public override void _Ready()
    {
        _healthBar = GetNode<ProgressBar>("MarginContainer/HealthBar");
        _deathScreen = GetNode<Control>("DeathScreen");
        _deathScreen.Hide();

        _healthBar.Modulate = new Color(1, 1, 1, 0);
        _isBarVisible = false;

        var restartButton = _deathScreen.GetNodeOrNull<Button>("VBoxContainer/RestartButton");

        if (restartButton != null)
            restartButton.Pressed += OnRestartPressed;
    }

    public override void _Process(double delta)
    {
        if (_isBarVisible && _healthController != null && GodotObject.IsInstanceValid(_healthController))
        {
            _timeSinceLastHealthChange += delta;
            if (_timeSinceLastHealthChange >= hideDelay)
            {
                HideHealthBarSmooth();
            }
        }

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

        _timeSinceLastHealthChange = 0.0;
        ShowHealthBarSmooth();
    }

    private void ShowHealthBarSmooth()
    {
        _isBarVisible = true;
        var tween = CreateTween();
        tween.TweenProperty(_healthBar, "modulate:a", 1.0f, 0.2f);
    }

    private void HideHealthBarSmooth()
    {
        _isBarVisible = false;
        var tween = CreateTween();
        tween.TweenProperty(_healthBar, "modulate:a", 0.0f, 0.5f);
    }

    private void OnPlayerDied()
    {
        HideHealthBarSmooth();

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
