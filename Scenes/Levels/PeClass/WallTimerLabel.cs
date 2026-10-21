using Godot;
using System;

public partial class WallTimerLabel : Label3D
{
    [Export] 
    private float timeLeft = 60.0f;

    [Export]
    private bool startOnReady = false;

    private bool isRunning = false;

    public override void _Ready()
    {
        UpdateTimerDisplay();

        if (startOnReady)
        {
            StartTimer();
        }
    }

    public override void _Process(double delta)
    {
        if (!isRunning) return;

        timeLeft -= (float)delta;

        if (timeLeft <= 0)
        {
            timeLeft = 0;
            isRunning = false;
            OnTimerFinished();
        }

        UpdateTimerDisplay();
    }


    public void StartTimer()
    {
        if (timeLeft > 0)
        {
            isRunning = true;
        }
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResetTimer(float newTime = -1f, bool startAfterReset = false)
    {
        if (newTime >= 0)
        {
            timeLeft = newTime;
        }
        
        isRunning = startAfterReset;
        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(timeLeft / 60);
        int seconds = Mathf.FloorToInt(timeLeft % 60);

        Text = $"{minutes:D2}:{seconds:D2}";
    }

    private void OnTimerFinished()
    {
    }
}
