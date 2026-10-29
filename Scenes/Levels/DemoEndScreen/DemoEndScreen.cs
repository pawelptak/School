using Godot;

public partial class DemoEndScreen : CanvasLayer
{
    public override async void _Ready()
    {
        await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);
        // SceneTransition.Instance.ChangeScene("res://Scenes/Levels/MainMenu/main_menu.tscn");

    }
}
