using Godot;

public partial class IntroGameTitle : CanvasLayer
{
	public override void _Ready()
	{
		SceneTransition.Instance.ChangeScene("res://Scenes/Levels/Tutorial/classroom_tutorial_inherited.tscn", 2.0f);
	}
}
