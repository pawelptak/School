public enum DialogueSpeaker
{
    Player,
    Mate,
    Bully,
    PolishTeacher,
    AsianStudentMale,
    Janitor,
    PeTeacher,
    BullyMinion1,
    BullyMinion2
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
            DialogueSpeaker.Janitor => JanitorText.Name,
            DialogueSpeaker.PeTeacher => PeTeacherText.Name,
            DialogueSpeaker.BullyMinion1 => BullyMinion1Text.Name,
            DialogueSpeaker.BullyMinion2 => BullyMinion2Text.Name,
            _ => ""
        };
    }
}
