public partial class AsianStudentMale : InteractableCharacter
{
    protected override DialogueLine[] GetHitReactionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.AsianStudentMale, AsianStudentMaleText.HitByObject, 2.5f)];
    }

    protected override DialogueLine[] GetInteractionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.AsianStudentMale, AsianStudentMaleText.Interaction, 2.5f)];
    }
}
