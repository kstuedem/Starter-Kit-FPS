using Godot;
using System;

// Code adapted from KidsCanCode
public partial class Audio : Node
{
    int num_players = 12;
    String bus = "master";

    Godot.Collections.Array<AudioStreamPlayer> available = [];  // The available players.
    Godot.Collections.Array<String> queue = [];  // The queue of sounds to play.

    public override void _Ready()
    {
        for (int i = 0; i < num_players; i++)
        {
            AudioStreamPlayer p = new AudioStreamPlayer();
            AddChild(p);

            available.Add(p);

            p.VolumeDb = -10;
            p.Finished += () => _OnStreamFinished(p);
            p.Bus = bus;
        }
    }

    private void _OnStreamFinished(AudioStreamPlayer stream)
    {
        available.Add(stream);
    }

    public void Play(String soundPath)  // Path (or multiple, separated by commas)
    {
        var sounds = soundPath.Split(",");

        queue.Add("res://" + sounds[new RandomNumberGenerator().Randi() % sounds.Length].StripEdges());
    }

    public override void _Process(double delta)
    {
        if (queue.Count > 0 && available.Count > 0)
        {
            available[0].Stream = ResourceLoader.Load<AudioStream>(queue[0]);
            queue.RemoveAt(0);
            available[0].Play();
            available[0].PitchScale = (float)(new RandomNumberGenerator().RandfRange(0.9f, 1.1f));

            available.RemoveAt(0);
        }
    }
}
