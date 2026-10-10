using Godot;
using System;

public partial class RockingChair : RigidBody3D
{
    [Export]
    private float _speed = 2.0f;

    private float _timeElapsed = 0.0f;

    public override void _PhysicsProcess(double delta)
    {
        _timeElapsed += (float)delta;

        float targetAngleX = 15.0f + 5.0f * MathF.Sin(_timeElapsed * _speed);

        Vector3 currentRotation = RotationDegrees;
        currentRotation.X = targetAngleX;
        RotationDegrees = currentRotation;
    }
}
