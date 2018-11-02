using com.veda.X4Lidar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace cser
{
    public partial class Form1 : Form, IShowInfo
    {
        Bitmap Backbuffer;
        public Form1()
        {
            InitializeComponent();
            this.SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.DoubleBuffer, true);

            System.Reflection.PropertyInfo controlProperty = typeof(System.Windows.Forms.Control)
        .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            controlProperty.SetValue(panelRadar, true, null);

            this.panelRadar.SetShowInfo(this);
        }

        LidarSerialControl comm = new LidarSerialControl();
        object lockobj = new object();

        X4Tran tran;
        private void Start_Click(object sender, EventArgs e)
        {
            try
            {
                //comm.Open();

            } catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }
        double zeroAng = 0;
        public List<RadAndLen> angleLen = new List<RadAndLen>();
        private void button1_Click(object sender, EventArgs e)
        {
            if (tran == null)
            {
                tran = new X4Tran((rl) =>
                {
                    lock (lockobj)
                    {
                        angleLen.Add(rl);
                        panelRadar.BeginInvoke(new Action(() =>
                        {
                            panelRadar.Invalidate();
                        }));
                    }
                }, z=>
                {
                    zeroAng = z;
                    Console.WriteLine("zero angle is " + z);
                    lock (lockobj)
                    {
                        panelRadar.AddPoints(angleLen);
                        angleLen.Clear();
                    }
                });
            }
            comm.Init(tran);
        }

        private void Stop_Click(object sender, EventArgs e)
        {
            comm.Stop();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            comm.Info();
        }

        
        private void Form1_Load(object sender, EventArgs e)
        {
            Backbuffer = new Bitmap(panelRadar.Width, panelRadar.Height);            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            comm.Stop();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            comm.Stop();
        }

        public void SetTextInfo(string text)
        {
            this.BeginInvoke(new Action(()=>
            {
                textBoxInfo.Text = text;
            }));
        }
    }
}

