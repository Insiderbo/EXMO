using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EXMOPR
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
        }

        public byte[] Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes)
        {
            byte[] encryptedBytes = null;
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }

            return encryptedBytes;
        }

        static string GenString()
        {
            var alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var name_len = new char[1000];
            var random = new Random();

            for (int i = 0; i < name_len.Length; i++)
            {
                name_len[i] = alphabet[random.Next(alphabet.Length)];
            }

            var rezult = new String(name_len);

            return rezult;
        }

        void Register()
        {
            if (textBox1.Text.Length > 3 && textBox2.Text.Length > 10 && textBox3.Text.Length > 10 && textBox4.Text.Length > 5)
            {

                byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes(textBox1.Text + "," + textBox2.Text + "," + textBox3.Text + "," + GenString());
                byte[] passwordBytes = Encoding.UTF8.GetBytes(textBox4.Text);

                passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

                byte[] bytesEncrypted = Encrypt(bytesToBeEncrypted, passwordBytes);

                File.WriteAllText("exmo.dat", Convert.ToBase64String(bytesEncrypted));

                MessageBox.Show("Successfully!");

                this.Hide();
                Form2 login = new Form2();
                login.ShowDialog();
                this.Close();
            }
            else
            {
                MessageBox.Show("Check you input!");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Register();
        }

        private void Form3_Load(object sender, EventArgs e)
        {

        }
    }
}
