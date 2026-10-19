
public static class GradeJournalText
{
    public static string GetGradeText(int grade)
    {
        return grade switch
        {
            1 => "Niedostateczny (1)",
            2 => "Dopuszczający (2)",
            3 => "Dostateczny (3)",
            4 => "Dobry (4)",
            5 => "Bardzo dobry (5)",
            6 => "Celujący (6)",
            _ => "Nieznana ocena"
        };
    }

    public const string PolishClassTitle = "Język polski";
}