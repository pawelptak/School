using Godot;

public partial class Corridor1 : Node3D
{
    private Mate _mate;
    private DialogueUI _dialogueUI;

    public override void _Ready()
    {
        _mate = GetNode<Mate>("Mate");
        _dialogueUI = GetNode<DialogueUI>("DialogueUI");

        _mate.Interacted += OnMateInteracted;
    }

    private void OnMateInteracted()
    {
        _dialogueUI.ShowDialogue(new string[]
        {
            GameText.MateHomeWorkQuestion,
            GameText.MateHomeWorkFollowup
        });
    }
}
