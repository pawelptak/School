using Godot;

public partial class Vignette : ColorRect
{
    [Export] private HealthController healthController;
    [Export] private float maxAlpha = 0.7f;
    [Export] private float threshold = 0.4f;

    public override void _Ready()
    {
        if (healthController != null)
        {
            healthController.HealthChanged += OnHealthChanged;
        }
    }

    public override void _ExitTree()
    {
        if (healthController != null)
        {
            healthController.HealthChanged -= OnHealthChanged;
        }
    }

    private void OnHealthChanged(float currentHealth)
    {
        float healthPercent = currentHealth / healthController.MaxHealth;
        UpdateVignette(healthPercent);
    }

    private void UpdateVignette(float healthPercent)
    {
        float targetAlpha = 0.0f;

        if (healthPercent < threshold)
        {
            targetAlpha = Mathf.Remap(healthPercent, threshold, 0.0f, 0.0f, maxAlpha);
            targetAlpha = Mathf.Clamp(targetAlpha, 0.0f, maxAlpha);
        }

        var material = Material as ShaderMaterial;
        if (material != null)
        {
            var tween = CreateTween();
            tween.TweenProperty(material, "shader_parameter/alpha", targetAlpha, 0.15);
        }
    }
}
