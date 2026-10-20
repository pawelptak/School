using Godot;
using System;

public partial class CorridorSplash : Node3D, IInteractable
{
    public Action Interacted;

    string IInteractable.Name => "Plama";

    public string InteractionText => "Zbadaj";

    public Vector3 PromptOffset => new Vector3(0, 0, 0);

    public bool CanInteract(Player player)
    {
        return true;
    }

    public void Interact(Player player)
    {
        Interacted?.Invoke();
    }
}
