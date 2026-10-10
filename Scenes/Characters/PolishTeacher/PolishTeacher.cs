public partial class PolishTeacher : InteractableCharacter
{
    protected override DialogueLine[] GetHitReactionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.PolishTeacher, PolishTeacherText.HitByObject, 3.0f)];
    }

    protected override DialogueLine[] GetInteractionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.PolishTeacher, PolishTeacherText.Interaction, 1.0f)];
    }
}
