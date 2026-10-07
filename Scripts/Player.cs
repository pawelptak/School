using Godot;
using System;

public partial class Player : CharacterBody3D
{
	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;

	private Camera3D _camera;
	private float _cameraPitch = 0.0f;
	private Node3D _holdPoint;
	private ThrowableObject? _heldObject;
	private float _throwCharge;
	private uint _heldCollisionLayer;
	private uint _heldCollisionMask;

	private const float MinThrowForce = 5.0f;
	private const float MaxThrowForce = 20.0f;
	private const float ThrowChargeSpeed = 15.0f;
	
	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		_camera = GetNode<Camera3D>("Camera3D");
		_holdPoint = GetNode<Node3D>("Camera3D/HoldPoint");
	}
	
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			RotateY(-mouseMotion.Relative.X * 0.01f);
			
			_cameraPitch -= mouseMotion.Relative.Y * 0.01f;
			_cameraPitch = Mathf.Clamp(
				_cameraPitch,
				Mathf.DegToRad(-80),
				Mathf.DegToRad(80)
			);
			_camera.Rotation = new Vector3(_cameraPitch, 0, 0);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		Vector2 inputDir = Input.GetVector(
			"move_left",
			"move_right",
			"move_forward",
			"move_backward"
		);

		Vector3 direction = (
			Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)
		).Normalized();

		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
		
		PushRigidBodies();
	}
	
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("interact"))
		{
			if (_heldObject != null)
			{
				DropObject();
			}
			else
			{
				TryPickup();
			}
		}
		
		if (_heldObject != null)
		{
			_heldObject.GlobalPosition = _holdPoint.GlobalPosition;

			if (Input.IsActionJustPressed("throw"))
			{
				_throwCharge = MinThrowForce;
			}

			if (Input.IsActionPressed("throw"))
			{
				_throwCharge = Mathf.MoveToward(
					_throwCharge,
					MaxThrowForce,
					ThrowChargeSpeed * (float)delta
				);
			}

			if (Input.IsActionJustReleased("throw"))
			{
				ThrowObject();
			}
		}
	}
	
	private void TryPickup()
	{
		var ray = GetNode<RayCast3D>("Camera3D/InteractRay");

		if (!ray.IsColliding())
			return;

		if (ray.GetCollider() is not ThrowableObject body)
			return;

		_heldObject = body;
		
		_heldCollisionLayer = body.CollisionLayer;
		_heldCollisionMask = body.CollisionMask;

		body.Freeze = true;
		body.CollisionLayer = 0;
		body.CollisionMask = 0;
		body.GlobalPosition = _holdPoint.GlobalPosition;
	}
	
	private void ThrowObject()
	{
		var throwDirection = -_camera.GlobalTransform.Basis.Z;

		if (_heldObject is not ThrowableObject throwable)
			return;

		throwable.ThrowForce = _throwCharge;

		throwable.Freeze = false;
		throwable.CollisionLayer = _heldCollisionLayer;
		throwable.CollisionMask = _heldCollisionMask;

		throwable.ApplyCentralImpulse(throwDirection * _throwCharge);

		_heldObject = null;
		_throwCharge = 0;
	}
	
	private void DropObject()
	{
		if (_heldObject is not ThrowableObject throwable)
			return;

		throwable.Freeze = false;
		throwable.CollisionLayer = _heldCollisionLayer;
		throwable.CollisionMask = _heldCollisionMask;

		_heldObject = null;
		_throwCharge = 0;
	}
	
	private void PushRigidBodies()
	{
		for (var i = 0; i < GetSlideCollisionCount(); i++)
		{
			var collision = GetSlideCollision(i);
			var body = collision.GetCollider();

			if (body is not RigidBody3D rigidBody)
				continue;

			var pushDirection = -collision.GetNormal();
			pushDirection.Y = 0;

			if (pushDirection.LengthSquared() == 0)
				continue;

			pushDirection = pushDirection.Normalized();

			rigidBody.ApplyCentralImpulse(pushDirection * 2.0f);
		}
	}
}
