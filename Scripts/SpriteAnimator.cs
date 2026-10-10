
using Godot;

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

    [ExportGroup("Moving")]
    [Export] public Godot.Collections.Array<Texture2D> MovingFrames { get; set; } = new();
    [Export] public float MovingFrameDuration { get; set; } = 0.12f;

    private bool _isTalking;
    private bool _isMoving;
    private bool _isPlayingHit;

    private Godot.Collections.Array<Texture2D> _currentFrames = new();
    private float _currentFrameDuration;
    private float _frameTimer;
    private int _currentFrame;
    private bool _currentAnimationLoops;

    public override void _Ready()
    {
        if (Sprite == null)
        {
            GD.PushError($"{Name}: przypisz Sprite3D w Inspectorze.");
            SetProcess(false);
            return;
        }

        PlayBaseAnimation();
    }

    public override void _Process(double delta)
    {
        if (_currentFrames.Count == 0 || _currentFrameDuration <= 0)
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
                    _isPlayingHit = false;
                    PlayBaseAnimation();
                    return;
                }

                _currentFrame = 0;
            }

            ApplyCurrentFrame();
        }
    }

    public void SetTalking(bool talking)
    {
        if (_isTalking == talking)
            return;

        _isTalking = talking;

        if (!_isPlayingHit)
            PlayBaseAnimation();
    }

    public void SetMoving(bool moving)
    {
        if (_isMoving == moving)
            return;

        _isMoving = moving;

        if (!_isPlayingHit)
            PlayBaseAnimation();
    }

    public void PlayHit()
    {
        if (HitFrames.Count == 0)
        {
            GD.PushWarning($"{Name}: nie przypisano klatek animacji Hit.");
            return;
        }

        _isPlayingHit = true;
        PlayAnimation(HitFrames, HitFrameDuration, false);
    }

    private void PlayBaseAnimation()
    {
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
            _currentFrames.Clear();
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
        _frameTimer = 0;

        ApplyCurrentFrame();
    }

    private void ApplyCurrentFrame()
    {
        if (_currentFrames.Count == 0 || Sprite == null)
            return;

        Sprite.Texture = _currentFrames[_currentFrame];
    }
}
