/*
	GenericDemo, Grafiti demo application

    Copyright 2008  Alessandro De Nardi <alessandro.denardi@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License as
    published by the Free Software Foundation; either version 3 of 
    the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using TUIO;
using Grafiti;
using Grafiti.TouchControls;

namespace GenericDemo
{
    public partial class MainForm : Form, TuioListener, IGrafitiClientGUIManager, IGestureListener
    {
        #region Variables
        // The Tuio client
        private TuioClient m_client;

        // Manages tuio objects
        private DemoObjectManager m_demoObjectManager;

        // Manages the visual feedback of Grafiti's groups of fingers
        private List<DemoGroup> m_demoGroups = new List<DemoGroup>();


        // Coordinates stuff
        private int m_width, m_height;
        private int m_window_width = 640;
        private int m_window_height = 480;
        private int m_window_left = 0;
        private int m_window_top = 0;
        private int m_screen_width = Screen.PrimaryScreen.Bounds.Width;
        private int m_screen_height = Screen.PrimaryScreen.Bounds.Height;
        private float m_renderingOffsetX = 0, m_renderingOffsetY = 0;


        // To simulate transparency GUI controls will be placed where they can't be seen..
        private const int OFFSET_VIRTUAL_AREA = 5000;

        // ..and they'll be rendered into a bitmap buffer that will be drawn in the right place
        private Bitmap m_bitmapBuffer;

        // Manual double buffering. Thanks to Bob Powell - http://www.bobpowell.net/doublebuffer.htm
        private Bitmap _backBuffer;  


        // Auxiliar variables
        private Random m_random = new Random();
        private static object m_lock = new object();
        private bool fullscreen, verbose;
        private long m_lastTimestampInvalidate = 0;
        private List<double> m_debug_rr = new List<double>();

        #endregion

        #region Constructor
        public MainForm(int port)
        {
            CheckForIllegalCrossThreadCalls = false;

            verbose = false;
            fullscreen = false;
            m_width = m_window_width;
            m_height = m_window_height;

            InitializeComponent();

            touchPanel1.Location = new Point(touchPanel1.Location.X + OFFSET_VIRTUAL_AREA,
                touchPanel1.Location.Y + OFFSET_VIRTUAL_AREA);

            m_bitmapBuffer = new Bitmap(touchPanel1.Size.Width, touchPanel1.Size.Height);

            this.ClientSize = new System.Drawing.Size(m_width, m_height);
            this.Name = "Grafiti Generic Demo";
            this.Text = "Grafiti Generic Demo";
            this.Resize += new EventHandler(OnResize);
            this.FormClosing += new FormClosingEventHandler(OnClosing);
            this.KeyDown += new KeyEventHandler(MainForm_KeyDown);

            m_demoObjectManager = new DemoObjectManager(this);

            Surface.Instance.SetGUIManager(this);

            m_client = new TuioClient(port);

            // Grafiti main listener - Tuio cursor listener
            m_client.addTuioListener(Surface.Instance);

            // Tuio object listener
            m_client.addTuioListener(m_demoObjectManager);

            // Auxiliar listener, that retrieves data from grafiti synchronously to output a visual feedback
            m_client.addTuioListener(this);

            m_client.connect();

            GestureEventManager.Instance.SetPriorityNumber(typeof(CircleGR), 0);
            GestureEventManager.Instance.RegisterHandler(typeof(CircleGR), "Circle", OnCircleGesture);
        }
        #endregion

        #region GUI event handlers
        void OnResize(object sender, EventArgs e)
        {
            if ((float)ClientSize.Width / (float)ClientSize.Height > Surface.SCREEN_RATIO)
            {
                m_renderingOffsetX =
                    (int)((ClientSize.Width - ClientSize.Height * Surface.SCREEN_RATIO) / 2);
                m_renderingOffsetY = 0;
            }
            else
            {
                m_renderingOffsetX = 0;
                m_renderingOffsetY = 0;
            }
            if (_backBuffer != null)
            {

                _backBuffer.Dispose();

                _backBuffer = null;

            }
            Invalidate();
            base.OnSizeChanged(e);
        }
        void OnClosing(object sender, FormClosingEventArgs e)
        {
            m_client.disconnect();
            m_client.removeTuioListener(Surface.Instance);
            m_client.removeTuioListener(m_demoObjectManager);
            m_client.removeTuioListener(this);

            //Console.Write("Debug rr:");
            //foreach (int i in m_debug_rr)
            //    Console.Write(i + ", ");
            //Console.WriteLine();

            System.Environment.Exit(0);
        }
        private void MainForm_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {

            if (e.KeyData == Keys.F1)
            {
                if (fullscreen == false)
                {

                    m_width = m_screen_width;
                    m_height = m_screen_height;

                    m_window_left = this.Left;
                    m_window_top = this.Top;

                    this.FormBorderStyle = FormBorderStyle.None;
                    this.Left = 0;
                    this.Top = 0;
                    this.Width = m_screen_width;
                    this.Height = m_screen_height;

                    fullscreen = true;
                }
                else
                {

                    m_width = m_window_width;
                    m_height = m_window_height;

                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    this.Left = m_window_left;
                    this.Top = m_window_top;
                    this.Width = m_window_width;
                    this.Height = m_window_height;

                    fullscreen = false;
                }
            }
            else if (e.KeyData == Keys.Escape)
            {
                this.Close();

            }
            else if (e.KeyData == Keys.V)
            {
                verbose = !verbose;
            }

        }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (_backBuffer == null)
                _backBuffer = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);

            Graphics g = Graphics.FromImage(_backBuffer);
            g.Clear(Color.White);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;

            //Graphics g = e.Graphics;
            g.TranslateTransform(m_renderingOffsetX, m_renderingOffsetY);

            if (touchPanel1.Visible)
            {
                touchPanel1.DrawToBitmap(m_bitmapBuffer, new Rectangle(0, 0, touchPanel1.Size.Width, touchPanel1.Size.Height));
                g.DrawImage(m_bitmapBuffer,
                    touchPanel1.Location.X - OFFSET_VIRTUAL_AREA,
                    touchPanel1.Location.Y - OFFSET_VIRTUAL_AREA);
            }
            lock (m_lock)
            {
                // draw objects
                m_demoObjectManager.Draw(g, (float)ClientSize.Height);

                // draw finger groups
                foreach (DemoGroup demoGroup in m_demoGroups)
                    demoGroup.Draw(g, (float)ClientSize.Height);
            }

            g.Dispose();

            //Copy the back buffer to the screen
            e.Graphics.DrawImageUnscaled(_backBuffer, 0, 0);
        }
        #endregion

        #region Gesture event handlers
        public void OnCircleGesture(object obj, GestureEventArgs args)
        {
            touchPanel1.Visible = !touchPanel1.Visible;

            if (touchPanel1.Visible)
            {
                CircleEventArgs cArgs = (CircleEventArgs)args;
                Size s = touchPanel1.Size;
                int w = s.Width;
                int h = s.Height;
                int x = (int)(cArgs.MeanCenterX * ClientSize.Height) - (int)(w / 2);
                int y = (int)(cArgs.MeanCenterY * ClientSize.Height) - (int)(h / 2);
                touchPanel1.Location = new Point(x + OFFSET_VIRTUAL_AREA, y + OFFSET_VIRTUAL_AREA);
            }
        }
        #endregion

        #region TuioClient interface
        public void addTuioObject(TuioObject o) { }
        public void updateTuioObject(TuioObject o) { }
        public void removeTuioObject(TuioObject o) { }
        public void addTuioCursor(TuioCursor c) { }
        public void updateTuioCursor(TuioCursor c) { }
        public void removeTuioCursor(TuioCursor c) { }
        public void refresh(long timestamp)
        {
            lock (m_lock)
            {
                // Add demo groups
                foreach (Group group in Surface.Instance.AddedGroups)
                {
                    m_demoGroups.Add(new DemoGroup(this, group, Color.FromArgb(m_random.Next(255), m_random.Next(255), m_random.Next(255))));
                }

                // Remove demo groups
                m_demoGroups.RemoveAll(delegate(DemoGroup demoGroup)
                {
                    return (Surface.Instance.RemovedGroups.Contains(demoGroup.Group));
                });

                foreach (DemoGroup demoGroup in m_demoGroups)
                    demoGroup.Update(timestamp);
            }

            if (timestamp - m_lastTimestampInvalidate >= 40)
            {
                //DateTime now1, now2;
                //now1 = DateTime.Now;
                Invalidate();
                //now2 = DateTime.Now;
                //m_debug_rr.Add(now2.Subtract(now1).TotalMilliseconds);
                m_lastTimestampInvalidate = timestamp;
            }
        }
        #endregion

        #region IGrafitiClientGUIManager Members
        public IGestureListener HitTest(float xf, float yf)
        {
            xf *= ClientSize.Height;
            yf *= ClientSize.Height;

            int x = (int)xf + OFFSET_VIRTUAL_AREA;
            int y = (int)yf + OFFSET_VIRTUAL_AREA;


            #region Why GetChildAtPoint doesn't work?
            //Control target = GetChildAtPoint(new Point(x, y), GetChildAtPointSkip.Invisible);
            //if (target == null)
            //    return null; 
            #endregion

            #region
            Control topTarget = null;
            foreach (Control c in Controls)
            {
                if (c.Visible &&
                    x >= c.Location.X && x < c.Location.X + c.Size.Width &&
                    y >= c.Location.Y && y < c.Location.Y + c.Size.Height)
                {
                    topTarget = c;
                    break;
                }
            }

            if (topTarget == null)
                return null;

            Point p = new Point(x - topTarget.Location.X, y - topTarget.Location.Y);
            Control target = topTarget.GetChildAtPoint(p);

            if (target == null)
                target = topTarget;
            #endregion

            while (!(target is IGestureListener) && target != this)
                target = target.Parent;

            if (target == this)
                return null;

            return (IGestureListener)target;
        }
        public IEnumerable<ITuioObjectGestureListener> HitTestTangibles(float x, float y)
        {
            return m_demoObjectManager.HitTestTangibles(x, y);
        }
        public Point PointToClient(IGestureListener target, float x, float y)
        {
            // target location
            Point targetLocation = PointToClient(((Control)target).PointToScreen(new Point(0, 0)));

            // point location
            x = x * ClientSize.Height + OFFSET_VIRTUAL_AREA;
            y = y * ClientSize.Height + OFFSET_VIRTUAL_AREA;

            return new Point((int)x - targetLocation.X, (int)y - targetLocation.Y);
        }
        #endregion


        #region Main
        [STAThread]
        public static void Main(String[] argv)
        {
            int port = 0;
            switch (argv.Length)
            {
                case 1:
                    port = int.Parse(argv[0], null);
                    if (port == 0) goto default;
                    break;
                case 0:
                    port = 3333;
                    break;
                default:
                    Console.WriteLine("usage: [mono] GrafitiGenericDemo [port]");
                    System.Environment.Exit(0);
                    break;
            }

            // these will force the compilation of the GR classes
            new PinchingGR(new GRConfigurator());
            new BasicMultiFingerGR(new GRConfigurator());
            new MultiTraceGR(new GRConfigurator());
            new RemovingLinkGR(new GRConfigurator());
            new CircleGR(new GRConfigurator());

            MainForm app = new MainForm(port);
            Application.Run(app);

        }
        #endregion
    }
}