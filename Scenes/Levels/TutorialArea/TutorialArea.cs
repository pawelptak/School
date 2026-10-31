using Godot;
using System;

public partial class TutorialArea : LevelBase
{
    private Door _door;
    private ReadableNote _announcement;

    public override void _Ready()
    {
        base._Ready();

        _door = GetNode<Door>("Door");
        _door.InteractionEnabled = true;
        _door.Interacted += OnDoorInteracted;

        _announcement = GetNode<ReadableNote>("WallAnnouncement");
        _announcement.NoteContent = string.Format(_announcement.NoteContent, DateTime.Now.AddDays(1).ToString("dd.MM.yyyy"));
    }

    private void OnDoorInteracted()
    {
        SceneTransition.Instance.ChangeScene("res://Scenes/Levels/Level1Corridor/corridor_1_inherited.tscn");
    }
}
