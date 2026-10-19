
using Godot;
using System;

public partial class SpriteAnimator : Node
{
    [Export] public Sprite3D Sprite { get; set; }

    [ExportGroup("Idle")]
    [Export] public Godot.Collections.Array<Texture2D> IdleFrames { get; set; } = new();
    [Export] public float IdleFrameDuration { get; set; } = 0.15f;

    [ExportGroup("Talking")]
    [Export] public Godot.Collections.Array<Texture2D> TalkingFrames { get; set; } = new();
    [Export] public float TalkingFrameDuration { get; set; } = 0.1f;

    [ExportGroup("Hit")]
    [Export] public Godot.Collections.Array<Texture2D> HitFrames { get; set; } = new();
    [Export] public float HitFrameDuration { get; set; } = 0.5f;

    [ExportGroup("Attack")]
    [Export] public Godot.Collections.Array<Texture2D> AttackFrames { get; set; } = new();
    [Export] public float AttackFrameDuration { get; set; } = 0.12f;
    [Export] public int AttackHitFrameIndex { get; set; } = 2;

    [ExportGroup("Heavy Attack")]
    [Export] public Godot.Collections.Array<Texture2D> HeavyAttackFrames { get; set; } = new();
    [Export] public float HeavyAttackFrameDuration { get; set; } = 0.16f;
    [Export] public int HeavyAttackHitFrameIndex { get; set; } = 3;

    [ExportGroup("Moving")]
    [Export] public Godot.Collections.Array<Texture2D> MovingFrames { get; set; } = new();
    [Export] public float MovingFrameDuration { get; set; } = 0.12f;

    [ExportGroup("Dead")]
    [Export] public Texture2D DeadTexture { get; set; }

    // Existing events retained for compatibility.
    public event Action AttackHitFrame;
    public event Action AttackAnimationFinished;

    // Provides the attack type to combat logic.
    public event Action<bool> AttackHitFrameWithType;

    private const float TemporaryDeadOffsetY = -0.8f;

    private bool _isTalking;
    private bool _isMoving;
    private bool _isPlayingHit;
    private bool _isPlayingAttack;
    private bool _isPlayingHeavyAttack;
    private bool _isDead;
    private bool _attackHitFrameTriggered;

    private Vector3 _originalSpritePosition;

    private Godot.Collections.Array<Texture2D> _currentFrames = new();
    private float _currentFrameDuration;
    private float _frameTimer;
    private int _currentFrame;
    private bool _currentAnimationLoops;

    private Color _originalModulate;
    private Tween _hitTween;

    public override void _Ready()
    {
        if (Sprite == null)
        {
            GD.PushError($"{Name}: przypisz Sprite3D w Inspectorze.");
            SetProcess(false);
            return;
        }

        _originalSpritePosition = Sprite.Position;
        _originalModulate = Sprite.Modulate;
        PlayBaseAnimation();
    }

    public void PlayHitFlash(Color flashColor)
    {
        if (Sprite == null || _isDead)
            return;

        _hitTween?.Kill();
        _hitTween = Sprite.CreateTween();

        Sprite.Modulate = flashColor;
        _hitTween.TweenProperty(Sprite, "modulate", Colors.White, 0.15f);

        Vector3 originalScale = new Vector3(1, 0.9f, 1);
        Sprite.Scale = originalScale * 1.15f;
        _hitTween.Parallel().TweenProperty(Sprite, "scale", originalScale, 0.12f);
    }

    public override void _Process(double delta)
    {
        if (_isDead || _currentFrames.Count == 0 || _currentFrameDuration <= 0)
            return;

        _frameTimer += (float)delta;

        while (_frameTimer >= _currentFrameDuration)
        {
            _frameTimer -= _currentFrameDuration;
            _currentFrame++;

            if (_currentFrame >= _currentFrames.Count)
            {
                if (!_currentAnimationLoops)
                {
                    bool wasPlayingAttack = IsPlayingAttack();

                    _isPlayingHit = false;
                    _isPlayingAttack = false;
                    _isPlayingHeavyAttack = false;
                    _attackHitFrameTriggered = false;

                    PlayBaseAnimation();

                    if (wasPlayingAttack)
                        AttackAnimationFinished?.Invoke();

                    return;
                }

                _currentFrame = 0;
            }

            ApplyCurrentFrame();
            CheckAttackHitFrame();
        }
    }

    private bool IsPlayingAttack()
    {
        return _isPlayingAttack || _isPlayingHeavyAttack;
    }

    public void SetTalking(bool talking)
    {
        if (_isDead || _isTalking == talking)
            return;

        _isTalking = talking;

        if (!_isPlayingHit && !IsPlayingAttack())
            PlayBaseAnimation();
    }

    public void SetMoving(bool moving)
    {
        if (_isDead || _isMoving == moving)
            return;

        _isMoving = moving;

        if (!_isPlayingHit && !IsPlayingAttack())
            PlayBaseAnimation();
    }

    public void PlayHit()
    {
        if (_isDead)
            return;

        CancelAttack();

        if (HitFrames.Count == 0)
        {
            GD.PushWarning($"{Name}: HitFrames not set.");
            return;
        }

        _isPlayingHit = true;
        PlayAnimation(HitFrames, HitFrameDuration, false);
    }

    public bool PlayAttack()
    {
        if (_isDead || IsPlayingAttack() || AttackFrames.Count == 0)
        {
            if (!_isDead && AttackFrames.Count == 0)
                GD.PushWarning($"{Name}: AttackFrames not set.");

            return false;
        }

        _isPlayingHit = false;
        _isPlayingAttack = true;
        _isPlayingHeavyAttack = false;
        _attackHitFrameTriggered = false;

        PlayAnimation(AttackFrames, AttackFrameDuration, false);
        CheckAttackHitFrame();

        return true;
    }

    public bool PlayHeavyAttack()
    {
        if (_isDead || IsPlayingAttack() || HeavyAttackFrames.Count == 0)
        {
            if (!_isDead && HeavyAttackFrames.Count == 0)
                GD.PushWarning($"{Name}: HeavyAttackFrames not set.");

            return false;
        }

        _isPlayingHit = false;
        _isPlayingAttack = false;
        _isPlayingHeavyAttack = true;
        _attackHitFrameTriggered = false;

        PlayAnimation(HeavyAttackFrames, HeavyAttackFrameDuration, false);
        CheckAttackHitFrame();

        return true;
    }

    public void CancelAttack()
    {
        if (!IsPlayingAttack())
            return;

        _isPlayingAttack = false;
        _isPlayingHeavyAttack = false;
        _attackHitFrameTriggered = false;

        AttackAnimationFinished?.Invoke();
        PlayBaseAnimation();
    }

    private void CheckAttackHitFrame()
    {
        if (!IsPlayingAttack() || _attackHitFrameTriggered)
            return;

        int configuredHitFrame = _isPlayingHeavyAttack
            ? HeavyAttackHitFrameIndex
            : AttackHitFrameIndex;

        int hitFrame = Mathf.Clamp(
            configuredHitFrame,
            0,
            _currentFrames.Count - 1
        );

        if (_currentFrame != hitFrame)
            return;

        _attackHitFrameTriggered = true;

        GD.Print($"[ATTACK DEBUG] Hit frame reached: {_currentFrame}");

        AttackHitFrame?.Invoke();
        AttackHitFrameWithType?.Invoke(_isPlayingHeavyAttack);
    }

    public void PlayPermanentDead()
    {
        PlayDead();
        Sprite.Position = _originalSpritePosition;
    }

    public void PlayTemporaryDead()
    {
        PlayDead();
        Sprite.Position = _originalSpritePosition
            + new Vector3(0, TemporaryDeadOffsetY, 0);
    }

    private void PlayDead()
    {
        if (Sprite == null)
            return;

        CancelAttack();

        _isDead = true;
        _isTalking = false;
        _isMoving = false;
        _isPlayingHit = false;
        _isPlayingAttack = false;
        _isPlayingHeavyAttack = false;

        _currentFrames = new Godot.Collections.Array<Texture2D>();
        _currentFrame = 0;
        _frameTimer = 0.0f;
        _currentFrameDuration = 0.0f;

        SetProcess(false);

        if (DeadTexture == null)
        {
            GD.PushWarning($"{Name}: DeadTexture not set.");
            return;
        }

        Sprite.Texture = DeadTexture;
    }

    public void RecoverFromDead()
    {
        if (Sprite == null)
            return;

        _isDead = false;
        _isTalking = false;
        _isMoving = false;
        _isPlayingHit = false;
        _isPlayingAttack = false;
        _isPlayingHeavyAttack = false;
        _attackHitFrameTriggered = false;

        Sprite.Position = _originalSpritePosition;

        _currentFrames = new Godot.Collections.Array<Texture2D>();
        _currentFrame = 0;
        _frameTimer = 0.0f;
        _currentFrameDuration = 0.0f;
        _currentAnimationLoops = true;

        SetProcess(true);
        PlayBaseAnimation();
    }

    private void PlayBaseAnimation()
    {
        if (_isDead || _isPlayingHit || IsPlayingAttack())
            return;

        if (_isTalking && TalkingFrames.Count > 0)
        {
            PlayAnimation(TalkingFrames, TalkingFrameDuration, true);
        }
        else if (_isMoving && MovingFrames.Count > 0)
        {
            PlayAnimation(MovingFrames, MovingFrameDuration, true);
        }
        else if (IdleFrames.Count > 0)
        {
            PlayAnimation(IdleFrames, IdleFrameDuration, true);
        }
        else
        {
            _currentFrames = new Godot.Collections.Array<Texture2D>();
        }
    }

    private void PlayAnimation(
        Godot.Collections.Array<Texture2D> frames,
        float frameDuration,
        bool loops)
    {
        if (frames.Count == 0)
            return;

        _currentFrames = frames;
        _currentFrameDuration = Mathf.Max(0.001f, frameDuration);
        _currentAnimationLoops = loops;
        _currentFrame = 0;
        _frameTimer = 0.0f;

        ApplyCurrentFrame();
    }

    private void ApplyCurrentFrame()
    {
        if (_currentFrames.Count == 0 || Sprite == null)
            return;

        Sprite.Texture = _currentFrames[_currentFrame];
    }
}
