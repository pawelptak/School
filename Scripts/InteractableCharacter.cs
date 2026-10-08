using Godot;
using System;

public partial class InteractableCharacter : CharacterBody3D
{
    public bool PlayerInRange { get; private set; }

    public event Action<ThrowableObject, float> HitByThrowable;

    public Func<ThrowableObject, float, bool> HitReactionOverride { get; set; }

    public override void _Ready()
    {
        var interactionArea = GetNode<Area3D>("InteractionArea");

        interactionArea.BodyEntered += OnBodyEntered;
        interactionArea.BodyExited += OnBodyExited;
    }

    public void NotifyHitByThrowable(ThrowableObject throwable, float throwForce)
    {
        HitByThrowable?.Invoke(throwable, throwForce);

        var controller = GetTree().CurrentScene
            ?.GetNodeOrNull<CutsceneController>("CutsceneController");

        if (controller?.IsDialogueVisible == true)
            return;

        if (HitReactionOverride?.Invoke(throwable, throwForce) == true)
            return;

        var dialogue = GetHitReactionDialogue();

        if (controller != null && dialogue.Length > 0)
        {
            controller.StartDialogue(this, dialogue);
        }
    }

    protected virtual DialogueLine[] GetHitReactionDialogue()
    {
        return Array.Empty<DialogueLine>();
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is Player)
        {
            PlayerInRange = true;
        }
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is Player)
        {
            PlayerInRange = false;
        }
    }
}