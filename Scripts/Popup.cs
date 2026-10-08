using Godot;

public partial class Popup : Control
{
    private Button _continueButton;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _continueButton = GetNode<Button>("Panel/MarginContainer/VBoxContainer/ContinueButton");
        _continueButton.Pressed += OnContinuePressed;

        GetTree().Paused = true;

        _continueButton.GrabFocus();
    }

    private void OnContinuePressed()
    {
        GetTree().Paused = false;
        Hide();
    }
}
