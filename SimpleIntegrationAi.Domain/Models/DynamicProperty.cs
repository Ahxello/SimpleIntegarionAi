using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SimpleIntegrationAi.Domain.Models;

public class DynamicProperty : INotifyPropertyChanged
{
    private string _name;
    private object _value;

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value) SetField(ref _name, value);
        }
    }

    public object Value
    {
        get => _value;
        set
        {
            if (_value != value) SetField(ref _value, value);
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