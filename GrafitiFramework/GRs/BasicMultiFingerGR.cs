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
using System.Collections.Generic;
using System.Threading;
using Grafiti;
using TUIO;

namespace Grafiti.GestureRecognizers
{
    public class BasicMultiFingerEventArgs : GestureEventArgs
    {
        // TODO:
        // private int m_traceIds;
        // private float m_centroidX, m_centroidY;
        private float m_x, m_y;
        private int m_nFingers;
        private IGestureListener m_validInitialZTarget;

        public float X { get { return m_x; } }
        public float Y { get { return m_y; } }
        public int NFingers { get { return m_nFingers; } }
        public IGestureListener ValidInitialZTarget { get { return m_validInitialZTarget; } }

        public BasicMultiFingerEventArgs() 
            : base() { }

        public BasicMultiFingerEventArgs(string eventId, int groupId, float x, float y, int nFingers, IGestureListener validInitialZTarget)
            : base(eventId, groupId)
        {
            m_x = x;
            m_y = y;
            m_nFingers = nFingers;
            m_validInitialZTarget = validInitialZTarget;
        }
    }

    public delegate void BasicMultiFingerEventHandler(object obj, BasicMultiFingerEventArgs args);


    public class BasicMultiFingerGRConfiguration : GRConfiguration
    {
        // the spatial tollerance for TAP is implicitly Settings.TRACE_TIME_GAP

        // time range for double/triple tap
        public readonly long TAP_TIME;
        public const long DEFAULT_TAP_TIME = 1000;

        // spatial tollerance for hover (radius defining a circle area within which 
        // the finger has to stay in order to raise a hover)
        public readonly float HOVER_SIZE;
        public const float DEFAULT_HOVER_SIZE = 0.02f;

        // the pause required to raise a hover
        public readonly long HOVER_TIME;
        public const long DEFAULT_HOVER_TIME = 1000;

        // flag enabling/disabling triple tap
        public readonly bool IS_TRIPLE_TAP_ENABLED;
        public const bool DEFAULT_IS_TRIPLE_TAP_ENABLED = false;

        public BasicMultiFingerGRConfiguration()
            : this(false) { } // default is not exclusive

        public BasicMultiFingerGRConfiguration(bool exclusive)
            : this(DEFAULT_TAP_TIME, DEFAULT_HOVER_SIZE, DEFAULT_HOVER_TIME, DEFAULT_IS_TRIPLE_TAP_ENABLED, exclusive) { }

        public BasicMultiFingerGRConfiguration(long tapTime, float hoverSize, long hoverTime, bool exclusive, bool isTripleTapEnabled)
            : base(exclusive)
        {
            TAP_TIME = tapTime;
            HOVER_SIZE = hoverSize;
            HOVER_TIME = hoverTime;
            IS_TRIPLE_TAP_ENABLED = isTripleTapEnabled;
        }
    }


    public class BasicMultiFingerGR : GlobalGestureRecognizer
    {
        // Configuration parameters
        private readonly long TAP_TIME;
        private readonly float HOVER_SIZE;
        private readonly long HOVER_TIME;
        private readonly bool IS_TRIPLE_TAP_ENABLED;

        private const int HOVER_THREAD_SLEEP_TIME = 5;
        private bool m_tapSpatialConstraintsOk;
        private Dictionary<Trace, CursorPoint> m_traceLastDownCurDict = new Dictionary<Trace, CursorPoint>();
        private long m_tapInitialTime;
        private bool m_haveSingleTapped, m_haveDoubleTapped, m_tapHasBeenReset;
        private int m_numberOfCurrentFingers;
        private int m_tapNumberOfFingers;

        private Thread m_hoverThread;
        private bool m_hoverEnabled = false;
        private bool m_hovering = false;
        private bool m_hoverHasBeenReset = false; // true if hover has been reset during the current refresh cycle
        private int m_hoveringNFingers = 0;
        private Dictionary<Trace, CursorPoint> m_traceCur4HoverDict = new Dictionary<Trace, CursorPoint>();
        private DateTime m_hoverTimeRef;

        private IGestureListener m_validInitialZTarget = null;
        private bool m_allUp = true;

        public BasicMultiFingerGR()
            : base(null) { }

        public BasicMultiFingerGR(GRConfiguration configuration)
            : base(configuration)
        {
            if (!(configuration is BasicMultiFingerGRConfiguration))
                Configuration = new BasicMultiFingerGRConfiguration();

            BasicMultiFingerGRConfiguration conf = (BasicMultiFingerGRConfiguration)Configuration;
            TAP_TIME = conf.TAP_TIME;
            HOVER_SIZE = conf.HOVER_SIZE;
            HOVER_TIME = conf.HOVER_TIME;
            IS_TRIPLE_TAP_ENABLED = conf.IS_TRIPLE_TAP_ENABLED;

            ClosestCurrentEvents = new string[] { "Down", "Up", "Tap", "DoubleTap", "TripleTap", "Hover", "EndHover", "Move" }; // add, remove?
            ClosestEnteringEvents = new string[] { "Enter" };
            ClosestLeavingEvents = new string[] { "Leave" };
            UnionEvents = new string[] { "Removed", "Terminated" };

            m_hoverThread = new Thread(new ThreadStart(HoverLoop));
            m_hoverThread.Start();
            m_tapSpatialConstraintsOk = true;
            m_numberOfCurrentFingers = 0;
            m_tapNumberOfFingers = 0;
        }

        public event GestureEventHandler Down;
        public event GestureEventHandler Up;
        public event GestureEventHandler Move;
        public event GestureEventHandler Enter;
        public event GestureEventHandler Leave;
        public event GestureEventHandler Tap;
        public event GestureEventHandler DoubleTap;
        public event GestureEventHandler TripleTap;
        public event GestureEventHandler Hover;
        public event GestureEventHandler EndHover;
        public event GestureEventHandler Removed;
        public event GestureEventHandler Terminated;

        // cursor events
        protected void OnDown(CursorPoint c) { AppendEvent(Down,      new BasicMultiFingerEventArgs("Down",      Group.Id, c.X,                   c.Y,                   m_numberOfCurrentFingers, m_validInitialZTarget)); }
        protected void OnUp(CursorPoint c)   { AppendEvent(Up,        new BasicMultiFingerEventArgs("Up",        Group.Id, c.X,                   c.Y,                   m_numberOfCurrentFingers, m_validInitialZTarget)); }
        protected void OnMove(CursorPoint c) { AppendEvent(Move,      new BasicMultiFingerEventArgs("Move",      Group.Id, c.X,                   c.Y,                   m_numberOfCurrentFingers, m_validInitialZTarget)); }
        // group events
        protected void OnEnter()             { AppendEvent(Enter,     new BasicMultiFingerEventArgs("Enter",     Group.Id, Group.ActiveCentroidX, Group.ActiveCentroidY, m_numberOfCurrentFingers, m_validInitialZTarget)); }
        protected void OnLeave()             { AppendEvent(Leave,     new BasicMultiFingerEventArgs("Leave",     Group.Id, Group.ActiveCentroidX, Group.ActiveCentroidY, m_numberOfCurrentFingers, m_validInitialZTarget)); }
        protected void OnTap()               { AppendEvent(Tap,       new BasicMultiFingerEventArgs("Tap",       Group.Id, Group.ActiveCentroidX, Group.ActiveCentroidY, m_tapNumberOfFingers,     m_validInitialZTarget)); }
        protected void OnDoubleTap()         { AppendEvent(DoubleTap, new BasicMultiFingerEventArgs("DoubleTap", Group.Id, Group.ActiveCentroidX, Group.ActiveCentroidY, m_tapNumberOfFingers,     m_validInitialZTarget)); }
        protected void OnTripleTap()         { AppendEvent(TripleTap, new BasicMultiFingerEventArgs("TripleTap", Group.Id, Group.ActiveCentroidX, Group.ActiveCentroidY, m_tapNumberOfFingers,     m_validInitialZTarget)); }
        protected void OnHover()             { AppendEvent(Hover,     new BasicMultiFingerEventArgs("Hover",     Group.Id, Group.ActiveCentroidX, Group.ActiveCentroidY, m_hoveringNFingers,       m_validInitialZTarget)); }
        protected void OnEndHover1()         { AppendEvent(EndHover,  new BasicMultiFingerEventArgs("EndHover",  Group.Id, Group.ActiveCentroidX, Group.ActiveCentroidY, m_hoveringNFingers,       m_validInitialZTarget)); }
        protected void OnRemove()            { AppendEvent(Removed,   new BasicMultiFingerEventArgs("Removed",   Group.Id, Group.ActiveCentroidX, Group.ActiveCentroidY, 0,                        m_validInitialZTarget)); }
        protected void OnTerminate()         { AppendEvent(Terminated,new BasicMultiFingerEventArgs("Terminated",Group.Id, Group.ActiveCentroidX, Group.ActiveCentroidY, m_numberOfCurrentFingers, m_validInitialZTarget)); }

        private void OnEndHover()
        {
            if (m_hovering)
            {
                m_hovering = false;
                OnEndHover1();
            }
        }

        public override void Process(List<Trace> traces)
        {
            // If the group is added or reset, then reset m_initialZTarget
            if (m_allUp && traces.Exists(delegate(Trace trace)
            {
                return trace.State == Trace.States.ADDED || trace.State == Trace.States.RESET;
            }))
            {
                m_allUp = false;
                if (Group.OnZControl)
                {
                    // If the Z-control has changed then reset the down times
                    if (m_validInitialZTarget != Group.ClosestCurrentTarget)
                    {
                        //ResetDownTimes();
                        m_validInitialZTarget = Group.ClosestCurrentTarget;
                    }
                }
                else
                    m_validInitialZTarget = null;
            }

            // Check tap's spatial constraints for every updated, removed and reset trace
            if (m_tapSpatialConstraintsOk)
            {
                foreach (Trace trace in traces)
                {
                    if (trace.State != Trace.States.ADDED && trace.State != Trace.States.TERMINATED)
                    {
                        #region on Z-target
                        // This works good for buttons, but not for large controls where it's not desired
                        // to have a tap without any spatial threshold. It's probably better to deal taps 
                        // at the client level by receiving only Down and Up events and by checking 
                        // the ValidInitialZTarget property.
                        if (m_validInitialZTarget != null)
                        {
                            if (!(Group.OnZControl && m_validInitialZTarget == Group.ClosestCurrentTarget))
                            {
                                m_tapSpatialConstraintsOk = false;
                                m_validInitialZTarget = null;
                                break;
                            }
                        }
                        #endregion

                        #region outside of the Z-target
                        else
                        {
                            if (trace.Last.SquareDistance(m_traceLastDownCurDict[trace]) >
                                Settings.TRACE_SPACE_GAP * Settings.TRACE_SPACE_GAP)
                            {
                                m_tapSpatialConstraintsOk = false;
                                break;
                            }
                        }
                        #endregion
                    }
                }
            }

            // enter
            OnEnter();

            // track cursors and send cursor events
            foreach (Trace trace in traces)
            {
                /*** ADDED ***/
                if (trace.State == Trace.States.ADDED)
                {
                    m_traceLastDownCurDict[trace] = trace.Last;
                    m_numberOfCurrentFingers++;
                    m_tapNumberOfFingers++;// = Math.Max(m_tapNumberOfFingers, m_numberOfCurrentFingers);
                    OnDown(trace.Last);
                }

                /*** UPDATED ***/
                else if (trace.State == Trace.States.UPDATED)
                {
                    OnMove(trace.Last);
                }

                /*** RESET ***/
                else if (trace.State == Trace.States.RESET)
                {
                    m_traceLastDownCurDict[trace] = trace.Last;
                    m_numberOfCurrentFingers++;
                    m_tapNumberOfFingers++;// = Math.Max(m_tapNumberOfFingers, m_numberOfCurrentFingers);
                    OnDown(trace.Last);
                }

                /*** REMOVED ***/
                else if (trace.State == Trace.States.REMOVED)
                {
                    m_numberOfCurrentFingers--;
                    OnUp(trace.Last);
                }
            }

            // tap stuff
            if (m_numberOfCurrentFingers == 0)
            {
                if (m_tapSpatialConstraintsOk)
                {
                    if (!m_haveSingleTapped)
                    {
                        OnTap();
                        m_haveSingleTapped = true;
                    }
                    else
                    {
                        // TAP's temporal constraint
                        if (Group.CurrentTimeStamp - m_tapInitialTime <= TAP_TIME)
                        {
                            if (!m_haveDoubleTapped)
                            {
                                OnDoubleTap();
                                m_haveDoubleTapped = true;
                                if (!IS_TRIPLE_TAP_ENABLED)
                                    ResetTap();
                            }
                            else
                            {
                                OnTripleTap();
                                ResetTap();
                            }
                        }
                        else
                        {
                            OnTap();
                            ResetTap();
                        }
                    }
                }
                else
                {
                    ResetTap();
                }
                m_tapNumberOfFingers = 0;
                m_allUp = true;
                m_tapSpatialConstraintsOk = true;
            }


            // hover stuff
            if (!m_hoverHasBeenReset)
            {
                if(traces.Exists(delegate(Trace trace)
                {
                   return trace.State == Trace.States.ADDED ||
                        trace.State == Trace.States.REMOVED ||
                        trace.State == Trace.States.RESET;
                }))
                {
                    OnEndHover();
                    ResetHover();
                }
                // (TODO configurable...)
                // Same thing as for tap, where on Z-controls there's no threshold.
                else if (!Group.OnZControl && Group.ClosestCurrentTarget != null)
                {
                    // updated traces must comply with the spatial constraint
                    if (!traces.TrueForAll(delegate(Trace trace)
                    {
                        return trace.State != Trace.States.UPDATED ||
                            CheckHoverSpatialConstraints(trace);
                    }))
                    {
                        OnEndHover();
                        ResetHover();
                    }
                }
            }
            if (m_numberOfCurrentFingers == 0)
            {
                OnRemove();
                m_hoverEnabled = false;
            }

            // set for next refresh cycle
            m_hoverHasBeenReset = false;
            m_tapHasBeenReset = false;

            if (Recognizing)
                ValidateGesture();
        }

        #region TAP functions
        private void ResetTap()
        {
            if (!m_tapHasBeenReset)
            {
                m_tapInitialTime = Group.CurrentTimeStamp;
                m_haveSingleTapped = false;
                m_haveDoubleTapped = false;
                m_tapHasBeenReset = true;
            }
        }
        #endregion

        #region HOVER functions
        private void ResetHover()
        {
            m_traceCur4HoverDict.Clear();
            foreach (Trace trace in Group.Traces)
            {
                if (trace.IsAlive)
                    m_traceCur4HoverDict.Add(trace, trace.Last);
            }
            m_hoverTimeRef = DateTime.Now;
            m_hoverEnabled = true;
            m_hoverHasBeenReset = true;
        }
        private bool CheckHoverSpatialConstraints(Trace trace)
        {
            return m_traceCur4HoverDict[trace].SquareDistance(trace.Last) <= HOVER_SIZE * HOVER_SIZE;
        }
        private void HoverLoop() // TODO synchronize
        {
            while (true)
            {
                if (m_hoverEnabled && DateTime.Now.Subtract(m_hoverTimeRef).TotalMilliseconds >= HOVER_TIME)
                {
                    m_hoveringNFingers = m_numberOfCurrentFingers;
                    OnHover();
                    m_hovering = true;
                    m_hoverEnabled = false;
                }
                try
                {
                    Thread.Sleep(HOVER_THREAD_SLEEP_TIME);
                }
                catch (ThreadInterruptedException e)
                {
                    break;
                }
            }
        }
        #endregion

        protected override void UpdateEventHandlers(
            bool initial, bool final,
            bool entering, bool current, bool leaving,
            bool intersect, bool union,
            bool newClosestEnt, bool newClosestCur, bool newClosestLvn, 
            bool newClosestIni, bool newClosestFin)
        {
            if (newClosestCur)
            {
                OnEndHover();
            }

            base.UpdateEventHandlers(initial, final, entering, current, leaving,
                intersect, union, newClosestEnt, newClosestCur, newClosestLvn, newClosestIni, newClosestFin);

            // if main target has changed, then reset tap and hover
            if (newClosestCur)
            {
                OnLeave();
                ResetTap();
                ResetHover();
                if (Group.ClosestCurrentTarget == null)
                    m_hoverEnabled = false;
            }
        }

        protected override void OnTerminating()
        {
            m_hoverThread.Interrupt();
            OnEndHover();
            OnTerminate();
        }
    }
}