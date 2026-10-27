using System;

public partial class BullyMinion2Prop : InteractableCharacter
{
    public override string Name => BullyMinion2Text.Name;

    protected override DialogueLine[] GetDefeatDialogue()
    {
        return Array.Empty<DialogueLine>();
    }

    protected override DialogueLine[] GetHitReactionDialogue()
    {
        return Array.Empty<DialogueLine>();
    }

    protected override DialogueLine[] GetInteractionDialogue()
    {
        return Array.Empty<DialogueLine>();
    }

    protected override DialogueLine[] GetRecoveryDialogue()
    {
        return Array.Empty<DialogueLine>();
    }
}
