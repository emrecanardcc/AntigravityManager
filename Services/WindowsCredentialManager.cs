using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace AntigravityManager.Services;

public static class WindowsCredentialManager
{
    private const int CRED_TYPE_GENERIC = 1;
    private const int CRED_PERSIST_LOCAL_MACHINE = 2;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public int Flags;
        public int Type;
        public string TargetName;
        public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    [DllImport("Advapi32.dll", SetLastError = true, EntryPoint = "CredReadW", CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPtr);

    [DllImport("Advapi32.dll", SetLastError = true, EntryPoint = "CredWriteW", CharSet = CharSet.Unicode)]
    private static extern bool CredWrite([In] ref CREDENTIAL userCredential, [In] uint flags);

    [DllImport("Advapi32.dll", SetLastError = true, EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode)]
    private static extern bool CredDelete(string target, int type, int flags);

    [DllImport("Advapi32.dll", SetLastError = true, EntryPoint = "CredFree")]
    private static extern void CredFree(IntPtr credential);

    public static string? ReadCredential(string target = "gemini:antigravity")
    {
        try
        {
            if (CredRead(target, CRED_TYPE_GENERIC, 0, out IntPtr credPtr))
            {
                var cred = (CREDENTIAL)Marshal.PtrToStructure(credPtr, typeof(CREDENTIAL))!;
                if (cred.CredentialBlobSize > 0 && cred.CredentialBlob != IntPtr.Zero)
                {
                    byte[] blob = new byte[cred.CredentialBlobSize];
                    Marshal.Copy(cred.CredentialBlob, blob, 0, cred.CredentialBlobSize);
                    CredFree(credPtr);
                    return Encoding.UTF8.GetString(blob);
                }
                CredFree(credPtr);
            }
        }
        catch (Exception)
        {
            // Ignore and return null
        }
        return null;
    }

    public static bool WriteCredential(string target, string secret, string userName = "antigravity")
    {
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(secret);
            IntPtr blobPtr = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, blobPtr, bytes.Length);

            var cred = new CREDENTIAL
            {
                Flags = 0,
                Type = CRED_TYPE_GENERIC,
                TargetName = target,
                Comment = "Antigravity OAuth Token Managed by Account Manager",
                CredentialBlobSize = bytes.Length,
                CredentialBlob = blobPtr,
                Persist = CRED_PERSIST_LOCAL_MACHINE,
                UserName = userName
            };

            bool success = CredWrite(ref cred, 0);
            Marshal.FreeHGlobal(blobPtr);
            return success;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool DeleteCredential(string target = "gemini:antigravity")
    {
        try
        {
            return CredDelete(target, CRED_TYPE_GENERIC, 0);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static (string email, string name) ParseGoogleIdentity(string credentialJson)
    {
        string email = "Bilinmeyen Hesap";
        string name = "Google Kullanıcısı";

        try
        {
            using var doc = JsonDocument.Parse(credentialJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("id_token", out var idTokenProp))
            {
                var idToken = idTokenProp.GetString();
                if (!string.IsNullOrEmpty(idToken))
                {
                    var parts = idToken.Split('.');
                    if (parts.Length >= 2)
                    {
                        var payload = parts[1];
                        while (payload.Length % 4 != 0)
                        {
                            payload += "=";
                        }

                        byte[] bytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
                        var jwtJson = Encoding.UTF8.GetString(bytes);

                        using var jwtDoc = JsonDocument.Parse(jwtJson);
                        var jwtRoot = jwtDoc.RootElement;

                        if (jwtRoot.TryGetProperty("email", out var emailProp))
                        {
                            email = emailProp.GetString() ?? email;
                        }

                        if (jwtRoot.TryGetProperty("name", out var nameProp))
                        {
                            name = nameProp.GetString() ?? name;
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            // Ignore parse errors, return defaults
        }

        return (email, name);
    }
}
