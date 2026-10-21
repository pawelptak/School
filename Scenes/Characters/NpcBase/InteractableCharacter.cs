using Godot;
using System;

public abstract partial class InteractableCharacter : CharacterBody3D, IInteractable
{
    public enum DefeatBehavior
    {
        RecoverAfterDelay,
        PermanentBody
    }

    [Export]
    public bool PhysicsLocked { get; set; } = false;

    [ExportGroup("Defeat")]
    [Export] public DefeatBehavior DefeatMode { get; set; }
    [Export] public float RecoveryTime { get; set; } = 10.0f;

    public bool PlayerInRange { get; private set; }

    public abstract new string Name { get; }

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
    private CutsceneController _cutsceneController;

    private const string DeadBodyScenePath =
        "res://Scenes/Objects/DeadNpcBody/dead_npc_body.tscn";

    private NpcHealthBar _healthBarComponent;

    [Signal]
    public delegate void ThrowableBodyCreatedEventHandler(ThrowableObject deadBody);

    private Vector3 _initialPosition;

    public override void _PhysicsProcess(double delta)
    {
        if (PhysicsLocked)
        {
            GlobalPosition = _initialPosition;
            Velocity = Vector3.Zero;

            return;
        }

        base._PhysicsProcess(delta);
    }

    public override void _Ready()
    {
        _initialPosition = GlobalPosition;

        SpriteAnimator = GetNode<SpriteAnimator>("SpriteAnimator");

        _healthController =
            GetNodeOrNull<NpcHealthController>("NpcHealthController");

        _npcController =
            GetNodeOrNull<NpcController>("NpcController");

        _deadBodyScene = GD.Load<PackedScene>(DeadBodyScenePath);

        _cutsceneController = GetTree().CurrentScene
            ?.GetNodeOrNull<CutsceneController>("CutsceneController");

        _healthBarComponent =
            GetNodeOrNull<NpcHealthBar>("NpcHealthBar");

        if (_healthController != null)
        {
            _healthController.Died += OnCharacterDied;
            _healthController.HealthChanged += OnHealthChanged;

            _healthBarComponent?.UpdateHealth(
                _healthController.CurrentHealth,
                _healthController.MaxHealth
            );
        }

        var interactionArea = GetNode<Area3D>("InteractionArea");

        interactionArea.BodyEntered += OnBodyEntered;
        interactionArea.BodyExited += OnBodyExited;
    }


    public bool CanInteract(Player player)
    {
        if (_isDefeated || !PlayerInRange || player.MovementLocked)
            return false;

        var controller = GetTree().CurrentScene
            ?.GetNodeOrNull<CutsceneController>("CutsceneController");

        if (controller?.IsCinematicDialogueActive == true)
            return false;

        return true;
    }


    public void SetTalking(bool talking)
    {
        SpriteAnimator?.SetTalking(talking);
    }




    public void Interact(Player player)
    {
        if (_isDefeated)
            return;

        var controller = GetTree().CurrentScene
            ?.GetNodeOrNull<CutsceneController>("CutsceneController");

        bool dialogueWasVisible = controller?.IsDialogueVisible == true;

        InteractionRequested?.Invoke(player);

        if (controller?.IsCinematicDialogueActive == true)
            return;

        if (!dialogueWasVisible && controller?.IsDialogueVisible == true)
            return;

        var dialogue = GetInteractionDialogue();

        if (dialogue == null || dialogue.Length == 0)
        {
            GD.PushError($"{Name}: interaction dialogue is empty.");
            return;
        }

        controller?.StartDialogue(this, dialogue);
    }




    public void NotifyHitByThrowable(
        ThrowableObject throwable,
        float throwForce)
    {
        if (_isDefeated || throwable == null)
            return;

        float damage = throwable.CalculateDamage();

        _healthController?.TakeDamage(damage);

        GD.Print(
            $"[COMBAT] {Name} hit by {throwable.Name}. "
            + $"ThrowForce={throwForce:F1}, Damage={damage:F1}, "
            + $"Knockback={throwable.Knockback:F1}"
        );

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

        controller?.TryStartReactionDialogue(this, dialogue);
    }

    public void NotifyHitByPunch(
        Player player,
        float damage,
        float knockback)
    {
        if (player == null || _isDefeated)
            return;

        if (_healthController == null)
        {
            GD.PushWarning(
                $"{Name}: NpcHealthController not found."
            );

            return;
        }

        _healthController.TakeDamage(damage);

        GD.Print(
            $"[COMBAT] {Name} hit by player's punch. "
            + $"Damage={damage:F1}, Knockback={knockback:F1}"
        );

        if (_isDefeated)
            return;

        Vector3 direction = GlobalPosition - player.GlobalPosition;
        direction.Y = 0.0f;

        _npcController?.ApplyKnockback(direction, knockback);

        var controller = GetTree().CurrentScene
            ?.GetNodeOrNull<CutsceneController>("CutsceneController");

        if (controller?.IsDialogueVisible == true)
            return;

        SpriteAnimator?.PlayHit();

        if (HitReactionOverride?.Invoke(null, 0.0f) == true)
            return;

        var dialogue = GetHitReactionDialogue();

        controller?.TryStartReactionDialogue(this, dialogue);
    }

    protected abstract DialogueLine[] GetHitReactionDialogue();

    protected abstract DialogueLine[] GetInteractionDialogue();

    protected abstract DialogueLine[] GetDefeatDialogue();

    protected abstract DialogueLine[] GetRecoveryDialogue();

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

        if (_healthBarComponent != null)
            _healthBarComponent.Visible = false;

        _cutsceneController?.TryStartReactionDialogue(
            this,
            GetDefeatDialogue()
        );

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

        _cutsceneController?.TryStartReactionDialogue(
            this,
            GetRecoveryDialogue()
        );

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

        var sourceShape =
            GetNodeOrNull<CollisionShape3D>("CollisionShape3D");

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

        //deadShape.Shape = (Shape3D)sourceShape.Shape.Duplicate();
        //deadShape.Transform = sourceShape.Transform;

        var scene = GetTree().CurrentScene;

        scene.AddChild(deadBody);
        deadBody.GlobalTransform = GlobalTransform;

        EmitSignal(SignalName.ThrowableBodyCreated, deadBody);

        sourceShape.QueueFree();
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
            _healthController.HealthChanged -= OnHealthChanged;
        }
    }

    private void OnHealthChanged(float currentHealth)
    {
        _healthBarComponent?.UpdateHealth(
            currentHealth,
            _healthController.MaxHealth
        );
    }
}
