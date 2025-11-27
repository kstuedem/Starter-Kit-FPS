using Godot;
using System;

public partial class Cloud : Node3D
{
    double time = 0.0f;

    RandomNumberGenerator randomNumber = new RandomNumberGenerator();

    float randomVelocity;
    float randomTime;

    public override void _Ready()
    {
        randomVelocity = randomNumber.RandfRange(0.1f, 2.0f);
        randomTime = randomNumber.RandfRange(0.1f, 2.0f);
    }

    public override void _Process(double delta)
    {
        Vector3 position = Position;
        position.Y += (float)((Godot.Mathf.Cos(time * randomTime) * randomVelocity) * delta); // Sine movement
        Position = position;
        time += delta;
    }
}
