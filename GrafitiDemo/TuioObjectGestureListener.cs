/*
	Grafiti Demo Application

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
using TUIO;
using ClientNamespace;


namespace ClientNamespace
{
    public class ClientGestureListener : IGestureListener
    {
        private const float INTERACTION_RANGE = 0.2f;
        private GestureEventManager m_gEvtMgr;
        private TuioObject m_tuioObject;

        public ClientGestureListener(TUIO.TuioObject tuioObject)
        {
            m_gEvtMgr = GestureEventManager.Instance;
            m_tuioObject = tuioObject;

            // these will force the classes to compile
            new Basic1FingerGR(new GRConfiguration());
            new SimpleGR(new GRConfiguration());
            new MultiTraceGR(new GRConfiguration());

            // LGRs
            m_gEvtMgr.SetPriorityNumber(1);
            m_gEvtMgr.RegisterHandler(typeof(SimpleGR), SimpleGR.Events.SimpleGesture, new GestureEventHandler(SimpleGestureEventHandler));

            // GGRs
            m_gEvtMgr.SetPriorityNumber(0);
            m_gEvtMgr.RegisterHandler(typeof(Basic1FingerGR), Basic1FingerGR.Events.Tap, OnBasicSingleFingerEvent);
            m_gEvtMgr.RegisterHandler(typeof(Basic1FingerGR), Basic1FingerGR.Events.DoubleTap, OnBasicSingleFingerEvent);
            m_gEvtMgr.RegisterHandler(typeof(Basic1FingerGR), Basic1FingerGR.Events.TripleTap, OnBasicSingleFingerEvent);
            m_gEvtMgr.RegisterHandler(typeof(Basic1FingerGR), Basic1FingerGR.Events.Down, OnBasicSingleFingerEvent);
            m_gEvtMgr.RegisterHandler(typeof(Basic1FingerGR), Basic1FingerGR.Events.Up, OnBasicSingleFingerEvent);
            m_gEvtMgr.RegisterHandler(typeof(Basic1FingerGR), Basic1FingerGR.Events.Enter, OnBasicSingleFingerEvent);
            m_gEvtMgr.RegisterHandler(typeof(Basic1FingerGR), Basic1FingerGR.Events.Leave, OnBasicSingleFingerEvent);
            m_gEvtMgr.RegisterHandler(typeof(Basic1FingerGR), Basic1FingerGR.Events.Hover, OnBasicSingleFingerEvent);
            //m_gEvtMgr.RegisterHandler(typeof(Basic1FingerGR), Basic1FingerGR.Events.Move, OnBasicSingleFingerEvent);

            //m_gEvtMgr.SetPriorityNumber(1);
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
            Basic1FingerEventArgs arg = (Basic1FingerEventArgs)args;
            Console.WriteLine("{0} on {1} ({2};{3})", args.EventId, this, arg.X, arg.Y);
        }

        public void OnMultiTraceEvent(object MultiTraceGR, GestureEventArgs args)
        {
            Console.WriteLine("{0} on {1}, number of fingers: {2}", ((MultiTraceEventArgs)args).EventId, this, ((MultiTraceEventArgs)args).NOfFingers);
        }

        public void SimpleGestureEventHandler(object source, GestureEventArgs args)
        {
            Console.WriteLine("SimpleGestureEvent on {0}", this);
        }

        public bool Contains(float x, float y)
        {
            float dx = Math.Abs(x - m_tuioObject.X);
            float dy = Math.Abs(y - m_tuioObject.Y);
            return (float)Math.Sqrt(dx * dx + dy * dy) <= INTERACTION_RANGE;
        }

        public float GetSquareDistance(float x, float y)
        {
            float dx = x - m_tuioObject.X;
            float dy = y - m_tuioObject.Y;
            return dx * dx + dy * dy;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("TuioGL# ");
            sb.Append(m_tuioObject.getFiducialID());
            sb.Append("(");
            sb.Append(m_tuioObject.SessionId);
            sb.Append(")");
            return sb.ToString();
        }


        
    }
}