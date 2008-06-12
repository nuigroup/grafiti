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
    public class BasicSingeFingerEventArgs : GestureEventArgs
    {
        private float m_x, m_y;
        public float X { get { return m_x; } }
        public float Y { get { return m_y; } }
        public BasicSingeFingerEventArgs(Enum id, float x, float y) : base(id)
        {
            m_x = x;
            m_y = y;
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

        private const float SPACE_THRESHOLD = 0.01f; // used for tap and hover
        private const long MULTI_TAP_TIME_THRESHOLD = 500;
        private const long HOVER_TIME_THRESHOLD = 500;

        private readonly object m_ctorParam;

        private TuioCursor m_lastCur;
        private bool m_terminated;

        private float m_x0, m_y0;
        private long m_t0;
        private bool m_haveSingleTapped, m_haveDoubleTapped, m_resetTap;

        private Thread m_hoverThread;
        private bool m_hoverEnabled;
        private float m_hoverXRef, m_hoverYRef;
        private DateTime m_hoverTimeRef;

        private GestureRecognitionResult m_defaultResult;


        public Basic1FingerGR(object obj) : base(obj)
        {
            m_ctorParam = obj;
            ClosestCurrentEvents = new Enum[] { Events.Down, Events.Up, Events.Tap, 
                Events.DoubleTap, Events.TripleTap, Events.Hover, Events.Move };
            ClosestEnteringEvents = new Enum[] { Events.Enter };
            ClosestLeavingEvents = new Enum[] { Events.Leave };


            Exclusive = false;
            m_hoverThread = new Thread(new ThreadStart(HoverLoop));
            m_defaultResult = new GestureRecognitionResult(false, true, true);
            m_terminated = false;
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


        protected virtual void OnEnter()
        {
            AppendEvent(Enter, new BasicSingeFingerEventArgs(Events.Enter, m_lastCur.XPos, m_lastCur.YPos));
        }
        protected virtual void OnLeave()
        {
            AppendEvent(Leave, new BasicSingeFingerEventArgs(Events.Leave, m_lastCur.XPos, m_lastCur.YPos));
        }
        protected virtual void OnDown()
        {
            AppendEvent(Down, new BasicSingeFingerEventArgs(Events.Down, m_lastCur.XPos, m_lastCur.YPos));
        }
        protected virtual void OnUp()
        {
            AppendEvent(Up, new BasicSingeFingerEventArgs(Events.Up, m_lastCur.XPos, m_lastCur.YPos));
        }
        protected virtual void OnTap()
        {
            AppendEvent(Tap, new BasicSingeFingerEventArgs(Events.Tap, m_lastCur.XPos, m_lastCur.YPos));
        }        
        protected virtual void OnDoubleTap()
        {
            AppendEvent(DoubleTap, new BasicSingeFingerEventArgs(Events.DoubleTap, m_lastCur.XPos, m_lastCur.YPos));
        }
        protected virtual void OnTripleTap()
        {
            AppendEvent(TripleTap, new BasicSingeFingerEventArgs(Events.TripleTap, m_lastCur.XPos, m_lastCur.YPos));
        }
        protected virtual void OnHover()
        {
            AppendEvent(Hover, new BasicSingeFingerEventArgs(Events.Hover, m_lastCur.XPos, m_lastCur.YPos));
        }
        protected virtual void OnMove()
        {
            AppendEvent(Move, new BasicSingeFingerEventArgs(Events.Move, m_lastCur.XPos, m_lastCur.YPos));
        }


        public override GestureRecognitionResult Process(Trace trace)
        {
            if (Group.NOfAliveTraces > 1)
            {
                m_terminated = true;
                m_hoverEnabled = false;
            }

            if(m_terminated)
                return m_defaultResult;

            m_lastCur = trace.Last;

            OnLeave();
            OnEnter();
            
            /*** ADD ***/
            if (trace.State == Trace.States.ADD)
            {
                ResetTap();
                OnDown();
                ResetHover();
                m_hoverThread.Start();
            }

            /*** SET ***/
            else if (trace.State == Trace.States.SET)
            {
                OnMove();
                if (Group.ClosestCurrentTarget != null && !CheckHoverSpace())
                    ResetHover();
            }

            /*** DEL ***/
            else if (trace.State == Trace.States.DEL)
            {
                m_hoverEnabled = false;
                OnUp();

                if (CheckTapSpace())
                    if (!m_haveSingleTapped)
                    {
                        OnTap();
                        m_haveSingleTapped = true;
                    }
                    else
                        if (CheckMultiTapTime())
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
                if (!CheckMultiTapTime() || m_resetTap)
                    ResetTap();
                OnDown();
                ResetHover();
            }

            return m_defaultResult;
        }


        #region TAP functions

        private void ResetTap()
        {
            m_x0 = m_lastCur.XPos;
            m_y0 = m_lastCur.YPos;
            m_t0 = m_lastCur.TimeStamp;
            m_haveSingleTapped = false;
            m_haveDoubleTapped = false;
            m_resetTap = false;
        }
        private bool CheckTapSpace()
        {
            return Math.Abs(m_lastCur.XPos - m_x0) <= SPACE_THRESHOLD && 
                   Math.Abs(m_lastCur.YPos - m_y0) <= SPACE_THRESHOLD;
        }
        private bool CheckMultiTapTime()
        {
            return m_lastCur.TimeStamp - m_t0 <= MULTI_TAP_TIME_THRESHOLD;
        }

        #endregion


        #region HOVER functions

        private void ResetHover()
        {
            m_hoverXRef = m_lastCur.XPos;
            m_hoverYRef = m_lastCur.YPos;
            m_hoverTimeRef = DateTime.Now;
            m_hoverEnabled = true;
        }
        private bool CheckHoverSpace()
        {
            return Math.Abs(m_lastCur.XPos - m_hoverXRef) <= SPACE_THRESHOLD &&
                   Math.Abs(m_lastCur.YPos - m_hoverYRef) <= SPACE_THRESHOLD;
        }
        private void HoverLoop()
        {
            while (true)
            {
                if (m_hoverEnabled && 
                    DateTime.Now.Subtract(m_hoverTimeRef).TotalMilliseconds >= HOVER_TIME_THRESHOLD)
                {
                    OnHover();
                    ResetHover();
                    m_hoverEnabled = false;
                }
                Thread.Sleep(1);
            }
        }

        #endregion


        protected override void OnUpdateHandlers(bool initial, bool final, bool entering, bool current, bool leaving, bool intersect, bool union, bool newClosestEnt, bool newClosestCur, bool newClosestLvn)
        {
            if (newClosestCur)
            {
                ResetTap();
                ResetHover();

                if (Group.ClosestCurrentTarget == null)
                    m_hoverEnabled = false;
            }
        }


        public override GestureRecognizer Copy()
        {
            return new Basic1FingerGR(m_ctorParam);
        }
    } 
}