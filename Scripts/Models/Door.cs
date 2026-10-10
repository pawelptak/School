
using Godot;
using System;

public partial class Door : AnimatableBody3D, IInteractable
{
    public bool InteractionEnabled { get; set; }

    public string InteractionText => "Wejdź";
    public Vector3 PromptOffset => new Vector3(0, 1.5f, 0);

    public event Action Interacted;

    public bool CanInteract(Player player)
    {
        return InteractionEnabled && !player.MovementLocked;
    }

    public void Interact(Player player)
    {
        if (!CanInteract(player))
            return;

        Interacted?.Invoke();
    }
}