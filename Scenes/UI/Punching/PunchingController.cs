
using Godot;
using System;

public partial class PunchingController : CanvasLayer
{
    [ExportGroup("Animation")]
    [Export] public Godot.Collections.Array<Texture2D> LeftFrames { get; set; } = new();
    [Export] public Godot.Collections.Array<Texture2D> RightFrames { get; set; } = new();

    [Export] public float FrameDuration { get; set; } = 0.02f;
    [Export] public int HitFrameIndex { get; set; } = 2;

    public event Action PunchHitFrame;

    private TextureRect _leftTexture;
    private TextureRect _rightTexture;

    private Godot.Collections.Array<Texture2D> _currentFrames = new();

    private int _currentFrame;
    private float _frameTimer;

    private bool _isPlaying;
    private bool _isReversing;
    private bool _hitFrameTriggered;
    private bool _nextRightHand = true;
    private bool _playingRightHand;

    public bool IsPlaying => _isPlaying;

    public override void _Ready()
    {
        _leftTexture = GetNode<TextureRect>("PunchLeft/TextureRect");
        _rightTexture = GetNode<TextureRect>("PunchRight/TextureRect");

        _leftTexture.Hide();
        _rightTexture.Hide();
        Hide();
    }

    public bool StartPunch()
    {
        if (_isPlaying)
            return false;

        _playingRightHand = _nextRightHand;

        _currentFrames = _playingRightHand
            ? RightFrames
            : LeftFrames;

        if (_currentFrames.Count == 0)
        {
            GD.PushWarning(
                $"PunchingController: no frames assigned for " +
                $"{(_playingRightHand ? "right" : "left")} hand."
            );

            return false;
        }

        _nextRightHand = !_nextRightHand;

        _currentFrame = 0;
        _frameTimer = 0.0f;
        _isReversing = false;
        _hitFrameTriggered = false;
        _isPlaying = true;

        _leftTexture.Hide();
        _rightTexture.Hide();

        Show();
        ApplyFrame();
        TryTriggerHitFrame();

        return true;
    }

    public override void _Process(double delta)
    {
        if (!_isPlaying)
            return;

        _frameTimer += (float)delta;

        float duration = Mathf.Max(0.001f, FrameDuration);

        while (_frameTimer >= duration && _isPlaying)
        {
            _frameTimer -= duration;

            if (!_isReversing)
            {
                if (_currentFrame < _currentFrames.Count - 1)
                {
                    _currentFrame++;
                    ApplyFrame();
                    TryTriggerHitFrame();
                }
                else
                {
                    // Osiągnięto ostatnią klatkę — zmiana kierunku.
                    _isReversing = true;

                    if (_currentFrame > 0)
                    {
                        _currentFrame--;
                        ApplyFrame();
                    }
                    else
                    {
                        FinishPunch();
                    }
                }
            }
            else
            {
                if (_currentFrame > 0)
                {
                    _currentFrame--;
                    ApplyFrame();
                }
                else
                {
                    // Powrót do pierwszej klatki kończy animację.
                    FinishPunch();
                }
            }
        }
    }

    public void CancelPunch()
    {
        if (!_isPlaying)
            return;

        _isPlaying = false;
        _isReversing = false;
        _hitFrameTriggered = false;
        _frameTimer = 0.0f;
        _currentFrame = 0;

        _leftTexture.Hide();
        _rightTexture.Hide();
        Hide();
    }

    private void ApplyFrame()
    {
        if (_currentFrame < 0 ||
            _currentFrame >= _currentFrames.Count)
        {
            return;
        }

        Texture2D texture = _currentFrames[_currentFrame];

        if (_playingRightHand)
        {
            _rightTexture.Texture = texture;
            _rightTexture.Show();
            _leftTexture.Hide();
        }
        else
        {
            _leftTexture.Texture = texture;
            _leftTexture.Show();
            _rightTexture.Hide();
        }
    }

    private void TryTriggerHitFrame()
    {
        if (_hitFrameTriggered || !_isPlaying)
            return;

        if (_currentFrame < Mathf.Max(0, HitFrameIndex))
            return;

        _hitFrameTriggered = true;
        PunchHitFrame?.Invoke();
    }

    private void FinishPunch()
    {
        _isPlaying = false;

        _leftTexture.Hide();
        _rightTexture.Hide();

        Hide();
    }
}
