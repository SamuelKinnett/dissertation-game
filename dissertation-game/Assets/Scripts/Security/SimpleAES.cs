using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Assets.Security;

/// <summary>
/// Simple encryption class for sending certain data to the server. 
/// Code adapted from https://stackoverflow.com/a/26518496
/// </summary>
public class SimpleAES : IDisposable
{
    private const int InitialisationVectorBytes = 16;

    private readonly UTF8Encoding encoder;
    private readonly ICryptoTransform encryptor;
    private readonly RijndaelManaged rijndael;

    public SimpleAES()
    {
        rijndael = new RijndaelManaged() { Key = EncryptionKey.Key };
        rijndael.GenerateIV();
        encryptor = rijndael.CreateEncryptor();
        encoder = new UTF8Encoding();
    }

    public void Dispose()
    {
        rijndael.Dispose();
        encryptor.Dispose();
    }

    public string Encrypt(string textToEncrypt)
    {
        return Convert.ToBase64String(Encrypt(encoder.GetBytes(textToEncrypt)));
    }

    public string Decrypt(string textToDecrypt)
    {
        return encoder.GetString(Decrypt(Convert.FromBase64String(textToDecrypt)));
    }

    private byte[] Encrypt(byte[] buffer)
    {
        // Prepend the crypto text with the initialisation vector
        byte[] inputBuffer = encryptor.TransformFinalBlock(buffer, 0, buffer.Length);
        return rijndael.IV.Concat(inputBuffer).ToArray();
    }

    private byte[] Decrypt(byte[] buffer)
    {
        // Extract the initialisation vector
        byte[] initialisationVector = buffer.Take(InitialisationVectorBytes).ToArray();
        using (var decryptor = rijndael.CreateDecryptor(rijndael.Key, initialisationVector))
        {
            return decryptor.TransformFinalBlock(buffer, InitialisationVectorBytes, buffer.Length - InitialisationVectorBytes);
        }
    }
}
