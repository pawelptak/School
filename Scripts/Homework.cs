using Godot;

public partial class Homework : Node3D
{
    private SubViewport _textViewport;
    private TextEdit _textInput;

    public override void _Ready()
    {
        var paper = GetNode<MeshInstance3D>("PlayerNotebook/PaperLeft");
        _textViewport = GetNode<SubViewport>("UI/TextViewport");
        _textInput = GetNode<TextEdit>("UI/TextViewport/TextInput");

        var paperBox = (BoxMesh)paper.Mesh;

        // BoxMesh UVs split the texture across all 6 faces, so draw the text on a separate plane lying on top of the paper.
        var writingSurface = new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(paperBox.Size.X, paperBox.Size.Z) },
            Position = new Vector3(0, paperBox.Size.Y / 2f + 0.001f, 0),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoTexture = _textViewport.GetTexture(),
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear
            }
        };
        paper.AddChild(writingSurface);

        _textInput.GrabFocus();
    }

    public override void _Input(InputEvent @event)
    {
        // A standalone SubViewport receives no input, so keyboard events must be forwarded manually.
        if (@event is InputEventKey)
        {
            _textViewport.PushInput(@event);
            GetViewport().SetInputAsHandled();
        }
    }
}
