using Godot;

public partial class NoteReader : CanvasLayer
{
    private Control _background;
    private Label _header;
    private RichTextLabel _content;
    private Button _closeButton;

    private bool _isOpen;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _background = GetNode<Control>("Background");
        _header = GetNode<Label>(
            "Background/CenterContainer/Paper/MarginContainer/VBoxContainer/Header");

        _content = GetNode<RichTextLabel>(
            "Background/CenterContainer/Paper/MarginContainer/VBoxContainer/Content");

        _closeButton = GetNode<Button>(
            "Background/CloseButton");

        _closeButton.Pressed += Close;

        Hide();
    }

    public void Open(string title, string content)
    {
        if (_isOpen)
            return;

        _isOpen = true;

        _header.Text = title;
        _content.Text = content;

        Input.MouseMode = Input.MouseModeEnum.Visible;

        var player = GetTree().GetFirstNodeInGroup("player") as Player;
        player?.SetInteractionUIBlocked(true);

        Show();
        GetTree().Paused = true;

        _closeButton.GrabFocus();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isOpen)
            return;

        if (@event.IsActionPressed("ui_cancel"))
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    private void Close()
    {
        if (!_isOpen)
            return;

        _isOpen = false;

        GetTree().Paused = false;
        Hide();

        var player = GetTree().GetFirstNodeInGroup("player") as Player;
        player?.SetInteractionUIBlocked(false);

        Input.MouseMode = Input.MouseModeEnum.Captured;
    }
}
