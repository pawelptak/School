public partial class Bully : InteractableCharacter
{
    protected override DialogueLine[] GetHitReactionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.Mate, BullyText.Interaction)];
    }

    protected override DialogueLine[] GetInteractionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.Mate, BullyText.HitByObject)];
    }
}
