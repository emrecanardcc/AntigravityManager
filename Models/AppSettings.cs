using System;
using System.Collections.Generic;
using System.IO;

namespace AntigravityManager.Models;

public class AppSettings
{
    public string AntigravityExePath { get; set; } = "";
    public string DefaultProjectPath { get; set; } = @"c:\Projeler\AkilliTahtaSistemi";
    public string ActiveAccountId { get; set; } = "";
    public List<AccountProfile> Accounts { get; set; } = new();

    public static string GetDefaultAntigravityPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var defaultPath = Path.Combine(localAppData, "Programs", "antigravity", "Antigravity.exe");
        return defaultPath;
    }
}
