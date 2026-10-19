using System;
using Godot;

public partial class Clock : StaticBody3D
{
    private Node3D _hourPivot;
    private Node3D _minutePivot;

    private bool _isTimelapseRunning = false;
    private double _timelapseElapsed = 0.0;
    private double _timelapseDuration = 0.0;
    private double _timelapseStartHours = 0.0;
    private double _timelapseTargetHours = 0.0;

    private double? _manualTimeSeconds = null;

    public override void _Ready()
    {
        _hourPivot = GetNode<Node3D>("HourPivot");
        _minutePivot = GetNode<Node3D>("MinutePivot");
    }

    public override void _Process(double delta)
    {
        double currentTotalSeconds;

        if (_isTimelapseRunning)
        {
            _timelapseElapsed += delta;
            double progress = Math.Clamp(_timelapseElapsed / _timelapseDuration, 0.0, 1.0);

            double currentHoursFloat = _timelapseStartHours + (_timelapseTargetHours - _timelapseStartHours) * progress;
            currentTotalSeconds = currentHoursFloat * 3600.0;

            if (progress >= 1.0)
            {
                _isTimelapseRunning = false;
                _manualTimeSeconds = currentTotalSeconds;
            }
        }
        else if (_manualTimeSeconds.HasValue)
        {
            currentTotalSeconds = _manualTimeSeconds.Value;
        }
        else
        {
            DateTime now = DateTime.Now;
            currentTotalSeconds = now.Hour * 3600.0 + now.Minute * 60.0 + now.Second + now.Millisecond / 1000.0;
        }

        UpdateClockHands(currentTotalSeconds);
    }

    private void UpdateClockHands(double totalSeconds)
    {
        double seconds = totalSeconds % 60.0;
        double minutes = (totalSeconds / 60.0) % 60.0;
        double hours = (totalSeconds / 3600.0) % 12.0;

        double minuteAngle = -(minutes / 60.0) * 2.0 * Math.PI;
        double hourAngle = Math.PI - (hours / 12.0) * 2.0 * Math.PI;

        if (_minutePivot != null)
        {
            _minutePivot.Rotation = new Vector3(
                _minutePivot.Rotation.X,
                (float)minuteAngle,
                _minutePivot.Rotation.Z
            );
        }

        if (_hourPivot != null)
        {
            _hourPivot.Rotation = new Vector3(
                _hourPivot.Rotation.X,
                (float)hourAngle,
                _hourPivot.Rotation.Z
            );
        }
    }
    public void SetTime(int hour, int minute, int second = 0)
    {
        _isTimelapseRunning = false;
        _manualTimeSeconds = (hour % 12) * 3600.0 + minute * 60.0 + second;
    }

    public void StartTimelapse(float hoursToAdd, float durationInSeconds)
    {
        double currentTotalSeconds = _manualTimeSeconds ?? (DateTime.Now.Hour * 3600.0 + DateTime.Now.Minute * 60.0 + DateTime.Now.Second);

        _timelapseStartHours = currentTotalSeconds / 3600.0;
        _timelapseTargetHours = _timelapseStartHours + hoursToAdd;

        _timelapseDuration = Math.Max(durationInSeconds, 0.001);
        _timelapseElapsed = 0.0;
        _isTimelapseRunning = true;
        _manualTimeSeconds = null;
    }
}
