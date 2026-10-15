using Godot;

public partial class TutorialAreaDoor : AnimatableBody3D, IInteractable
{
    public string InteractionText => "Wejdź";

    public Vector3 PromptOffset => new Vector3(0, 1.5f, 0);

    string IInteractable.Name => "Drzwi";

    public bool CanInteract(Player player)
    {
        return true;
    }

    public void Interact(Player player)
    {
        SceneTransition.Instance.ChangeScene("res://Scenes/Levels/TutorialArea/tutorial_area.tscn");
    }
}
