public partial class Bully : InteractableCharacter
{
    public override string Name => DialogueSpeakerExtensions.GetDisplayName(DialogueSpeaker.Bully);

    protected override DialogueLine[] GetDefeatDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.Bully, BullyText.Defeat, 2.0f)];
    }

    protected override DialogueLine[] GetHitReactionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.Bully, BullyText.HitByObject, 3.0f)];
    }

    protected override DialogueLine[] GetInteractionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.Bully, BullyText.Interaction, 1.0f)];
    }

    protected override DialogueLine[] GetRecoveryDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.Bully, BullyText.Recovery, 2.0f)];
    }
}
