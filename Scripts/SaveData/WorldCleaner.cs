using Godot;
using System.Collections.Generic;

public partial class WorldCleaner : Node
{
    public override void _Ready()
    {
        Node rootScene = GetParent();
        if (rootScene == null || GameManager.Instance == null) return;

        var collectedItems = GameManager.Instance.CurrentSaveData.CollectedWorldItems;
        if (collectedItems == null || collectedItems.Count == 0) return;

        CheckAndRemoveRecursive(rootScene, collectedItems);
    }

    private void CheckAndRemoveRecursive(Node currentNode, List<string> collectedItems)
    {
        foreach (Node child in currentNode.GetChildren())
        {
            if (child is ThrowableObject throwableObj)
            {
                if (throwableObj.DroppedFromInventory) continue;

                string worldId = $"{throwableObj.SceneFilePath}_{throwableObj.GlobalPosition.X:F2}_{throwableObj.GlobalPosition.Y:F2}_{throwableObj.GlobalPosition.Z:F2}";

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
