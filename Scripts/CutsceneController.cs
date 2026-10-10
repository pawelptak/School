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
    private bool _autoAdvanceDialogue = true;

    private Transform3D _originalCameraTransform;
    private float _originalCameraFov;
    private bool _hasCapturedOriginalCamera = false;

    private bool _cinematicDialogueActive;
    private bool _keepPlayerLockedOnFinish = false;

    private const float CinematicFov = 50f;

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

    public async void StartDialogue(
        Node3D character,
        DialogueLine[] messages,
        bool cinematic = false,
        Action onFinished = null,
        bool keepPlayerLockedOnFinish = false,
        Vector3? cameraReferencePosition = null,
        float delaySeconds = 0f)
    {
        _keepPlayerLockedOnFinish = keepPlayerLockedOnFinish;

        if (_currentCharacter != null)
            _currentCharacter.SetTalking(false);

        _dialogueSession++;
        int currentSession = _dialogueSession;

        _currentCharacter = character as InteractableCharacter;
        _dialogueLines = messages;
        _currentDialogueLine = 0;
        _autoAdvanceDialogue = true;

        _cinematicDialogueActive = cinematic;

        if (cinematic)
        {
            if (_player != null)
            {
                _player.MovementLocked = true;
                _player.CameraLocked = true;
            }

            if (delaySeconds > 0f)
            {
                await ToSignal(
                    GetTree().CreateTimer(delaySeconds),
                    SceneTreeTimer.SignalName.Timeout
                );

                if (currentSession != _dialogueSession)
                    return;
            }
        }

        UpdateTalkingAnimation();

        _dialogueUI.ShowDialogue(messages, onFinished);
        StartAutoAdvanceDialogue(_dialogueSession);

        if (!cinematic)
            return;

        if (!_hasCapturedOriginalCamera && _camera != null)
        {
            _originalCameraTransform = _camera.GlobalTransform;
            _originalCameraFov = _camera.Fov;
            _hasCapturedOriginalCamera = true;
        }

        var lookAtPoint =
            character.GetNodeOrNull<Marker3D>("DialogueLookAtPoint");

        if (lookAtPoint == null)
        {
            GD.PrintErr(
                $"Nie znaleziono DialogueLookAtPoint w {character.Name}!"
            );

            return;
        }

        Vector3 targetPosition;

        if (cameraReferencePosition.HasValue)
        {
            targetPosition = cameraReferencePosition.Value;
        }
        else
        {
            var referencePosition = _originalCameraTransform.Origin;
            var directionFromCharacterToCamera = referencePosition - character.GlobalPosition;
            directionFromCharacterToCamera.Y = 0;

            if (directionFromCharacterToCamera.LengthSquared() == 0)
            {
                directionFromCharacterToCamera = Vector3.Forward;
            }

            directionFromCharacterToCamera = directionFromCharacterToCamera.Normalized();

            targetPosition = character.GlobalPosition + directionFromCharacterToCamera * 0.9f;
            targetPosition.Y = lookAtPoint.GlobalPosition.Y;
        }

        var targetTransform = new Transform3D();
        targetTransform.Origin = targetPosition;
        targetTransform = targetTransform.LookingAt(
            lookAtPoint.GlobalPosition,
            Vector3.Up
        );

        var tween = CreateTween();
        tween.SetParallel();

        tween.TweenProperty(
            _camera,
            "global_transform",
            targetTransform,
            0.4
        );

        tween.TweenProperty(
            _camera,
            "fov",
            CinematicFov,
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

        if (!_cinematicDialogueActive)
            return;

        _cinematicDialogueActive = false;

        if (_keepPlayerLockedOnFinish)
        {
            CinematicDialogueFinished?.Invoke();
            return;
        }

        _hasCapturedOriginalCamera = false;

        var tween = CreateTween();
        tween.SetParallel();

        tween.TweenProperty(
            _camera,
            "global_transform",
            _originalCameraTransform,
            0.4
        );

        tween.TweenProperty(
            _camera,
            "fov",
            _originalCameraFov,
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

        var currentLine = _dialogueLines[_currentDialogueLine];

        bool playerIsSpeaking = currentLine.Speaker == DialogueSpeaker.Player;

        var textEmpty = string.IsNullOrWhiteSpace(currentLine.Text);

        _currentCharacter.SetTalking(!playerIsSpeaking && !textEmpty);
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
