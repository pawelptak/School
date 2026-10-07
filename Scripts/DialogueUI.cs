using Godot;
using System;

public partial class DialogueUI : Control
{
    private Label _text;

    private string[] _messages;
    private int _currentMessage;
    private Action _onFinished;

    public event Action DialogueFinished;

    public override void _Ready()
    {
        _text = GetNode<Label>("Panel/Text");
        Hide();
    }

    public void ShowDialogue(
        string[] messages,
        Action onFinished = null
    )
    {
        _messages = messages;
        _currentMessage = 0;
        _onFinished = onFinished;

        _text.Text = _messages[_currentMessage];
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

        _text.Text = _messages[_currentMessage];
    }

    public bool IsVisible()
    {
        return Visible;
    }
}