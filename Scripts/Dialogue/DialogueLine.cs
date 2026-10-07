public class DialogueLine
{
    public DialogueSpeaker Speaker { get; }
    public string SpeakerName  => Speaker.GetDisplayName();
    public string Text { get; }

    public DialogueLine(
        DialogueSpeaker speaker,
        string text
    )
    {
        Speaker = speaker;
        Text = text;
    }
}
