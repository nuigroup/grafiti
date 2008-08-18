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
    /// <summary>
    /// Represents a collection of Cursors, i.e. the path of a finger in the surface.
    /// </summary>
    public class Trace : IComparable
    {
        #region Private members
        private static int m_idCounter = 0;
        private readonly int m_id;
        private States m_state;
        private readonly Group m_group; // belonging group
        private List<CursorPoint> m_path;
        private CursorPoint m_first, m_last;

        // A trace is alive when its last cursor in the path is in the state ADDED or UPDATED.
        // When the cursor is removed the trace dies, but it can "resurrect" if another ADDED cursor
        // is added to the path.
        private bool m_alive;

        private List<IGestureListener> m_initialTargets, m_finalTargets;
        private List<IGestureListener> m_enteringTargets, m_currentTargets, m_leavingTargets;
        private List<IGestureListener> m_intersectionTargets, m_unionTargets;

        private bool m_onGUIControl; 
        #endregion

        #region Public properties
        public enum States
        {
            /// <summary>
            /// cursor added, the trace has born
            /// </summary>
            ADDED,
            /// <summary>
            /// cursor updated
            /// </summary>
            UPDATED,
            /// <summary>
            /// cursor removed
            /// </summary>
            REMOVED,
            /// <summary>
            /// cursor added, the trace has resurrected
            /// </summary>
            RESET,
            /// <summary>
            /// when it was removed since at least a time equal to Settings.TRACE_TIME_GAP
            /// </summary>
            TERMINATED
        }
        /// <summary>
        /// Identification number
        /// </summary>
        public int Id { get { return m_id; } }
        /// <summary>
        /// Belonging group
        /// </summary>
        public Group Group { get { return m_group; } }
        /// <summary>
        /// Current state
        /// </summary>
        public States State { get { return m_state; } }
        /// <summary>
        /// List of cursor points of which the trace is composed
        /// </summary>
        public List<CursorPoint> Path { get { return m_path; } }
        /// <summary>
        /// Initial cursor point
        /// </summary>
        public CursorPoint First { get { return m_first; } }
        /// <summary>
        /// Last added cursor point
        /// </summary>
        public CursorPoint Last { get { return m_last; } }
        /// <summary>
        /// The trace is indexed on the cursor points of which it is composed
        /// </summary>
        /// <param name="index">Index of the cursor point in the path list</param>
        /// <returns>The cursor point at the given position in the path</returns>
        public CursorPoint this[int index] { get { return m_path[index]; } }
        /// <summary>
        /// Number of cursor points of which the trace is composed
        /// </summary>
        public int Count { get { return m_path.Count; } }
        /// <summary>
        /// True iff the last added cursor point is on state 'ADDED' or 'UPDATED', iff the finger is currently
        /// on the surface.
        /// </summary>
        public bool Alive { get { return m_alive; } }

        // Target lists
        public List<IGestureListener> InitialTargets { get { return m_initialTargets; } }
        public List<IGestureListener> FinalTargets { get { return m_finalTargets; } }
        public List<IGestureListener> EnteringTargets { get { return m_enteringTargets; } }
        public List<IGestureListener> CurrentTargets { get { return m_currentTargets; } }
        public List<IGestureListener> LeavingTargets { get { return m_leavingTargets; } }
        public List<IGestureListener> IntersectionTargets { get { return m_intersectionTargets; } }
        public List<IGestureListener> UnionTargets { get { return m_unionTargets; } }

        /// <summary>
        /// True iff the last added cursor point is on a GUI component
        /// </summary>
        public bool OnGUIControl
        {
            get { return m_onGUIControl; }
            private set
            {
                if (m_onGUIControl != value)
                    m_onGUIControl = value;
            }
        } 
        #endregion

        #region Internal constructor
		internal Trace(CursorPoint cursor, Group group, List<IGestureListener> targets, bool guiTargets)
        {
            m_id = m_idCounter++;
            m_group = group;
            m_path = new List<CursorPoint>();
            m_path.Add(cursor);
            m_first = m_last = cursor;
            m_state = States.ADDED;
            m_alive = true;

            m_initialTargets = new List<IGestureListener>();
            m_finalTargets = new List<IGestureListener>();
            m_enteringTargets = new List<IGestureListener>();
            m_currentTargets = new List<IGestureListener>();
            m_leavingTargets = new List<IGestureListener>();
            m_intersectionTargets = new List<IGestureListener>();
            m_unionTargets = new List<IGestureListener>();

            m_group.StartTrace(this);
            UpdateTargets(targets, guiTargets);
        }
	    #endregion

        #region Private or internal methods
        internal void AppendAddingOrUpdatingCursor(CursorPoint cursor, List<IGestureListener> targets, bool guiTargets)
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
            UpdateTargets(targets, guiTargets);
        }
        internal void AppendRemovingCursor(CursorPoint cursor, List<IGestureListener> targets, bool guiTargets)
        {
            UpdateCursorValues(cursor);
            m_path.Add(cursor);
            m_last = cursor;
            m_alive = false;

            m_state = States.REMOVED;

            m_group.EndTrace(this);
            UpdateTargets(targets, guiTargets);
        }
        internal void Terminate()
        {
            m_state = States.TERMINATED;
            Group.TerminateTrace(this);
        }
        private void UpdateCursorValues(CursorPoint cursor)
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
        private void UpdateTargets(List<IGestureListener> targets, bool guiTargets)
        {
            m_enteringTargets.Clear();
            foreach (IGestureListener target in targets)
            {
                if (!m_currentTargets.Contains(target))
                {
                    m_enteringTargets.Add(target); // entering

                    if (!m_unionTargets.Contains(target))
                        m_unionTargets.Add(target); // union
                }
            }

            m_leavingTargets.Clear();
            foreach (IGestureListener target in m_currentTargets)
            {
                if (!targets.Contains(target)) // TODO can be optimized
                {
                    m_leavingTargets.Add(target); // leaving
                    m_intersectionTargets.Remove(target); // intersection
                }
            }

            foreach (IGestureListener leavingTarget in m_leavingTargets)
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

            OnGUIControl = guiTargets;
        }
        #endregion

        #region IComparable Members
        int IComparable.CompareTo(object obj)
        {
            return (int)(m_id - ((Trace)obj).Id);
        }
        #endregion
    } 
}