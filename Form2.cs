using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Grep_For_Tibetan
{
    public partial class Form2 : Form
    {
        public Action Worker { get; set; }
      
        public Form2(Action worker)
        {
            InitializeComponent();
            if (worker == null)
                throw new ArgumentNullException();
            Worker = worker;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Task.Factory.StartNew(Worker).ContinueWith(t => { this.Close(); }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public Form2(IContainer components, Label label1, ProgressBar progressBar1, Action worker)
        {
            this.components = components;
            this.label1 = label1;
            this.progressBar1 = progressBar1;
            Worker = worker;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (prog.counter > 0 && prog.counter <= 100)
            {
                this.progressBar1.Value = prog.counter;
                this.label2.Text = prog.counter.ToString() + "%";
                this.NotifyPropertyChanged();
            }
            else if (prog.counter > 100)
            {
                this.progressBar1.Value = 100;
                this.label2.Text = "100%";
            }
            this.NotifyPropertyChanged();
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }
    }
}
