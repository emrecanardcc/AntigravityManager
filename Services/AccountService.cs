using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AntigravityManager.Models;

namespace AntigravityManager.Services;

public class AccountService
{
    private readonly string _configDir;
    private readonly string _configFile;
    private readonly string _profilesRoot;
    public AppSettings Settings { get; private set; } = new();
    public ObservableCollection<AccountProfile> Accounts { get; } = new();

    public AccountService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _configDir = Path.Combine(appData, "AntigravityManager");
        _configFile = Path.Combine(_configDir, "config.json");
        _profilesRoot = Path.Combine(_configDir, "Profiles");

        Directory.CreateDirectory(_configDir);
        Directory.CreateDirectory(_profilesRoot);

        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_configFile))
            {
                var json = File.ReadAllText(_configFile);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                {
                    Settings = loaded;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Config load error: {ex.Message}");
        }

        if (string.IsNullOrEmpty(Settings.AntigravityExePath) || !File.Exists(Settings.AntigravityExePath))
        {
            Settings.AntigravityExePath = AppSettings.GetDefaultAntigravityPath();
        }

        Accounts.Clear();
        foreach (var acc in Settings.Accounts)
        {
            Accounts.Add(acc);
        }

        // Eğer hiç hesap kayıtlı değilse, Windows Credential Manager'daki mevcut oturumu otomatik tanı
        if (Accounts.Count == 0)
        {
            AutoImportCurrentAccount();
        }
    }

    private void AutoImportCurrentAccount()
    {
        var currentToken = WindowsCredentialManager.ReadCredential("gemini:antigravity");
        if (!string.IsNullOrEmpty(currentToken))
        {
            var (email, name) = WindowsCredentialManager.ParseGoogleIdentity(currentToken);
            var defaultProfileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Antigravity");

            var primaryAccount = new AccountProfile
            {
                Id = Guid.NewGuid().ToString("N")[..8],
                Name = name,
                Email = email,
                ProfileDir = defaultProfileDir,
                CredentialData = currentToken,
                IsActive = true,
                CreatedAt = DateTime.Now,
                LastUsed = DateTime.Now,
                UseCount = 1
            };

            Accounts.Add(primaryAccount);
            Settings.ActiveAccountId = primaryAccount.Id;
            Save();
        }
    }

    public void Save()
    {
        try
        {
            Settings.Accounts = new List<AccountProfile>(Accounts);
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configFile, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Config save error: {ex.Message}");
        }
    }

    public void DeleteAccount(AccountProfile account)
    {
        Accounts.Remove(account);
        Save();
    }

    public bool IsAntigravityRunning()
    {
        return Process.GetProcessesByName("Antigravity").Length > 0;
    }

    public async Task<bool> CloseAntigravityAsync()
    {
        var procs = Process.GetProcessesByName("Antigravity");
        if (procs.Length == 0) return true;

        foreach (var p in procs)
        {
            try
            {
                if (p.MainWindowHandle != IntPtr.Zero)
                {
                    p.CloseMainWindow();
                }
            }
            catch { }
        }

        for (int i = 0; i < 15; i++)
        {
            if (Process.GetProcessesByName("Antigravity").Length == 0) return true;
            await Task.Delay(200);
        }

        foreach (var p in Process.GetProcessesByName("Antigravity"))
        {
            try { p.Kill(); } catch { }
        }

        await Task.Delay(400);
        return true;
    }

    public async Task SwitchToAccountAsync(AccountProfile targetAccount, string? projectPath = null, bool closePrevious = true)
    {
        if (closePrevious && IsAntigravityRunning())
        {
            await CloseAntigravityAsync();
        }

        // 1. Mevcut aktif hesabın son tokenını güncelle (eğer yenilendiyse)
        var activeAcc = Accounts.FirstOrDefault(a => a.IsActive);
        if (activeAcc != null && activeAcc.Id != targetAccount.Id)
        {
            var currentToken = WindowsCredentialManager.ReadCredential("gemini:antigravity");
            if (!string.IsNullOrEmpty(currentToken))
            {
                activeAcc.CredentialData = currentToken;
            }
        }

        // 2. Seçilen hesabın jetonunu Windows Credential Manager'a yaz
        if (!string.IsNullOrEmpty(targetAccount.CredentialData))
        {
            WindowsCredentialManager.WriteCredential("gemini:antigravity", targetAccount.CredentialData);
        }

        // 3. Durumları güncelle
        foreach (var acc in Accounts)
        {
            acc.IsActive = (acc.Id == targetAccount.Id);
        }
        targetAccount.LastUsed = DateTime.Now;
        targetAccount.UseCount++;
        Settings.ActiveAccountId = targetAccount.Id;
        Save();

        // 4. Antigravity'yi başlat
        var arguments = "";
        var defaultAppdata = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Antigravity");
        if (!string.Equals(targetAccount.ProfileDir, defaultAppdata, StringComparison.OrdinalIgnoreCase))
        {
            Directory.CreateDirectory(targetAccount.ProfileDir);
            arguments = $"--user-data-dir=\"{targetAccount.ProfileDir}\"";
        }

        var targetProject = !string.IsNullOrWhiteSpace(projectPath) ? projectPath : Settings.DefaultProjectPath;
        if (!string.IsNullOrWhiteSpace(targetProject) && Directory.Exists(targetProject))
        {
            arguments = string.IsNullOrEmpty(arguments) 
                ? $"\"{targetProject}\"" 
                : $"{arguments} \"{targetProject}\"";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = Settings.AntigravityExePath,
            Arguments = arguments,
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }

    public async Task<(string newId, string tempDir)> StartNewAccountSessionAsync()
    {
        // 1. Mevcut açık Antigravity varsa kapat
        if (IsAntigravityRunning())
        {
            await CloseAntigravityAsync();
        }

        // 2. Mevcut aktif hesabın tokenını kasaya kaydet
        var activeAcc = Accounts.FirstOrDefault(a => a.IsActive);
        if (activeAcc != null)
        {
            var currentToken = WindowsCredentialManager.ReadCredential("gemini:antigravity");
            if (!string.IsNullOrEmpty(currentToken))
            {
                activeAcc.CredentialData = currentToken;
                Save();
            }
        }

        // 3. Windows Credential Manager'daki anahtarı geçici olarak temizle
        WindowsCredentialManager.DeleteCredential("gemini:antigravity");

        // 4. Yepyeni, temiz bir profil klasörü oluştur
        var newId = Guid.NewGuid().ToString("N")[..8];
        var tempDir = Path.Combine(_profilesRoot, $"Account_{newId}");
        Directory.CreateDirectory(tempDir);

        // 5. Antigravity'yi tertemiz profil ile başlat (Sign In ekranı gelsin)
        var startInfo = new ProcessStartInfo
        {
            FileName = Settings.AntigravityExePath,
            Arguments = $"--user-data-dir=\"{tempDir}\"",
            UseShellExecute = true
        };
        Process.Start(startInfo);

        return (newId, tempDir);
    }

    public async Task<AccountProfile?> WaitForNewAccountLoginAsync(string newId, string profileDir, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1500, cancellationToken);

            var token = WindowsCredentialManager.ReadCredential("gemini:antigravity");
            if (!string.IsNullOrEmpty(token))
            {
                var (email, name) = WindowsCredentialManager.ParseGoogleIdentity(token);

                // Token başarıyla geldi ve Google kimliği çözüldü
                var existing = Accounts.FirstOrDefault(a => string.Equals(a.Email, email, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    // Zaten bu e-posta varmış, güncelle
                    existing.CredentialData = token;
                    existing.ProfileDir = profileDir;
                    existing.IsActive = true;
                    foreach (var acc in Accounts)
                    {
                        if (acc.Id != existing.Id) acc.IsActive = false;
                    }
                    Save();
                    return existing;
                }

                var newAccount = new AccountProfile
                {
                    Id = newId,
                    Name = name,
                    Email = email,
                    ProfileDir = profileDir,
                    CredentialData = token,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    LastUsed = DateTime.Now,
                    UseCount = 1
                };

                foreach (var acc in Accounts)
                {
                    acc.IsActive = false;
                }

                Accounts.Add(newAccount);
                Settings.ActiveAccountId = newAccount.Id;
                Save();
                return newAccount;
            }
        }
        return null;
    }

    public void RestoreActiveCredential()
    {
        var activeAcc = Accounts.FirstOrDefault(a => a.IsActive);
        if (activeAcc != null && !string.IsNullOrEmpty(activeAcc.CredentialData))
        {
            WindowsCredentialManager.WriteCredential("gemini:antigravity", activeAcc.CredentialData);
        }
    }

    public bool CreateShortcut(AccountProfile account)
    {
        var exe = Settings.AntigravityExePath;
        if (!File.Exists(exe)) return false;

        var defaultAppdata = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Antigravity");
        var args = string.Equals(account.ProfileDir, defaultAppdata, StringComparison.OrdinalIgnoreCase) 
            ? "" 
            : $"--user-data-dir=\"{account.ProfileDir}\"";

        if (!string.IsNullOrWhiteSpace(Settings.DefaultProjectPath))
        {
            args = string.IsNullOrEmpty(args) ? $"\"{Settings.DefaultProjectPath}\"" : $"{args} \"{Settings.DefaultProjectPath}\"";
        }

        var shortcutName = $"Antigravity - {account.Name}";
        return ShortcutHelper.CreateDesktopShortcut(shortcutName, exe, args, $"Antigravity - {account.Name} Oturumu");
    }
}
