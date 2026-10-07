public enum DialogueSpeaker
{
    Player,
    Mate
}

public static class DialogueSpeakerExtensions
{
    public static string GetDisplayName(this DialogueSpeaker speaker)
    {
        return speaker switch
        {
            DialogueSpeaker.Player => CharacterText.PlayerName,
            DialogueSpeaker.Mate => CharacterText.MateName,
            _ => ""
        };
    }
}
