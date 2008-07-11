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
        private readonly long m_id;
        private readonly Group m_group; // belonging group
        private States m_state;
        private List<TuioCursor> m_history;
        private TuioCursor m_first, m_last;
        private bool m_alive;

        private List<IGestureListener> m_initialTargets, m_finalTargets;
        private List<IGestureListener> m_enteringTargets, m_currentTargets, m_leavingTargets;
        private List<IGestureListener> m_intersectionTargets, m_unionTargets;


        public enum States
        {
            ADDED,   // cursor added, the trace has born
            UPDATED, // cursor updated
            REMOVED, // cursor removed
            RESET,   // cursor added, the trace has resurrected
        }
        public long Id                   { get { return m_id; } }
        public Group Group               { get { return m_group; } }
        public States State              { get { return m_state; } }
        public List<TuioCursor> History  { get { return m_history; } }
        public TuioCursor First          { get { return m_first; } }
        public TuioCursor Last           { get { return m_last; } }
        public TuioCursor this[int index]{ get { return m_history[index]; } }
        public int Count                 { get { return m_history.Count; } }
        public bool Alive                { get { return m_alive; } }

        public List<IGestureListener> InitialTargets      { get { return m_initialTargets; } }
        public List<IGestureListener> FinalTargets        { get { return m_finalTargets; } }
        public List<IGestureListener> EnteringTargets     { get { return m_enteringTargets; } }
        public List<IGestureListener> CurrentTargets      { get { return m_currentTargets; } }
        public List<IGestureListener> LeavingTargets      { get { return m_leavingTargets; } }
        public List<IGestureListener> IntersectionTargets { get { return m_intersectionTargets; } }
        public List<IGestureListener> UnionTargets        { get { return m_unionTargets; } }

        public Trace(TuioCursor cursor, Group group, List<IGestureListener> targets)
        {
            m_id = m_idCounter++;
            m_history = new List<TuioCursor>();
            m_history.Add(cursor);
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

            m_group = group;

            m_group.StartTrace(this);

            UpdateTargets(targets);
        }
        public void UpdateCursor(TuioCursor cursor, List<IGestureListener> targets)
        {
            m_history.Add(cursor);
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
        public void RemoveCursor(TuioCursor cursor, List<IGestureListener> targets)
        {
            m_history.Add(cursor);
            m_last = cursor;
            m_alive = false;

            m_state = States.REMOVED;

            m_group.EndTrace(this);

            UpdateTargets(targets);
        }
        private void UpdateTargets(List<IGestureListener> targets)
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
        }


        // Traces are compared by session id
        public int CompareTo(object obj)
        {
            return (int) (m_id - ((Trace)obj).Id);
        }
    } 
}