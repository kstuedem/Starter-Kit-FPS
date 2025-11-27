using Godot;
using System;
using System.ComponentModel;

public partial class Player : CharacterBody3D
{
	[ExportGroup("Properties")]
	[Export]
	private int movement_speed = 5;
	[Export(PropertyHint.Range, "0,100,")]
	private int number_of_jumps = 2;
	[Export]
	private int jump_strength = 8;

	[ExportGroup("Weapons")]
	[Export]
	private Godot.Collections.Array<Weapon> weapons = [];

	private Weapon weapon;
	private int weaponIndex = 0;

	private int mouseSensitivity = 700;
	private float gamepadSensitivity = 0.075f;

	private bool mouseCaptured = true;

	private Vector3 movementVelocity;
	private Vector3 rotationTarget;

	private Vector2 input_mouse;

	int health = 100;
	private float gravity = 0.0f;

	private bool previouslyFloored = false;

	private int jumpsRemaining;

	private Vector3 containerOffset = new Vector3(1.2f, -1.1f, -2.75f);

	private Tween tween;

	private Camera3D camera;
	private RayCast3D raycast;
	private AnimatedSprite3D muzzle;
	private Node3D container;
	private AudioStreamPlayer soundFootsteps;
	private Timer blasterCooldown;
	RandomNumberGenerator randomNumber = new RandomNumberGenerator();

	[Export]
	private TextureRect crosshair;

	private PackedScene impact;

	[Signal]
	public delegate void HealthUpdatedEventHandler(int health);

	public override void _Ready()
	{
		camera = GetNode<Camera3D>("Head/Camera");
		raycast = GetNode<RayCast3D>("Head/Camera/RayCast");
		muzzle = GetNode<AnimatedSprite3D>("Head/Camera/SubViewportContainer/SubViewport/CameraItem/Muzzle");
		container = GetNode<Node3D>("Head/Camera/SubViewportContainer/SubViewport/CameraItem/Container");
		soundFootsteps = GetNode<AudioStreamPlayer>("SoundFootsteps");
		blasterCooldown = GetNode<Timer>("Cooldown");

		impact = ResourceLoader.Load<PackedScene>("res://objects/impact.tscn");

		Input.MouseMode = Input.MouseModeEnum.Captured;

		weapon = weapons[weaponIndex]; // Weapon must never be null
		InitiateWeaponChange(weaponIndex);
	}

	public override void _Process(double delta)
	{
		// Handle functions
		HandleControls(delta);
		HandleGravity(delta);

		// Movement
		Vector3 applied_velocity;

		movementVelocity = Transform.Basis * movementVelocity; // Move forward

		applied_velocity = Velocity.Lerp(movementVelocity, (float)(delta * 10.0));
		applied_velocity.Y = -gravity;

		Velocity = applied_velocity;
		MoveAndSlide();

		// Rotation 
		container.Position = container.Position.Lerp(containerOffset - (Basis.Inverse() * applied_velocity / 30), (float)(delta * 10.0));

		// Movement sound
		soundFootsteps.StreamPaused = true;

		if (IsOnFloor())
		{
			if (Godot.Mathf.Abs(Velocity.X) > 1 || Godot.Mathf.Abs(Velocity.Z) > 1)
			{
				soundFootsteps.StreamPaused = false;
			}
		}

		// Landing after jump or falling
		Vector3 cameraPosition = camera.Position;
		cameraPosition.Y = Godot.Mathf.Lerp(cameraPosition.Y, 0.0f, (float)(delta * 5.0));

		if (IsOnFloor() && gravity > 1 && !previouslyFloored) // Landed
		{
			GetNode<Audio>("/root/Audio").Play("sounds/land.ogg");
			cameraPosition.Y = -0.1f;
		}
		camera.Position = cameraPosition;

		previouslyFloored = IsOnFloor();

		// Falling/respawning
		if (Position.Y < -10)
		{
			GetTree().ReloadCurrentScene();
		}
	}

	public override void _Input(InputEvent @evt)
	{
		if (evt is InputEventMouseMotion && mouseCaptured)
		{
			var inputEventMouseMotion = (evt as InputEventMouseMotion);

			input_mouse = inputEventMouseMotion.Relative / mouseSensitivity;
			HandleRotation(inputEventMouseMotion.Relative.X, inputEventMouseMotion.Relative.Y, false);
		}
	}

	public void HandleControls(double delta)
	{
		// Mouse capture
		if (Input.IsActionJustPressed("mouse_capture"))
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
			mouseCaptured = true;
		}

		if (Input.IsActionJustPressed("mouse_capture_exit"))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
			mouseCaptured = false;

			input_mouse = Vector2.Zero;
		}

		// Movement
		Vector2 input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		movementVelocity = new Vector3(input.X, 0, input.Y).Normalized() * movement_speed;

		// Handle Controller Rotation
		Vector2 rotation_input = Input.GetVector("camera_right", "camera_left", "camera_down", "camera_up");
		if (!rotation_input.IsZeroApprox())
		{
			HandleRotation(rotation_input.X, rotation_input.Y, true, delta);
		}

		// Shooting
		ActionShoot();

		// Jumping
		if (Input.IsActionJustPressed("jump"))
		{
			if (jumpsRemaining > 0)
			{
				ActionJump();
			}
		}

		// Weapon switching
		ActionWeaponToggle();
	}

	// Camera rotation
	public void HandleRotation(float xRot, float yRot, bool isController, double delta = 0.0)
	{
		if (isController)
		{
			rotationTarget -= new Vector3(-yRot, -xRot, 0).LimitLength(1.0f) * gamepadSensitivity;
			rotationTarget.X = Godot.Mathf.Clamp(rotationTarget.X, Godot.Mathf.DegToRad(-90), Godot.Mathf.DegToRad(90));
			Vector3 cameraRotation = camera.Rotation;
			cameraRotation.X = Godot.Mathf.LerpAngle(cameraRotation.X, rotationTarget.X, (float)(delta * 25));
			camera.Rotation = cameraRotation;

			Vector3 rotation = Rotation;
			rotation.Y = Godot.Mathf.LerpAngle(Rotation.Y, rotationTarget.Y, (float)(delta * 25));
			Rotation = rotation;
		}
		else
		{
			rotationTarget += (new Vector3(-yRot, -xRot, 0) / mouseSensitivity);
			rotationTarget.X = Godot.Mathf.Clamp(rotationTarget.X, Godot.Mathf.DegToRad(-90), Godot.Mathf.DegToRad(90));

			Vector3 cameraRotation = camera.Rotation;
			cameraRotation.X = rotationTarget.X;
			camera.Rotation = cameraRotation;

			Vector3 rotation = Rotation;
			rotation.Y = rotationTarget.Y;
			Rotation = rotation;
		}
	}

	// Handle gravity
	public void HandleGravity(double delta)
	{
		gravity += (float)(20.0 * delta);

		if (gravity > 0 && IsOnFloor())
		{
			jumpsRemaining = number_of_jumps;

			gravity = 0;
		}
	}

	// Jumping

	public void ActionJump()
	{
		GetNode<Audio>("/root/Audio").Play("sounds/jump_a.ogg, sounds/jump_b.ogg, sounds/jump_c.ogg");
		gravity = -jump_strength;

		jumpsRemaining -= 1;
	}

	// Shooting
	public void ActionShoot()
	{
		if (!Input.IsActionPressed("shoot") ||
			!blasterCooldown.IsStopped())// Cooldown for shooting
		{
			return;
		}

		GetNode<Audio>("/root/Audio").Play(weapon.soundShoot);

		// Set muzzle flash position, play animation
		muzzle.Play("default");

		Vector3 muzzleRotation = muzzle.RotationDegrees;
		muzzleRotation.Z = randomNumber.RandfRange(-45, 45);
		muzzle.RotationDegrees = muzzleRotation;

		muzzle.Scale = Vector3.One * randomNumber.RandfRange(0.40f, 0.75f);
		muzzle.Position = container.Position - weapon.muzzlePosition;

		blasterCooldown.Start(weapon.cooldown);

		// Shoot the weapon, amount based on shot count

		for (int i = 0; i < weapon.shotCount; i++)
		{
			Vector3 raycastTarget = raycast.TargetPosition;
			raycastTarget.X = randomNumber.RandfRange(-weapon.spread, weapon.spread);
			raycastTarget.Y = randomNumber.RandfRange(-weapon.spread, weapon.spread);
			raycast.TargetPosition = raycastTarget;
			raycast.ForceRaycastUpdate();

			if (!raycast.IsColliding())
			{
				continue; // Don't create impact when raycast didn't hit
			}

			GodotObject collider = raycast.GetCollider();

			// Hitting an enemy
			if (collider is Enemy)
			{
				(collider as Enemy).Damage(weapon.damage);
			}

			// Creating an impact animation
			AnimatedSprite3D impactInstance = impact.Instantiate() as AnimatedSprite3D;
			impactInstance.Play("shot");

			GetTree().Root.AddChild(impactInstance);

			impactInstance.Position = raycast.GetCollisionPoint() + (raycast.GetCollisionNormal() / 10);
			impactInstance.LookAt(camera.GlobalTransform.Origin, Vector3.Up, true);
		}

		Vector2 knockback = randomVec2(weapon.minKnockback, weapon.maxKnockback);
		Vector3 containerPosition = container.Position;
		containerPosition.Z += 0.25f;// Knockback of weapon visual
		container.Position = containerPosition;

		Vector3 cameraRotation = camera.Rotation;
		cameraRotation.X += knockback.X;// Knockback of camera
		camera.Rotation = cameraRotation;

		Vector3 rotation = Rotation;
		rotation.Y += knockback.Y;
		Rotation = rotation;
		rotationTarget.X += knockback.X;
		rotationTarget.X += knockback.Y;

		movementVelocity += new Vector3(0, 0, weapon.knockback); // Knockback
	}

	// Toggle between available weapons (listed in 'weapons')
	public void ActionWeaponToggle()
	{
		if (Input.IsActionJustPressed("weapon_toggle"))
		{
			weaponIndex = Godot.Mathf.Wrap(weaponIndex + 1, 0, weapons.Count);
			InitiateWeaponChange(weaponIndex);

			GetNode<Audio>("/root/Audio").Play("sounds/weapon_change.ogg");
		}
	}

	// Initiates the weapon changing animation (tween)
	void InitiateWeaponChange(int index)
	{
		weaponIndex = index;

		tween = GetTree().CreateTween();
		tween.SetEase(Tween.EaseType.OutIn);
		tween.TweenProperty(container, "position", containerOffset - new Vector3(0, 1, 0), 0.1);
		tween.TweenCallback(Callable.From(ChangeWeapon)); // Changes the model
	}

	// Switches the weapon model (off-screen)
	void ChangeWeapon()
	{
		weapon = weapons[weaponIndex];

		// Step 1. Remove previous weapon model(s) from container
		foreach (Node n in container.GetChildren())
		{
			container.RemoveChild(n);
		}

		// Step 2. Place new weapon model in container
		Node3D weaponModel = weapon.model.Instantiate() as Node3D;
		container.AddChild(weaponModel);

		weaponModel.Position = weapon.position;
		weaponModel.RotationDegrees = weapon.rotation;

		// Step 3. Set model to only render on layer 2 (the weapon camera)
		foreach (Node child in weaponModel.FindChildren("*", "MeshInstance3D"))
		{
			(child as MeshInstance3D).Layers = 2;
		}

		// Set weapon data
		raycast.TargetPosition = new Vector3(0, 0, -1) * weapon.maxDistance;
		crosshair.Texture = weapon.crosshair;
	}

	public void Damage(int amount)
	{
		health -= amount;
		EmitSignal(SignalName.HealthUpdated, health); // Update health on HUD
	
		if (health < 0)
		{
			GetTree().ReloadCurrentScene(); // Reset when out of health
		}
	}

	// Create a random knockback vector
	public static Vector2 randomVec2(Vector2 _min, Vector2 _max)
	{
		RandomNumberGenerator rand = new RandomNumberGenerator();
		var _sign = (rand.Randi() % 2 == 0) ? -1 : 1;
		return new Vector2(rand.RandfRange(_min.X, _max.X), rand.RandfRange(_min.Y, _max.Y) * _sign);
	}
}
