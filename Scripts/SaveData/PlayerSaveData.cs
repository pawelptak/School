using System.Collections.Generic;
using System;

[Serializable]
public class PlayerSaveData
{
    public bool AttackingUnlocked { get; set; } = false;
    
    public List<InventoryItemSaveData> InventoryItems { get; set; } = new List<InventoryItemSaveData>();
}

[Serializable]
public class InventoryItemSaveData
{
    public string ItemName { get; set; }
    public string SceneFilePath { get; set; }
    public int Count { get; set; }
}
