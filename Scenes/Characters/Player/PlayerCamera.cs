using Godot;

public partial class PlayerCamera : Camera3D
{
    [Export] private HealthController healthController;

    public override void _Ready()
    {
        if (healthController != null)
        {
            healthController.TookDamage += OnPlayerTookDamage;
        }
    }

    public override void _ExitTree()
    {
        if (healthController != null)
        {
            healthController.TookDamage -= OnPlayerTookDamage;
        }
    }

    private void OnPlayerTookDamage(float amount)
    {
        ShakeOrTiltCamera();
    }

    private void ShakeOrTiltCamera()
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "rotation_degrees:z", 5.0f, 0.05f);
        tween.TweenProperty(this, "rotation_degrees:z", -5.0f, 0.05f);
        tween.TweenProperty(this, "rotation_degrees:z", 0.0f, 0.1f);
    }
}
