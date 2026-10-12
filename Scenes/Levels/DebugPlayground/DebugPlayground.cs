using Godot;
using System;
using System.Linq;

public partial class DebugPlayground : Node3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var npcs = GetTree().GetNodesInGroup("NPCs")
		.OfType<InteractableCharacter>();

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
		}

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
