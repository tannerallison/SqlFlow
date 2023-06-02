using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SqlFlow.ViewModels;

public class BaseViewModel : INotifyPropertyChanged
{
    protected class ValueViewModel<T> : BaseViewModel
    {
        private T _value;

        public T Value
        {
            get => _value;
            set => SetField(ref _value, value);
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
