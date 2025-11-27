using Godot;
using System;

public partial class Hud : CanvasLayer
{
    public override void _Ready()
    {
        GetNode<Player>("/root/Main/Player").HealthUpdated += OnHealthUpdated;
    }

    public void OnHealthUpdated(int health)
    {
        GetNode<Label>("Health").Text = health.ToString() + "%";
    }
}
