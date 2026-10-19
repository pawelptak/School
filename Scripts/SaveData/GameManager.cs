using Godot;
using System.Collections.Generic;
using System.Text.Json;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    public PlayerSaveData CurrentSaveData { get; set; } = new PlayerSaveData();

    private const string SavePath = "user://savegame.json";

    public override void _Ready()
    {
        if (Instance != null && Instance != this)
        {
            QueueFree();
            return;
        }
        Instance = this;
    }

    public void SaveGame()
    {
        var player = GetTree().CurrentScene?.GetNodeOrNull<Player>("Player");
        if (player != null)
        {
            player.SaveToData(CurrentSaveData);
        }

        try
        {
            string jsonString = JsonSerializer.Serialize(CurrentSaveData, new JsonSerializerOptions { WriteIndented = true });
            var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
            if (file != null)
            {
                file.StoreString(jsonString);
                file.Close();
                GD.Print("Game saved to: " + ProjectSettings.GlobalizePath(SavePath));
            }
        }
        catch (System.Exception e)
        {
            GD.PrintErr("Error saving game: " + e.Message);
        }
    }

    public void LoadGame()
    {
        if (!FileAccess.FileExists(SavePath))
        {
            GD.Print("No save file found. Creating a new game state.");
            CurrentSaveData = new PlayerSaveData();
            return;
        }

        try
        {
            var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
            string jsonString = file.GetAsText();
            file.Close();

            CurrentSaveData = JsonSerializer.Deserialize<PlayerSaveData>(jsonString) ?? new PlayerSaveData();

            var player = GetTree().CurrentScene?.GetNodeOrNull<Player>("Player");
            if (player != null)
            {
                player.LoadFromData(CurrentSaveData);
            }

            GD.Print("Game loaded successfully.");
        }
        catch (System.Exception e)
        {
            GD.PrintErr("Error loading game: " + e.Message);
            CurrentSaveData = new PlayerSaveData();
        }
    }

    public void SaveMinigameResult(string minigameId, double score, int grade)
    {
        if (string.IsNullOrWhiteSpace(minigameId))
        {
            GD.PrintErr("Cannot save minigame result: ID is empty.");
            return;
        }

        if (grade < 1 || grade > 6)
        {
            GD.PrintErr($"Cannot save minigame result: invalid grade {grade}.");
            return;
        }

        if (double.IsNaN(score) || double.IsInfinity(score))
        {
            GD.PrintErr("Cannot save minigame result: score is invalid.");
            return;
        }

        CurrentSaveData ??= new PlayerSaveData();
        CurrentSaveData.MinigameResults ??= new Dictionary<string, MinigameResultSaveData>();

        CurrentSaveData.MinigameResults[minigameId] = new MinigameResultSaveData
        {
            Score = score,
            Grade = grade
        };

        SaveGame();

        GD.Print($"Saved minigame '{minigameId}': score={score}, grade={grade}");
    }

    public MinigameResultSaveData GetMinigameResult(string minigameId)
    {
        if (CurrentSaveData?.MinigameResults == null)
            return null;

        return CurrentSaveData.MinigameResults.TryGetValue(
            minigameId,
            out var result)
            ? result
            : null;
    }

    public string GetActionKeyName(string actionName)
    {
        foreach (var inputEvent in InputMap.ActionGetEvents(actionName))
        {
            if (inputEvent is InputEventKey keyEvent)
            {
                var keycode = keyEvent.Keycode != Key.None
                    ? keyEvent.Keycode
                    : keyEvent.PhysicalKeycode;

                if (keycode != Key.None)
                    return OS.GetKeycodeString(keycode);
            }
        }

        return actionName;
    }
}
