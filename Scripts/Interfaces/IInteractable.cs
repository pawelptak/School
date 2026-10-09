
using Godot;

public interface IInteractable
{
    string InteractionText { get; }
    Vector3 PromptOffset { get; }
    bool CanInteract(Player player);
    void Interact(Player player);
}