using Godot;

public partial class NpcStunIndicator : Sprite3D
{
    [Export] private SubViewport _viewport;
    [Export] private ProgressBar _progressBar;

    private float _stunTimer = 0f;
    private bool _isStunned = false;

    public override void _Ready()
    {
        HideIndicator();
    }

    public override void _Process(double delta)
    {
        if (!_isStunned) return;

        _stunTimer -= (float)delta;

        if (_progressBar != null)
        {
            _progressBar.Value = _stunTimer;
        }

        if (_stunTimer <= 0f)
        {
            HideIndicator();
        }
    }

    public void ApplyStun(float durationInSeconds)
    {
        if (durationInSeconds <= 0f) return;

        _stunTimer = durationInSeconds;
        _isStunned = true;

        if (_progressBar != null)
        {
            _progressBar.MaxValue = durationInSeconds;
            _progressBar.Value = durationInSeconds;
        }

        Visible = true;
        SetProcess(true);
    }

    public void HideIndicator()
    {
        _isStunned = false;
        _stunTimer = 0f;
        Visible = false;
        SetProcess(false);
    }
}
