using Godot;

public partial class ThrowableObject : RigidBody3D, IInteractable
{
    public float ThrowForce { get; set; }

    [ExportGroup("Damage")]
    [Export] public float Damage { get; set; } = 10.0f;
    [Export] public float MinDamageMultiplier { get; set; } = 0.5f;
    [Export] public float MaxDamageMultiplier { get; set; } = 2.0f;
    [Export] public float Knockback { get; set; } = 3.0f;
    [Export] public float StunDuration { get; set; } = 0.0f;

    private const float MinThrowForce = 5.0f;
    private const float MaxThrowForce = 20.0f;

    public string InteractionText => "Podnieś";
    public Vector3 PromptOffset => new Vector3(0, 0.5f, 0);

    public bool CanInteract(Player player)
    {
        return !Freeze;
    }

    public void Interact(Player player)
    {
        player.TryPickup(this);
    }

    public override void _Ready()
    {
        ContactMonitor = true;
        MaxContactsReported = 4;

        BodyEntered += OnBodyEntered;
    }

    public float CalculateDamage()
    {
        float forcePercent = Mathf.InverseLerp(
            MinThrowForce,
            MaxThrowForce,
            ThrowForce
        );

        float multiplier = Mathf.Lerp(
            MinDamageMultiplier,
            MaxDamageMultiplier,
            forcePercent
        );

        return Damage * multiplier;
    }

    private void OnBodyEntered(Node body)
    {
        GD.Print($"Throwable hit: {body.Name}");

        if (body is InteractableCharacter character)
            character.NotifyHitByThrowable(this, ThrowForce);
    }
}
