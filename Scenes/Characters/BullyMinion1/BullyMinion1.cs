public partial class BullyMinion1 : InteractableCharacter
{
    public override string Name => BullyMinion1Text.Name;

    protected override DialogueLine[] GetDefeatDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.BullyMinion1, BullyMinion1Text.Defeat, 2.0f)];
    }

    protected override DialogueLine[] GetHitReactionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.BullyMinion1, BullyMinion1Text.HitReaction, 2.0f)];
    }

    protected override DialogueLine[] GetInteractionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.BullyMinion1, BullyMinion1Text.Interaction, 2.0f)];
    }

    protected override DialogueLine[] GetRecoveryDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.BullyMinion1, BullyMinion1Text.Recovery, 1.0f)];
    }
}
