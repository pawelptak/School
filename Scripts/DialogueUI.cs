using Godot;
using System;

public partial class DialogueUI : Control
{
	private RichTextLabel _text;

	private DialogueLine[] _messages;
	private int _currentMessage;
	private Action _onFinished;

	public event Action DialogueFinished;

	public override void _Ready()
	{
		_text = GetNode<RichTextLabel>("MarginContainer/Text");

		Hide();
	}

	public void ShowDialogue(
		DialogueLine[] messages,
		Action onFinished = null
	)
	{
		_messages = messages;
		_currentMessage = 0;
		_onFinished = onFinished;

		ShowCurrentMessage();
		Show();
	}

	public void NextMessage()
	{
		_currentMessage++;

		if (_currentMessage >= _messages.Length)
		{
			Hide();

			var onFinished = _onFinished;
			_onFinished = null;

			DialogueFinished?.Invoke();
			onFinished?.Invoke();

			return;
		}

		ShowCurrentMessage();
	}

	public bool IsVisible()
	{
		return Visible;
	}

	private void ShowCurrentMessage()
	{
		var message = _messages[_currentMessage];

		_text.Text = $"[color=#ff9900]{message.SpeakerName}:[/color] {message.Text}";
	}
}
