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

        StartMateDialogue();

        GetViewport().SetInputAsHandled();
    }

    private void StartMateDialogue()
    {
        _dialogueUI.ShowDialogue(
            new DialogueLine[]
            {
            new DialogueLine(
                DialogueSpeaker.Mate,
                Corridor1Text.MateHomeWorkQuestion
            ),
            new DialogueLine(
                DialogueSpeaker.Mate,
                Corridor1Text.MateHomeWorkFollowup
            ),
            new DialogueLine(
                DialogueSpeaker.Player,
                Corridor1Text.HomeworkPlayerResponse
            ),
            new DialogueLine(
                DialogueSpeaker.Player,
                Corridor1Text.HomeworkPlayerRequest
            ),
            new DialogueLine(
                DialogueSpeaker.Mate,
                Corridor1Text.MatePlayerResponse1
            ),
            new DialogueLine(
                DialogueSpeaker.Mate,
                Corridor1Text.MatePlayerResponse2
            )
            },
            StartHomeworkMinigame
        );
    }

    private void StartHomeworkMinigame()
    {
        GetTree().ChangeSceneToFile("res://Scenes/homework.tscn");
    }
}
