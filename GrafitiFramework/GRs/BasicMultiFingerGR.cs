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

namespace Grafiti
{
    public class BasicMultiFingerEventArgs : GestureEventArgs
    {
        // TODO:
        // private int m_traceIds;
        // private float m_centroidX, m_centroidY;
        private float m_x, m_y;
        private int m_nFingers;

        public float X { get { return m_x; } }
        public float Y { get { return m_y; } }
        public int NFingers { get { return m_nFingers; } }

        public BasicMultiFingerEventArgs(Enum eventId, int groupId, float x, float y, int nFingers)
            : base(eventId, groupId)
        {
            m_x = x;
            m_y = y;
            m_nFingers = nFingers;
        }
    }

    public class BasicMultiFingerGRConfiguration : GRConfiguration
    {
        // the spatial tollerance for TAP is Surface.TRACE_TIME_GAP

        // time range for double/triple tap
        public readonly long TAP_TIME;
        public const long DEFAULT_TAP_TIME = 5000;

        // spatial tollerance for hover (radius defining a circle area within which 
        // the finger has to stay in order to raise a hover)
        public readonly float HOVER_SIZE;
        public const float DEFAULT_HOVER_SIZE = 0.02f;

        // the pause required to raise a hover
        public readonly long HOVER_TIME;
        public const long DEFAULT_HOVER_TIME = 8000;

        // flag enabling/disabling triple tap
        public readonly bool IS_TRIPLE_TAP_ENABLED;
        public const bool DEFAULT_IS_TRIPLE_TAP_ENABLED = false;

        public BasicMultiFingerGRConfiguration()
            : this(false) { }

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
        public enum Events
        {
            Enter,
            Leave,
            Down,
            Up,
            Tap,
            DoubleTap,
            TripleTap,
            Move,
            Hover
        }

        // Configuration parameters
        private readonly long TAP_TIME;
        private readonly float HOVER_SIZE;
        private readonly long HOVER_TIME;
        private readonly bool IS_TRIPLE_TAP_ENABLED;

        private TuioCursor m_currentProcessingCursor;
        private bool m_tapSizeOk;
        private bool m_newClosestCurrentTarget;
        private Dictionary<Trace, int> m_traceDownTimesDict;
        private Dictionary<Trace, TuioPoint> m_traceLastDownCurDict;
        private long m_t0;
        private bool m_haveSingleTapped, m_haveDoubleTapped, m_needResetTap;
        private int m_numberOfAliveFingers;
        private int m_maxNumberOfAliveFingers;

        private Thread m_hoverThread;
        private bool m_hoverEnabled;
        private float m_hoverXRef, m_hoverYRef;
        private DateTime m_hoverTimeRef;

        private GestureRecognitionResult m_defaultResult;

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

            ClosestCurrentEvents = new Enum[] { Events.Down, Events.Up, Events.Tap, 
                Events.DoubleTap, Events.TripleTap, Events.Hover, Events.Move };
            ClosestEnteringEvents = new Enum[] { Events.Enter };
            ClosestLeavingEvents = new Enum[] { Events.Leave };

            m_traceDownTimesDict = new Dictionary<Trace, int>();
            m_traceLastDownCurDict = new Dictionary<Trace, TuioPoint>();

            m_hoverThread = new Thread(new ThreadStart(HoverLoop));
            m_defaultResult = new GestureRecognitionResult(false, true, true);
            m_tapSizeOk = true;
            m_newClosestCurrentTarget = false;
            m_numberOfAliveFingers = 0;
            m_maxNumberOfAliveFingers = 0;
        }

        public event GestureEventHandler Enter;
        public event GestureEventHandler Leave;
        public event GestureEventHandler Down;
        public event GestureEventHandler Up;
        public event GestureEventHandler Tap;
        public event GestureEventHandler DoubleTap;
        public event GestureEventHandler TripleTap;
        public event GestureEventHandler Hover;
        public event GestureEventHandler Move;

        protected void OnEnter()     { AppendEvent(Enter,    new BasicMultiFingerEventArgs(Events.Enter,     Group.Id, Group.CentroidX, Group.CentroidY, Group.NOfAliveTraces)); }
        protected void OnLeave()     { AppendEvent(Leave,    new BasicMultiFingerEventArgs(Events.Leave,     Group.Id, Group.CentroidX, Group.CentroidY, Group.NOfAliveTraces)); }
        protected void OnDown()      { AppendEvent(Down,     new BasicMultiFingerEventArgs(Events.Down,      Group.Id, m_currentProcessingCursor.X, m_currentProcessingCursor.Y, m_numberOfAliveFingers)); }
        protected void OnUp()        { AppendEvent(Up,       new BasicMultiFingerEventArgs(Events.Up,        Group.Id, m_currentProcessingCursor.X, m_currentProcessingCursor.Y, m_numberOfAliveFingers)); }
        protected void OnTap()       { AppendEvent(Tap,      new BasicMultiFingerEventArgs(Events.Tap,       Group.Id, Group.CentroidX, Group.CentroidY, m_maxNumberOfAliveFingers)); }
        protected void OnDoubleTap() { AppendEvent(DoubleTap,new BasicMultiFingerEventArgs(Events.DoubleTap, Group.Id, Group.CentroidX, Group.CentroidY, m_maxNumberOfAliveFingers)); }
        protected void OnTripleTap() { AppendEvent(TripleTap,new BasicMultiFingerEventArgs(Events.TripleTap, Group.Id, Group.CentroidX, Group.CentroidY, m_maxNumberOfAliveFingers)); }
        protected void OnHover()     { AppendEvent(Hover,    new BasicMultiFingerEventArgs(Events.Hover,     Group.Id, Group.CentroidX, Group.CentroidY, m_numberOfAliveFingers)); }
        protected void OnMove()      { AppendEvent(Move,     new BasicMultiFingerEventArgs(Events.Move,      Group.Id, m_currentProcessingCursor.X, m_currentProcessingCursor.Y, Group.NOfAliveTraces)); }

        public override GestureRecognitionResult Process(List<Trace> traces)
        {
            OnLeave();
            OnEnter();

            foreach (Trace trace in traces)
                if (trace.State == Trace.States.UPDATED)
                {
                    OnMove();
                    break;
                }

            foreach (Trace trace in traces)
            {
                m_currentProcessingCursor = trace.Last;

                if (m_newClosestCurrentTarget)
                {
                    m_newClosestCurrentTarget = false;
                    ResetTap();
                    ResetHover();
                    if (Group.ClosestCurrentTarget == null)
                        m_hoverEnabled = false;
                }

                /*** ADD ***/
                if (trace.State == Trace.States.ADDED)
                {
                    m_traceDownTimesDict[trace] = 1;
                    m_traceLastDownCurDict[trace] = trace.Last;
                    m_numberOfAliveFingers++;
                    m_maxNumberOfAliveFingers = Math.Max(m_maxNumberOfAliveFingers, m_numberOfAliveFingers);

                    OnDown();
                    ResetHover();
                    if (m_numberOfAliveFingers == 1)
                    {
                        ResetTap();
                        m_hoverThread.Start();
                    }
                }

                /*** SET ***/
                else if (trace.State == Trace.States.UPDATED)
                {
                    if (Group.ClosestCurrentTarget != null && !CheckHoverSize())
                        ResetHover();
                }

                /*** DEL ***/
                else if (trace.State == Trace.States.REMOVED)
                {
                    m_numberOfAliveFingers--;
                    m_hoverEnabled = false;
                    OnUp();

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
                }

                /*** RESET ***/
                else
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
                    ResetHover();
                }
            }
            return m_defaultResult;
        }

        #region TAP functions
        private void ResetTap()
        {
            m_t0 = m_currentProcessingCursor.TimeStamp;
            m_haveSingleTapped = false;
            m_haveDoubleTapped = false;
            m_needResetTap = false;
        }
        private bool CheckTapSize(Trace trace)
        {
            return (trace.Last.SquareDistance(m_traceLastDownCurDict[trace]) <=
                Surface.TRACE_SPACE_GAP * Surface.TRACE_SPACE_GAP);
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
                if (m_hoverEnabled &&
                    DateTime.Now.Subtract(m_hoverTimeRef).TotalMilliseconds >= HOVER_TIME)
                {
                    OnHover();
                    ResetHover();
                    m_hoverEnabled = false;
                }
                Thread.Sleep(1);
            }
        }
        #endregion

        protected override void OnUpdateHandlers(
            bool initial, bool final,
            bool entering, bool current, bool leaving,
            bool intersect, bool union,
            bool newClosestEnt, bool newClosestCur, bool newClosestLvn)
        {
            m_newClosestCurrentTarget = newClosestCur;
        }
    }
}