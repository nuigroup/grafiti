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
        private IGestureListener m_dragStartingListener;

        public float X { get { return m_x; } }
        public float Y { get { return m_y; } }
        public int NFingers { get { return m_nFingers; } }
        public IGestureListener DragStartingListener { get { return m_dragStartingListener; } }

        public BasicMultiFingerEventArgs() 
            : base() { }

        public BasicMultiFingerEventArgs(string eventId, int groupId, float x, float y, int nFingers, IGestureListener initialListener)
            : base(eventId, groupId)
        {
            m_x = x;
            m_y = y;
            m_nFingers = nFingers;
            m_dragStartingListener = initialListener;
        }
    }

    public delegate void BasicMultiFingerEventHandler(object obj, BasicMultiFingerEventArgs args);


    public class BasicMultiFingerGRConfigurator : GRConfigurator
    {
        public static readonly BasicMultiFingerGRConfigurator DEFAULT_CONFIGURATOR = new BasicMultiFingerGRConfigurator();

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
        public const long DEFAULT_HOVER_TIME = 2000;

        // flag enabling/disabling triple tap
        public readonly bool IS_TRIPLE_TAP_ENABLED;
        public const bool DEFAULT_IS_TRIPLE_TAP_ENABLED = false;

        public BasicMultiFingerGRConfigurator()
            : this(false) { } // default is not exclusive

        public BasicMultiFingerGRConfigurator(bool exclusive)
            : this(DEFAULT_TAP_TIME, DEFAULT_HOVER_SIZE, DEFAULT_HOVER_TIME, DEFAULT_IS_TRIPLE_TAP_ENABLED, exclusive) { }

        public BasicMultiFingerGRConfigurator(long tapTime, float hoverSize, long hoverTime, bool exclusive, bool isTripleTapEnabled)
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
        private CursorPoint m_currentProcessingCursor;
        private bool m_tapSizeOk;
        private bool m_newClosestCurrentTarget;
        private Dictionary<Trace, int> m_traceDownTimesDict;
        private Dictionary<Trace, CursorPoint> m_traceLastDownCurDict;
        private List<Trace> m_removingTraces = new List<Trace>();
        private long m_t0;
        private bool m_haveSingleTapped, m_haveDoubleTapped, m_needResetTap;
        private int m_numberOfAliveFingers;
        private int m_maxNumberOfAliveFingers;
        private int m_nFingersHovering;

        private Thread m_hoverThread;
        private bool m_hoverEnabled = false;
        private bool m_hoverStarted = false;
        private float m_hoverXRef, m_hoverYRef;
        private DateTime m_hoverTimeRef;

        private IGestureListener m_dragStartingListener;

        public BasicMultiFingerGR(GRConfigurator configurator)
            : base(configurator)
        {
            if (!(configurator is BasicMultiFingerGRConfigurator))
                Configurator = BasicMultiFingerGRConfigurator.DEFAULT_CONFIGURATOR;

            BasicMultiFingerGRConfigurator conf = (BasicMultiFingerGRConfigurator)Configurator;
            TAP_TIME = conf.TAP_TIME;
            HOVER_SIZE = conf.HOVER_SIZE;
            HOVER_TIME = conf.HOVER_TIME;
            IS_TRIPLE_TAP_ENABLED = conf.IS_TRIPLE_TAP_ENABLED;

            ClosestCurrentEvents = new string[] { "Down", "Up", "Tap", "DoubleTap", "TripleTap", "Hover", "EndHover", "Move" };
            ClosestEnteringEvents = new string[] { "Enter" };
            ClosestLeavingEvents = new string[] { "Leave" };

            m_traceDownTimesDict = new Dictionary<Trace, int>();
            m_traceLastDownCurDict = new Dictionary<Trace, CursorPoint>();

            m_hoverThread = new Thread(new ThreadStart(HoverLoop));
            m_hoverThread.Start();
            m_tapSizeOk = true;
            m_newClosestCurrentTarget = false;
            m_numberOfAliveFingers = 0;
            m_maxNumberOfAliveFingers = 0;
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


        protected void OnDown()      { AppendEvent(Down,     new BasicMultiFingerEventArgs("Down",      Group.Id, m_currentProcessingCursor.X, m_currentProcessingCursor.Y, m_numberOfAliveFingers, m_dragStartingListener)); }
        protected void OnUp()        { AppendEvent(Up,       new BasicMultiFingerEventArgs("Up",        Group.Id, m_currentProcessingCursor.X, m_currentProcessingCursor.Y, m_numberOfAliveFingers, m_dragStartingListener)); }
        protected void OnMove()      { AppendEvent(Move,     new BasicMultiFingerEventArgs("Move",      Group.Id, m_currentProcessingCursor.X, m_currentProcessingCursor.Y, Group.NumberOfPresentTraces, m_dragStartingListener)); }
        protected void OnEnter()     { AppendEvent(Enter,    new BasicMultiFingerEventArgs("Enter",     Group.Id, Group.CentroidX, Group.CentroidY, Group.NumberOfPresentTraces, m_dragStartingListener)); }
        protected void OnLeave()     { AppendEvent(Leave,    new BasicMultiFingerEventArgs("Leave",     Group.Id, Group.CentroidX, Group.CentroidY, Group.NumberOfPresentTraces, m_dragStartingListener)); }
        protected void OnTap()       { AppendEvent(Tap,      new BasicMultiFingerEventArgs("Tap",       Group.Id, Group.CentroidX, Group.CentroidY, m_maxNumberOfAliveFingers, m_dragStartingListener)); }
        protected void OnDoubleTap() { AppendEvent(DoubleTap,new BasicMultiFingerEventArgs("DoubleTap", Group.Id, Group.CentroidX, Group.CentroidY, m_maxNumberOfAliveFingers, m_dragStartingListener)); }
        protected void OnTripleTap() { AppendEvent(TripleTap,new BasicMultiFingerEventArgs("TripleTap", Group.Id, Group.CentroidX, Group.CentroidY, m_maxNumberOfAliveFingers, m_dragStartingListener)); }
        protected void OnHover()     { AppendEvent(Hover,    new BasicMultiFingerEventArgs("Hover",     Group.Id, Group.CentroidX, Group.CentroidY, m_nFingersHovering, m_dragStartingListener)); }
        protected void OnEndHover()
        {
            if (m_hoverStarted)
            {
                AppendEvent(EndHover, new BasicMultiFingerEventArgs("EndHover", Group.Id, Group.CentroidX, Group.CentroidY, m_nFingersHovering, m_dragStartingListener));
                m_hoverStarted = false;
            }
        }

        public override void Process(List<Trace> traces)
        {
            foreach (Trace trace in traces)
            {
                if (trace.State == Trace.States.ADDED || trace.State == Trace.States.RESET)
                {
                    m_dragStartingListener = Group.ClosestCurrentTarget;
                }
            }

            OnLeave();
            OnEnter();

            foreach (Trace trace in traces)
                if (trace.State == Trace.States.UPDATED)
                {
                    m_currentProcessingCursor = trace.Last;
                    OnMove();
                    break;
                }

            bool hoverReset = false;
            if (m_newClosestCurrentTarget)
            {
                m_newClosestCurrentTarget = false;
                ResetTap();
                OnEndHover();
                ResetHover();
                hoverReset = true;
                if (Group.ClosestCurrentTarget == null)
                    m_hoverEnabled = false;
            }

            foreach (Trace trace in traces)
            {
                m_currentProcessingCursor = trace.Last;

                /*** ADD ***/
                if (trace.State == Trace.States.ADDED)
                {
                    m_traceDownTimesDict[trace] = 1;
                    m_traceLastDownCurDict[trace] = trace.Last;
                    m_numberOfAliveFingers++;
                    m_maxNumberOfAliveFingers = Math.Max(m_maxNumberOfAliveFingers, m_numberOfAliveFingers);

                    OnDown();
                    if (!hoverReset)
                    {
                        OnEndHover();
                        ResetHover();
                        hoverReset = true;
                    }
                }

                else
                {
                    /*** SET ***/
                    if (trace.State == Trace.States.UPDATED)
                    {
                        if (!hoverReset && Group.ClosestCurrentTarget != null && !CheckHoverSize())
                        {
                            OnEndHover();
                            ResetHover();
                            hoverReset = true;
                        }
                    }

                    /*** RESET ***/
                    else if (trace.State == Trace.States.RESET)
                    {
                        m_traceDownTimesDict[trace]++;
                        m_traceLastDownCurDict[trace] = trace.Last;
                        m_numberOfAliveFingers++;

                        OnDown();
                        if (m_needResetTap)
                        {
                            foreach (Trace t in Group.Traces)
                                if (t != trace)
                                    m_traceDownTimesDict[t] = 0;

                            ResetTap();
                        }
                        if (!hoverReset)
                        {
                            OnEndHover();
                            ResetHover();
                            hoverReset = true;
                        }
                    }

                    /*** DEL ***/
                    else
                    {
                        m_removingTraces.Add(trace);
                    }
                }
            }

            foreach (Trace trace in m_removingTraces)
            {
                if (trace.State == Trace.States.REMOVED)
                {
                    m_numberOfAliveFingers--;
                    m_hoverEnabled = false;
                    OnUp();

                    if (!hoverReset)
                        OnEndHover();

                    if (!CheckTapSize(trace))
                        m_tapSizeOk = false;

                    if (m_numberOfAliveFingers == 0)
                    {
                        if (CheckDownTimes(m_traceDownTimesDict[trace]))
                        {
                            if (m_tapSizeOk)
                            {
                                if (!m_haveSingleTapped)
                                {
                                    OnTap();
                                    m_haveSingleTapped = true;
                                }
                                else
                                    if (CheckTapTime())
                                        if (!m_haveDoubleTapped)
                                        {
                                            OnDoubleTap();
                                            m_haveDoubleTapped = true;
                                            if (!IS_TRIPLE_TAP_ENABLED)
                                                m_needResetTap = true;
                                        }
                                        else
                                        {
                                            OnTripleTap();
                                            m_needResetTap = true;
                                        }
                                    else
                                    {
                                        OnTap();
                                        m_needResetTap = true;
                                    }
                            }
                            else
                            {
                                m_needResetTap = true;
                            }
                        }
                        else
                            foreach (Trace t in Group.Traces)
                                m_traceDownTimesDict[t] = 0;
                    }
                    else
                        if (!hoverReset)
                        {
                            ResetHover();
                            hoverReset = true;
                        }
                }
            }

            m_removingTraces.Clear();

            if (Recognizing)
                GestureHasBeenRecognized();
        }

        #region TAP functions
        private void ResetTap()
        {
            m_t0 = Group.CurrentTimeStamp;
            m_haveSingleTapped = false;
            m_haveDoubleTapped = false;
            m_needResetTap = false;
        }
        private bool CheckTapSize(Trace trace)
        {
            #region Optional (configurable?)
            // This works good for buttons, but not for large controls where it's not desired
            // to have a tap without any spatial threshold. It's probably better to deal taps 
            // at the client level by receiving only Down and Up events and by checking 
            // the DragStartingListener property.
            //if (Group.OnGUIControl)
            //    return m_dragStartingListener == Group.ClosestCurrentTarget;
            //else
            #endregion

            return (trace.Last.SquareDistance(m_traceLastDownCurDict[trace]) <=
                Settings.TRACE_SPACE_GAP * Settings.TRACE_SPACE_GAP);
        }
        private bool CheckTapTime()
        {
            return m_currentProcessingCursor.TimeStamp - m_t0 <= TAP_TIME;
        }
        // Check whether all traces have been reset the same number of times
        private bool CheckDownTimes(int n)
        {
            return Group.Traces.TrueForAll(delegate(Trace trace)
            {
                return m_traceDownTimesDict[trace] == n;
            });
        }
        #endregion

        #region HOVER functions
        private void ResetHover()
        {
            m_hoverXRef = Group.CentroidX;
            m_hoverYRef = Group.CentroidY;
            m_hoverTimeRef = DateTime.Now;
            m_hoverEnabled = true;
        }
        private bool CheckHoverSize()
        {
            return Math.Abs(Group.CentroidX - m_hoverXRef) <= HOVER_SIZE &&
                   Math.Abs(Group.CentroidY - m_hoverYRef) <= HOVER_SIZE;
        }
        private void HoverLoop()
        {
            while (true)
            {
                if (m_hoverEnabled && DateTime.Now.Subtract(m_hoverTimeRef).TotalMilliseconds >= HOVER_TIME)
                {
                    m_nFingersHovering = m_numberOfAliveFingers;
                    OnHover();
                    m_hoverStarted = true;
                    ResetHover();
                    m_hoverEnabled = false;
                }
                Thread.Sleep(HOVER_THREAD_SLEEP_TIME);
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
                OnEndHover();

            base.UpdateEventHandlers(initial, final, entering, current, leaving,
                intersect, union, newClosestEnt, newClosestCur, newClosestLvn, newClosestIni, newClosestFin);

            m_newClosestCurrentTarget = newClosestCur;
            if (initial)
                m_dragStartingListener = Group.ClosestInitialTarget;
        }
    }
}