public partial class PolishTeacher : InteractableCharacter
{
    public override string Name => DialogueSpeakerExtensions.GetDisplayName(DialogueSpeaker.PolishTeacher);

    protected override DialogueLine[] GetDefeatDialogue()
	{
		return [new DialogueLine(DialogueSpeaker.PolishTeacher, PolishTeacherText.Defeat, 2.0f)];
	}

	protected override DialogueLine[] GetHitReactionDialogue()
	{
		return [new DialogueLine(DialogueSpeaker.PolishTeacher, PolishTeacherText.HitByObject, 3.0f)];
	}

	protected override DialogueLine[] GetInteractionDialogue()
	{
		return [new DialogueLine(DialogueSpeaker.PolishTeacher, PolishTeacherText.Interaction, 1.0f)];
	}

	protected override DialogueLine[] GetRecoveryDialogue()
	{
		return [new DialogueLine(DialogueSpeaker.PolishTeacher, PolishTeacherText.Recovery, 2.0f)];
	}
}
