using Godot;
using System;

public abstract partial class InteractableCharacter : CharacterBody3D, IInteractable
{
    public enum DefeatBehavior
    {
        RecoverAfterDelay,
        PermanentBody
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
    private CutsceneController _cutsceneController;
    private const string DeadBodyScenePath =
        "res://Scenes/Objects/DeadNpcBody/dead_npc_body.tscn";

    // Zmieniono typ z ProgressBar na nasz skrypt NpcHealthBar (Sprite3D)
    private NpcHealthBar _healthBarComponent;

    public override void _Ready()
    {
        SpriteAnimator = GetNode<SpriteAnimator>("SpriteAnimator");
        _healthController = GetNodeOrNull<NpcHealthController>("NpcHealthController");
        _npcController = GetNodeOrNull<NpcController>("NpcController");
        _deadBodyScene = GD.Load<PackedScene>(DeadBodyScenePath);
        _cutsceneController = GetTree().CurrentScene?.GetNodeOrNull<CutsceneController>("CutsceneController");

        _healthBarComponent = GetNodeOrNull<NpcHealthBar>("NpcHealthBar");

        if (_healthController != null)
        {
            _healthController.Died += OnCharacterDied;
            _healthController.HealthChanged += OnHealthChanged;

            // Poprawne wywołanie metody UpdateHealth przy starcie
            _healthBarComponent?.UpdateHealth(_healthController.CurrentHealth, _healthController.MaxHealth);
        }

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

        _cutsceneController?.StartDialogue(this, dialogue);
    }

    public void NotifyHitByThrowable(ThrowableObject throwable, float throwForce)
    {
        if (_isDefeated || throwable == null)
            return;

        float damage = throwable.CalculateDamage();
        _healthController?.TakeDamage(damage);

        GD.Print(
            $"[COMBAT] {Name} hit by {throwable.Name}. " +
            $"ThrowForce={throwForce:F1}, Damage={damage:F1}, Knockback={throwable.Knockback:F1}"
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

        if (controller != null && dialogue != null && dialogue.Length > 0)
            controller.StartDialogue(this, dialogue);
    }

    public void NotifyHitByPunch(Player player, float damage, float knockback)
    {
        if (player == null || _isDefeated)
            return;

        if (_healthController == null)
        {
            GD.PushWarning($"{Name}: NpcHealthController not found.");
            return;
        }

        _healthController.TakeDamage(damage);

        GD.Print(
            $"[COMBAT] {Name} hit by player's punch. " +
            $"Damage={damage:F1}, Knockback={knockback:F1}"
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

        if (controller != null && dialogue != null && dialogue.Length > 0)
            controller.StartDialogue(this, dialogue);
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

        _cutsceneController.StartDialogue(this, GetDefeatDialogue());

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

        _cutsceneController.StartDialogue(this, GetRecoveryDialogue());
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

        var sourceShape = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
        var sprite = SpriteAnimator?.Sprite;

        if (sourceShape?.Shape == null || sprite == null)
        {
            GD.PushError($"{Name}: missing CollisionShape3D or Sprite3D.");
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
        _healthBarComponent?.UpdateHealth(currentHealth, _healthController.MaxHealth);
    }
}
