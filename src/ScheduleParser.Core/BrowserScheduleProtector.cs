using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace ScheduleParser.Core
{
    public static class BrowserScheduleProtector
    {
        private const string DefaultBrowserPassword = "Student2026";
        private const int SaltSize = 16;
        private const int IvSize = 16;
        private const int AesKeySize = 32;
        private const int MacKeySize = 32;

        public const int Iterations = 120000;
        public const string Algorithm = "PBKDF2-SHA256-AES-256-CBC-HMAC-SHA256";

        public static string ProtectJson(string plainJson)
        {
            if (plainJson == null)
            {
                throw new ArgumentNullException("plainJson");
            }

            byte[] salt = RandomBytes(SaltSize);
            byte[] iv = RandomBytes(IvSize);
            byte[] derived = DeriveKey(Encoding.UTF8.GetBytes(DefaultBrowserPassword), salt, Iterations, AesKeySize + MacKeySize);
            byte[] aesKey = Slice(derived, 0, AesKeySize);
            byte[] macKey = Slice(derived, AesKeySize, MacKeySize);
            byte[] cipherText = EncryptAesCbc(Encoding.UTF8.GetBytes(plainJson), aesKey, iv);
            byte[] tag = ComputeTag(macKey, salt, iv, cipherText);

            ProtectedSchedulePayload payload = new ProtectedSchedulePayload();
            payload.Protected = true;
            payload.Kind = "schedule-parser-protected";
            payload.Algorithm = Algorithm;
            payload.Iterations = Iterations;
            payload.Salt = Convert.ToBase64String(salt);
            payload.Iv = Convert.ToBase64String(iv);
            payload.Ciphertext = Convert.ToBase64String(cipherText);
            payload.Tag = Convert.ToBase64String(tag);

            JavaScriptSerializer serializer = CreateSerializer();
            return serializer.Serialize(payload);
        }

        public static string UnprotectJsonWithDefaultPassword(string json)
        {
            return UnprotectJson(json, DefaultBrowserPassword);
        }

        public static bool IsProtectedJson(string json)
        {
            ProtectedSchedulePayload payload = TryReadPayload(json);
            return payload != null && payload.Protected;
        }

        public static string UnprotectJson(string json, string password)
        {
            if (json == null)
            {
                throw new ArgumentNullException("json");
            }

            ProtectedSchedulePayload payload = TryReadPayload(json);
            if (payload == null || !payload.Protected)
            {
                return json;
            }

            if (!string.Equals(payload.Algorithm, Algorithm, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Формат защищенного расписания не поддерживается этой версией программы.");
            }

            byte[] salt = Convert.FromBase64String(payload.Salt);
            byte[] iv = Convert.FromBase64String(payload.Iv);
            byte[] cipherText = Convert.FromBase64String(payload.Ciphertext);
            byte[] tag = Convert.FromBase64String(payload.Tag);
            byte[] derived = DeriveKey(Encoding.UTF8.GetBytes(password ?? string.Empty), salt, payload.Iterations, AesKeySize + MacKeySize);
            byte[] aesKey = Slice(derived, 0, AesKeySize);
            byte[] macKey = Slice(derived, AesKeySize, MacKeySize);
            byte[] expectedTag = ComputeTag(macKey, salt, iv, cipherText);

            if (!FixedTimeEquals(tag, expectedTag))
            {
                throw new InvalidOperationException("Защищенное расписание не удалось открыть.");
            }

            byte[] plain = DecryptAesCbc(cipherText, aesKey, iv);
            return Encoding.UTF8.GetString(plain);
        }

        private static ProtectedSchedulePayload TryReadPayload(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                JavaScriptSerializer serializer = CreateSerializer();
                return serializer.Deserialize<ProtectedSchedulePayload>(json);
            }
            catch
            {
                return null;
            }
        }

        private static byte[] RandomBytes(int length)
        {
            byte[] bytes = new byte[length];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            return bytes;
        }

        private static byte[] EncryptAesCbc(byte[] plain, byte[] key, byte[] iv)
        {
            using (AesManaged aes = new AesManaged())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Key = key;
                aes.IV = iv;

                using (ICryptoTransform transform = aes.CreateEncryptor())
                {
                    return transform.TransformFinalBlock(plain, 0, plain.Length);
                }
            }
        }

        private static byte[] DecryptAesCbc(byte[] cipherText, byte[] key, byte[] iv)
        {
            using (AesManaged aes = new AesManaged())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Key = key;
                aes.IV = iv;

                using (ICryptoTransform transform = aes.CreateDecryptor())
                {
                    return transform.TransformFinalBlock(cipherText, 0, cipherText.Length);
                }
            }
        }

        private static byte[] ComputeTag(byte[] macKey, byte[] salt, byte[] iv, byte[] cipherText)
        {
            using (HMACSHA256 hmac = new HMACSHA256(macKey))
            {
                return hmac.ComputeHash(Combine(salt, iv, cipherText));
            }
        }

        private static byte[] DeriveKey(byte[] password, byte[] salt, int iterations, int length)
        {
            if (iterations <= 0)
            {
                throw new ArgumentOutOfRangeException("iterations");
            }

            using (HMACSHA256 hmac = new HMACSHA256(password))
            {
                int hashLength = hmac.HashSize / 8;
                int blockCount = (int)Math.Ceiling((double)length / hashLength);
                byte[] output = new byte[length];
                int offset = 0;

                for (int block = 1; block <= blockCount; block++)
                {
                    byte[] blockBytes = BuildSaltBlock(salt, block);
                    byte[] u = hmac.ComputeHash(blockBytes);
                    byte[] f = (byte[])u.Clone();

                    for (int i = 1; i < iterations; i++)
                    {
                        u = hmac.ComputeHash(u);
                        for (int j = 0; j < f.Length; j++)
                        {
                            f[j] ^= u[j];
                        }
                    }

                    int copy = Math.Min(hashLength, length - offset);
                    Buffer.BlockCopy(f, 0, output, offset, copy);
                    offset += copy;
                }

                return output;
            }
        }

        private static byte[] BuildSaltBlock(byte[] salt, int block)
        {
            byte[] blockBytes = new byte[salt.Length + 4];
            Buffer.BlockCopy(salt, 0, blockBytes, 0, salt.Length);
            blockBytes[salt.Length] = (byte)((block >> 24) & 0xff);
            blockBytes[salt.Length + 1] = (byte)((block >> 16) & 0xff);
            blockBytes[salt.Length + 2] = (byte)((block >> 8) & 0xff);
            blockBytes[salt.Length + 3] = (byte)(block & 0xff);
            return blockBytes;
        }

        private static byte[] Combine(params byte[][] parts)
        {
            int length = 0;
            foreach (byte[] part in parts)
            {
                if (part != null)
                {
                    length += part.Length;
                }
            }

            byte[] result = new byte[length];
            int offset = 0;
            foreach (byte[] part in parts)
            {
                if (part == null)
                {
                    continue;
                }

                Buffer.BlockCopy(part, 0, result, offset, part.Length);
                offset += part.Length;
            }

            return result;
        }

        private static byte[] Slice(byte[] source, int offset, int length)
        {
            byte[] result = new byte[length];
            Buffer.BlockCopy(source, offset, result, 0, length);
            return result;
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            int diff = 0;
            for (int i = 0; i < left.Length; i++)
            {
                diff |= left[i] ^ right[i];
            }

            return diff == 0;
        }

        private static JavaScriptSerializer CreateSerializer()
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = int.MaxValue;
            serializer.RecursionLimit = 100;
            return serializer;
        }
    }

    public sealed class ProtectedSchedulePayload
    {
        public bool Protected { get; set; }
        public string Kind { get; set; }
        public string Algorithm { get; set; }
        public int Iterations { get; set; }
        public string Salt { get; set; }
        public string Iv { get; set; }
        public string Ciphertext { get; set; }
        public string Tag { get; set; }
    }
}
