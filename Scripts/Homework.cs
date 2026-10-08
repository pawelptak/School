using Godot;

public partial class Homework : Node3D
{
    private SubViewport _textViewport;
    private TextEdit _textInput;

    private readonly string[] _texts =
    {
        "The quick brown fox jumps over the lazy dog.",
        "A small bird landed on the old wooden fence.",
        "The weather outside was cold but surprisingly pleasant."
    };

    private int _textIndex;
    private string _targetText = "";
    private int _wordIndex;
    private string _currentWord = "";
    private int _currentIndex;

    private int _correctWords;
    private int _totalWords;

    public override void _Ready()
    {
        var paper = GetNode<MeshInstance3D>("PlayerNotebook/PaperLeft");
        _textViewport = GetNode<SubViewport>("UI/TextViewport");
        _textInput = GetNode<TextEdit>("UI/TextViewport/TextInput");

        var paperBox = (BoxMesh)paper.Mesh;

        var writingSurface = new MeshInstance3D
        {
            Mesh = new PlaneMesh
            {
                Size = new Vector2(paperBox.Size.X, paperBox.Size.Z)
            },
            Position = new Vector3(
                0,
                paperBox.Size.Y / 2f + 0.001f,
                0
            ),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoTexture = _textViewport.GetTexture(),
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear
            }
        };

        paper.AddChild(writingSurface);

        StartText();
        _textInput.GrabFocus();
    }

    private void StartText()
    {
        _targetText = _texts[_textIndex];

        _wordIndex = 0;
        _currentWord = "";
        _currentIndex = 0;

        _textInput.Text = "";
    }

    private void FinishWord()
    {
        var targetWords = _targetText.Split(' ');

        if (_wordIndex < targetWords.Length)
        {
            if (_currentWord == targetWords[_wordIndex])
                _correctWords++;

            _totalWords++;
        }

        _wordIndex++;
        _currentWord = "";
    }

    private void FinishText()
    {
        if (_currentWord.Length > 0)
            FinishWord();

        GD.Print($"Text {_textIndex + 1} finished.");
        GD.Print($"Score: {_correctWords}/{_totalWords}");

        if (_textIndex + 1 < _texts.Length)
        {
            _textIndex++;
            StartText();
        }
        else
        {
            GD.Print("All texts finished!");
            GD.Print($"Final score: {_correctWords}/{_totalWords}");
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent ||
            !keyEvent.Pressed ||
            keyEvent.Echo)
        {
            return;
        }

        if (keyEvent.Keycode is Key.Backspace or Key.Delete)
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (keyEvent.Unicode == 0)
            return;

        var character = char.ConvertFromUtf32((int)keyEvent.Unicode);

        if (_currentIndex >= _targetText.Length)
            return;

        if (character == " ")
        {
            FinishWord();

            _textViewport.PushInput(@event);
            _currentIndex++;
        }
        else
        {
            _currentWord += character;

            _textViewport.PushInput(@event);
            _currentIndex++;

            if (_currentIndex == _targetText.Length)
            {
                FinishText();
            }
        }

        GD.Print(
            $"Text: {_textIndex + 1}/{_texts.Length}, " +
            $"Words: {_correctWords}/{_totalWords}"
        );

        GetViewport().SetInputAsHandled();
    }
}