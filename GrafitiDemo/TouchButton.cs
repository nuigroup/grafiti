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
using System.Text;
using Tao.FreeGlut;
using Tao.OpenGl;
using Grafiti;
using Grafiti.GestureRecognizers;

namespace GrafitiDemo
{
    public class TouchButton : IGestureListener//, ISimpleTouchListener
    {
        private float m_x, m_y, m_width, m_height;

        public event BasicMultiFingerEventHandler FingerDown;
        public event BasicMultiFingerEventHandler FingerUp;
        public event BasicMultiFingerEventHandler FingerMove;
        public event BasicMultiFingerEventHandler Tap;
        public event BasicMultiFingerEventHandler Enter;
        public event BasicMultiFingerEventHandler Leave;
        public event BasicMultiFingerEventHandler Hover;
        public event BasicMultiFingerEventHandler EndHover;
        //public event BasicMultiFingerEventHandler Press;
        //public event BasicMultiFingerEventHandler Lift;

        private bool m_isPressed = false;
        private int m_owningGroup = -1; // id of the group that is pressing or tapping
        private bool m_hovering = false;
        private int m_hoveringGroup = -1; // if of the group that is hovering


        public float X { get { return m_x; } set { m_x = value; } }
        public float Y { get { return m_y; } set { m_y = value; } }
        public float Width { get { return m_width; } set { m_width = value; } }
        public float Height { get { return m_height; } set { m_height = value; } }

        public bool IsPressed { get { return m_isPressed; } }
        public bool Hovering { get { return m_hovering; } }

        public TouchButton(float x, float y, float w, float h)
        {
            m_x = x;
            m_y = y;
            m_width = w;
            m_height = h;

            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), "Down", OnGestureEvent);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), "Up", OnGestureEvent);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), "Move", OnGestureEvent);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), "Enter", OnGestureEvent);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), "Leave", OnGestureEvent);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), "Tap", OnGestureEvent);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), "DoubleTap", OnGestureEvent);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), "TripleTap", OnGestureEvent);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), "Hover", OnGestureEvent);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), "EndHover", OnGestureEvent);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), "Removed", OnGestureEvent);
        }

        public TouchButton(float x, float y, float w, float h, BasicMultiFingerGRConfiguration configuration)
        {
            m_x = x;
            m_y = y;
            m_width = w;
            m_height = h;

            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), configuration, "Down", OnGestureEvent);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), configuration, "Up", OnGestureEvent);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), configuration, "Move", OnGestureEvent);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), configuration, "Enter", OnGestureEvent);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), configuration, "Leave", OnGestureEvent);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), configuration, "Tap", OnGestureEvent);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), configuration, "DoubleTap", OnGestureEvent);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), configuration, "TripleTap", OnGestureEvent);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), configuration, "Hover", OnGestureEvent);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), configuration, "EndHover", OnGestureEvent);
        }
        
        private void OnDown(BasicMultiFingerEventArgs cArgs)        { if (FingerDown != null)     FingerDown(this, cArgs); }
        private void OnUp(BasicMultiFingerEventArgs cArgs)          { if (FingerUp != null)       FingerUp(this, cArgs); }
        private void OnMove(BasicMultiFingerEventArgs cArgs)        { if (FingerMove != null)     FingerMove(this, cArgs); }
        private void OnEnter(BasicMultiFingerEventArgs cArgs)       { if (Enter != null)          Enter(this, cArgs); }
        private void OnLeave(BasicMultiFingerEventArgs cArgs)       { if (Leave != null)          Leave(this, cArgs); }
        private void OnTap(BasicMultiFingerEventArgs cArgs)         { if (Tap != null)            Tap(this, cArgs); }
        private void OnHover(BasicMultiFingerEventArgs cArgs)       { if (Hover != null)          Hover(this, cArgs); }
        private void OnEndHover(BasicMultiFingerEventArgs cArgs)    { if (EndHover != null)       EndHover(this, cArgs); }
        //private void OnPress(BasicMultiFingerEventArgs cArgs)       { if (Press != null)          Press(this, cArgs); }
        //private void OnLift(BasicMultiFingerEventArgs cArgs)        { if (Lift != null)           Lift(this, cArgs); }

        private void OnGestureEvent(object obj, GestureEventArgs args)
        {
            BasicMultiFingerEventArgs cArgs = (BasicMultiFingerEventArgs)args;
            //Console.WriteLine("event: " + cArgs.EventId + ", n: " + cArgs.NFingers);

            switch (cArgs.EventId)
            {
                case "Down":
                    if ((m_owningGroup == -1 || m_owningGroup == cArgs.GroupId) && 
                        cArgs.ValidInitialZTarget == this)
                    {
                        if (!m_isPressed)
                        {
                            m_isPressed = true;
                            m_owningGroup = cArgs.GroupId;
                        }
                        OnDown(cArgs);
                    }
                    break;

                case "Up":
                    if (m_isPressed && m_owningGroup == cArgs.GroupId)
                    {
                        if (cArgs.NFingers == 0)
                            m_isPressed = false;
                        OnUp(cArgs);
                    }
                    break;

                case "Tap":
                case "DoubleTap":
                case "TripleTap":
                    if (m_owningGroup == cArgs.GroupId)
                    {
                        OnTap(cArgs);
                    }
                    break;

                case "Enter":
                    if (m_owningGroup == -1 || m_owningGroup == cArgs.GroupId)
                    {
                        m_owningGroup = cArgs.GroupId;
                        OnEnter(cArgs);
                    }
                    break;

                case "Leave":
                    if (m_owningGroup == -1 || m_owningGroup == cArgs.GroupId)
                    {
                        m_isPressed = false;
                        m_owningGroup = -1;
                        OnLeave(cArgs);
                    }
                    break;

                case "Move":
                    if (m_owningGroup == cArgs.GroupId)
                        OnMove(cArgs);
                    break;

                case "Hover":
                    if (m_owningGroup == cArgs.GroupId)
                    {
                        m_hovering = true;
                        m_hoveringGroup = cArgs.GroupId;
                        OnHover(cArgs);
                    }
                    break;

                case "EndHover":
                    if (m_hoveringGroup == cArgs.GroupId)
                    {
                        m_hovering = false;
                        m_hoveringGroup = -1;
                        OnEndHover(cArgs);
                    }
                    break;

                case "Removed":
                    if (m_owningGroup == cArgs.GroupId)
                    {
                        m_owningGroup = -1;
                    }
                    break;
            }
        }

        
        #region ISimpleTouchListener Members

        //public void OnTraceEnter(int traceId, Trace.States state, CursorPoint cursorPoint)
        //{
        //    OnTraceUpdate(traceId, state, cursorPoint);
        //}

        //public void OnTraceLeave(int traceId, Trace.States state, CursorPoint cursorPoint)
        //{
        //    if (m_tracesDown.Remove(traceId) && m_tracesDown.Count == 0)
        //        m_isPressed = false;
        //}

        //public void OnTraceUpdate(int traceId, Trace.States state, CursorPoint cursorPoint)
        //{
        //    if (state == Trace.States.ADDED || state == Trace.States.RESET)
        //    {
        //        m_tracesDown.Add(traceId);
        //        m_isPressed = true;
        //    }
        //    else
        //        if (state == Trace.States.REMOVED)
        //        {
        //            if (m_tracesDown.Remove(traceId) && m_tracesDown.Count == 0)
        //            {
        //                if (Tap != null)
        //                    Tap(this, null);
        //                m_isPressed = false;
        //            }
        //        }
        //}

        #endregion
    }
}
