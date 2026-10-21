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
    private Tween _cameraTween;

    private int _currentLineToken = 0;

    private Transform3D _originalCameraTransform;
    private float _originalCameraFov;
    private bool _hasCapturedOriginalCamera = false;

    private bool _cinematicDialogueActive;
    private bool _keepPlayerLockedOnFinish = false;
    private bool _isSkippable = true;

    private const float CinematicFov = 50f;
    private const double CameraTransitionDuration = 0.4;

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

    /// <summary>
    /// Starts a reaction dialogue only if no other dialogue or
    /// cinematic dialogue is currently active.
    /// Reaction dialogues never interrupt existing conversations.
    /// </summary>
    public bool TryStartReactionDialogue(
        Node3D character,
        DialogueLine[] messages)
    {
        if (_dialogueUI == null)
            return false;

        if (_dialogueUI.Visible || _cinematicDialogueActive)
            return false;

        if (messages == null || messages.Length == 0)
            return false;

        StartDialogue(character, messages);
        return true;
    }

    public void StopForSceneTransition()
    {
        _dialogueSession++;
        _currentLineToken++;
        _autoAdvanceDialogue = false;
        _cinematicDialogueActive = false;
        _isSkippable = true;

        _cameraTween?.Kill();
        _cameraTween = null;

        _currentCharacter?.SetTalking(false);
        _currentCharacter = null;
        _dialogueLines = null;

        if (_player != null)
        {
            _player.MovementLocked = true;
            _player.CameraLocked = true;
        }

        _dialogueUI.Hide();
    }

    public override void _Input(InputEvent @event)
    {
        if (_dialogueUI == null)
            return;

        if (!_cinematicDialogueActive)
            return;

        if (!_isSkippable)
            return;

        bool leftMousePressed = @event is InputEventMouseButton mouseButton
            && mouseButton.ButtonIndex == MouseButton.Left
            && mouseButton.Pressed;

        if (!@event.IsActionPressed("interact") && !leftMousePressed)
            return;

        if (!_dialogueUI.IsVisible())
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
        float delaySeconds = 0f,
        bool isSkippable = true,
        bool smoothCameraTransition = true,
        Camera3D referenceCamera = null)
    {
        _keepPlayerLockedOnFinish = keepPlayerLockedOnFinish;
        _isSkippable = isSkippable;

        if (!_isSkippable)
        {
            _autoAdvanceDialogue = true;
        }

        if (_currentCharacter != null)
            _currentCharacter.SetTalking(false);

        _dialogueSession++;
        _currentLineToken++;
        int currentSession = _dialogueSession;

        _currentCharacter = character as InteractableCharacter;
        _dialogueLines = messages;
        _currentDialogueLine = 0;

        if (_isSkippable)
        {
            _autoAdvanceDialogue = true;
        }

        _cinematicDialogueActive = cinematic;

        if (cinematic)
        {
            SetHudVisibility(false);

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
        StartAutoAdvanceForCurrentLine(_dialogueSession);

        if (!cinematic)
            return;

        if (_camera == null)
        {
            GD.PrintErr("CutsceneController: nie ustawiono kamery przez SetCamera().");
            return;
        }

        // Save the original player-camera view only once for a sequence
        // of cinematic dialogues.
        if (!_hasCapturedOriginalCamera)
        {
            _originalCameraTransform = _camera.GlobalTransform;
            _originalCameraFov = _camera.Fov;
            _hasCapturedOriginalCamera = true;
        }

        Transform3D targetTransform;
        float targetFov;

        if (referenceCamera != null)
        {
            // Manual shot: use the editor-configured camera transform and FOV.
            targetTransform = referenceCamera.GlobalTransform;
            targetFov = referenceCamera.Fov;
        }
        else
        {
            // Automatic shot: calculate a position near the character and
            // point the camera at DialogueLookAtPoint.
            var lookAtPoint =
                character.GetNodeOrNull<Marker3D>("DialogueLookAtPoint");

            if (lookAtPoint == null)
            {
                GD.PrintErr(
                    $"Nie znaleziono DialogueLookAtPoint w {character.Name}!"
                );
                return;
            }

            var referencePosition = _originalCameraTransform.Origin;
            var directionFromCharacterToCamera =
                referencePosition - character.GlobalPosition;

            directionFromCharacterToCamera.Y = 0;

            if (directionFromCharacterToCamera.LengthSquared() == 0)
                directionFromCharacterToCamera = Vector3.Forward;

            directionFromCharacterToCamera =
                directionFromCharacterToCamera.Normalized();

            Vector3 targetPosition =
                character.GlobalPosition
                + directionFromCharacterToCamera * 0.9f;

            targetPosition.Y = lookAtPoint.GlobalPosition.Y;

            targetTransform = new Transform3D();
            targetTransform.Origin = targetPosition;
            targetTransform = targetTransform.LookingAt(
                lookAtPoint.GlobalPosition,
                Vector3.Up
            );

            targetFov = CinematicFov;
        }

        _cameraTween?.Kill();
        _cameraTween = null;

        if (smoothCameraTransition)
        {
            _cameraTween = CreateTween();
            var tween = _cameraTween;

            tween.SetTrans(Tween.TransitionType.Cubic);
            tween.SetEase(Tween.EaseType.Out);
            tween.SetParallel();

            tween.TweenProperty(
                _camera,
                "global_transform",
                targetTransform,
                CameraTransitionDuration
            );

            tween.TweenProperty(
                _camera,
                "fov",
                targetFov,
                CameraTransitionDuration
            );
        }
        else
        {
            // Hard cut: apply the full target view immediately.
            _camera.GlobalTransform = targetTransform;
            _camera.Fov = targetFov;
        }
    }

    private void SetHudVisibility(bool visible)
    {
        var hud = GetNodeOrNull<CanvasLayer>("/root/Hud");

        if (hud != null)
            hud.Visible = visible;
    }

    private void OnDialogueFinished()
    {
        _currentLineToken++;
        DialogueFinished?.Invoke();

        _currentCharacter?.SetTalking(false);
        _currentCharacter = null;
        _dialogueLines = null;
        _currentDialogueLine = 0;
        _isSkippable = true;

        if (!_cinematicDialogueActive)
            return;

        _cinematicDialogueActive = false;

        if (_keepPlayerLockedOnFinish)
        {
            CinematicDialogueFinished?.Invoke();
            return;
        }

        _cameraTween?.Kill();
        _cameraTween = null;

        if (_camera == null)
        {
            _hasCapturedOriginalCamera = false;
            return;
        }

        _cameraTween = CreateTween();
        var tween = _cameraTween;

        tween.SetParallel();

        tween.TweenProperty(
            _camera,
            "global_transform",
            _originalCameraTransform,
            CameraTransitionDuration
        );

        tween.TweenProperty(
            _camera,
            "fov",
            _originalCameraFov,
            CameraTransitionDuration
        );

        tween.SetParallel(false);

        tween.TweenCallback(
            Callable.From(() =>
            {
                _hasCapturedOriginalCamera = false;

                if (_player != null)
                {
                    _player.MovementLocked = false;
                    _player.CameraLocked = false;
                }

                SetHudVisibility(true);
                CinematicDialogueFinished?.Invoke();
            })
        );
    }

    private void UpdateTalkingAnimation()
    {
        if (_currentCharacter == null || _dialogueLines == null)
            return;

        var currentLine = _dialogueLines[_currentDialogueLine];

        bool playerIsSpeaking =
            currentLine.Speaker == DialogueSpeaker.Player;

        bool textEmpty =
            string.IsNullOrWhiteSpace(currentLine.Text);

        _currentCharacter.SetTalking(
            !playerIsSpeaking && !textEmpty
        );
    }

    private void NextDialogueLine()
    {
        _currentLineToken++;

        bool hasNextLine = _dialogueLines != null
            && _currentDialogueLine < _dialogueLines.Length - 1;

        _dialogueUI.NextMessage();

        if (!hasNextLine)
            return;

        _currentDialogueLine++;
        UpdateTalkingAnimation();

        StartAutoAdvanceForCurrentLine(_dialogueSession);
    }

    private async void StartAutoAdvanceForCurrentLine(
        int dialogueSession)
    {
        int token = _currentLineToken;

        if ((!_isSkippable && _dialogueLines == null)
            || (_isSkippable && !_autoAdvanceDialogue)
            || _dialogueLines == null
            || _currentDialogueLine >= _dialogueLines.Length)
        {
            return;
        }

        double duration =
            _dialogueLines[_currentDialogueLine].DisplayDurationSeconds;

        await ToSignal(
            GetTree().CreateTimer(duration),
            SceneTreeTimer.SignalName.Timeout
        );

        if (token != _currentLineToken
            || dialogueSession != _dialogueSession
            || (!_isSkippable ? false : !_autoAdvanceDialogue)
            || !_dialogueUI.IsVisible())
        {
            return;
        }

        NextDialogueLine();
    }
}
