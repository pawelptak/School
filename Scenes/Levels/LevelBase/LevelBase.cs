using Godot;
using System.Collections.Generic;

public partial class LevelBase : Node3D
{
    public override void _Ready()
    {
        GameManager.Instance?.LoadGame();
        RemoveCollectedWorldItems();
        GD.Print("LevelBase is ready. Removed collected world items.");
    }

    public override void _Process(double delta)
    {
    }

    private void RemoveCollectedWorldItems()
    {
        if (GameManager.Instance == null) return;

        var collectedItems = GameManager.Instance.CurrentSaveData.CollectedWorldItems;
        if (collectedItems == null || collectedItems.Count == 0) return;

        CheckAndRemoveRecursive(this, collectedItems);
    }

    private void CheckAndRemoveRecursive(Node currentNode, List<string> collectedItems)
    {
        foreach (Node child in currentNode.GetChildren())
        {
            if (child is ThrowableObject throwableObj)
            {
                string worldId = throwableObj.WorldItemID;

                if (collectedItems.Contains(worldId))
                {
                    throwableObj.QueueFree();
                    continue;
                }
            }

            if (child.GetChildCount() > 0)
            {
                CheckAndRemoveRecursive(child, collectedItems);
            }
        }
    }
}
