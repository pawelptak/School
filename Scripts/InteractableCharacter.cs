
using Godot;
using System;

public abstract partial class InteractableCharacter : CharacterBody3D, IInteractable
{
    public bool PlayerInRange { get; private set; }

    public string InteractionText => "Rozmawiaj";
    public Vector3 PromptOffset => new Vector3(0, 2.0f, 0);

    public event Action<ThrowableObject, float> HitByThrowable;
    public event Action<Player> InteractionRequested;

    public Func<ThrowableObject, float, bool> HitReactionOverride { get; set; }

    public override void _Ready()
    {
        var interactionArea = GetNode<Area3D>("InteractionArea");

        interactionArea.BodyEntered += OnBodyEntered;
        interactionArea.BodyExited += OnBodyExited;
    }

    public bool CanInteract(Player player)
    {
        return PlayerInRange
            && !player.MovementLocked
            && !IsDialogueVisible();
    }

    public void Interact(Player player)
    {
        var dialogue = GetInteractionDialogue();

        if (dialogue == null || dialogue.Length == 0)
        {
            GD.PushError($"{Name}: interaction dialogue is empty.");
            return;
        }

        InteractionRequested?.Invoke(player);

        var controller = GetTree().CurrentScene
            ?.GetNodeOrNull<CutsceneController>("CutsceneController");

        controller?.StartDialogue(this, dialogue);
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
            controller.StartDialogue(this, dialogue);
    }

    protected abstract DialogueLine[] GetHitReactionDialogue();

    protected abstract DialogueLine[] GetInteractionDialogue();

    private bool IsDialogueVisible()
    {
        var controller = GetTree().CurrentScene
            ?.GetNodeOrNull<CutsceneController>("CutsceneController");

        return controller?.IsDialogueVisible == true;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is Player)
            PlayerInRange = true;
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is Player)
            PlayerInRange = false;
    }
}