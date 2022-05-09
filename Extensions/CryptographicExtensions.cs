
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

    public static string Encrypt(this RoomToken roomToken)
    {
        using (Aes aes = Aes.Create())
        {
            if (aesKey == null)
            {
                aesKey = aes.Key;
                aesIV = aes.IV;
            }

            string json = JsonSerializer.Serialize(roomToken);

            using (var memory = new MemoryStream())
            using (var cryptoStream = new CryptoStream(memory,aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (var encryptWriter = new StreamWriter(cryptoStream))
            {
                encryptWriter.Write(json);
                memory.Seek(0, SeekOrigin.Begin);

                // Read the first 20 bytes from the stream.
                var bytes = new byte[memory.Length];
                return System.Convert.ToBase64String(bytes);
            }
        }
    }

    public static RoomToken DecryptToRoomToken(string data)
    {
        // TODO https://docs.microsoft.com/en-us/dotnet/standard/security/decrypting-data
        throw new NotImplementedException();
    }
}