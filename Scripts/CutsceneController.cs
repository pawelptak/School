
using Godot;
using System;

public partial class CutsceneController : Node
{
    private Camera3D _camera;
    private Player _player;
    private DialogueUI _dialogueUI;
    private InteractableCharacter _currentCharacter;
    private DialogueLine[] _dialogueLines;
    private int _currentDialogueLine;
    private int _dialogueSession;
    private bool _autoAdvanceDialogue;

    private Vector3 _originalCameraPosition;
    private Vector3 _originalCameraRotation;

    private bool _cinematicDialogueActive;

    public bool IsDialogueVisible => _dialogueUI.Visible;
    public bool IsCinematicDialogueActive => _cinematicDialogueActive;

    public event Action DialogueFinished;
    public event Action CinematicDialogueFinished;

    public override void _Ready()
    {
        _dialogueUI = GetNode<DialogueUI>("DialogueUI");
        _dialogueUI.DialogueFinished += OnDialogueFinished;
    }

    public void SetCamera(Camera3D camera)
    {
        _camera = camera;
    }

    public void SetPlayer(Player player)
    {
        _player = player;
    }

    public override void _Input(InputEvent @event)
    {
        if (_dialogueUI == null)
            return;

        bool leftMousePressed = @event is InputEventMouseButton mouseButton
            && mouseButton.ButtonIndex == MouseButton.Left
            && mouseButton.Pressed;

        if (!@event.IsActionPressed("interact") && !leftMousePressed)
            return;

        if (!_dialogueUI.IsVisible())
            return;

        if (_autoAdvanceDialogue)
            return;

        GetViewport().SetInputAsHandled();
        NextDialogueLine();
    }

    public void StartDialogue(
        Node3D character,
        DialogueLine[] messages,
        bool cinematic = false,
        Action onFinished = null)
    {
        if (_currentCharacter != null)
            _currentCharacter.SetTalking(false);

        _dialogueSession++;
        _currentCharacter = character as InteractableCharacter;
        _dialogueLines = messages;
        _currentDialogueLine = 0;
        _autoAdvanceDialogue = !cinematic;
        UpdateTalkingAnimation();

        _dialogueUI.ShowDialogue(messages, onFinished);

        if (!cinematic)
        {
            StartAutoAdvanceDialogue(_dialogueSession);
            return;
        }

        _cinematicDialogueActive = true;

        if (_player != null)
        {
            _player.MovementLocked = true;
            _player.CameraLocked = true;
        }

        _originalCameraPosition = _camera.GlobalPosition;
        _originalCameraRotation = _camera.GlobalRotation;

        var lookAtPoint =
            character.GetNodeOrNull<Marker3D>("DialogueLookAtPoint");

        if (lookAtPoint == null)
        {
            GD.PrintErr(
                $"Nie znaleziono DialogueLookAtPoint w {character.Name}!"
            );

            return;
        }

        var directionFromCharacterToCamera =
            _camera.GlobalPosition - character.GlobalPosition;

        directionFromCharacterToCamera.Y = 0;

        if (directionFromCharacterToCamera.LengthSquared() == 0)
        {
            GD.PrintErr(
                "Kamera i rozmówca są w tej samej pozycji."
            );

            return;
        }

        directionFromCharacterToCamera =
            directionFromCharacterToCamera.Normalized();

        var targetPosition =
            character.GlobalPosition
            + directionFromCharacterToCamera * 0.9f;

        targetPosition.Y = lookAtPoint.GlobalPosition.Y;

        var targetRotation = _camera.GlobalTransform
            .LookingAt(
                lookAtPoint.GlobalPosition,
                Vector3.Up
            )
            .Basis
            .GetEuler();

        var tween = CreateTween();

        tween.SetParallel();

        tween.TweenProperty(
            _camera,
            "global_position",
            targetPosition,
            0.4
        );

        tween.TweenProperty(
            _camera,
            "global_rotation",
            targetRotation,
            0.4
        );
    }

    private void OnDialogueFinished()
    {
        DialogueFinished?.Invoke();

        _currentCharacter?.SetTalking(false);
        _currentCharacter = null;
        _dialogueLines = null;
        _currentDialogueLine = 0;
        _autoAdvanceDialogue = false;

        if (!_cinematicDialogueActive)
            return;

        _cinematicDialogueActive = false;

        var tween = CreateTween();

        tween.SetParallel();

        tween.TweenProperty(
            _camera,
            "global_position",
            _originalCameraPosition,
            0.4
        );

        tween.TweenProperty(
            _camera,
            "global_rotation",
            _originalCameraRotation,
            0.4
        );

        tween.SetParallel(false);

        tween.TweenCallback(
            Callable.From(() =>
            {
                if (_player != null)
                {
                    _player.MovementLocked = false;
                    _player.CameraLocked = false;
                }

                CinematicDialogueFinished?.Invoke();
            })
        );
    }

    private void UpdateTalkingAnimation()
    {
        if (_currentCharacter == null || _dialogueLines == null)
            return;

        bool playerIsSpeaking = _dialogueLines[_currentDialogueLine].Speaker
            == DialogueSpeaker.Player;

        _currentCharacter.SetTalking(!playerIsSpeaking);
    }

    private void NextDialogueLine()
    {
        bool hasNextLine = _dialogueLines != null
            && _currentDialogueLine < _dialogueLines.Length - 1;

        _dialogueUI.NextMessage();

        if (!hasNextLine)
            return;

        _currentDialogueLine++;
        UpdateTalkingAnimation();
    }

    private async void StartAutoAdvanceDialogue(int dialogueSession)
    {
        while (_autoAdvanceDialogue
            && dialogueSession == _dialogueSession
            && _dialogueUI.IsVisible())
        {
            double duration = _dialogueLines[_currentDialogueLine]
                .DisplayDurationSeconds;

            await ToSignal(
                GetTree().CreateTimer(duration),
                SceneTreeTimer.SignalName.Timeout
            );

            if (!_autoAdvanceDialogue
                || dialogueSession != _dialogueSession
                || !_dialogueUI.IsVisible())
            {
                return;
            }

            NextDialogueLine();
        }
    }
}
