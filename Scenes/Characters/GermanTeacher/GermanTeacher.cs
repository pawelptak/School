public partial class GermanTeacher : InteractableCharacter
{
    public override string Name => GermanTeacherText.Name;

    protected override DialogueLine[] GetDefeatDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.GermanTeacher, GermanTeacherText.Defeat, 2.5f)];
    }

    protected override DialogueLine[] GetHitReactionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.GermanTeacher, GermanTeacherText.HitByObject, 2.5f)];
    }

    protected override DialogueLine[] GetInteractionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.GermanTeacher, GermanTeacherText.Interaction, 2.0f)];
    }

    protected override DialogueLine[] GetRecoveryDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.GermanTeacher, GermanTeacherText.Recovery, 2.5f)];
    }
}
