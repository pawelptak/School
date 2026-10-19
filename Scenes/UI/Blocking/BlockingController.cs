
using Godot;

public partial class BlockingController : CanvasLayer
{
	[ExportGroup("Animation")]
	[Export] public Godot.Collections.Array<Texture2D> RaiseFrames { get; set; } = new();
	[Export] public Godot.Collections.Array<Texture2D> LowerFrames { get; set; } = new();
	[Export] public float FrameDuration { get; set; } = 0.02f;

	private TextureRect _texture;

	private int _currentFrame;
	private float _frameTimer;

	private bool _isPlaying;
	private bool _isRaising;
	private bool _isHolding;

	public bool IsBlocking => _isHolding;
	public bool IsPlaying => _isPlaying;

	public override void _Ready()
	{
		_texture = GetNode<TextureRect>("Container/TextureRect");
		_texture.Hide();
		Hide();
	}

	public void StartBlocking()
	{
		if (_isPlaying || _isHolding)
			return;

		if (RaiseFrames.Count == 0)
		{
			GD.PushWarning("BlockingController: RaiseFrames is empty.");
			return;
		}

		_isRaising = true;
		_isHolding = false;
		_isPlaying = true;
		_currentFrame = 0;
		_frameTimer = 0.0f;

		Show();
		ApplyFrame();
	}

	public void StopBlocking()
	{
		if (_isRaising)
		{
			_isRaising = false;
			_isPlaying = false;
			_isHolding = false;
			_texture.Hide();
			Hide();
			return;
		}

		if (!_isHolding)
			return;

		_isHolding = false;

		if (LowerFrames.Count == 0)
		{
			_texture.Hide();
			Hide();
			return;
		}

		_isPlaying = true;
		_currentFrame = 0;
		_frameTimer = 0.0f;
		ApplyFrame();
	}

	public override void _Process(double delta)
	{
		if (!_isPlaying)
			return;

		var frames = _isRaising ? RaiseFrames : LowerFrames;

		if (frames.Count == 0)
		{
			FinishAnimation();
			return;
		}

		_frameTimer += (float)delta;
		float duration = Mathf.Max(0.001f, FrameDuration);

		while (_frameTimer >= duration && _isPlaying)
		{
			_frameTimer -= duration;

			if (_currentFrame < frames.Count - 1)
			{
				_currentFrame++;
				ApplyFrame();
			}
			else
			{
				FinishAnimation();
			}
		}
	}

	private void ApplyFrame()
	{
		var frames = _isRaising ? RaiseFrames : LowerFrames;

		if (_currentFrame < 0 || _currentFrame >= frames.Count)
			return;

		_texture.Texture = frames[_currentFrame];
		_texture.Show();
	}

	private void FinishAnimation()
	{
		_isPlaying = false;

		if (_isRaising)
		{
			_isHolding = true;
			_texture.Texture = RaiseFrames[^1];
			_texture.Show();
			return;
		}

		_texture.Hide();
		Hide();
	}
}
