﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SimpleIntegrationAi.Domain.Models;

public class MessageEntity : INotifyPropertyChanged
{
    private ObservableCollection<DynamicProperty> _properties;

    public MessageEntity(string message)
    {
        Message = message;
        Properties = new ObservableCollection<DynamicProperty>();
    }

    public string Message { get; init; }

    public ObservableCollection<DynamicProperty> Properties
    {
        get => _properties;
        set
        {
            if (_properties != value) SetField(ref _properties, value);
        }
    }

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
}