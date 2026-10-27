using Godot;

public partial class Downstairs : LevelBase
{
    private Door _ceilingDoor;
    private CutsceneController _cutsceneController;
    private Player _player;
    private Camera3D _camera;
    private Door _doorLeft;
    private Door _doorRight;
    private Door _teacherDoor;
    private GermanTeacher _teacher;
    private NpcController _teacherController;
    private Marker3D _teacherGoToPoint;

    public override void _Ready()
    {
        base._Ready();

        _cutsceneController = GetNode<CutsceneController>("CutsceneController");
        _player = GetNode<Player>("Player");
        _camera = _player.GetNode<Camera3D>("Camera3D");
        _cutsceneController.SetCamera(_camera);
        _cutsceneController.SetPlayer(_player);

        _ceilingDoor = GetNode<Door>("CeilingDoor");
        _ceilingDoor.InteractionEnabled = true;
        _ceilingDoor.Interacted += OnCeilingDoorInteracted;

        _doorLeft = GetNode<Door>("GymnasiumDoorLeft");
        _doorRight = GetNode<Door>("GymnasiumDoorRight");
        _teacherDoor = GetNode<Door>("TeacherRoomDoor");

        _doorLeft.InteractionEnabled = true;
        _doorRight.InteractionEnabled = true;
        _teacherDoor.InteractionEnabled = true;

        _doorLeft.Interacted += OnGymnasiumDoorInteracted;
        _doorRight.Interacted += OnGymnasiumDoorInteracted;
        _teacherDoor.Interacted += OnTeacherRoomDoorInteracted;

        _teacher = GetNode<GermanTeacher>("GermanTeacher");
        _teacherController = _teacher.GetNode<NpcController>("NpcController");
        _teacherController.MoveToFinished += Dissapear;

        _teacherGoToPoint = GetNode<Marker3D>("TeacherGoToPoint");
        _teacherController.MoveTo(_teacherGoToPoint);
    }

    private void OnCeilingDoorInteracted()
    {
        _cutsceneController.StartDialogue(
            _player,
            [new DialogueLine(DialogueSpeaker.Player, DownstairsText.CelingDoorInteraction, 2.0f)]
        );
    }

    private void OnGymnasiumDoorInteracted()
    {
        SceneTransition.Instance.ChangeScene("res://Scenes/Levels/PeClass/pe_class_intro.tscn");
    }

    private void OnTeacherRoomDoorInteracted()
    {
        SceneTransition.Instance.ChangeScene("res://Scenes/Levels/PeTeacherRoom/pe_teacher_room.tscn");
    }

    private void Dissapear()
    {
        _teacher.QueueFree();
    }
}
