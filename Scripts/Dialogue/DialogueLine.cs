using System;

public class DialogueLine
{
    public DialogueSpeaker Speaker { get; }
    public string SpeakerName  => Speaker.GetDisplayName();
    public string Text { get; }
    public float DisplayDurationSeconds { get; }

    public DialogueLine(
        DialogueSpeaker speaker,
        string text,
        float displayDurationSeconds
    )
    {
        if (displayDurationSeconds <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(displayDurationSeconds),
                "Dialogue duration must be greater than zero."
            );

        Speaker = speaker;
        Text = text;
        DisplayDurationSeconds = displayDurationSeconds;
    }
}
