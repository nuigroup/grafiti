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

namespace GenericDemo
{
    public class MainForm : Form, TuioListener
    {
        private TuioClient m_client;
        DemoObjectManager m_demoObjectManager;

        public static int width, height;
        private int window_width = 640;
        private int window_height = 480;
        private int window_left = 0;
        private int window_top = 0;
        private int screen_width = Screen.PrimaryScreen.Bounds.Width;
        private int screen_height = Screen.PrimaryScreen.Bounds.Height;

        private bool fullscreen, verbose;

        Random m_random = new Random();

        private static object m_lock = new object();

        List<DemoGroup> m_demoGroups = new List<DemoGroup>();

        public MainForm(int port)
        {
            verbose = false;
            fullscreen = false;
            width = window_width;
            height = window_height;

            this.ClientSize = new System.Drawing.Size(width, height);
            this.Name = "Grafiti Generic Demo";
            this.Text = "Grafiti Generic Demo";
            this.FormClosing += new FormClosingEventHandler(MainForm_FormClosing);
            this.KeyDown += new KeyEventHandler(MainForm_KeyDown);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                            ControlStyles.UserPaint |
                            ControlStyles.DoubleBuffer, true);

            m_demoObjectManager = new DemoObjectManager(this);

            m_client = new TuioClient(port);

            // Grafiti main listener - Tuio cursor listener
            m_client.addTuioListener(Surface.Instance);

            // Tuio object listener
            m_client.addTuioListener(m_demoObjectManager);

            // Auxiliar listener, that retrieves data from grafiti synchronously to output a visual feedback
            m_client.addTuioListener(this);

            m_client.connect();
        }

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
            
            Invalidate();
        }
        #endregion

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            lock (m_lock)
            {
                // draw background
                e.Graphics.FillRectangle(Brushes.White, 0, 0, width, height);

                // draw objects
                m_demoObjectManager.OnPaint(e);

                // draw finger groups
                foreach (DemoGroup demoGroup in m_demoGroups)
                    demoGroup.OnPaint(e);
            }
        }

        void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_client.removeTuioListener(this);
            m_client.removeTuioListener(m_demoObjectManager);
            m_client.removeTuioListener(Surface.Instance);
            m_client.disconnect();
            System.Environment.Exit(0);
        }

        private void MainForm_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {

            if (e.KeyData == Keys.F1)
            {
                if (fullscreen == false)
                {

                    width = screen_width;
                    height = screen_height;

                    window_left = this.Left;
                    window_top = this.Top;

                    this.FormBorderStyle = FormBorderStyle.None;
                    this.Left = 0;
                    this.Top = 0;
                    this.Width = screen_width;
                    this.Height = screen_height;

                    fullscreen = true;
                }
                else
                {

                    width = window_width;
                    height = window_height;

                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    this.Left = window_left;
                    this.Top = window_top;
                    this.Width = window_width;
                    this.Height = window_height;

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

            MainForm app = new MainForm(port);
            Application.Run(app);

        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(292, 266);
            this.Name = "Grafiti Generic Demo";
            this.ResumeLayout(false);
        }
    }
}
