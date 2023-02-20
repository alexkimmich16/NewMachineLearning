using System;
using System.Security.Cryptography;
//using System.Text.Json;
using UnityEngine;
public static class Encrypt
{
    public static byte[] GenerateKey(string saltVal, string passphrase)
    {
        byte[] salt = ConvertHexStringToByteArray("858c119fdd6b6dc9f2ab43f026eb8a80");
        int iterations = 1;
        var rfc2898 = new Rfc2898DeriveBytes(passphrase, salt, iterations);
        byte[] key = rfc2898.GetBytes(16);
        return key;
    }

    public static string EncryptString(byte[] key, string stringToEncrypt)
    {
        AesManaged aesCipher = new AesManaged();
        aesCipher.KeySize = 128;
        aesCipher.BlockSize = 128;
        aesCipher.Mode = CipherMode.CBC;
        aesCipher.Padding = PaddingMode.PKCS7;
        aesCipher.Key = key;
        aesCipher.IV = ConvertHexStringToByteArray("858c119fdd6b6dc9f2ab43f026eb8a80");
        byte[] b = System.Text.Encoding.UTF8.GetBytes(stringToEncrypt);
        ICryptoTransform encryptTransform = aesCipher.CreateEncryptor();
        byte[] ctext = encryptTransform.TransformFinalBlock(b, 0, b.Length);
        System.Console.WriteLine("IV:" + Convert.ToBase64String(aesCipher.IV));
        System.Console.WriteLine("Cipher text: " + Convert.ToBase64String(ctext));
        return Convert.ToBase64String(ctext);
    }

    public static byte[] ConvertHexStringToByteArray(string hexString)
    {
        int NumberChars = hexString.Length;
        byte[] bytes = new byte[NumberChars / 2];
        for (int i = 0; i < NumberChars; i += 2)
            bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
        return bytes;
    }

    

    public static string Encode(string Uncoded)
    {

        byte[] MyKey = GenerateKey("858c119fdd6b6dc9f2ab43f026eb8a80", "Ggq76QCwHmcWJgTHa94yWYNFZ9SfjBMM");

        // Use this to get the encrypted string
        string EncryptedString = EncryptString(MyKey, Uncoded);
        //Debug.Log(EncryptedString);
        return EncryptedString;
    }
    /*
    public static string Encode(string Uncoded)
    {
        Encrypt aes = new Encrypt();

        byte[] MyKey = aes.GenerateKey("858c119fdd6b6dc9f2ab43f026eb8a80", "Ggq76QCwHmcWJgTHa94yWYNFZ9SfjBMM");

        // Use this to get the encrypted string
        string EncryptedString = aes.EncryptString(MyKey, Uncoded);
        //Debug.Log(EncryptedString);
        return EncryptedString;
    }
   

    public void Main()
    {
        Encrypt aes = new Encrypt();

        byte[] MyKey = aes.GenerateKey("858c119fdd6b6dc9f2ab43f026eb8a80", "Ggq76QCwHmcWJgTHa94yWYNFZ9SfjBMM");

        // Use this to get the encrypted string
        var EncryptedString = aes.EncryptString(MyKey, "{nodes:[{name:TEST,value:2}]}");
    }
     */
}
