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
    public class Basic1FingerEventArgs : GestureEventArgs
    {
        // TODO:
        // private int m_traceIds;
        // private float m_centroidX, m_centroidY;
        private float m_x, m_y;

        public float X { get { return m_x; } }
        public float Y { get { return m_y; } }
        
        public Basic1FingerEventArgs(Enum eventId, int groupId, float x, float y)
            : base(eventId, groupId)
        {
            m_x = x;
            m_y = y;
        }
    }

    public class Basic1FingerGRConfiguration : GRConfiguration
    {
        // spacial tollerance for single/double/triple tap
        public readonly float TAP_SIZE;
        public const float DEFAULT_TAP_SIZE = 0.02f;

        // time range for double/triple tap
        public readonly long TAP_TIME;
        public const long DEFAULT_TAP_TIME = 500;

        // spatial tollerance for hover (radius defining a circle area within which 
        // the finger has to stay in order to raise a hover)
        public readonly float HOVER_SIZE;
        public const float DEFAULT_HOVER_SIZE = 0.02f;

        // the pause required to raise a hover
        public readonly long HOVER_TIME;
        public const long DEFAULT_HOVER_TIME = 800;


        public Basic1FingerGRConfiguration()
            : this(false) { }

        public Basic1FingerGRConfiguration(bool exclusive)
            : this(DEFAULT_TAP_SIZE, DEFAULT_TAP_TIME, DEFAULT_HOVER_SIZE, DEFAULT_HOVER_TIME, exclusive) { }
        
        public Basic1FingerGRConfiguration(float tapSize, long tapTime, float hoverSize, long hoverTime, bool exclusive)
            : base(exclusive)
        {
            TAP_SIZE = tapSize;
            TAP_TIME = tapTime;
            HOVER_SIZE = hoverSize;
            HOVER_TIME = hoverTime;
        }

    }

    public class Basic1FingerGR : GlobalGestureRecognizer
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
        private readonly float TAP_SIZE;
        private readonly long TAP_TIME;
        private readonly float HOVER_SIZE;
        private readonly long HOVER_TIME;

        private TuioCursor m_currentProcessingCursor;
        private bool m_terminated;
        private bool m_newClosestCurrentTarget;

        private float m_x0, m_y0;
        private long m_t0;
        private bool m_haveSingleTapped, m_haveDoubleTapped, m_resetTap;

        private Thread m_hoverThread;
        private bool m_hoverEnabled;
        private float m_hoverXRef, m_hoverYRef;
        private DateTime m_hoverTimeRef;

        private GestureRecognitionResult m_defaultResult;

        public Basic1FingerGR(GRConfiguration configuration) : base(configuration)
        {
            if (!(configuration is Basic1FingerGRConfiguration))
                Configuration = new Basic1FingerGRConfiguration();

            Basic1FingerGRConfiguration conf = (Basic1FingerGRConfiguration)Configuration;
            TAP_SIZE = conf.TAP_SIZE;
            TAP_TIME = conf.TAP_TIME;
            HOVER_SIZE = conf.HOVER_SIZE;
            HOVER_TIME = conf.HOVER_TIME;

            ClosestCurrentEvents = new Enum[] { Events.Down, Events.Up, Events.Tap, 
                Events.DoubleTap, Events.TripleTap, Events.Hover, Events.Move };
            ClosestEnteringEvents = new Enum[] { Events.Enter };
            ClosestLeavingEvents = new Enum[] { Events.Leave };

            m_hoverThread = new Thread(new ThreadStart(HoverLoop));
            m_defaultResult = new GestureRecognitionResult(false, true, true);
            m_terminated = false;
            m_newClosestCurrentTarget = false;
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

        protected void OnEnter()     { AppendEvent(Enter,     new Basic1FingerEventArgs(Events.Enter,     Group.Id, m_currentProcessingCursor.X, m_currentProcessingCursor.Y)); }
        protected void OnLeave()     { AppendEvent(Leave,     new Basic1FingerEventArgs(Events.Leave,     Group.Id, m_currentProcessingCursor.X, m_currentProcessingCursor.Y)); }
        protected void OnDown()      { AppendEvent(Down,      new Basic1FingerEventArgs(Events.Down,      Group.Id, m_currentProcessingCursor.X, m_currentProcessingCursor.Y)); }
        protected void OnUp()        { AppendEvent(Up,        new Basic1FingerEventArgs(Events.Up,        Group.Id, m_currentProcessingCursor.X, m_currentProcessingCursor.Y)); }
        protected void OnTap()       { AppendEvent(Tap,       new Basic1FingerEventArgs(Events.Tap,       Group.Id, m_currentProcessingCursor.X, m_currentProcessingCursor.Y)); }
        protected void OnDoubleTap() { AppendEvent(DoubleTap, new Basic1FingerEventArgs(Events.DoubleTap, Group.Id, m_currentProcessingCursor.X, m_currentProcessingCursor.Y)); }
        protected void OnTripleTap() { AppendEvent(TripleTap, new Basic1FingerEventArgs(Events.TripleTap, Group.Id, m_currentProcessingCursor.X, m_currentProcessingCursor.Y)); }
        protected void OnHover()     { AppendEvent(Hover,     new Basic1FingerEventArgs(Events.Hover,     Group.Id, m_currentProcessingCursor.X, m_currentProcessingCursor.Y)); }
        protected void OnMove()      { AppendEvent(Move,      new Basic1FingerEventArgs(Events.Move,      Group.Id, m_currentProcessingCursor.X, m_currentProcessingCursor.Y)); }

        public override GestureRecognitionResult Process(List<Trace> traces)
        {
            if (Group.NOfAliveTraces > 1)
            {
                m_terminated = true;
                m_hoverEnabled = false;
            }

            if(m_terminated)
                return m_defaultResult;

            Trace trace = traces[0];
            m_currentProcessingCursor = trace.Last;

            if(m_newClosestCurrentTarget)
            {
                m_newClosestCurrentTarget = false;
                ResetTap();
                ResetHover();
                if (Group.ClosestCurrentTarget == null)
                    m_hoverEnabled = false;
            }

            OnLeave();
            OnEnter();
            
            /*** ADD ***/
            if (trace.State == Trace.States.ADDED)
            {
                ResetTap();
                OnDown();
                ResetHover();
                m_hoverThread.Start();
            }

            /*** SET ***/
            else if (trace.State == Trace.States.UPDATED)
            {
                OnMove();
                if (Group.ClosestCurrentTarget != null && !CheckHoverSize())
                    ResetHover();
            }

            /*** DEL ***/
            else if (trace.State == Trace.States.REMOVED)
            {
                m_hoverEnabled = false;
                OnUp();

                if (CheckTapSize())
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
                            }
                            else
                            {
                                OnTripleTap();
                                m_resetTap = true;
                            }
                        else
                            OnTap();
                else
                    m_resetTap = true;
            }

            /*** RST ***/
            else
            {
                if (!CheckTapTime() || m_resetTap)
                    ResetTap();
                OnDown();
                ResetHover();
            }

            return m_defaultResult;
        }


        #region TAP functions

        private void ResetTap()
        {
            m_x0 = m_currentProcessingCursor.X;
            m_y0 = m_currentProcessingCursor.Y;
            m_t0 = m_currentProcessingCursor.TimeStamp;
            m_haveSingleTapped = false;
            m_haveDoubleTapped = false;
            m_resetTap = false;
        }
        private bool CheckTapSize()
        {
            return Math.Abs(m_currentProcessingCursor.X - m_x0) <= TAP_SIZE && 
                   Math.Abs(m_currentProcessingCursor.Y - m_y0) <= TAP_SIZE;
        }
        private bool CheckTapTime()
        {
            return m_currentProcessingCursor.TimeStamp - m_t0 <= TAP_TIME;
        }

        #endregion


        #region HOVER functions

        private void ResetHover()
        {
            m_hoverXRef = m_currentProcessingCursor.X;
            m_hoverYRef = m_currentProcessingCursor.Y;
            m_hoverTimeRef = DateTime.Now;
            m_hoverEnabled = true;
        }
        private bool CheckHoverSize()
        {
            return Math.Abs(m_currentProcessingCursor.X - m_hoverXRef) <= HOVER_SIZE &&
                   Math.Abs(m_currentProcessingCursor.Y - m_hoverYRef) <= HOVER_SIZE;
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