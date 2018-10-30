using com.veda.X4Lidar;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace cser
{
    public class PaintPanel : Panel
    {
        const int MAXKEEP = 5;
        Bitmap Backbuffer;
        List<RadAndLen> _angleLen = new List<RadAndLen>();
        object lockobj = new object();
        protected int mouseX, mouseY;
        protected bool mouseMoved = false;
        protected IShowInfo _showInfo;
        public void SetShowInfo(IShowInfo s)
        {
            _showInfo = s;
        }
        public void AddPoints(List<RadAndLen> al)
        {
            lock(lockobj)
            {                
                _angleLen.Clear();
                _angleLen.AddRange(al);
            }
        }
        public PaintPanel()
        {
            this.SetStyle(
                System.Windows.Forms.ControlStyles.UserPaint |
                System.Windows.Forms.ControlStyles.AllPaintingInWmPaint |
                System.Windows.Forms.ControlStyles.OptimizedDoubleBuffer,
                true);
            Backbuffer = new Bitmap(Width, Height);
        }
        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            int w = Width;
            int h = Height;
            if (w < 100) w = 100;
            if (h < 100) h = 100;
            Backbuffer = new Bitmap(w, h);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            mouseMoved = true;
            mouseX = e.X;
            mouseY = e.Y;
        }

        string fmt(double d, int pad = 8)
        {
            return string.Format("{0:0.00}", d).PadLeft(pad);
        }
        protected override void OnPaint(PaintEventArgs e) {
            List<RadAndLen> points = new List<RadAndLen>();
            lock(lockobj)
            {
                points.AddRange(_angleLen);
            }
            int w = Width / 2;
            int h = Height / 2;

            int max = 3000; // Math.Max(lpoints.Max(p => Math.Abs(p.X)), lpoints.Max(p => Math.Abs(p.Y)))*2+1;
            using (var g = Graphics.FromImage(Backbuffer))
            {
                Action<RadAndLen, Brush,bool> plot = (p,br, isLine) =>
                {
                    int x = w - (p.X * h / max) ;
                    int y = h - ((p.Y * h) / max);
                    if (isLine)
                        g.DrawLine(Pens.Red,x, y, w, h);
                    else
                        g.FillRectangle(br, x, y, 1, 1);
                };
                g.Clear(Color.White);
                g.DrawLine(Pens.Black, 0, h, Width, h);
                g.DrawLine(Pens.Black, w, 0, w, Height);
                points.ForEach(p=>plot(p, Brushes.Black,false));

                if (mouseMoved && points.Any())
                {
                    SortedList<double, List<RadAndLen>> sortedList = new SortedList<double, List<RadAndLen>>();
                    g.DrawLine(Pens.Blue, mouseX, mouseY, w, h);
                    int x = w- mouseX;
                    int y = h - mouseY;
                    var rad = Math.Atan2(y, x);
                    if (_showInfo != null)
                    {
                        
                        foreach(var al in points)
                        {
                            var diff = Math.Abs(al.Rad - rad);                            
                            if (diff > 2 * Math.PI) diff -= 2 * Math.PI;
                            if (sortedList.ContainsKey(diff))
                            {
                                sortedList[diff].Add(al);
                            }
                            else
                            {
                                sortedList.Add(diff, new List<RadAndLen> { al });
                            }
                            if (sortedList.Count > MAXKEEP)
                            {
                                sortedList.RemoveAt(sortedList.Count - 1);
                            }
                        }

                        foreach(var diff in sortedList.Keys)
                        {
                            var lst = sortedList[diff];
                            foreach (var al in lst)
                            {
                                plot(al, Brushes.Blue, true);
                            }
                        }
                        double minDiff = sortedList.First().Key;
                        RadAndLen minal = sortedList.First().Value[0];
                        if (minal != null)
                        {
                            plot(minal, Brushes.Red,true);
                            _showInfo.SetTextInfo($"{fmt(mouseX - w)} {fmt(h - mouseY)} {fmt(rad * 180 / Math.PI)}  ang={fmt(minal.Rad * 180 / Math.PI)} len={fmt(minal.Len)}");
                        }
                    }
                }
            }
            e.Graphics.DrawImageUnscaled(Backbuffer, 0, 0);
        }
    }
}
