using Godot;
using System;
using System.Collections.Generic;

[Serializable]
public class PlayerSaveData
{
    public bool AttackingUnlocked { get; set; } = false;
    public bool BlockingUnlocked { get; set; } = false;
    public bool GradeJournalUnlocked { get; set; } = false;

    public List<InventoryItemSaveData> InventoryItems { get; set; } = new List<InventoryItemSaveData>();

    public List<string> CollectedWorldItems { get; set; } = new List<string>();

    public Dictionary<string, MinigameResultSaveData> MinigameResults { get; set; } = new();

}

[Serializable]
public class InventoryItemSaveData
{
    public string ItemName { get; set; }
    public string SceneFilePath { get; set; }
    public int Count { get; set; }
    public List<string> WorldItemIDs { get; set; } = new();
}

[Serializable]
public class MinigameResultSaveData
{
    public double Score { get; set; }
    public int Grade { get; set; }
}
