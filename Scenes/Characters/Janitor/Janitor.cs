public partial class Janitor : InteractableCharacter
{
    public override string Name => JanitorText.Name;

    protected override DialogueLine[] GetDefeatDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.Janitor, JanitorText.Defeat, 2.0f)];
    }

    protected override DialogueLine[] GetHitReactionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.Janitor, JanitorText.HitReaction, 2.0f)];
    }

    protected override DialogueLine[] GetInteractionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.Janitor, JanitorText.Interaction, 2.0f)];
    }

    protected override DialogueLine[] GetRecoveryDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.Janitor, string.Empty, 2.0f)];
    }
}
