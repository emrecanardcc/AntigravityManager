using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace AntigravityManager.Models;

public class AccountProfile : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString("N")[..8];
    private string _name = "Google Hesabı";
    private string _email = "";
    private string _profileDir = "";
    private string _credentialData = "";
    private bool _isActive = false;
    private DateTime _createdAt = DateTime.Now;
    private DateTime? _lastUsed;
    private int _useCount = 0;

    public string Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public string Email
    {
        get => _email;
        set => SetField(ref _email, value);
    }

    public string ProfileDir
    {
        get => _profileDir;
        set => SetField(ref _profileDir, value);
    }

    public string CredentialData
    {
        get => _credentialData;
        set => SetField(ref _credentialData, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (SetField(ref _isActive, value))
            {
                OnPropertyChanged(nameof(ActiveBadgeText));
                OnPropertyChanged(nameof(ActiveBadgeColor));
                OnPropertyChanged(nameof(ActiveBorderColor));
            }
        }
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetField(ref _createdAt, value);
    }

    public DateTime? LastUsed
    {
        get => _lastUsed;
        set => SetField(ref _lastUsed, value);
    }

    public int UseCount
    {
        get => _useCount;
        set => SetField(ref _useCount, value);
    }

    [JsonIgnore]
    public string ActiveBadgeText => IsActive ? "🔵 Şu Anda Aktif" : "⚪ Beklemede";

    [JsonIgnore]
    public string ActiveBadgeColor => IsActive ? "#3B82F6" : "#45475A";

    [JsonIgnore]
    public string ActiveBorderColor => IsActive ? "#3B82F6" : "#313244";

    [JsonIgnore]
    public string LastUsedText => LastUsed.HasValue
        ? $"Son Kullanım: {LastUsed.Value:dd.MM.yyyy HH:mm}"
        : "Henüz kullanılmadı";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
