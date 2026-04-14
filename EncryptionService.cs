using System.Security.Cryptography;
using System.Text;

namespace CUChatNet.Api.Services;

public class EncryptionService
{
    // IMPORTANTE: Estas claves deben tener 32 y 16 caracteres exactamente.
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("12345678901234567890123456789012");
    private static readonly byte[] Iv = Encoding.UTF8.GetBytes("1234567890123456");

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        using Aes aes = Aes.Create();
        aes.Key = Key;
        aes.IV = Iv;

        ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using MemoryStream ms = new();
        using (CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write))
        {
            using (StreamWriter sw = new(cs))
            {
                sw.Write(plainText);
            }
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        try
        {
            using Aes aes = Aes.Create();
            aes.Key = Key;
            aes.IV = Iv;

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using MemoryStream ms = new(Convert.FromBase64String(cipherText));
            using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
            using StreamReader sr = new(cs);
            return sr.ReadToEnd();
        }
        catch
        {
            // Si el mensaje no está cifrado (mensajes viejos), devuelve el texto tal cual
            return cipherText;
        }
    }
}