public partial class BullyMinion2 : InteractableCharacter
{
    public override string Name => BullyMinion2Text.Name;

    protected override DialogueLine[] GetDefeatDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.BullyMinion2, BullyMinion2Text.Defeat, 2.0f)];
    }

    protected override DialogueLine[] GetHitReactionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.BullyMinion2, BullyMinion2Text.HitReaction, 2.0f)];
    }

    protected override DialogueLine[] GetInteractionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.BullyMinion2, BullyMinion2Text.Interaction, 2.0f)];
    }

    protected override DialogueLine[] GetRecoveryDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.BullyMinion2, BullyMinion2Text.Recovery, 1.0f)];
    }
}
