
using Godot;

public interface IInteractable
{
    string InteractionText { get; }
    Vector3 PromptOffset { get; }
    bool CanInteract(Player player);
    void Interact(Player player);
    bool RequiresHold => false;
    float GetInteractionDuration() => 0.0f;
    string GetPromptText() => $"{InteractionText} [E]";
}