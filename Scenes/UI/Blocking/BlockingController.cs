
using Godot;

public partial class BlockingController : CanvasLayer
{
	[ExportGroup("Animation")]
	[Export] public Godot.Collections.Array<Texture2D> RaiseFrames { get; set; } = new();
	[Export] public Godot.Collections.Array<Texture2D> LowerFrames { get; set; } = new();
	[Export] public float FrameDuration { get; set; } = 0.02f;

	[ExportGroup("Blocking Sway")]
	[Export] public float SwaySpeed { get; set; } = 1.8f;
	[Export] public float SwayPositionX { get; set; } = 3.0f;
	[Export] public float SwayPositionY { get; set; } = 0.0f;
	[Export] public float SwayRotationDegrees { get; set; } = 0.5f;

	[ExportGroup("Hit Reaction")]
	[Export] public float HitReactionDuration { get; set; } = 0.18f;
	[Export] public float HitReactionPositionX { get; set; } = 14.0f;
	[Export] public float HitReactionPositionY { get; set; } = 7.0f;
	[Export] public float HitReactionRotationDegrees { get; set; } = 1.0f;

	private TextureRect _texture;
	private Vector2 _restPosition;
	private float _restRotation;

	private int _currentFrame;
	private float _frameTimer;
	private float _swayTime;
	private float _hitReactionTimer;
	private float _hitDirection = 1.0f;

	private bool _isPlaying;
	private bool _isRaising;
	private bool _isHolding;

	public bool IsBlocking => _isHolding;
	public bool IsPlaying => _isPlaying;

	public override void _Ready()
	{
		_texture = GetNode<TextureRect>("Container/TextureRect");

		_restPosition = _texture.Position;
		_restRotation = _texture.Rotation;

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
		_hitReactionTimer = 0.0f;

		ResetPose();

		Show();
		ApplyFrame();
	}

	public void StopBlocking()
	{
		GD.Print("BLOCK: StopBlocking() called");
		GD.Print($"_isRaising={_isRaising}, _isHolding={_isHolding}");
		
		if (_isRaising)
		{
			_isRaising = false;
			_isPlaying = false;
			_isHolding = false;

			ResetPose();
			_texture.Hide();
			Hide();
			return;
		}

		if (!_isHolding)
			return;

		_isHolding = false;
		_hitReactionTimer = 0.0f;

		if (LowerFrames.Count == 0)
		{
			ResetPose();
			_texture.Hide();
			Hide();
			return;
		}

		_isRaising = false;
		_isPlaying = true;
		_currentFrame = 0;
		_frameTimer = 0.0f;
		ApplyFrame();
	}

	/// <summary>
	/// Plays a short defensive recoil when an enemy hits the block.
	/// </summary>
	public void PlayHitReaction()
	{
		GD.Print($"BLOCK: PlayHitReaction(), holding={_isHolding}");
		
		if (!_isHolding)
			return;

		_hitDirection *= -1.0f;
		_hitReactionTimer = Mathf.Max(0.01f, HitReactionDuration);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		if (_isPlaying)
			ProcessAnimation(dt);

		if (_isHolding)
			UpdateBlockingPose(dt);
	}

	private void ProcessAnimation(float dt)
	{
		var frames = _isRaising ? RaiseFrames : LowerFrames;

		if (frames.Count == 0)
		{
			FinishAnimation();
			return;
		}

		_frameTimer += dt;
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

	private void UpdateBlockingPose(float dt)
	{
		if (!_isHolding)
			return;

		_swayTime += dt * SwaySpeed;

		float swayX = Mathf.Sin(_swayTime) * SwayPositionX;
		float swayY = Mathf.Sin(_swayTime * 2.0f) * SwayPositionY;
		float swayRotation = Mathf.Sin(_swayTime)
			* Mathf.DegToRad(SwayRotationDegrees);

		float hitProgress = 0.0f;

		if (_hitReactionTimer > 0.0f)
		{
			_hitReactionTimer = Mathf.Max(0.0f, _hitReactionTimer - dt);

			float duration = Mathf.Max(0.01f, HitReactionDuration);
			float progress = 1.0f - (_hitReactionTimer / duration);

			hitProgress = Mathf.Pow(
				Mathf.Max(0.0f, Mathf.Sin(progress * Mathf.Pi)),
				0.65f
			);
		}

		float hitX = -HitReactionPositionX * _hitDirection * hitProgress;
		float hitY = HitReactionPositionY * hitProgress;
		float hitRotation = Mathf.DegToRad(
			-HitReactionRotationDegrees * _hitDirection * hitProgress
		);

		_texture.Position = _restPosition + new Vector2(
			swayX + hitX,
			swayY + hitY
		);

		_texture.Rotation = _restRotation + swayRotation + hitRotation;
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
			_isRaising = false;
			_isHolding = true;

			_texture.Texture = RaiseFrames[^1];
			_texture.Show();
			return;
		}

		_isHolding = false;
		ResetPose();
		_texture.Hide();
		Hide();
	}


	private void ResetPose()
	{
		if (_texture == null)
			return;

		_texture.Position = _restPosition;
		_texture.Rotation = _restRotation;
	}
}
