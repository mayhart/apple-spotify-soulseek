using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Spotify.Slsk.Integration.Desktop.Models;

namespace Spotify.Slsk.Integration.Desktop.Services;

public class SettingsService
{
    private static string SettingsFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".spotseek");

    private static string SettingsFile =>
        Path.Combine(SettingsFolder, "desktop-settings.json");

    private static string EncryptKey
    {
        get
        {
            const int keyLen = 32;
            string key = Environment.UserName;
            if (key.Length > keyLen) return key[..keyLen];
            while (key.Length < keyLen)
                key += (char)(65 + (key.Length - Environment.UserName.Length));
            return key;
        }
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                string json = File.ReadAllText(SettingsFile, Encoding.UTF8);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { /* return defaults on any error */ }
        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsFolder);
        string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsFile, json, Encoding.UTF8);
    }

    public static string Encrypt(string plainText)
    {
        byte[] key = Encoding.UTF8.GetBytes(EncryptKey);
        using Aes aes = Aes.Create();
        using ICryptoTransform encryptor = aes.CreateEncryptor(key, aes.IV);
        using MemoryStream ms = new();
        using (CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write))
        using (StreamWriter sw = new(cs))
            sw.Write(plainText);
        byte[] result = new byte[aes.IV.Length + ms.ToArray().Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(ms.ToArray(), 0, result, aes.IV.Length, ms.ToArray().Length);
        return Convert.ToBase64String(result);
    }

    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return string.Empty;
        try
        {
            byte[] fullCipher = Convert.FromBase64String(cipherText);
            byte[] iv = new byte[16];
            byte[] cipher = new byte[fullCipher.Length - 16];
            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);
            byte[] key = Encoding.UTF8.GetBytes(EncryptKey);
            using Aes aes = Aes.Create();
            using ICryptoTransform decryptor = aes.CreateDecryptor(key, iv);
            using MemoryStream ms = new(cipher);
            using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
            using StreamReader sr = new(cs);
            return sr.ReadToEnd();
        }
        catch { return string.Empty; }
    }
}
