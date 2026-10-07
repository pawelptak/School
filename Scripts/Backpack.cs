using Godot;

public partial class Backpack : ThrowableObject
{
    public override void _Ready()
    {
        base._Ready();

        BodyEntered += OnBackpackBodyEntered;
    }

    private void OnBackpackBodyEntered(Node body)
    {
        if (body is InteractableCharacter character)
        {
            character.NotifyHitByThrowable(ThrowForce);
        }
    }
}