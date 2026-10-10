public partial class Mate : InteractableCharacter
{
    protected override DialogueLine[] GetHitReactionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.Mate, MateText.HitByObject)];
    }

    protected override DialogueLine[] GetInteractionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.Mate, MateText.Interaction)];
    }
}
