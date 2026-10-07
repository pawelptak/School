using Godot;
using System;

public partial class Mate : InteractableCharacter
{
    private DialogueUI _dialogueUI;

    private bool _waitingForDoorObjective;

    public bool IntroFinished { get; set; }

    public event Action GoToDoor;

    public override void _Ready()
    {
        base._Ready();

        _dialogueUI = GetTree().CurrentScene.GetNode<DialogueUI>("DialogueUI");

        HitByThrowable += OnHitByThrowable;
        _dialogueUI.DialogueFinished += OnDialogueFinished;
    }

    protected override void OnInteraction()
    {
        if (!IntroFinished)
            return;
    }

    private void OnHitByThrowable(float throwForce)
    {
        if (throwForce < 15.0f)
        {
            _dialogueUI.ShowDialogue(new string[]
            {
                string.Format(GameText.MateThrowAgain, "LPM")
            });

            return;
        }

        _waitingForDoorObjective = true;

        _dialogueUI.ShowDialogue(new string[]
        {
            GameText.MateThrowGood,
            GameText.MateGoToDoor
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