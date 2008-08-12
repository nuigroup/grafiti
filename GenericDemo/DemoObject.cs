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
    public class DemoObject : ITangibleGestureListener
    {
        private float m_targetRadius;
        private float m_targetRadiusRef;

        private Form m_form;
        private DemoObjectManager m_objectManager;
        private static GestureEventManager m_gEvtMgr = GestureEventManager.Instance;
        private List<DemoObjectLink> m_links = new List<DemoObjectLink>();
        private bool m_selected = false;

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

        public List<DemoObjectLink> Links { get { return m_links; } }
        public float TargetRadius { get { return m_targetRadius; } }

        internal bool Selected
        {
            get { return m_selected; }
            set
            {
                m_selected = value;
            }
        }

        public DemoObject(DemoObjectManager objectManager, Form form, int fiducialId, float x, float y, float angle)
        {
            m_targetRadius = 0.4f;

            m_objectManager = objectManager;
            m_form = form;
            m_id = fiducialId++;
            m_x = x;
            m_y = y;
            m_angle = angle;
            m_size = m_form.ClientSize.Height / 20;

            Random random = new Random();
            m_color = Color.FromArgb(random.Next(255), random.Next(255), random.Next(255));
            m_pen = new Pen(m_color);
            m_brush = new SolidBrush(m_color);
            m_font = new Font("Arial", 12.0f);

            m_gEvtMgr.SetPriorityNumber(typeof(BasicMultiFingerGR), m_objectManager.BasicMultiFingerGRConf, -3);
            m_gEvtMgr.RegisterHandler(typeof(BasicMultiFingerGR), "Hover", OnHover);
            m_gEvtMgr.RegisterHandler(typeof(BasicMultiFingerGR), "EndHover", OnEndHover);
            m_gEvtMgr.RegisterHandler(typeof(BasicMultiFingerGR), "Tap", OnTap);
            //m_gEvtMgr.RegisterHandler(typeof(BasicMultiFingerGR), "DoubleTap", OnDoubleTap);

            m_gEvtMgr.SetPriorityNumber(typeof(PinchingGR), m_objectManager.PinchingConf, 0);
            m_gEvtMgr.RegisterHandler(typeof(PinchingGR), m_objectManager.PinchingConf, "Translate", OnPinchingEvent);
            m_gEvtMgr.RegisterHandler(typeof(PinchingGR), m_objectManager.PinchingConf, "Scale", OnPinchingEvent);
            m_gEvtMgr.RegisterHandler(typeof(PinchingGR), m_objectManager.PinchingConf, "Rotate", OnPinchingEvent);
            m_gEvtMgr.RegisterHandler(typeof(PinchingGR), m_objectManager.PinchingConf, "TranslateOrScaleBegin", OnPinchBegin);
            m_gEvtMgr.RegisterHandler(typeof(PinchingGR), m_objectManager.PinchingConf, "RotateBegin", OnPinchBegin);

        }

        internal void RemoveFromSurface()
        {
            GestureEventManager.Instance.UnregisterAllHandlersOf(this);
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
            Selected = false;
        }
        public void OnDoubleTap(object obj, GestureEventArgs args)
        {
            //Console.WriteLine("double tap");
            //BasicMultiFingerEventArgs cArgs = (BasicMultiFingerEventArgs)args;
        }
        public void OnHover(object obj, GestureEventArgs args)
        {
            BasicMultiFingerEventArgs cArgs = (BasicMultiFingerEventArgs)args;
            //Console.WriteLine("hover on {0} ({1} fingers)", m_id, cArgs.NFingers);
            if (cArgs.NFingers > 2)
                m_objectManager.OpenLinkRequest(cArgs.NFingers, this);
        }
        public void OnEndHover(object obj, GestureEventArgs args)
        {
            BasicMultiFingerEventArgs cArgs = (BasicMultiFingerEventArgs)args;
            //Console.WriteLine("end hover on {0} ({1} fingers)", m_id, cArgs.NFingers);
            if (cArgs.NFingers > 2)
                m_objectManager.CloseLinkRequest(cArgs.NFingers, this);
        }


        internal void Update(float x, float y, float angle)
        {
            m_x = x;
            m_y = y;
            m_angle = angle;
        }

        public void Draw(Graphics g, float screen)
        {
            int x = (int)(m_x * screen);
            int y = (int)(m_y * screen);
            int targetRadius = (int)(m_targetRadius * screen);

            g.TranslateTransform(x, y);
            g.RotateTransform((float)(m_angle / Math.PI * 180.0f));
            g.TranslateTransform(-1 * x, -1 * y);

            if (m_selected)
                g.FillRectangle(Brushes.Yellow, x - m_size * 0.75f, y - m_size * 0.75f, m_size * 1.5f, m_size * 1.5f);
            g.FillRectangle(m_brush, x - m_size / 2, y - m_size / 2, m_size, m_size);
            
            g.FillRectangle(m_brush, x - m_size / 2, y - m_size / 2, m_size, m_size);
            g.DrawEllipse(m_pen, x - targetRadius, y - targetRadius, 2 * targetRadius, 2 * targetRadius);

            g.TranslateTransform(x, y);
            g.RotateTransform(-1 * (float)(m_angle / Math.PI * 180.0f));
            g.TranslateTransform(-1 * x, -1 * y);


            g.DrawString(m_id.ToString(), m_font, white, x - 10, y - 10);
        }

        public bool ContainsPoint(float x, float y)
        {
            return GetSquareDistance(x, y) <= m_targetRadius * m_targetRadius;
        }


        #region IGestureListener Members

        public float GetSquareDistance(float x, float y)
        {
            float dx = x - m_x;
            float dy = y - m_y;
            return dx * dx + dy * dy;
        }

        #endregion

        public override string ToString()
        {
            return "Demo object " + m_id.ToString();
        }
    }
}
