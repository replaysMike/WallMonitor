using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WallMonitor.Desktop.Models;

public class Server : INotifyPropertyChanged, IEquatable<Server>
{
    private bool _isChecking;
    private State _currentState;
    private string _name = string.Empty;

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public State CurrentState
    {
        get => _currentState;
        set => SetField(ref _currentState, value);
    }

    public bool IsChecking
    {
        get => _isChecking;
        set => SetField(ref _isChecking, value);
    }

    public Guid MonitorId { get; set; }
    public int MonitorOrderId { get; set; }
    public string? HostName { get; set; }
    public int OrderId { get; set; }
    public bool IsEnabled { get; set; }
    public byte ImageTheme { get; set; } = 1;
    public byte ImageSize { get; set; } = 3;

    public List<ServiceState> Services { get; set; }
    public double UpTimePercentage { get; set; }
    public TimeSpan UpTime { get; set; }

    public Server(Guid monitorId, int monitorOrderId, string name, string? hostName, int orderId, bool isEnabled, List<ServiceState> services)
    {
        MonitorId = monitorId;
        MonitorOrderId = monitorOrderId;
        if (string.IsNullOrEmpty(name))
            Name = "NotSpecified";
        else
            Name = name;
        HostName = hostName;
        OrderId = orderId;
        IsEnabled = isEnabled;
        Services = services;
        IsChecking = false;
        CurrentState = State.ServerIsNotRunning;
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

    public bool Equals(Server? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _name == other._name && MonitorId.Equals(other.MonitorId);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Server)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_name, MonitorId);
    }
}