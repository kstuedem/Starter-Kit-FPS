using Godot;
using System;

public partial class Enemy : Node3D
{
	[Export]
	Node3D player;

	RayCast3D raycast;
	AnimatedSprite3D muzzleA;
    AnimatedSprite3D muzzleB;

	RandomNumberGenerator randomNumber = new RandomNumberGenerator();

    float health = 100.0f;
	float time = 0.0f;
	Vector3 targetPosition;
	bool destroyed = false;

	// When ready, save the initial position
	public override void _Ready()
	{
        raycast = GetNode<RayCast3D>("RayCast");
        muzzleA = GetNode<AnimatedSprite3D>("MuzzleA");
        muzzleB = GetNode<AnimatedSprite3D>("MuzzleB");

		GetNode<Timer>("Timer").Timeout += OnTimerTimeout;

        targetPosition = Position;
	}

	public override void _Process(double delta)
	{
		LookAt(player.Position + new Vector3(0, 0.5f, 0), Vector3.Up, true);  // Look at player
		targetPosition.Y += (float)((Godot.Mathf.Cos(time * 5) * 1) * delta);  // Sine movement (up and down)

		time += (float)delta;

		Position = targetPosition;
	}

	// Take damage from player
	public void Damage(float amount)
	{
		GetNode<Audio>("/root/Audio").Play("sounds/enemy_hurt.ogg");

		health -= amount;

		if (health <= 0 &&!destroyed)
		{
			Destroy();
		}
	}

	// Destroy the enemy when out of health
	void Destroy()
	{
		GetNode<Audio>("/root/Audio").Play("sounds/enemy_destroy.ogg");

		destroyed = true;
		QueueFree();
	}

	// Shoot when timer hits 0
	void OnTimerTimeout()
	{
		raycast.ForceRaycastUpdate();

		if (raycast.IsColliding())
		{
			GodotObject collider = raycast.GetCollider();

			if (collider is Player)  // Raycast collides with player
			{
				// Play muzzle flash animation(s)
				muzzleA.Frame = 0;
				muzzleA.Play("default");
				Vector3 muzzleRotation = muzzleA.RotationDegrees;
				muzzleRotation.Z = randomNumber.RandfRange(-45, 45);
				muzzleA.RotationDegrees = muzzleRotation;

				muzzleB.Frame = 0;
				muzzleB.Play("default");
				muzzleRotation = muzzleB.RotationDegrees;
				muzzleRotation.Z = randomNumber.RandfRange(-45, 45);
				muzzleB.RotationDegrees = muzzleRotation;

				GetNode<Audio>("/root/Audio").Play("sounds/enemy_attack.ogg");

				(collider as Player).Damage(5);  // Apply damage to player
			}
		}
	}
}
