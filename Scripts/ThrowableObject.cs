using Godot;

public abstract partial class ThrowableObject : RigidBody3D, IInteractable
{
    public float ThrowForce { get; set; }

    [ExportGroup("Inventory Setup")]
    [Export] public bool IsEquippable { get; set; } = false;
    [Export] public Texture2D Icon { get; set; }

    [ExportGroup("Damage")]
    [Export] public float Damage { get; set; } = 10.0f;
    [Export] public float MinDamageMultiplier { get; set; } = 0.5f;
    [Export] public float MaxDamageMultiplier { get; set; } = 2.0f;
    [Export] public float Knockback { get; set; } = 5.0f;
    [Export] public float StunDuration { get; set; } = 0.0f;

    [ExportGroup("Interaction")]
    [Export] public float PickupTime { get; set; } = 0.5f;

    public abstract new string Name { get; }
    private const float MinThrowForce = 5.0f;
    private const float MaxThrowForce = 20.0f;

    [ExportGroup("Persistence")]
    [Export]
    public string WorldItemID { get; set; } = "";

    public bool DroppedFromInventory { get; set; } = false;

    public string InteractionText => "Podnieś";
    public Vector3 PromptOffset => new Vector3(0, 0.5f, 0);
    public bool RequiresHold => true;
    public float GetInteractionDuration() => PickupTime;
    public string GetPromptText() => $"[E] {InteractionText}";

    public float GetPickupDuration() => PickupTime;

    public bool CanInteract(Player player) => !Freeze;

    public void Interact(Player player)
    {
        player.TryPickup(this);
    }

    public override void _Ready()
    {
        ContactMonitor = true;
        MaxContactsReported = 4;
        BodyEntered += OnBodyEntered;

        if (string.IsNullOrEmpty(WorldItemID))
        {
            WorldItemID = $"{SceneFilePath}_{GlobalPosition.X:F2}_{GlobalPosition.Y:F2}_{GlobalPosition.Z:F2}";
        }
    }

    public float CalculateDamage()
    {
        float forcePercent = Mathf.InverseLerp(MinThrowForce, MaxThrowForce, ThrowForce);
        float multiplier = Mathf.Lerp(MinDamageMultiplier, MaxDamageMultiplier, forcePercent);
        return Damage * multiplier;
    }

    private void OnBodyEntered(Node body)
    {
        if (body is InteractableCharacter character)
            character.NotifyHitByThrowable(this, ThrowForce);
    }
}
