using Godot;
using System;

public partial class CountdownScreen : CanvasLayer
{
    [Signal]
    public delegate void CountdownFinishedEventHandler();

    private Label _label;
    private ColorRect _colorRect;

    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
        _colorRect = GetNode<ColorRect>("ColorRect");
        
        Visible = false;
    }

    public async void StartCountdown()
    {
        Visible = true;
        GetTree().Paused = true;

        for (int i = 3; i > 0; i--)
        {
            _label.Text = i.ToString();

            await ToSignal(GetTree().CreateTimer(1.0f, true, false, true), SceneTreeTimer.SignalName.Timeout);
        }

        _label.Text = "START!";
        await ToSignal(GetTree().CreateTimer(0.5f, true, false, true), SceneTreeTimer.SignalName.Timeout);

        Visible = false;
        GetTree().Paused = false;

        EmitSignal(SignalName.CountdownFinished);
        
        QueueFree();
    }
}
