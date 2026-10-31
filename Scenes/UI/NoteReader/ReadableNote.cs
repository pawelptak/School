
using Godot;

public partial class ReadableNote : StaticBody3D, IInteractable
{
    [Export]
    public string NoteTitle { get; set; } = "Notatka";

    [Export(PropertyHint.MultilineText)]
    public string NoteContent { get; set; } = "Treść notatki...";

    [Export]
    public FontFile NoteFont { get; set; }

    [Export]
    public int NoteFontSize { get; set; } = 0;

    public string Name => NoteTitle;
    public string InteractionText => "Przeczytaj";
    public Vector3 PromptOffset => new(0, 0.3f, 0);

    public bool CanInteract(Player player) => true;

    public void Interact(Player player)
    {
        var noteReader = GetNode<NoteReader>("/root/NoteReader");

        noteReader.Open(
            NoteTitle,
            NoteContent,
            NoteFont
        );
    }
}
