using Godot;
using System;

public partial class BullyBody : Node3D, IInteractable
{
    public bool InteractionEnabled { get; set; }
    public event Action Interacted;
    string IInteractable.Name => BullyText.Name;

    public string InteractionText => "Weź strój";

    public Vector3 PromptOffset => new Vector3(0, 1.5f, 0);

    public bool CanInteract(Player player)
    {
        return InteractionEnabled;
    }

    public void Interact(Player player)
    {
        Interacted?.Invoke();
    }
}
