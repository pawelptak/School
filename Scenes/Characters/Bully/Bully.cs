public partial class Bully : InteractableCharacter
{
    protected override DialogueLine[] GetHitReactionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.Bully, BullyText.HitByObject, 3.0f)];
    }

    protected override DialogueLine[] GetInteractionDialogue()
    {
        return [new DialogueLine(DialogueSpeaker.Bully, BullyText.Interaction, 1.0f)];
    }
}
