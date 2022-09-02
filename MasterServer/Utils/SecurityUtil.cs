using Serilog;
using System.Security.Cryptography;
using System.Text;
namespace MasterServer
{
    public static class SecurityUtil
    {
        public static string MD5Encrypt(string txt)
        {
            byte[] data = Encoding.UTF8.GetBytes(txt);
            MD5 md5 = MD5.Create();
            data = md5.ComputeHash(data);
            string byte2String = "";
            for (int i = 0; i < data.Length; i++)
            {
                byte2String += data[i].ToString("x2");
            }
            return byte2String;
        }

        /// <summary>
        /// RSA产生密钥
        /// </summary>
        /// <param name="xmlPrivateKey">私钥</param>
        /// <param name="xmlPublicKey">公钥</param>
        public static void GenerateRsaKey(out string xmlPrivateKey, out string xmlPublicKey)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            xmlPrivateKey = rsa.ToXmlString(true);
            xmlPublicKey = rsa.ToXmlString(false);
        }
        public static string RsaEncrypt(string txt, string xmlPublicKey)
        {
            byte[] data = Encoding.UTF8.GetBytes(txt);
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(xmlPublicKey);
            byte[] encryptData = rsa.Encrypt(data, false);
            return Convert.ToBase64String(encryptData);
        }

        public static string RsaDecrypt(string txt, string xmlPrivateKey)
        {
            byte[] data = Convert.FromBase64String(txt);
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(xmlPrivateKey);
            byte[] decryptData = rsa.Decrypt(data, false);
            return Encoding.UTF8.GetString(decryptData);
        }


        /// <summary>
        /// Aes
        /// </summary>
        /// <param name="txt"></param>
        /// <param name="key">支持16，24，32长度，分别对应128，192，256位加密</param>
        /// <returns></returns>
        public static string AESEncrypt(string txt, string key)
        {
            if (key.Length != 16 && key.Length != 24 && key.Length != 32)
            {
                Log.Error("key length error!");
                return "";
            }
            Aes aes = Aes.Create();
            byte[] data = Encoding.UTF8.GetBytes(txt);
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = Encoding.UTF8.GetBytes(key.Substring(0, 16));
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string AESDecrypt(string txt, string key)
        {
            if (key.Length != 16 && key.Length != 24 && key.Length != 32)
            {
                Log.Error("key length error!");
                return "";
            }
            Aes aes = Aes.Create();
            byte[] data = Convert.FromBase64String(txt);
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = Encoding.UTF8.GetBytes(key.Substring(0, 16));
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
        }
    }
}