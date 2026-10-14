using Godot;

public partial class NpcHealthBar : Sprite3D
{
	[Export] public ProgressBar ProgressBar { get; set; }
	[Export] public bool HideWhenFull { get; set; } = true;
	[Export] public float DisplayDuration { get; set; } = 3.0f;

	private Timer _hideTimer;

	public override void _Ready()
	{
		if (ProgressBar == null)
			ProgressBar = GetNode<ProgressBar>("SubViewport/ProgressBar");

		// Timer do automatycznego ukrywania paska po zadaniu obrażeń
		_hideTimer = new Timer();
		_hideTimer.OneShot = true;
		_hideTimer.WaitTime = DisplayDuration;
		_hideTimer.Timeout += OnHideTimeout;
		AddChild(_hideTimer);

		// Domyślnie ukryj pasek, jeśli HP jest pełne
		Visible = !HideWhenFull;
	}

	public void UpdateHealth(float currentHealth, float maxHealth)
	{
		if (ProgressBar != null)
		{
			ProgressBar.MaxValue = maxHealth;
			ProgressBar.Value = currentHealth;
		}

		// Pokaż pasek przy zmianie zdrowia
		Visible = true;
		_hideTimer.Start();

		// Jeśli postać ma pełne zdrowie lub zginęła, ukryj pasek
		if ((currentHealth >= maxHealth && HideWhenFull) || currentHealth <= 0)
		{
			Visible = false;
		}
	}

	private void OnHideTimeout()
	{
		// Ukryj pasek po czasie, jeśli HP jest pełne
		Visible = false;
	}
}
