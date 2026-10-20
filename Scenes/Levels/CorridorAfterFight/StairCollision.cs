using Godot;

public partial class StairCollision : StaticBody3D, IInteractable
{
    string IInteractable.Name => "Schody na dół";

    public string InteractionText => "Zejdź";

    public Vector3 PromptOffset => new Vector3(0, 0, 0);

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public bool CanInteract(Player player)
    {
        return true;
    }

    public void Interact(Player player)
    {
        // SceneTransition.Instance.ChangeScene("res://Scenes/Levels/Downstairs/downstairs.tscn");
    }
}
