public partial class Mate : InteractableCharacter
{
    protected override DialogueLine[] GetHitReactionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.Mate, MateText.HitByObject, 2.5f)];
    }

    protected override DialogueLine[] GetInteractionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.Mate, MateText.Interaction, 2.0f)];
    }
}
