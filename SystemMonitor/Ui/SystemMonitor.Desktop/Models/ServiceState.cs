using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SystemMonitor.Common;
using SystemMonitor.Common.IO;

namespace SystemMonitor.Desktop.Models;

public class ServiceState : INotifyPropertyChanged, IEquatable<ServiceState>
{
    private State _currentState;
    private GraphType _graphType;
    private bool _isDown;
    private bool _isChecking;
    private double? _value;
    private string _valueFormatted = "";
    private string? _range;
    private Units _units;
    private TimeSpan _responseTime;
    private string _responseTimeFormatted = "1 ms";
    private readonly GraphingContainer _graphingContainer;

    public string Name { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? LastChecked { get; set; }

    public int TimeScale
    {
        get => _graphingContainer.TimeScale;
        set => _graphingContainer.TimeScale = value;
    }

    public State CurrentState
    {
        get => _currentState;
        set => SetField(ref _currentState, value);
    }

    public GraphType GraphType
    {
        get => _graphType;
        set => SetField(ref _graphType, value);
    }

    public bool IsChecking
    {
        get => _isChecking;
        set => SetField(ref _isChecking, value);
    }

    public bool IsDown
    {
        get => _isDown;
        set => SetField(ref _isDown, value);
    }

    public double? Value
    {
        get => _value;
        set
        {
            SetField(ref _value, value);
            ValueFormatted = FormatValue(value);
            if (GraphType == GraphType.Value)
                _graphingContainer.AddDataPoint(value ?? 0, !_isDown);
        }
    }

    public string ValueFormatted
    {
        get => _valueFormatted;
        set => SetField(ref _valueFormatted, value);
    }

    public string? Range
    {
        get => _range;
        set
        {
            SetField(ref _range, value);
            _graphingContainer.SetRange(value);
        }
    }

    public Units Units
    {
        get => _units;
        set
        {
            SetField(ref _units, value);
            _graphingContainer.SetUnits(value);
        }
    }

    public TimeSpan ResponseTime
    {
        get => _responseTime;
        set
        {
            SetField(ref _responseTime, value);
            ResponseTimeFormatted = $"{value.TotalMilliseconds:n0} ms";

            //if (GraphType == GraphType.ResponseTime)
            //    _graphingContainer.AddDataPoint(value.TotalMilliseconds);
        }
    }

    public string ResponseTimeFormatted
    {
        get => _responseTimeFormatted;
        set => SetField(ref _responseTimeFormatted, value);
    }

    public GraphingContainer Data => _graphingContainer;

    public ServiceState(string name)
    {
        Name = name;
        IsEnabled = true;
        _graphingContainer = new GraphingContainer(name);
    }

    public ServiceState(string name, bool isEnabled)
    {
        Name = name;
        IsEnabled = isEnabled;
        _graphingContainer = new GraphingContainer(name);
    }

    public ServiceState(string name, bool isEnabled, bool isChecking)
    {
        Name = name;
        IsEnabled = isEnabled;
        IsChecking = isChecking;
        _graphingContainer = new GraphingContainer(name);
    }

    private string FormatValue(double? value)
    {
        switch (Units)
        {
            case Units.Percentage:
                return $"{(value * 100):n0}%";
            case Units.Bytes:
                return $"{value:n0}B";
            case Units.Kb:
                return $"{value / 1000:n0}KB";
            case Units.Mb:
                return $"{value / 1000 / 1000:n1}MB";
            case Units.Gb:
                return $"{value / 1000 / 1000 / 1000:n1}GB";
            case Units.Tb:
                return $"{value / 1000 / 1000 / 1000 / 1000:n2}TB";
            case Units.Pb:
                return $"{value / 1000 / 1000 / 1000 / 1000 / 1000:n3}PB";
            case Units.Time:
                return $"{value:n0}ms";
            case Units.Value:
                return $"{value}";
            default:
            case Units.Auto:
            {
                // format large values as bytes size
                if (value > 1000)
                    return IOHelper.GetFriendlyBytes((long)(value ?? 0));
                // format percentages
                if ((value >= 0.000000000001 && value <= 1.0) || (value % 1 != 0))
                    return $"{(value * 100):n0}%";
                // format integers
                return $"{value:n0}";
            }
        }
    }

    public override string ToString() => Name;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public bool Equals(ServiceState? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ServiceState)obj);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}