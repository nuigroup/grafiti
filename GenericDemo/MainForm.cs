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
using Grafiti.GestureRecognizers;
using GenericDemo.TouchControls;

namespace GenericDemo
{
    public partial class MainForm : Form, TuioListener, IGrafitiClientGUIManager, IGestureListener
    {
        #region Declarations
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
        private float m_centerScreenOffsetX = 0, m_centerScreenOffsetY = 0;


        // To simulate transparency GUI controls will be placed where they can't be seen..
        private const int OFFSET_VIRTUAL_AREA = 5000;
        private int m_offsetVirtualArea = OFFSET_VIRTUAL_AREA;

        // ..and they'll be rendered into a bitmap buffer that will be drawn in the right place
        private Bitmap m_bitmapBuffer;

        // Manual double buffering. Thanks to Bob Powell - http://www.bobpowell.net/doublebuffer.htm
        private Bitmap _backBuffer;  


        // Auxiliar variables
        private const string m_name = "Grafiti Demo";
        private Random m_random = new Random();
        private static object m_lock = new object();
        private bool m_fullscreen, m_tuioGraphics;
        private long m_lastTimestampInvalidate = 0;
        #endregion

        #region Constructor
        public MainForm(int port)
        {
            CheckForIllegalCrossThreadCalls = false;

            m_fullscreen = false;
            m_width = m_window_width;
            m_height = m_window_height;

            InitializeComponent();
            ClientSize = new System.Drawing.Size(m_width, m_height);
            
            m_tuioGraphics = true;
            foreach (Control c in Controls)
                c.Location = new Point(c.Location.X + m_offsetVirtualArea, c.Location.Y + m_offsetVirtualArea);

            m_bitmapBuffer = new Bitmap(m_touchPanel.Size.Width, m_touchPanel.Size.Height);


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


            UpdateFormText();

            GestureEventManager.SetPriorityNumber(typeof(CircleGR), 0);
            GestureEventManager.RegisterHandler(typeof(CircleGR), "Circle", OnCircleGesture);
        }
        #endregion

        #region Private members
        private void UpdateFormText()
        {
            Text = m_name + (m_tuioGraphics ? "  (TUIO graphics ON)" : "  (TUIO graphics OFF)");
        } 
        #endregion

        #region GUI event handlers
        private void touchButtonAdd_Click(object sender, EventArgs e)
        {
            OnTouchButtonAdd_FingerTap(this, new BasicMultiFingerEventArgs());
        }
        private void touchButtonClear_Click(object sender, EventArgs e)
        {
            OnTouchButtonClear_FingerTap(this, new BasicMultiFingerEventArgs());
        }
        private void touchButtonClose_Click(object sender, EventArgs e)
        {
            OnTouchButtonClose_FingerTap(this, new BasicMultiFingerEventArgs());
        }
        void OnResize(object sender, EventArgs e)
        {
            if ((float)ClientSize.Width / (float)ClientSize.Height > Surface.SCREEN_RATIO)
            {
                PointF p = new PointF(-m_centerScreenOffsetX, -m_centerScreenOffsetY);

                m_centerScreenOffsetX =
                    ((float)ClientSize.Width - ClientSize.Height * Surface.SCREEN_RATIO) / 2;
                m_centerScreenOffsetY = 0f;

                foreach (Control c in Controls)
                    c.Location = new Point(
                        c.Location.X + (int)(p.X + m_centerScreenOffsetX),
                        c.Location.Y + (int)(p.Y + m_centerScreenOffsetY));
            }
            else
            {
                foreach (Control c in Controls)
                    c.Location = new Point(c.Location.X - (int)m_centerScreenOffsetX, c.Location.Y - (int)m_centerScreenOffsetY);

                m_centerScreenOffsetX = 0;
                m_centerScreenOffsetY = 0;
            }

            if (_backBuffer != null)
            {
                _backBuffer.Dispose();
                _backBuffer = null;
            }
            Invalidate();
        }
        void OnClosing(object sender, FormClosingEventArgs e)
        {
            m_client.removeTuioListener(this);
            m_client.removeTuioListener(m_demoObjectManager);
            m_client.removeTuioListener(Surface.Instance);
            m_client.disconnect();

            if (_backBuffer != null)
            {
                _backBuffer.Dispose();
                _backBuffer = null;
            }

            System.Environment.Exit(0);
        }
        private void OnKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {

            if (e.KeyData == Keys.F1)
            {
                if (m_fullscreen == false)
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

                    m_fullscreen = true;
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

                    m_fullscreen = false;
                }
            }
            else if (e.KeyData == Keys.Escape)
            {
                this.Close();

            }
            else if (e.KeyData == Keys.T)
            {
                m_tuioGraphics = !m_tuioGraphics;
                if (m_tuioGraphics)
                {
                    foreach (Control c in Controls)
                        c.Location = new Point(c.Location.X + OFFSET_VIRTUAL_AREA, c.Location.Y + OFFSET_VIRTUAL_AREA);
                    m_offsetVirtualArea = OFFSET_VIRTUAL_AREA;
                }
                else
                {
                    foreach (Control c in Controls)
                        c.Location = new Point(c.Location.X - OFFSET_VIRTUAL_AREA, c.Location.Y - OFFSET_VIRTUAL_AREA);
                    m_offsetVirtualArea = 0;
                }
                Invalidate();

                UpdateFormText();
            }

        }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (ClientSize.Height <= 0 || ClientSize.Width <= 0)
                return;

            if (!m_tuioGraphics)
            {
                e.Graphics.Clear(Color.White);
                return;
            }

            if (_backBuffer == null)
                _backBuffer = new Bitmap(ClientSize.Width, ClientSize.Height);

            Graphics g = Graphics.FromImage(_backBuffer);
            g.Clear(Color.White);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;

            if (m_touchPanel.Visible)
            {
                m_touchPanel.DrawToBitmap(m_bitmapBuffer, new Rectangle(0, 0, m_touchPanel.Size.Width, m_touchPanel.Size.Height));
                g.DrawImage(m_bitmapBuffer,
                    m_touchPanel.Location.X - m_offsetVirtualArea,
                    m_touchPanel.Location.Y - m_offsetVirtualArea);
            }

            g.TranslateTransform(m_centerScreenOffsetX, m_centerScreenOffsetY);
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
        void OnTouchButtonClose_FingerTap(object obj, BasicMultiFingerEventArgs args)
        {
            m_touchPanel.Visible = false;
        }
        void OnTouchButtonClear_FingerTap(object obj, BasicMultiFingerEventArgs args)
        {
            m_listBox.Items.Clear();
        }
        void OnTouchButtonAdd_FingerTap(object obj, BasicMultiFingerEventArgs args)
        {
            TouchRadioButton radioButton;
            if (m_touchRadioButton1.Checked)
                radioButton = m_touchRadioButton1;
            else
                if (m_touchRadioButton2.Checked)
                    radioButton = m_touchRadioButton2;
                else
                    radioButton = m_touchRadioButton3;
            m_listBox.Items.Add(radioButton.Text);

        }
        public void OnCircleGesture(object gr, GestureEventArgs args)
        {
            m_touchPanel.Visible = !m_touchPanel.Visible;

            if (m_touchPanel.Visible)
            {
                CircleGREventArgs cArgs = (CircleGREventArgs)args;
                Size s = m_touchPanel.Size;
                int w = s.Width;
                int h = s.Height;
                int x = (int)(cArgs.MeanCenterX * ClientSize.Height) - (int)(w / 2);
                int y = (int)(cArgs.MeanCenterY * ClientSize.Height) - (int)(h / 2);
                m_touchPanel.Location = new Point(x + m_offsetVirtualArea, y + m_offsetVirtualArea);
            }
        }
        #endregion

        #region TuioListener members
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

            if (m_tuioGraphics && timestamp - m_lastTimestampInvalidate >= 40)
            {
                m_lastTimestampInvalidate = timestamp;
                Invalidate();
            }
        }
        #endregion

        #region IGrafitiClientGUIManager Members
        public IGestureListener HitTest(float xf, float yf)
        {
            xf *= ClientSize.Height;
            yf *= ClientSize.Height;

            int x = (int)xf + m_offsetVirtualArea + (int)m_centerScreenOffsetX;
            int y = (int)yf + m_offsetVirtualArea + (int)m_centerScreenOffsetY;


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
        public List<ITangibleGestureListener> HitTestTangibles(float x, float y)
        {
            return m_demoObjectManager.HitTestTangibles(x, y);
        }
        public void PointToClient(IGestureListener target, float x, float y, out float cx, out float cy)
        {
            // target's location relative to this
            Point targetLocation = PointToClient(((Control)target).PointToScreen(new Point(0, 0)));

            // given point's location relative to this
            x = x * ClientSize.Height + m_offsetVirtualArea;
            y = y * ClientSize.Height + m_offsetVirtualArea;

            // point relative to target
            cx = x - targetLocation.X;
            cy = y - targetLocation.Y;
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
