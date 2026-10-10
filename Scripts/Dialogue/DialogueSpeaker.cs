public enum DialogueSpeaker
{
    Player,
    Mate,
    Bully
}

public static class DialogueSpeakerExtensions
{
    public static string GetDisplayName(this DialogueSpeaker speaker)
    {
        return speaker switch
        {
            DialogueSpeaker.Player => PlayerText.Name,
            DialogueSpeaker.Mate => MateText.Name,
            DialogueSpeaker.Bully => BullyText.Name,
            _ => ""
        };
    }
}
