using Godot;

public partial class Corridor1 : Node3D
{
    private Mate _mate;
    private DialogueUI _dialogueUI;

    public override void _Ready()
    {
        _mate = GetNode<Mate>("Mate");
        _dialogueUI = GetNode<DialogueUI>("DialogueUI");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("interact"))
            return;

        if (_dialogueUI.IsVisible())
            return;

        if (!_mate.PlayerInRange)
            return;

        _dialogueUI.ShowDialogue(new string[]
        {
            GameText.MateHomeWorkQuestion,
            GameText.MateHomeWorkFollowup
        });

        GetViewport().SetInputAsHandled();
    }
}