public partial class AsianStudentMale : InteractableCharacter
{
    public override string Name => DialogueSpeakerExtensions.GetDisplayName(DialogueSpeaker.AsianStudentMale);

    protected override DialogueLine[] GetDefeatDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.AsianStudentMale, AsianStudentMaleText.Defeat, 2.5f)];
    }

    protected override DialogueLine[] GetHitReactionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.AsianStudentMale, AsianStudentMaleText.HitByObject, 2.5f)];
    }

    protected override DialogueLine[] GetInteractionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.AsianStudentMale, AsianStudentMaleText.Interaction, 2.5f)];
    }

    protected override DialogueLine[] GetRecoveryDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.AsianStudentMale, AsianStudentMaleText.Recovery, 2.5f)];
    }
}
