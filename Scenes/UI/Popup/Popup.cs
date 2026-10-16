using Godot;

public partial class Popup : Control
{
    private Button _continueButton;
    private Label _popupHeader;
    private RichTextLabel _popupText;
    private CanvasLayer _hud;

    [Signal]
    public delegate void PopupClosedEventHandler();

    public override void _Ready()
    {
        _hud = GetNodeOrNull<CanvasLayer>("/root/Hud");
        _popupHeader = GetNode<Label>("Panel/MarginContainer/VBoxContainer/Header");
        _popupText = GetNode<RichTextLabel>("Panel/MarginContainer/VBoxContainer/Text");

        ProcessMode = ProcessModeEnum.Always;

        _continueButton = GetNode<Button>("Panel/MarginContainer/VBoxContainer/ContinueButton");
        _continueButton.Pressed += OnContinuePressed;

        Hide();
    }

    private void OnContinuePressed()
    {
        GetTree().Paused = false;
        Hide();
        _hud.Visible = true;
        EmitSignal(SignalName.PopupClosed);
    }

    public void ShowPopup(string header, string text)
    {
        _hud.Visible = false;
        _popupHeader.Text = header;
        _popupText.Text = text;

        Show();
        GetTree().Paused = true;
        _continueButton.GrabFocus();
    }
}
