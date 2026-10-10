using Godot;

public partial class Homework : Node3D
{
    private SubViewport _textViewport;
    private TextEdit _textInput;

    private SubViewport _targetViewport;
    private Label _targetLabel;
    private CutsceneController _cutsceneController;

    private readonly string[] _texts =
    {
        HomeworkMinigameText.Text1,
        HomeworkMinigameText.Text2,
        HomeworkMinigameText.Text3
    };

    private static readonly Color PaperColor = new(0.9882218f, 0.9882218f, 0.9882218f, 1);
    private static readonly Color InkColor = new(0.05f, 0.1f, 0.4f, 1);

    private int _textIndex;
    private string _targetText = "";
    private int _wordIndex;
    private string _currentWord = "";
    private int _currentIndex;

    private int _correctWords;
    private int _totalWords;

    private const double GameDuration = 10.0; // 60 be default
    private double _timeRemaining;
    private bool _gameFinished;
    private double _score;

    private Control _timerElement;
    private RichTextLabel _timerLabel;

    public override void _Ready()
    {
        var playerPaper = GetNode<MeshInstance3D>("PlayerNotebook/PaperLeft");
        var matePaper = GetNode<MeshInstance3D>("MateNotebook/PaperRight");

        var popupHeader = GetNode<Label>("Popup/Panel/MarginContainer/VBoxContainer/Header");
        var popupText = GetNode<RichTextLabel>("Popup/Panel/MarginContainer/VBoxContainer/Text");
        popupHeader.Text = HomeworkMinigameText.PopupHeader;
        popupText.Text = HomeworkMinigameText.PopupText;

        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _cutsceneController.SetCamera(GetNode<Camera3D>("Camera3D"));

        CreateTextViewports();

        CreateWritingSurface(
            playerPaper,
            _textViewport.GetTexture());

        CreateWritingSurface(
            matePaper,
            _targetViewport.GetTexture());

        StartText();

        _timeRemaining = GameDuration;

        _timerElement = GetNode<Control>("Timer");
        _timerLabel = _timerElement.GetNode<RichTextLabel>("MarginContainer/VBoxContainer/Time");
        _timerLabel.Text = $"{Mathf.CeilToInt((float)_timeRemaining)}";
    }

    public override void _Process(double delta)
    {
        if (_gameFinished)
            return;

        _timeRemaining -= delta;

        if (_timeRemaining <= 0)
        {
            _timeRemaining = 0;
            _timerLabel.Text = "0";
            FinishGame();
            return;
        }

        _timerLabel.Text = $"{Mathf.CeilToInt((float)_timeRemaining)}";
    }

    private void FinishGame()
    {
        _gameFinished = true;

        _score = _totalWords > 0
            ? (double)_correctWords / _totalWords
            : 0;

        GD.Print("Game finished!");
        GD.Print($"Final score: {_correctWords}/{_totalWords}");
        GD.Print($"Score: {_score:P0}");

        _timerElement.Hide();
        StartEndCutscene();
    }

    private void StartEndCutscene()
    {
        _cutsceneController.StartDialogue(
            GetNode<Node3D>("Mate"),
            new DialogueLine[]
            {
                new DialogueLine(
                    DialogueSpeaker.Mate,
                    MateText.EndCutscene
                )
            },
            true,
            () =>
            {
                GetTree().ChangeSceneToFile("res://Scenes/Levels/PolishClass/polish_class.tscn");
            }
        );
    }

    private void CreateTextViewports()
    {
        _textViewport = CreateTextViewport();

        _textInput = new TextEdit
        {
            Position = new Vector2(40, 40),
            Size = new Vector2(920, 620),
            WrapMode = TextEdit.LineWrappingMode.Boundary
        };

        _textInput.AddThemeStyleboxOverride(
            "normal",
            new StyleBoxEmpty());

        _textInput.AddThemeStyleboxOverride(
            "focus",
            new StyleBoxEmpty());

        _textInput.AddThemeColorOverride("font_color", InkColor);
        _textInput.AddThemeColorOverride("caret_color", InkColor);
        _textInput.AddThemeFontSizeOverride("font_size", 60);

        _textViewport.AddChild(_textInput);

        _targetViewport = CreateTextViewport();

        _targetLabel = new Label
        {
            Position = new Vector2(40, 40),
            Size = new Vector2(920, 620),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };

        _targetLabel.AddThemeFontSizeOverride("font_size", 60);
        _targetLabel.AddThemeColorOverride("font_color", InkColor);

        _targetViewport.AddChild(_targetLabel);
    }

    private SubViewport CreateTextViewport()
    {
        var viewport = new SubViewport
        {
            Size = new Vector2I(1000, 700),
            TransparentBg = false,
            GuiDisableInput = false,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };

        var background = new ColorRect
        {
            Color = PaperColor,
            Size = new Vector2(1000, 700)
        };

        AddChild(viewport);
        viewport.AddChild(background);

        return viewport;
    }

    private void CreateWritingSurface(
        MeshInstance3D paper,
        Texture2D texture)
    {
        var paperBox = (BoxMesh)paper.Mesh;

        var writingSurface = new MeshInstance3D
        {
            Mesh = new PlaneMesh
            {
                Size = new Vector2(
                    paperBox.Size.X,
                    paperBox.Size.Z)
            },
            Position = new Vector3(
                0,
                paperBox.Size.Y / 2f + 0.001f,
                0),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoTexture = texture,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear
            }
        };

        paper.AddChild(writingSurface);
    }

    private void StartText()
    {
        _targetText = _texts[_textIndex];

        _wordIndex = 0;
        _currentWord = "";
        _currentIndex = 0;

        _textInput.Text = "";
        _targetLabel.Text = _targetText;
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
            FinishGame();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_gameFinished)
            return;

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

        var character = char.ConvertFromUtf32(
            (int)keyEvent.Unicode);

        if (_currentIndex >= _targetText.Length)
            return;

        if (character == " ")
        {
            FinishWord();

            _textInput.InsertTextAtCaret(character);
            _currentIndex++;
        }
        else
        {
            _currentWord += character;

            _textInput.InsertTextAtCaret(character);
            _currentIndex++;
        }

        if (_currentIndex == _targetText.Length)
            FinishText();

        GetViewport().SetInputAsHandled();
    }
}
