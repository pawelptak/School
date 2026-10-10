public enum DialogueSpeaker
{
    Player,
    Mate,
    Bully,
    PolishTeacher
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
            DialogueSpeaker.PolishTeacher => PolishTeacherText.Name,
            _ => ""
        };
    }
}
