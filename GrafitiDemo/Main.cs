/*
	GrafitiDemo, Grafiti demo application

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
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using TUIO;
using Grafiti;
using Grafiti.GestureRecognizers;
using Tao.FreeGlut;
using Tao.OpenGl;
using System.Runtime.InteropServices;

namespace GrafitiDemo
{
    public class MainClass : TuioListener, IGrafitiClientGUIManager, IGestureListener
    {
        #region Declarations
        private static MainClass s_instance = null;

        private static object m_lock = new object();

        // The Tuio client
        private TuioClient m_client;

        // Manages tuio objects
        private DemoObjectManager m_demoObjectManager;

        // Manages the visual feedback of Grafiti's groups of fingers
        private List<DemoGroup> m_demoGroups = new List<DemoGroup>();


        private bool m_teapotEnabled = false;
        private float m_teapotX = 0.5f, m_teapotY = 0.5f, m_teapotScale = 0.2f;


        // Coordinates stuff
        internal int m_width, m_height;
        private int m_window_width = 640;
        private int m_window_height = 480;
        private int m_window_left = 0;
        private int m_window_top = 0;
        private float m_centerScreenOffsetX = 0;


        // Auxiliar variables
        private const string m_name = "Grafiti Demo";
        internal Random m_random = new Random();
        private bool m_fullscreen = false;
        private long m_lastTimestampInvalidate = 0;
        #endregion

        #region Constructor
        private MainClass(int port)
        {
            m_width = m_window_width;
            m_height = m_window_height;

            m_demoObjectManager = new DemoObjectManager(this);

            Surface.Initialize(this);

            m_client = new TuioClient(port);

            // Grafiti main listener - Tuio cursor listener
            m_client.addTuioListener(Surface.Instance);

            // Tuio object listener
            m_client.addTuioListener(m_demoObjectManager);

            // Auxiliar listener, that retrieves data from grafiti synchronously to output a visual feedback
            m_client.addTuioListener(this);

            m_client.connect();

            GestureEventManager.SetPriorityNumber(typeof(CircleGR), 2);
            GestureEventManager.RegisterHandler(typeof(CircleGR), "Circle", OnCircleGesture);
        }
        #endregion

        #region Gesture event handlers
        public void OnCircleGesture(object gr, GestureEventArgs args)
        {
            CircleGREventArgs cArgs = (CircleGREventArgs)args;
            
            lock (m_lock)
            {
                m_teapotEnabled = !m_teapotEnabled;
                if (m_teapotEnabled)
                {
                    m_teapotX = cArgs.MeanCenterX;
                    m_teapotY = cArgs.MeanCenterY;
                    m_teapotScale = cArgs.MeanRadius;
                } 
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
                    m_demoGroups.Add(new DemoGroup(this, group, new MyColor(m_random.NextDouble(),m_random.NextDouble(),m_random.NextDouble())));
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
                m_lastTimestampInvalidate = timestamp;
                //Invalidate();
            }
            //refresh
        }
        #endregion

        #region IGrafitiClientGUIManager Members
        public IGestureListener HitTest(float xf, float yf)
        {
            return null;
        }
        public List<ITangibleGestureListener> HitTestTangibles(float x, float y)
        {
            return m_demoObjectManager.HitTestTangibles(x, y);
        }
        public void PointToClient(IGestureListener target, float x, float y, out float cx, out float cy)
        {
            cx = x;
            cy = y;
        }
        #endregion



        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(String[] argv)
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

            s_instance = new MainClass(port);




            // initialise glut library
            Glut.glutInit();

            // initialise the window settings
            Glut.glutInitDisplayMode(Glut.GLUT_DOUBLE | Glut.GLUT_RGB);
            Glut.glutInitWindowSize(300, 300);
            Glut.glutCreateWindow("Grafiti Demo");

            // do our own initialisation
            Init();

            // keyboard callback functions
            Glut.glutKeyboardFunc(new Glut.KeyboardCallback(S_KeyPressed));

            // set the callback functions
            Glut.glutDisplayFunc(new Glut.DisplayCallback(S_Display));
            Glut.glutReshapeFunc(new Glut.ReshapeCallback(S_Reshape));
            Glut.glutTimerFunc(40, new Glut.TimerCallback(S_Timer), 0);

            // enter the main loop. Glut will be here permanently from now on
            // until we quit the program
            Glut.glutMainLoop();
        }


        #region Private members
        private void Exit()
        {
            m_client.removeTuioListener(this);
            m_client.removeTuioListener(m_demoObjectManager);
            m_client.removeTuioListener(Surface.Instance);
            m_client.disconnect();

            Glut.glutLeaveMainLoop();

            System.Environment.Exit(0);
        }
        #endregion


        /// <summary>
        /// initialises the openGL settings
        /// </summary>
        private static void Init()
        { 
            Gl.glClearColor(0.0f, 0.0f, 0.3f, 1.0f);
            //Gl.glShadeModel(Gl.GL_SMOOTH);
            Gl.glPushAttrib(Gl.GL_DEPTH_BUFFER_BIT);
            Gl.glDisable(Gl.GL_DEPTH_TEST);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glEnable(Gl.GL_POLYGON_SMOOTH);

            ////////////
            //Gl.glClearColor(0f, 0f, 0f, 1f);
            //Gl.glShadeModel(Gl.GL_SMOOTH);
            //Gl.glClearDepth(1.0f);
            //Gl.glEnable(Gl.GL_DEPTH_TEST);
            //Gl.glDepthFunc(Gl.GL_LEQUAL);
            //Gl.glHint(Gl.GL_PERSPECTIVE_CORRECTION_HINT, Gl.GL_NICEST);
            //Gl.glCullFace(Gl.GL_BACK);
        }

        /// <summary>
        /// called when the window changes size
        /// </summary>
        /// <param name="w">new width of the window</param>
        /// <param name="h">ne height of the window</param>
        private static void S_Reshape(int w, int h)
        {
            s_instance.Reshape(w, h);
        }
        private void Reshape(int w, int h)
        {
            if (h == 0)     // Prevent A Divide By Zero By
                h = 1;		// Making Height Equal One

            Gl.glViewport(0, 0, w, h); // Reset The Current Viewport

            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();
            //int[] vPort = new int[4];
            //Gl.glGetIntegerv(Gl.GL_VIEWPORT, vPort);
            //Gl.glOrtho(vPort[0], vPort[0] + vPort[2], vPort[1] + vPort[3], vPort[1], -1, 1);

            //Glu.gluPerspective(45f, 4f / 3f, 0.1, 5);
            //Gl.glFrustum(0, w, h, 0, 20, 500);
            
            Gl.glOrtho(0, w, h, 0, -1, 1);

            Gl.glMatrixMode(Gl.GL_MODELVIEW);


            if ((float)w / (float)h > Settings.GetScreenRatio())
                m_centerScreenOffsetX = ((float)w - h * Settings.GetScreenRatio()) / 2;
            else
                m_centerScreenOffsetX = 0;

            m_width = w;
            m_height = h;
        }

        private static void S_Timer(int val)
        {
            Glut.glutPostRedisplay();
            Glut.glutTimerFunc(42, S_Timer, 0);
            Thread.Sleep(30);
        }

        /// <summary>
        /// the display callback function 
        /// </summary>
        private static void S_Display()
        {
            s_instance.Display();
        }
        private void Display()
        {
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);
            Gl.glLoadIdentity();
            Gl.glTranslatef(m_centerScreenOffsetX + 0.375f, 0.375f, 0f);

            //Gl.glRotatef(45, 1, 0, 0);

            Gl.glPushMatrix();
                Gl.glScalef(m_height, m_height, 1f);
                lock (m_lock)
                {
                    // teapot
                    if (m_teapotEnabled)
                    {
                        Gl.glColor3d(1, 1, 1);
                        Gl.glPushMatrix();
                            Gl.glTranslatef(m_teapotX, m_teapotY, 0f);
                            Gl.glScalef(1, -1, 1);
                            Glut.glutSolidTeapot(m_teapotScale);
                        Gl.glPopMatrix();
                    }

                    // draw finger groups
                    foreach (DemoGroup demoGroup in m_demoGroups)
                        demoGroup.Draw();
                }


            // draw objects
            m_demoObjectManager.Draw();
            Gl.glPopMatrix();



            //Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            //Gl.glEnable(Gl.GL_BLEND);
            //Gl.glEnable(Gl.GL_LINE_SMOOTH);
            //Gl.glLineWidth(2.0f);

            Glut.glutSwapBuffers();
        }

        #region Keyboard input functions
        /// <summary>
        /// handles 'normal' key presses. 
        /// </summary>
        /// <param name="key"> the key that was pressed </param>
        /// <param name="x"> the x coord of the mouse at the time </param>
        /// <param name="y"> the y coord of the mouse at the time </param>
        private static void S_KeyPressed(byte key, int x, int y)
        {
            s_instance.KeyPressed(key, x, y);
        }
        private void KeyPressed(byte key, int x, int y)
        {
            switch (key)
            {
                case (byte)'a':
                    break;
                case (byte)'q':
                    Exit();
                    break;
                case (byte)'f':
                    m_fullscreen = !m_fullscreen;
                    if (m_fullscreen)
                    {
                        // get the size of the window
                        int[] viewport = new int[4];
                        Gl.glGetIntegerv(Gl.GL_VIEWPORT, viewport);
                        m_window_width = viewport[2];
                        m_window_height = viewport[3];

                        //RECT rect = new RECT();
                        //if (GetWindowRect( ?, out rect))
                        m_window_left = 0;// xPos;
                        m_window_top = 0;// yPos;
                        Glut.glutFullScreen();
                    }
                    else
                    {
                        Glut.glutPositionWindow(m_window_left, m_window_top);
                        Glut.glutReshapeWindow(m_window_width, m_window_height);
                    }
                    break;
                // etc 
            }
            // force re-display
            Glut.glutPostRedisplay();

            //HWND z;
        }

        //[DllImport("user32.dll", SetLastError = true)] 
        //static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
        //[StructLayout(LayoutKind.Sequential)]
        //public struct RECT
        //{
        //    public int X;
        //    public int Y;
        //    public int Width;
        //    public int Height;
        //}
        #endregion
    }
}