using School.Scenes.Characters.PeTeacher;

public partial class PeTeacher : InteractableCharacter
{
    public override string Name => PeTeacherText.Name;

    protected override DialogueLine[] GetDefeatDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.PeTeacher, PeTeacherText.Defeat, 2.5f)];
    }

    protected override DialogueLine[] GetHitReactionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.PeTeacher, PeTeacherText.HitByObject, 2.5f)];
    }

    protected override DialogueLine[] GetInteractionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.PeTeacher, PeTeacherText.Interaction, 2.0f)];

    }

    protected override DialogueLine[] GetRecoveryDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.PeTeacher, PeTeacherText.Recovery, 2.5f)];
    }
}
