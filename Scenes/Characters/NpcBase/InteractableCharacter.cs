
using Godot;
using System;

public abstract partial class InteractableCharacter : CharacterBody3D, IInteractable
{
    public enum DefeatBehavior
    {
        PermanentBody,
        RecoverAfterDelay
    }

    [ExportGroup("Defeat")]
    [Export] public DefeatBehavior DefeatMode { get; set; }
    [Export] public float RecoveryTime { get; set; } = 10.0f;

    public bool PlayerInRange { get; private set; }

    public string InteractionText => "Rozmawiaj";
    public Vector3 PromptOffset => new Vector3(0, 2.0f, 0);

    public event Action<ThrowableObject, float> HitByThrowable;
    public event Action<Player> InteractionRequested;

    public Func<ThrowableObject, float, bool> HitReactionOverride { get; set; }

    protected SpriteAnimator SpriteAnimator { get; private set; }
    private NpcHealthController _healthController;
    private NpcController _npcController;
    private bool _isDefeated;
    private PackedScene _deadBodyScene;
    private const string DeadBodyScenePath = "res://Scenes/Objects/DeadNpcBody/dead_npc_body.tscn";

    public override void _Ready()
    {
        SpriteAnimator = GetNode<SpriteAnimator>("SpriteAnimator");
        _healthController = GetNodeOrNull<NpcHealthController>("NpcHealthController");
        _npcController = GetNodeOrNull<NpcController>("NpcController");
        _deadBodyScene = GD.Load<PackedScene>(DeadBodyScenePath);

        if (_healthController != null)
            _healthController.Died += OnCharacterDied;

        var interactionArea = GetNode<Area3D>("InteractionArea");

        interactionArea.BodyEntered += OnBodyEntered;
        interactionArea.BodyExited += OnBodyExited;
    }

    public bool CanInteract(Player player)
    {
        return !_isDefeated
            && PlayerInRange
            && !player.MovementLocked
            && !IsDialogueVisible();
    }

    public void SetTalking(bool talking)
    {
        SpriteAnimator?.SetTalking(talking);
    }

    public void Interact(Player player)
    {
        if (_isDefeated)
            return;

        InteractionRequested?.Invoke(player);

        if (IsDialogueVisible())
            return;

        var dialogue = GetInteractionDialogue();

        if (dialogue == null || dialogue.Length == 0)
        {
            GD.PushError($"{Name}: interaction dialogue is empty.");
            return;
        }

        var controller = GetTree().CurrentScene
            ?.GetNodeOrNull<CutsceneController>("CutsceneController");

        controller?.StartDialogue(this, dialogue);
    }

    public void NotifyHitByThrowable(ThrowableObject throwable, float throwForce)
    {
        if (_isDefeated)
            return;

        _healthController?.TakeDamage(throwable.Damage);

        _npcController?.ApplyHitEffects(
            throwable,
            throwable.Knockback,
            throwable.StunDuration
        );

        HitByThrowable?.Invoke(throwable, throwForce);

        var controller = GetTree().CurrentScene
            ?.GetNodeOrNull<CutsceneController>("CutsceneController");

        if (controller?.IsDialogueVisible == true)
            return;

        SpriteAnimator?.PlayHit();

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

    private void OnCharacterDied()
    {
        if (_isDefeated)
            return;

        _isDefeated = true;

        switch (DefeatMode)
        {
            case DefeatBehavior.PermanentBody:
                SpriteAnimator?.PlayPermanentDead();
                BecomeThrowableBody();
                break;

            case DefeatBehavior.RecoverAfterDelay:
                SpriteAnimator?.PlayTemporaryDead();
                RecoverAfterDelay();
                break;
        }
    }

    private async void RecoverAfterDelay()
    {
        float delay = Mathf.Max(0.1f, RecoveryTime);

        await ToSignal(
            GetTree().CreateTimer(delay),
            SceneTreeTimer.SignalName.Timeout
        );

        if (!IsInsideTree() || !GodotObject.IsInstanceValid(this))
            return;

        _healthController?.RestoreHealth();
        SpriteAnimator?.RecoverFromDead();

        _isDefeated = false;

        _npcController?.RecoverFromDefeat();

        GD.Print($"[NPC] {Name} got back up.");
    }

    private void BecomeThrowableBody()
    {
        if (_deadBodyScene == null)
        {
            GD.PushError($"{Name}: DeadBodyScene is not assigned.");
            return;
        }

        var sourceShape = GetNodeOrNull<CollisionShape3D>(
            "CollisionShape3D"
        );

        var sprite = SpriteAnimator?.Sprite;

        if (sourceShape?.Shape == null || sprite == null)
        {
            GD.PushError(
                $"{Name}: missing CollisionShape3D or Sprite3D."
            );
            return;
        }

        var deadBody = _deadBodyScene.Instantiate<ThrowableObject>();

        var deadShape = deadBody.GetNodeOrNull<CollisionShape3D>(
            "CollisionShape3D"
        );

        if (deadShape == null)
        {
            GD.PushError(
                "CollisionShape3D needs to be child of DeadBodyScene."
            );

            deadBody.Free();
            return;
        }

        deadShape.Shape = (Shape3D)sourceShape.Shape.Duplicate();
        deadShape.Transform = sourceShape.Transform;

        var scene = GetTree().CurrentScene;

        scene.AddChild(deadBody);
        deadBody.GlobalTransform = GlobalTransform;

        sprite.Reparent(deadBody, true);

        GD.Print($"[NPC] {Name} became a throwable body.");

        QueueFree();
    }

    public override void _ExitTree()
    {
        if (_healthController != null &&
            GodotObject.IsInstanceValid(_healthController))
        {
            _healthController.Died -= OnCharacterDied;
        }
    }
}
