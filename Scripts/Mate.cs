using Godot;
using System;

public partial class Mate : InteractableCharacter
{
    private CutsceneController _cutsceneController;

    private bool _waitingForDoorObjective;

    public event Action GoToDoor;

    public override void _Ready()
    {
        base._Ready();

        _cutsceneController = GetTree().CurrentScene.GetNode<CutsceneController>("CutsceneController");

        HitByThrowable += OnHitByThrowable;
        _cutsceneController.DialogueFinished += OnDialogueFinished;
    }

    private void OnHitByThrowable(float throwForce)
    {
        if (throwForce < 15.0f)
        {
            _cutsceneController.StartDialogue(this, new DialogueLine[]
            {
                new DialogueLine(
                    DialogueSpeaker.Mate,
                    string.Format(TutorialText.MateThrowAgain, "LPM")
                )
            });

            return;
        }

        _waitingForDoorObjective = true;

        _cutsceneController.StartDialogue(this, new DialogueLine[]
        {
            new DialogueLine(
                DialogueSpeaker.Mate,
                TutorialText.MateThrowGood
            ),
            new DialogueLine(
                DialogueSpeaker.Mate,
                TutorialText.MateGoToDoor
            )
        });
    }

    private void OnDialogueFinished()
    {
        if (!_waitingForDoorObjective)
            return;

        _waitingForDoorObjective = false;

        GoToDoor?.Invoke();
    }
}
