using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Simple encryption class for sending certain data to the server. 
/// Code adapted from https://stackoverflow.com/a/26518496
/// </summary>
public class SimpleAES : IDisposable
{
    private const int InitialisationVectorBytes = 16;

    // This key should be manually changed before building the test clients for
    // each session. While not the most secure possible method, this should 
    // then sufficiently prevent an attacker from reverse engineering the game,
    // getting the key and then decrypting subsequent playthroughs, since the
    // key will then have changed.
    private static readonly byte[] key = Convert.FromBase64String("fc9c36aede5d9128463bfbb3f5e5bc8589eddc01ca33a49ea89138ba7d64955dba90a4caf88389204c5cf521");

    private readonly UTF8Encoding encoder;
    private readonly ICryptoTransform encryptor;
    private readonly RijndaelManaged rijndael;

    public SimpleAES()
    {
        rijndael = new RijndaelManaged() { Key = key };
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
