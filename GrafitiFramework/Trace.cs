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
using System.Diagnostics;
using TUIO;
using Grafiti;


namespace Grafiti
{
    public class Trace : IComparable
    {
        private static int m_idCounter = 0;
        private readonly int m_id;
        private States m_state;
        private readonly Group m_group; // belonging group
        private List<Cursor> m_path;
        private Cursor m_first, m_last;

        // A trace is alive when its last cursor in the path is in the state ADDED or UPDATED.
        // When the cursor is removed the trace dies, but it can "resurrect" if another ADDED cursor
        // is added to the path.
        private bool m_alive;

        private List<ITuioObjectGestureListener> m_initialTargets, m_finalTargets;
        private List<ITuioObjectGestureListener> m_enteringTargets, m_currentTargets, m_leavingTargets;
        private List<ITuioObjectGestureListener> m_intersectionTargets, m_unionTargets;


        public enum States
        {
            ADDED,   // cursor added, the trace has born
            UPDATED, // cursor updated
            REMOVED, // cursor removed
            RESET,   // cursor added, the trace has resurrected
        }
        public int Id                    { get { return m_id; } }
        public Group Group               { get { return m_group; } }
        public States State              { get { return m_state; } }
        public List<Cursor> Path         { get { return m_path; } }
        public Cursor First              { get { return m_first; } }
        public Cursor Last               { get { return m_last; } }
        public Cursor this[int index]    { get { return m_path[index]; } }
        public int Count                 { get { return m_path.Count; } }
        public bool Alive                { get { return m_alive; } }

        public List<ITuioObjectGestureListener> InitialTargets      { get { return m_initialTargets; } }
        public List<ITuioObjectGestureListener> FinalTargets        { get { return m_finalTargets; } }
        public List<ITuioObjectGestureListener> EnteringTargets     { get { return m_enteringTargets; } }
        public List<ITuioObjectGestureListener> CurrentTargets      { get { return m_currentTargets; } }
        public List<ITuioObjectGestureListener> LeavingTargets      { get { return m_leavingTargets; } }
        public List<ITuioObjectGestureListener> IntersectionTargets { get { return m_intersectionTargets; } }
        public List<ITuioObjectGestureListener> UnionTargets        { get { return m_unionTargets; } }

        public Trace(Cursor cursor, Group group, List<ITuioObjectGestureListener> targets)
        {
            m_id = m_idCounter++;
            m_path = new List<Cursor>();
            m_path.Add(cursor);
            m_first = m_last = cursor;
            m_state = States.ADDED;
            m_alive = true;

            m_initialTargets = new List<ITuioObjectGestureListener>();
            m_finalTargets = new List<ITuioObjectGestureListener>();
            m_enteringTargets = new List<ITuioObjectGestureListener>();
            m_currentTargets = new List<ITuioObjectGestureListener>();
            m_leavingTargets = new List<ITuioObjectGestureListener>();
            m_intersectionTargets = new List<ITuioObjectGestureListener>();
            m_unionTargets = new List<ITuioObjectGestureListener>();

            m_group = group;

            m_group.StartTrace(this);

            UpdateTargets(targets);
        }
        public void UpdateCursor(Cursor cursor, List<ITuioObjectGestureListener> targets)
        {
            UpdateCursorValues(cursor);
            m_path.Add(cursor);
            m_last = cursor;

            if (m_state == States.REMOVED)
            {
                m_state = States.RESET;
                m_alive = true;
            }
            else
                m_state = States.UPDATED;


            m_group.UpdateTrace(this);

            UpdateTargets(targets);
        }
        public void RemoveCursor(Cursor cursor, List<ITuioObjectGestureListener> targets)
        {
            UpdateCursorValues(cursor);
            m_path.Add(cursor);
            m_last = cursor;
            m_alive = false;

            m_state = States.REMOVED;

            m_group.EndTrace(this);

            UpdateTargets(targets);
        }
        private void UpdateCursorValues(Cursor cursor)
        {
            if (m_state != States.REMOVED)
            {
                double dt = (double)(cursor.TimeStamp - m_last.TimeStamp) / 1000;
                double xSpeed = ((double)cursor.X - (double)m_last.X) / dt;
                double ySpeed = ((double)cursor.Y - (double)m_last.Y) / dt;
                double motionSpeed = Math.Sqrt(xSpeed * xSpeed + ySpeed * ySpeed);
                double motionAccel = (motionSpeed - (double)m_last.MotionSpeed) / dt;
                cursor.XSpeed = (float)xSpeed;
                cursor.YSpeed = (float)ySpeed;
                cursor.MotionSpeed = (float)motionSpeed;
                cursor.MotionAcceleration = (float)motionAccel;
            }
            //Console.WriteLine("x_sp={0},\ty_sp={1},\tsp={2},\tac={3}", cursor.XSpeed, cursor.YSpeed, cursor.MotionSpeed, cursor.MotionAcceleration);
        }
        private void UpdateTargets(List<ITuioObjectGestureListener> targets)
        {
            m_enteringTargets.Clear();
            foreach (ITuioObjectGestureListener target in targets)
            {
                if (!m_currentTargets.Contains(target))
                {
                    m_enteringTargets.Add(target); // entering

                    if (!m_unionTargets.Contains(target))
                        m_unionTargets.Add(target); // union
                }
            }

            m_leavingTargets.Clear();
            foreach (ITuioObjectGestureListener target in m_currentTargets)
            {
                if (!targets.Contains(target)) // TODO can be optimized
                {
                    m_leavingTargets.Add(target); // leaving
                    m_intersectionTargets.Remove(target); // intersection
                }
            }

            foreach (ITuioObjectGestureListener leavingTarget in m_leavingTargets)
                m_currentTargets.Remove(leavingTarget); // current -
            m_currentTargets.AddRange(m_enteringTargets); // current +

            
            if (m_state == States.ADDED) // this happens only once, at the beginning
            { 
                m_initialTargets.AddRange(targets);
                m_intersectionTargets.AddRange(targets);
            }
            else if (m_state == States.REMOVED)
            {
                m_finalTargets.AddRange(targets);
            }
            else if (m_state == States.RESET)
            {
                if (m_finalTargets.Count > 0)
                    m_finalTargets.Clear();
            }

            //Console.Write("Trace current targets:");
            //foreach (IGestureListener target in m_currentTargets)
            //    Console.Write(target.ToString() + ", ");
            //Console.WriteLine();
        }


        // Traces are compared by session id
        public int CompareTo(object obj)
        {
            return (int) (m_id - ((Trace)obj).Id);
        }
    } 
}