using Godot;

public partial class Mate : InteractableCharacter
{
    protected override DialogueLine[] GetHitReactionDialogue()
    {
        return new DialogueLine[]
        {
            new DialogueLine(
                DialogueSpeaker.Mate,
                CharacterText.MateHitByObject
            )
        };
    }

    protected override DialogueLine[] GetInteractionDialogue()
    {
        return new DialogueLine[]
        {
            new DialogueLine(
                DialogueSpeaker.Mate,
                CharacterText.MateInteraction
            )
        };

    }
}
