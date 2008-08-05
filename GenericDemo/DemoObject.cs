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
using System.Collections.Generic;
using TUIO;
using Grafiti;

namespace GenericDemo
{
    public class DemoObject : ITuioObjectGestureListener
    {
        private float m_targetRadius = 0.4f;
        private float m_targetRadiusRef;

        private Form m_ParentForm;
        private DemoObjectManager m_objectManager;
        private static GestureEventManager m_gEvtMgr = GestureEventManager.Instance;
        internal List<DemoObjectLink> m_links = new List<DemoObjectLink>();

        private Color m_color;
        private Pen m_pen;
        private Brush m_brush;
        private Font m_font;

        private int m_id;
        private float m_x, m_y;
        private float m_angle;
        private int m_size;

        private SolidBrush black = new SolidBrush(Color.Black);
        private SolidBrush white = new SolidBrush(Color.White);

        public float X { get { return m_x; } }
        public float Y { get { return m_y; } }
        public Color Color { get { return m_color; } }


        public DemoObject(DemoObjectManager objectManager, Form form, int fiducialId, float x, float y, float angle)
        {
            m_objectManager = objectManager;
            m_ParentForm = form;
            m_id = fiducialId++;
            m_x = x;
            m_y = y;
            m_angle = angle;
            m_size = MainForm.height / 20;

            Random random = new Random();
            m_color = Color.FromArgb(random.Next(255), random.Next(255), random.Next(255));
            m_pen = new Pen(m_color);
            m_brush = new SolidBrush(m_color);
            m_font = new Font("Arial", 12.0f);

            m_gEvtMgr.SetPriorityNumber(0);
            m_gEvtMgr.RegisterHandler(typeof(PinchingGR), m_objectManager.PinchingConfigurator, "Translate", OnPinchingEvent);
            m_gEvtMgr.RegisterHandler(typeof(PinchingGR), m_objectManager.PinchingConfigurator, "Scale", OnPinchingEvent);
            m_gEvtMgr.RegisterHandler(typeof(PinchingGR), m_objectManager.PinchingConfigurator, "Rotate", OnPinchingEvent);
            m_gEvtMgr.RegisterHandler(typeof(PinchingGR), m_objectManager.PinchingConfigurator, "TranslateOrScaleBegin", OnPinchBegin);
            m_gEvtMgr.RegisterHandler(typeof(PinchingGR), m_objectManager.PinchingConfigurator, "RotateBegin", OnPinchBegin);
            //m_gEvtMgr.RegisterHandler(typeof(BasicMultiFingerGR), "Tap", OnTap);
            //m_gEvtMgr.RegisterHandler(typeof(BasicMultiFingerGR), "DoubleTap", OnDoubleTap);
            //m_gEvtMgr.RegisterHandler(typeof(BasicMultiFingerGR), "Hover", OnHover);
        }

        public void OnPinchBegin(object obj, GestureEventArgs args)
        {
            PinchBeginEventArgs cArgs = (PinchBeginEventArgs)args;

            if (cArgs.EventId == "TranslateOrScaleBegin")
            {
                m_targetRadiusRef = m_targetRadius;
            }
        }
        public void OnPinchingEvent(object obj, GestureEventArgs args)
        {
            PinchEventArgs cArgs = (PinchEventArgs)args;
            if (cArgs.EventId == "Scale")
            {
                m_targetRadius = m_targetRadiusRef + cArgs.Scaling * 2;
            }
        }
        public void OnTap(object obj, GestureEventArgs args)
        {
            //Console.WriteLine("tap");
            //BasicMultiFingerEventArgs cArgs = (BasicMultiFingerEventArgs)args;
        }
        public void OnDoubleTap(object obj, GestureEventArgs args)
        {
            //Console.WriteLine("double tap");
            //BasicMultiFingerEventArgs cArgs = (BasicMultiFingerEventArgs)args;
        }
        public void OnHover(object obj, GestureEventArgs args)
        {
            //Console.WriteLine("hover");
            //BasicMultiFingerEventArgs cArgs = (BasicMultiFingerEventArgs)args;
        }


        internal void Update(float x, float y, float angle)
        {
            m_x = x;
            m_y = y;
            m_angle = angle;
        }

        public void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            int scale = MainForm.height;
            int x = (int)(m_x * scale);
            int y = (int)(m_y * scale);
            int targetRadius = (int)(m_targetRadius * scale);

            g.TranslateTransform(x, y);
            g.RotateTransform((float)(m_angle / Math.PI * 180.0f));
            g.TranslateTransform(-1 * x, -1 * y);

            g.FillRectangle(m_brush, x - m_size / 2, y - m_size / 2, m_size, m_size);
            g.DrawEllipse(m_pen, x - targetRadius, y - targetRadius, 2 * targetRadius, 2 * targetRadius);

            g.TranslateTransform(x, y);
            g.RotateTransform(-1 * (float)(m_angle / Math.PI * 180.0f));
            g.TranslateTransform(-1 * x, -1 * y);


            g.DrawString(m_id.ToString(), m_font, white, x - 10, y - 10);
        }



        #region IGestureListener Members

        public bool Contains(float x, float y)
        {
            float dx = Math.Abs(x - m_x);
            float dy = Math.Abs(y - m_y);
            return (float)Math.Sqrt(dx * dx + dy * dy) <= m_targetRadius;
        }

        public float GetSquareDistance(float x, float y)
        {
            float dx = x - m_x;
            float dy = y - m_y;
            return dx * dx + dy * dy;
        }

        #endregion
    }
}
