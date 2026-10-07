using Godot;

public partial class DialogueUI : Control
{
	private Label _text;

	private string[] _messages;
	private int _currentMessage;

	public override void _Ready()
	{
		_text = GetNode<Label>("Panel/Text");
		Hide();
	}

	public void ShowDialogue(string[] messages)
	{
		_messages = messages;
		_currentMessage = 0;

		_text.Text = _messages[_currentMessage];
		Show();
	}

	public void NextMessage()
	{
		_currentMessage++;

		if (_currentMessage >= _messages.Length)
		{
			Hide();
			return;
		}

		_text.Text = _messages[_currentMessage];
	}

	public bool IsVisible()
	{
		return Visible;
	}
}
