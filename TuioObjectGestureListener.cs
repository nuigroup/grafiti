/*
	Grafiti library

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
using System.Text;
using System.Collections.Generic;

using Grafiti;
using SimpleGRNS;


namespace Client
{
    public class TuioObjectGestureListener : IGestureListener
    {
        private const float INTERACTION_RANGE = 0.2f;
        private GestureEventManager m_gEvtMgr;
        private readonly int m_sessionId, m_fId;
        private float m_x, m_y;

        public TuioObjectGestureListener(GestureEventManager gestureEventManager, int sessionId, int fId, float x, float y)
        {
            m_gEvtMgr = gestureEventManager;
            m_sessionId = sessionId;
            m_fId = fId;
            m_x = x;
            m_y = y;

            // these will force the classes to compile
            new Basic1FingerGR(this);
            new SimpleGR();
            new MultiTraceGR(this);

            // LGRs
            //m_surface.SetPriorityNumber(0);
            //m_surface.RegisterHandler(typeof(SimpleGR), SimpleGR.Events.SimpleGesture, new GestureEventHandler(SimpleGestureEventHandler));

            //// GGRs

            m_gEvtMgr.SetPriorityNumber(1);
            m_gEvtMgr.RegisterHandler(typeof(Basic1FingerGR), Basic1FingerGR.Events.Tap, OnBasicSingleFingerEvent);
            m_gEvtMgr.RegisterHandler(typeof(Basic1FingerGR), Basic1FingerGR.Events.DoubleTap, OnBasicSingleFingerEvent);
            m_gEvtMgr.RegisterHandler(typeof(Basic1FingerGR), Basic1FingerGR.Events.TripleTap, OnBasicSingleFingerEvent);
            m_gEvtMgr.RegisterHandler(typeof(Basic1FingerGR), Basic1FingerGR.Events.Down, OnBasicSingleFingerEvent);
            m_gEvtMgr.RegisterHandler(typeof(Basic1FingerGR), Basic1FingerGR.Events.Up, OnBasicSingleFingerEvent);
            m_gEvtMgr.RegisterHandler(typeof(Basic1FingerGR), Basic1FingerGR.Events.Enter, OnBasicSingleFingerEvent);
            m_gEvtMgr.RegisterHandler(typeof(Basic1FingerGR), Basic1FingerGR.Events.Leave, OnBasicSingleFingerEvent);
            m_gEvtMgr.RegisterHandler(typeof(Basic1FingerGR), Basic1FingerGR.Events.Hover, OnBasicSingleFingerEvent);
            //m_gEvtMgr.RegisterHandler(typeof(Basic1FingerGR), Basic1FingerGR.Events.Move, OnBasicSingleFingerEvent);

            //m_gEvtMgr.SetPriorityNumber(2);
            //m_gEvtMgr.RegisterHandler(typeof(MultiTraceGR), MultiTraceGR.Events.MultiTraceStarted, OnMultiTraceEvent);
            //m_gEvtMgr.RegisterHandler(typeof(MultiTraceGR), MultiTraceGR.Events.MultiTraceEnter, OnMultiTraceEvent);
            //m_gEvtMgr.RegisterHandler(typeof(MultiTraceGR), MultiTraceGR.Events.MultiTraceLeave, OnMultiTraceEvent);
            ////m_gEvtMgr.RegisterHandler(typeof(MultiTraceGR), MultiTraceGR.Events.MultiTraceMove, OnMultiTraceEvent);
            //m_gEvtMgr.RegisterHandler(typeof(MultiTraceGR), MultiTraceGR.Events.MultiTraceDown, OnMultiTraceEvent);
            //m_gEvtMgr.RegisterHandler(typeof(MultiTraceGR), MultiTraceGR.Events.MultiTraceUp, OnMultiTraceEvent);
            //m_gEvtMgr.RegisterHandler(typeof(MultiTraceGR), MultiTraceGR.Events.MultiTraceEnd, OnMultiTraceEvent);
        }

        public void OnBasicSingleFingerEvent(object obj, GestureEventArgs args)
        {
            BasicSingeFingerEventArgs arg = (BasicSingeFingerEventArgs)args;
            Console.WriteLine("{0} on {1} ({2};{3})", args.EventId, this, arg.X, arg.Y);
        }

        public void OnMultiTraceEvent(object MultiTraceGR, GestureEventArgs args)
        {
            Console.WriteLine("{0} received the MultiTraceEvent {1}", ToString(), ((MultiTraceEventArgs)args).EventId);
        }

        public void SimpleGestureEventHandler(object source, GestureEventArgs args)
        {
            Console.WriteLine("{0} received the SimpleGestureEvent", ToString());
        }

        public bool Contains(float x, float y)
        {
            float dx = Math.Abs(x - m_x);
            float dy = Math.Abs(y - m_y);
            return (float)Math.Sqrt(dx * dx + dy * dy) <= INTERACTION_RANGE;
        }

        public float GetSquareDistance(float x, float y)
        {
            float dx = x - m_x;
            float dy = y - m_y;
            return dx * dx + dy * dy;
        }

        public void UpdatePosition(float x, float y)
        {
            m_x = x;
            m_y = y;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("TuioGL# ");
            sb.Append(m_fId);
            sb.Append("(");
            sb.Append(m_sessionId);
            sb.Append(")");
            return sb.ToString();
        }


        
    } 

    //public class RectGestureListener : IGestureListener
    //{
    //    private static int counter = 0;
    //    private readonly int id;
    //    private readonly float x, y, w, h;

    //    public RectGestureListener(float x, float y, float w, float h)
    //    {
    //        this.x = x;
    //        this.y = y;
    //        this.w = w;
    //        this.h = h;
    //        this.id = counter++;
    //    }

    //    public bool Contains(float x, float y)
    //    {
    //        return x >= this.x && x < this.x + this.w && y >= this.y && y < this.y + this.h;
    //    }

    //    public override string ToString()
    //    {
    //        return "RGL n." + id;
    //    }
    //} 
}