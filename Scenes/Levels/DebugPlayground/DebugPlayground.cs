using Godot;
using System;
using System.Linq;

public partial class DebugPlayground : Node3D
{
	private CutsceneController _cutsceneController;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var npcs = GetTree().GetNodesInGroup("NPCs")
		.OfType<InteractableCharacter>();

		_cutsceneController = GetNode<CutsceneController>("CutsceneController");

		var player = GetTree().GetFirstNodeInGroup("player") as Player;

		foreach (var node in GetTree().GetNodesInGroup("NPCs"))
		{
			GD.Print($"Found node in NPCs group: {node.Name}");
			if (node is not InteractableCharacter npc)
			{
				GD.Print($"Node {node.Name} is not an InteractableCharacter.");
				continue;
			}

			var npcController = npc.GetNodeOrNull<NpcController>("NpcController");

			npcController?.StartChase(player);

			npc.InteractionRequested += (player) =>
			{
				_cutsceneController.StartDialogue(
					npc,
					[
						new DialogueLine(DialogueSpeaker.AsianStudentMale, "hej!", 1.0f),
					],
					onFinished: () => npcController.StartChase(player)
				);
			};
		}
	}
}
