
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IdentityModel.Tokens;

using p2p_api.Models;

namespace p2p_api.Extensions;


public static class CryptographicExtensions
{
    private static byte[]? aesKey = null;
    private static byte[]? aesIV = null;

    public static string HashSHA1(this string data, string secret)
    {
        var secretBytes = Encoding.ASCII.GetBytes(secret);
        var dataBytes = Encoding.ASCII.GetBytes(data);
        var hmacSha = new HMACSHA1(secretBytes);
        var signatureBytes = hmacSha.ComputeHash(dataBytes);
        return System.Convert.ToBase64String(signatureBytes);
    }

    public static async Task<string> Encrypt(this RoomToken roomToken)
    {
        string json = JsonSerializer.Serialize(roomToken);

        using (var aes = Aes.Create())
        {
            if (aesKey == null)
            {
                aesKey = aes.Key;
                aesIV = aes.IV;
            }

            using (var memory = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream(memory, aes.CreateEncryptor(aesKey, aesIV), CryptoStreamMode.Write))
                using (var encryptWriter = new StreamWriter(cryptoStream))
                {
                    await encryptWriter.WriteAsync(json);
                    await encryptWriter.FlushAsync();
                    await cryptoStream.FlushFinalBlockAsync();
                    memory.Seek(0, SeekOrigin.Begin);
                    var bytes = new byte[memory.Length];
                    int count = await memory.ReadAsync(bytes, 0, (int)memory.Length);
                    return System.Convert.ToBase64String(bytes);
                }
            }
        }
    }

    public static async Task<RoomToken?> DecryptToRoomToken(this string data)
    {
        if (aesKey == null)
        {
            throw new InvalidOperationException();
        }
    
        using (var aes = Aes.Create())
        {
            using (var memory = new MemoryStream(System.Convert.FromBase64String(data)))
            using (var cryptoStream = new CryptoStream(memory, aes.CreateDecryptor(aesKey, aesIV), CryptoStreamMode.Read))
            using (var decryptReader = new StreamReader(cryptoStream))
            {
                return await JsonSerializer.DeserializeAsync<RoomToken>(decryptReader.BaseStream);
            }
        }
    }
}