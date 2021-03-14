using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Grep_For_Tibetan
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            //this.textBox1.HorizontalScrollbar = true;
            this.textBox1.Clear();
            foreach (KeyValuePair<string, string> item in prog.SFound)
            {
                string added = Environment.NewLine + item.Value + ": " + Environment.NewLine +
                    item.Key + Environment.NewLine;
                this.textBox1.AppendText(added);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
        void Save_it(Encoding code)
        {
            if (textBox1.Text.Length > 0)
            {
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.InitialDirectory = Application.StartupPath;
                saveFileDialog1.Title = "Save text Files";
                saveFileDialog1.DefaultExt = "txt";
                saveFileDialog1.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog1.FilterIndex = 2;
                saveFileDialog1.RestoreDirectory = true;
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    var frm = new WaitFrm();
                    frm.Show();
                    File.WriteAllText(saveFileDialog1.FileName, textBox1.Text.ToString(), code);
                    frm.Hide();
                }
                else
                {
                    MessageBox.Show("File Not save yet", "warning",MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

            }
        }
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save_it(Encoding.UTF8);
        }

        private void saveAsUnicodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save_it(Encoding.Unicode);
        }

        private void disclaimerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var disc = new Form4();
            disc.Show();
;        }

        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var about = new frmAbout();
            about.Show();
        }
    }
}
