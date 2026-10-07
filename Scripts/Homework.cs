using Godot;

public partial class Homework : Node3D
{
    private SubViewport _textViewport;
    private TextEdit _textInput;

    public override void _Ready()
    {
        _textViewport = GetNode<SubViewport>("UI/TextViewport");
        _textInput = GetNode<TextEdit>("UI/TextViewport/TextInput");

        _textViewport.RenderTargetUpdateMode =
            SubViewport.UpdateMode.Always;

        _textViewport.TransparentBg = true;

        var leftPages = GetNode<MeshInstance3D>("Notebook/PaperLeft");

        var material = new StandardMaterial3D();

        var viewportTexture = new ViewportTexture();
        viewportTexture.ViewportPath = _textViewport.GetPath();

        material.AlbedoTexture = viewportTexture;

        leftPages.MaterialOverride = material;

        _textInput.Text = "TEST";
    }
}
