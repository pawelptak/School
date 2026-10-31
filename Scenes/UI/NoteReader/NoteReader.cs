
using Godot;

public partial class NoteReader : CanvasLayer
{
    private Control _background;
    private Label _header;
    private RichTextLabel _content;
    private Button _closeButton;
    private HSeparator _separator;

    private bool _isOpen;

    private int _defaultHeaderFontSize;
    private int _defaultContentFontSize;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _background = GetNode<Control>("Background");

        _header = GetNode<Label>(
            "Background/CenterContainer/Paper/MarginContainer/VBoxContainer/Header");

        _content = GetNode<RichTextLabel>(
            "Background/CenterContainer/Paper/MarginContainer/VBoxContainer/Content");

        _separator = GetNode<HSeparator>(
            "Background/CenterContainer/Paper/MarginContainer/VBoxContainer/Separator");

        _closeButton = GetNode<Button>("Background/CloseButton");
        _closeButton.Pressed += Close;

        Hide();
    }

    public void Open(string title, string content, FontFile font)
    {
        if (_isOpen)
            return;

        _isOpen = true;

        bool hasTitle = !string.IsNullOrWhiteSpace(title);

        _header.Text = title;
        _header.Visible = hasTitle;
        _separator.Visible = hasTitle;

        _content.Text = content;

        _header.RemoveThemeFontOverride("font");
        _content.RemoveThemeFontOverride("normal_font");

        if (font != null)
        {
            _header.AddThemeFontOverride("font", font);
            _content.AddThemeFontOverride("normal_font", font);
        }

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
