using Godot;

public partial class TutorialArea : Node3D
{
    private Door _door;

    public override void _Ready()
    {
        _door = GetNode<Door>("Door");
        _door.InteractionEnabled = true;
        _door.Interacted += OnDoorInteracted;
    }

    private void OnDoorInteracted()
    {
        SceneTransition.Instance.ChangeScene("res://Scenes/Levels/Level1Corridor/corridor_1.tscn");
    }
}
