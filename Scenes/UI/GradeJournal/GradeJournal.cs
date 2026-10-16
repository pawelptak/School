
using Godot;
using School.Scenes.UI.GradeJournal;

public partial class GradeJournal : CanvasLayer
{
    [Signal]
    public delegate void JournalOpenedEventHandler();

    [Signal]
    public delegate void JournalClosedEventHandler();

    private VBoxContainer _gradesList;
    private Button _closeButton;
    private CanvasLayer _hud;

    public override void _Ready()
    {
        _hud = GetNodeOrNull<CanvasLayer>("/root/Hud");
        _gradesList = GetNode<VBoxContainer>(
            "MarginContainer/Panel/MarginContainer/VBoxContainer/GradesList");

        _closeButton = GetNode<Button>(
            "MarginContainer/Panel/MarginContainer/VBoxContainer/CloseButton");

        _closeButton.Pressed += CloseJournal;

        Hide();
    }

    public void ToggleJournal()
    {
        if (Visible)
        {
            CloseJournal();
            _hud.Visible = true;

            return;
        }

        RefreshGrades();
        _hud.Visible = false;
        Show();
        EmitSignal(SignalName.JournalOpened);
    }

    private void CloseJournal()
    {
        Hide();
        EmitSignal(SignalName.JournalClosed);
    }

    private void RefreshGrades()
    {
        foreach (Node child in _gradesList.GetChildren())
        {
            _gradesList.RemoveChild(child);
            child.QueueFree();
        }

        AddGradeRow(GradeJournalText.PolishClassTitle, "homework_minigame");
    }

    private void AddGradeRow(string title, string minigameId)
    {
        var result = GameManager.Instance.GetMinigameResult(minigameId);

        var row = new HBoxContainer();

        var subjectLabel = new Label
        {
            Text = title,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        var gradeLabel = new Label
        {
            Text = result != null ? GradeJournalText.GetGradeText(result.Grade) : "—",
            CustomMinimumSize = new Vector2(50, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        row.AddChild(subjectLabel);
        row.AddChild(gradeLabel);
        _gradesList.AddChild(row);
    }
}
