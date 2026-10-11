public enum DialogueSpeaker
{
    Player,
    Mate,
    Bully,
    PolishTeacher,
    AsianStudentMale,
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
            DialogueSpeaker.AsianStudentMale => AsianStudentMaleText.Name,
            _ => ""
        };
    }
}
