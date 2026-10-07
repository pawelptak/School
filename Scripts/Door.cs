using Godot;
using System;

public partial class Door : AnimatableBody3D
{
    private Node3D _player;
    private Node3D _hinge;
    private bool _isOpen = false;
    private float _targetRotation = 0.0f;
    
    public override void _Ready()
    {
        _player = GetTree().GetFirstNodeInGroup("player") as Node3D;
        _hinge = GetNode<Node3D>("Hinge");
    }
}
