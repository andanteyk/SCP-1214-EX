using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace random_number_test
{
    public partial class Form1 : Form
    {
        private RandomNumberGenerator Rng = new();

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            if (!File.Exists("seed.dat"))
            {
                MessageBox.Show("Run seed_generator to generate `seed.dat`.", "seed.dat not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            await Task.Run(() =>
            {
                using (var fileStream = new FileStream("seed.dat", FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var decompressedStream = new DeflateStream(fileStream, CompressionMode.Decompress))
                {
                    Rng.LoadState(decompressedStream);
                };
            });

            var chars = Rng.NextChars().GetEnumerator();
            var buffer = new char[50];
            string newline = Environment.NewLine;
            while (true)
            {
                for (int i = 0; i < 50; i++)
                {
                    chars.MoveNext();
                    buffer[i] = chars.Current;
                }

                string text = textBox1.Text;
                if (text.Length >= (50 + newline.Length) * 100)
                    text = text.Substring(50 + newline.Length);
                textBox1.Text = text + new string(buffer) + newline;

                textBox1.SelectionStart = textBox1.Text.Length;
                textBox1.ScrollToCaret();

                await Task.Delay(1000);
            }
        }
    }
}
