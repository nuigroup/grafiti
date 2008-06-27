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
using TUIO;
using Grafiti;

namespace Grafiti
{
    public class Surface : TuioListener
    {
        #region Configuration parameters
        // TODO: make them settable via xml

        // Group's targeting method
        internal static readonly bool INTERSECTION_MODE = false;

        // The maximum time in milliseconds between cursors to determine the group's INITIAL and FINAL lists
        internal static readonly int GROUPING_SYNCH_TIME = 200;

        // Maximum space between traces to be grouped together
        internal static readonly float GROUPING_SPACE = 0.2f;

        // Maximum time in millisecond for a trace to resurrect (e.g. for double tap)
        internal static readonly int TRACE_RESURRECTION_TIME = 200;

        // Maximum time in millisecond for a trace to resurrect
        internal static readonly float TRACE_RESURRECTION_SPACE = 0.05f;

        // Group's target lists used to determine which LGRs will be called
        internal enum LGRTargetLists
        {
            INITIAL_TARGET_LIST = 0,
            INTERSECTION_TARGET_LIST = 1,
            FINAL_TARGET_LIST = 2
        }
        internal static readonly LGRTargetLists LGR_TARGET_LIST = LGRTargetLists.INTERSECTION_TARGET_LIST;
        #endregion

        #region Private members
        private static Surface m_instance = null;
        private static readonly object m_lock = new object();

        private List<IGestureListener> m_listeners;

        private List<TuioCursor> m_currentAddingCursors, m_currentUpdatingCursors, m_currentRemovingCursors;
        private List<Group> m_currentUpdatingGroups;
        private List<Group> m_joinableGroups;
        private List<Trace> m_resurrectableTraces;
        private Dictionary<long, Trace> m_cursorTraceTable;
        private long m_lastProcessedRefreshTimeStamp; // only for debug
        private DateTime m_startTime; // only for debug
        #endregion

        #region Private constructor
        private Surface()
		{
            m_listeners = new List<IGestureListener>();
            m_currentAddingCursors = new List<TuioCursor>();
            m_currentUpdatingCursors = new List<TuioCursor>();
            m_currentRemovingCursors = new List<TuioCursor>();
            m_resurrectableTraces = new List<Trace>();
            m_joinableGroups = new List<Group>();
            m_currentUpdatingGroups = new List<Group>();
            m_cursorTraceTable = new Dictionary<long, Trace>();
            m_lastProcessedRefreshTimeStamp = 0;
            m_startTime = DateTime.Now;
        }
        #endregion

        #region Singleton
        public static Surface Instance
        {
            get 
            {
                lock (m_lock)
                {
                    if (m_instance == null)
                        m_instance = new Surface();
                    return m_instance;
                }
            }
        }
        #endregion

        #region Client's interface methods
        public void AddListener(IGestureListener listener)
        {
            m_listeners.Add(listener);
        }
        public void RemoveListener(IGestureListener listener)
        {
            m_listeners.Remove(listener);
            GestureEventManager.Instance.UnregisterAllHandlersOf(listener);
        }
        #endregion

        #region TuioListener, members of
        void TuioListener.addTuioObject(TuioObject obj) { }
        void TuioListener.updateTuioObject(TuioObject obj) { }
        void TuioListener.removeTuioObject(TuioObject obj) { }
        void TuioListener.addTuioCursor(TuioCursor cursor)
        {
            // only for debug
            //m_currentAddingCursors.RemoveAll(delegate(TuioCursor cur) { return cur.SessionId == cursor.SessionId; }); 
            
            m_currentAddingCursors.Add(cursor);
        }
        void TuioListener.updateTuioCursor(TuioCursor cursor)
        {
            // only for debug
            //m_currentUpdatingCursors.RemoveAll(delegate(TuioCursor cur) { return cur.SessionId == cursor.SessionId; });
            
            m_currentUpdatingCursors.Add(cursor);
        }
        void TuioListener.removeTuioCursor(TuioCursor cursor)
        {
            // only for debug
            //m_currentRemovingCursors.RemoveAll(delegate(TuioCursor cur) { return cur.SessionId == cursor.SessionId; });
            
            m_currentRemovingCursors.Add(cursor);
        }
        void TuioListener.refresh(long timeStamp)
        {
            // only for debug
            //if (timeStamp - lastProcessedRefreshTimeStamp < 2000)
            //    return;
            //lastProcessedRefreshTimeStamp = timeStamp;
            //Console.WriteLine("Refresh");

            RemoveNonResurrectableTraces(timeStamp);
            RemoveNonResurrectableGroups(timeStamp);

            ProcessCurrentAddingCursors();
            ProcessCurrentUpdatingCursors();
            ProcessCurrentRemovingCursors();

            foreach (Group group in m_currentUpdatingGroups)
                group.Process();

            m_currentAddingCursors.Clear();
            m_currentUpdatingCursors.Clear();
            m_currentRemovingCursors.Clear();
            m_currentUpdatingGroups.Clear();
        }
        #endregion

        #region Private methods
        private void RemoveNonResurrectableTraces(long timeStamp)
        {
            int i;
            for (i = 0; i < m_resurrectableTraces.Count && timeStamp - m_resurrectableTraces[i].Last.TimeStamp <= TRACE_RESURRECTION_TIME; i++) ;
            m_resurrectableTraces.RemoveRange(i, m_resurrectableTraces.Count - i);
        }
        private void RemoveNonResurrectableGroups(long timeStamp)
        {
            m_joinableGroups.RemoveAll(delegate(Group group)
            {
                return (!group.Alive && timeStamp - group.LastTimeStamp > TRACE_RESURRECTION_TIME);
            });
        }
        private void ProcessCurrentAddingCursors()
        {
            foreach (TuioCursor cursor in m_currentAddingCursors)
            {
                Group group;
                Trace trace;

                // try finding a resurrecting trace
                // TODO: don't give priority to the first added cursor
                trace = TryResurrectTrace(cursor);

                if (trace != null) // a trace has resurrected
                {
                    group = trace.Group;
                    trace.UpdateCursor(cursor, ListTargetsAt(cursor.X, cursor.Y));
                }
                else
                {
                    group = GetMatchingGroup(cursor);
                    trace = new Trace(cursor, group, ListTargetsAt(cursor.X, cursor.Y));
                }

                // index the cursor
                m_cursorTraceTable[cursor.SessionId] = trace;

                // refresh updated group list
                if (!m_currentUpdatingGroups.Contains(group))
                    m_currentUpdatingGroups.Add(group);
            }
        }
        private void ProcessCurrentUpdatingCursors()
        {
            foreach (TuioCursor cursor in m_currentUpdatingCursors)
            {
                // determine belonging trace
                Trace trace = m_cursorTraceTable[cursor.SessionId];

                // update trace
                trace.UpdateCursor(cursor, ListTargetsAt(cursor.X, cursor.Y));

                // refresh updated group list
                if (!m_currentUpdatingGroups.Contains(trace.Group))
                    m_currentUpdatingGroups.Add(trace.Group); 
            }
        }
        private void ProcessCurrentRemovingCursors()
        {
            foreach (TuioCursor cursor in m_currentRemovingCursors)
            {
                // determine trace
                Trace trace = m_cursorTraceTable[cursor.SessionId];

                // update trace
                trace.RemoveCursor(cursor, ListTargetsAt(cursor.X, cursor.Y));

                // remove index
                m_cursorTraceTable.Remove(cursor.SessionId);

                // refresh updated group list
                if (!m_currentUpdatingGroups.Contains(trace.Group))
                    m_currentUpdatingGroups.Add(trace.Group);

                m_resurrectableTraces.Insert(0,trace);
            }
        }
        private Trace TryResurrectTrace(TuioCursor cursor)
        {
            Trace resurrectingTrace = null;
            float minDist = TRACE_RESURRECTION_SPACE * TRACE_RESURRECTION_SPACE;
            float dist;
            foreach (Trace trace in m_resurrectableTraces)
            {
                dist = trace.Last.SquareDistance(cursor);
                if (dist < minDist)
                {
                    minDist = dist;
                    resurrectingTrace = trace;
                }
            }
            if (resurrectingTrace != null)
            {
                m_resurrectableTraces.Remove(resurrectingTrace);
            } 
            return resurrectingTrace;
        }
        private Group GetMatchingGroup(TuioCursor cursor)
        {
            Group matchingGroup = null;
            float minDist = GROUPING_SPACE * GROUPING_SPACE;
            float tempDist;

            foreach (Group group in m_joinableGroups)
            {
                // filter out groups that don't accept the trace
                if (!group.AcceptNewCursor(cursor))
                    continue;

                // find the closest
                tempDist = group.SquareMinDist(cursor);
                if (tempDist < minDist)
                {
                    minDist = tempDist;
                    matchingGroup = group;
                }
            }

            // if no group is found, create a new one
            if (matchingGroup == null)
                matchingGroup = CreateGroup();

            return matchingGroup;
        }
        private Group CreateGroup()
        {
            Group group = new Group();
            m_joinableGroups.Add(group);
            return group;
        }
        private List<IGestureListener> ListTargetsAt(float x, float y)
        {
            List<IGestureListener> targets = new List<IGestureListener>();
            foreach (IGestureListener listener in m_listeners)
            {
                if (listener.Contains(x, y))
                    targets.Add(listener);
            }

            // TODO: optimize
            targets.Sort(new Comparison<IGestureListener>(
                delegate(IGestureListener a, IGestureListener b)
                {
                    // BUG: objects updated during statement execution (?)
                    int d = (int)((a.GetSquareDistance(x, y) - b.GetSquareDistance(x, y)) * 1000000000);
                    if (a == b && d != 0)
                        Console.WriteLine(d); // breakpoint
                    return d;
                }
            ));

            return targets;
        }
        #endregion  
    }
}
