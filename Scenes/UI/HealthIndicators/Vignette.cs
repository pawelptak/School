using Godot;

public partial class Vignette : ColorRect
{
    [Export] private HealthController healthController;
    [Export] private Player player;
    [Export] private float maxAlpha = 0.7f;
    [Export] private float threshold = 0.4f;


    public override void _Ready()
    {
        if (healthController != null)
        {
            healthController.HealthChanged += OnHealthChanged;
            healthController.TookDamage += OnTookDamage;
        }
    }

    public override void _ExitTree()
    {
        if (healthController != null)
        {
            healthController.HealthChanged -= OnHealthChanged;
            healthController.TookDamage -= OnTookDamage;
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

    private void OnTookDamage(float amount)
    {
        if (player != null && player.IsBlocking)
            return;

        PlayDamageFlash();
    }

    public void PlayDamageFlash()
    {
        var material = Material as ShaderMaterial;
        if (material == null) return;

        var tween = CreateTween();
        tween.TweenProperty(material, "shader_parameter/alpha", 0.4f, 0.1f);
        tween.TweenCallback(Callable.From(() => {
            float currentPercent = healthController.CurrentHealth / healthController.MaxHealth;
            UpdateVignette(currentPercent);
        }));
    }
}
