using Godot;
using System;

public partial class Impact : AnimatedSprite3D
{
    public override void _Ready()
    {
        AnimationFinished += OnAnimationFinished;
    }

    // Remove this impact effect after the animation has completed
    void OnAnimationFinished()
    {
        QueueFree();
    }
}
