using Godot;
using System;

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
    public event Action<ThrowableObject, Node> HitSomething;

    private NavigationObstacle3D _navObstacle;

    public void Interact(Player player)
    {
        if (_navObstacle != null)
            _navObstacle.AvoidanceEnabled = false;

        player.TryPickup(this);
    }

    public void SetHeldTransparency(bool held)
    {
        SetTransparencyRecursive(this, held ? 0.3f : 0.0f);
    }

    private void SetTransparencyRecursive(Node node, float transparency)
    {
        if (node is GeometryInstance3D geometry)
            geometry.Transparency = transparency;

        foreach (Node child in node.GetChildren())
            SetTransparencyRecursive(child, transparency);
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

        _navObstacle = new NavigationObstacle3D();
        _navObstacle.AvoidanceEnabled = true;
        _navObstacle.Radius = CalculateAutoRadius();
        _navObstacle.Height = 1.0f;

        // Jeśli chcesz, żeby leżący obiekt fizycznie wycinał dziurę w siatce nawigacji (dla Godot 4.2+):
        // _navObstacle.AffectNavigationMesh = true; 

        AddChild(_navObstacle);
    }

    private float CalculateAutoRadius()
    {
        // Szukamy węzła CollisionShape3D w dzieciach obiektu
        foreach (Node child in GetChildren())
        {
            if (child is CollisionShape3D collShape && collShape.Shape is Shape3D shape)
            {
                // W zależności od typu kształtu, wyciągamy promień lub szerokość
                if (shape is BoxShape3D box)
                {
                    // Dla skrzyni bierzemy przekątną/szerokość podstawy
                    return Mathf.Max(box.Size.X, box.Size.Z) * 0.5f;
                }
                else if (shape is SphereShape3D sphere)
                {
                    return sphere.Radius;
                }
                else if (shape is CylinderShape3D cylinder)
                {
                    return cylinder.Radius;
                }
                else if (shape is CapsuleShape3D capsule)
                {
                    return capsule.Radius;
                }
            }
        }

        // Wartość domyślna, jeśli obiekt nie ma standardowego kształtu
        return 0.5f;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (_navObstacle != null)
        {
            _navObstacle.AvoidanceEnabled = !Freeze && !Sleeping;

            // Ponieważ NavigationObstacle3D jako dziecko RigidBody mogłoby mieć opóźnienie w synchronizacji,
            // możemy wymusić jego pozycję na bieżąco, jeśli silnik tego wymaga:
            // _navObstacle.GlobalPosition = GlobalPosition;
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

        HitSomething?.Invoke(this, body);
    }
}
