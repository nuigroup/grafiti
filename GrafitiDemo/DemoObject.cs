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
using Tao.FreeGlut;
using Tao.OpenGl;
using TUIO;
using Grafiti;
using Grafiti.GestureRecognizers;

namespace GrafitiDemo
{
    public class DemoObject : ITangibleGestureListener
    {
        private MainClass m_mainClass;
        private DemoObjectManager m_objectManager;
        private List<DemoObjectLink> m_links = new List<DemoObjectLink>();
        private bool m_selected = false;
        private int m_id;
        private float m_x, m_y;
        private float m_angle;
        private float m_targetRadius;
        private float m_targetRadiusRef;
        private float m_size = 0.1f;
        private MyColor m_color;

        public float X { get { return m_x; } }
        public float Y { get { return m_y; } }
        public List<DemoObjectLink> Links { get { return m_links; } }
        public float TargetRadius { get { return m_targetRadius; } }

        internal bool Selected { get { return m_selected; } set { m_selected = value; } }


        public DemoObject(DemoObjectManager objectManager, MainClass mainClass, int fiducialId, float x, float y, float angle)
        {
            m_targetRadius = 0.4f;

            m_objectManager = objectManager;
            m_mainClass = mainClass;
            m_id = fiducialId++;
            m_x = x;
            m_y = y;
            m_angle = angle;

            Random random = new Random();
            m_color = new MyColor(random.NextDouble(), random.NextDouble(), random.NextDouble());

            GestureEventManager.SetPriorityNumber(typeof(BasicMultiFingerGR), m_objectManager.BasicMultiFingerGRConf, 0);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), m_objectManager.BasicMultiFingerGRConf, "Hover", OnHover);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), m_objectManager.BasicMultiFingerGRConf, "EndHover", OnEndHover);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), m_objectManager.BasicMultiFingerGRConf, "Tap", OnTap);
            //GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), m_objectManager.BasicMultiFingerGRConf, "DoubleTap", OnDoubleTap);

            GestureEventManager.SetPriorityNumber(typeof(PinchingGR), m_objectManager.PinchingConf, 2);
            GestureEventManager.RegisterHandler(typeof(PinchingGR), m_objectManager.PinchingConf, "Translate", OnPinchingEvent);
            GestureEventManager.RegisterHandler(typeof(PinchingGR), m_objectManager.PinchingConf, "Scale", OnPinchingEvent);
            GestureEventManager.RegisterHandler(typeof(PinchingGR), m_objectManager.PinchingConf, "Rotate", OnPinchingEvent);
            GestureEventManager.RegisterHandler(typeof(PinchingGR), m_objectManager.PinchingConf, "TranslateOrScaleBegin", OnPinchBegin);
            GestureEventManager.RegisterHandler(typeof(PinchingGR), m_objectManager.PinchingConf, "RotateBegin", OnPinchBegin);

        }

        internal void RemoveFromSurface()
        {
            GestureEventManager.UnregisterAllHandlersOf(this);
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

        public void Draw()
        {
            Gl.glPushMatrix();
                Gl.glTranslatef(m_x, m_y, 0f);
                Gl.glRotatef((float)(m_angle / Math.PI * 180.0f), 0f, 0f, 1);
                if (m_selected)
                {
                    Gl.glColor3d(1, 1, 0);
                    Utilities.DrawPlainSquare(m_size * 1.15f);
                }
                Gl.glColor3d(m_color.R, m_color.G, m_color.B);
                Utilities.DrawPlainSquare(m_size);
                Gl.glLineWidth(1);
                Utilities.DrawEmptyCircle(m_targetRadius);
                //g.DrawString(m_id.ToString(), m_font, white, x - 10, y - 10);


            Gl.glPopMatrix();

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
