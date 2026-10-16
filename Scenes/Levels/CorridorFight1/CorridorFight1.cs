using Godot;
using System;

public partial class CorridorFight1 : Node3D
{
    public override void _Ready()
    {
        base._Ready();
        // GameManager.Instance.CurrentSaveData.CanAttack = true;
    }

    public override void _Process(double delta)
    {
    }
}
