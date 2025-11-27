using Godot;
using System;

public partial class Weapon : Resource
{
    [ExportGroup("Model")]
    [Export]
    public PackedScene model;  // Model of the weapon
    [Export]
    public Vector3 position;  // On-screen position
    [Export]
    public Vector3 rotation;  // On-screen rotation
    [Export]
    public Vector3 muzzlePosition;  // On-screen position of muzzle flash

    [ExportGroup("Properties")]
    [Export(PropertyHint.Range, "0.1,1,")]
    public float cooldown = 0.1f;  // Firerate
    [Export(PropertyHint.Range, "1,20,")]
    public int maxDistance = 10;  // Fire distance
    [Export(PropertyHint.Range, "0,100,")]
    public float damage = 25.0f;  // Damage per hit
    [Export(PropertyHint.Range, "0,5,")]
    public float spread = 0.0f;  // Spread of each shot
    [Export(PropertyHint.Range, "1,5,")]
    public int shotCount = 1;  // Amount of shots
    [Export(PropertyHint.Range, "0,50,")]
    public int knockback = 20;  // Amount of knockback

    [Export]
    public Vector2 minKnockback = new Vector2(0.001f, 0.001f); // x for vertical knockback, y for horizontal knockback
    [Export]
    public Vector2 maxKnockback = new Vector2(0.0025f, 0.002f); // x for vertical knockback, y for horizontal knockback

    [ExportGroup("Sounds")]
    [Export]
    public String soundShoot;  // Sound path

    [ExportGroup("Crosshair")]
    [Export]
    public Texture2D crosshair;  // Image of crosshair on-screen
}
