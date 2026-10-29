using System;
using System.IO;

namespace AntigravityManager.Services;

public static class ShortcutHelper
{
    public static bool CreateDesktopShortcut(string name, string targetExe, string arguments, string description, string? iconPath = null)
    {
        try
        {
            var desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var shortcutPath = Path.Combine(desktopDir, $"{name}.lnk");

            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return false;

            dynamic? shell = Activator.CreateInstance(shellType);
            if (shell == null) return false;

            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetExe;
            shortcut.Arguments = arguments;
            shortcut.Description = description;
            shortcut.WorkingDirectory = Path.GetDirectoryName(targetExe);
            if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
            {
                shortcut.IconLocation = $"{iconPath},0";
            }
            else
            {
                shortcut.IconLocation = $"{targetExe},0";
            }
            shortcut.Save();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
