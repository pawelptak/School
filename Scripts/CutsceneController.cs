using Godot;

public partial class CutsceneController : Node
{
	private Player _player;
	private DialogueUI _dialogueUI;

	public override void _Ready()
	{
		_player = GetTree().CurrentScene.GetNode<Player>("Player");
		_dialogueUI = GetTree().CurrentScene.GetNode<DialogueUI>("DialogueUI");
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("interact") && _dialogueUI.IsVisible())
		{
			_dialogueUI.NextMessage();
		}
	}

	public void StartDialogue(Node3D character, string[] messages)
	{
		_player.MovementLocked = true;
		_player.CameraLocked = true;

		_player.GetNode<Camera3D>("Camera3D").LookAt(
			character.GlobalPosition + Vector3.Up * 1.0f
		);

		_dialogueUI.ShowDialogue(messages);
	}
}
