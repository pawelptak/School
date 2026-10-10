using Godot;

public partial class SceneTransition : CanvasLayer
{
    public static SceneTransition Instance { get; private set; }

    private ColorRect _colorRect;
    private bool _isTransitioning;

    public override void _Ready()
    {
        Instance = this;

        _colorRect = GetNode<ColorRect>("ColorRect");
        _colorRect.Color = Colors.Black;
        _colorRect.Modulate = Colors.White with { A = 0.0f };
        _colorRect.Visible = false;

        ProcessMode = ProcessModeEnum.Always;
    }

    public async void ChangeScene(string targetScenePath, float duration = 0.1f)
    {
        if (_isTransitioning)
            return;

        _isTransitioning = true;

        var cutsceneController = GetTree().CurrentScene
            ?.GetNodeOrNull<CutsceneController>("CutsceneController");

        cutsceneController?.StopForSceneTransition();

        // Fade out
        _colorRect.Visible = true;
        _colorRect.Modulate = Colors.White with { A = 0.0f };

        var tweenOut = CreateTween();
        tweenOut.TweenProperty(_colorRect, "modulate:a", 1.0f, duration);

        await ToSignal(tweenOut, Tween.SignalName.Finished);

        Error error = GetTree().ChangeSceneToFile(targetScenePath);

        if (error != Error.Ok)
        {
            GD.PrintErr($"Failed to change scene: {error}");
            await FadeIn(duration);
            _isTransitioning = false;

            return;
        }

        await ToSignal(GetTree(), SceneTree.SignalName.SceneChanged);

        await FadeIn(duration);

        _isTransitioning = false;
    }

    private async System.Threading.Tasks.Task FadeIn(float duration)
    {
        var tweenIn = CreateTween();
        tweenIn.TweenProperty(_colorRect, "modulate:a", 0.0f, duration);

        await ToSignal(tweenIn, Tween.SignalName.Finished);

        _colorRect.Visible = false;
    }
}
