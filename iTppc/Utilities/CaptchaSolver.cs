using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace iTppc.Utilities
{
    public class CaptchaSolver
    {
        private Dictionary<string, string> _database;

        public CaptchaSolver(string binLocation)
        {
            BinaryFormatter b = new BinaryFormatter();

            using (FileStream f = new FileStream(binLocation, FileMode.Open))
            {
                _database = (Dictionary<string, string>)b.Deserialize(f);
                f.Close();
            }
        }

        public CaptchaSolver(byte[] binBytes)
        {
            BinaryFormatter b = new BinaryFormatter();

            using (MemoryStream m = new MemoryStream(binBytes))
            {
                _database = (Dictionary<string, string>)b.Deserialize(m);
                m.Close();
            }
        }

        public string solve(Bitmap captcha)
        {
            string captchaHash = BitConverter.ToString(HashAlgorithm.Create("MD5").ComputeHash(getPixelBytes(captcha)));

            if (!_database.ContainsKey(captchaHash))
                return random();

            return solve(captchaHash);
        }

        public string solve(string captchaHash)
        {
            return _database[captchaHash];
        }

        private string random()
        {
            Random rand = new Random();
            string vals = "ABCDEF0123456789";
            string ret = string.Empty;

            for (int i = 0; i < 6; i++)
                ret += vals[rand.Next(0, vals.Length)];

            return ret;
        }

        private byte[] getPixelBytes(Bitmap b)
        {
            List<byte> l = new List<byte>();

            for (int i = 0; i < b.Width; i++)
            {
                for (int j = 0; j < b.Height; j++)
                {
                    Color c = b.GetPixel(i, j);
                    l.Add(c.R);
                    l.Add(c.G);
                    l.Add(c.B);
                }
            }

            return l.ToArray();
        }
    }
}
