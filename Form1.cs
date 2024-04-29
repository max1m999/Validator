using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Validator
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            richTextBox1.BackColor = ColorTranslator.FromHtml("#222E33");
            richTextBox1.ForeColor = Color.White;
            richTextBox1.Font = new Font("Tahoma", 12, FontStyle.Regular);
            richTextBox1.BorderStyle = BorderStyle.None;
        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "html files (*.html)|*.html|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    StreamReader sr = new StreamReader(filePath, Encoding.GetEncoding(DetectEncoding(filePath)));
                    richTextBox1.Text = sr.ReadToEnd();
                }
            }
        }

        public static string DetectEncoding(string filePath) // доработать, чтобы угадывало кодировку. либо сделать меню, чтобы пользователь менял сам
        {
            // Пытаемся определить кодировку по метаданным в HTML
            string text = File.ReadAllText(filePath);
            string metaCharsetPattern = "<meta.*?charset\\s*=\\s*[\"']?(?<charset>[\\w-]+)[\"']?";
            Match metaCharsetMatch = Regex.Match(text, metaCharsetPattern, RegexOptions.IgnoreCase);
            if (metaCharsetMatch.Success)
            {
                string charsetName = metaCharsetMatch.Groups["charset"].Value;
                try
                {
                    return charsetName;
                }
                catch (ArgumentException) //???
                { 
                
                }
            }
            return "Default";
        }
    }
}
