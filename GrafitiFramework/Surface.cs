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
using System.Xml;
using System.Collections.Generic;
using TUIO;
using Grafiti;

namespace Grafiti
{
    public class Surface : TuioListener
    {
        #region Private members

        // The instance object
        private static Surface m_instance = null;
        // Locking object
        private static readonly object m_lock = new object();

        // List of gesture listeners associated to tuio objects.
        private List<ITuioObjectGestureListener> m_gListeners;

        // Accumulators for adding, updating and removing cursors.
        // These lists will be cleared at the end of every TuioListener.refresh() call,
        // thus their purpose is merely private to this class
        private List<Cursor> m_addingCursors, m_updatingCursors, m_removingCursors;

        // List of recently died traces. A trace dies when a REMOVED cursor is added to their path,
        // (that is when the user lifts the finger from the table). Within a time specified by
        // Settings.TRACE_TIME_GAP and a space specified by Settings.TRACE_SPACE_GAP the trace can resurrect,
        // that is a new cursor can be added to the path of the trace so that this reborns.
        private List<Trace> m_resurrectableTraces;

        // List of alive groups. A group is alive when it contains alive or resurrectable traces.
        private List<Group> m_aliveGroups;

        // Accumulators for added, removed or modified alive groups (this includes also adding and removing).
        // These lists will be cleared at the beginning of every TuioListener.refresh() call, so
        // that they will be accessible by the client through the relative properties.
        private List<Group> m_addedGroups, m_removedGroups, m_touchedGroups;

        // A dictionary to associate tuio cursors' ids with the relative trace
        private Dictionary<int, Trace> m_cursorTraceTable;

        // Debugging variables
        private int m_lastProcessedRefreshTimeStamp;
        private DateTime m_startTime;
        #endregion

        #region Public properties

        // Ratio of the input screen.
        public const float SCREEN_RATIO = 4f / 3f;

        public List<Group> AddingGroups { get { return m_addedGroups; } }
        public List<Group> RemovingGroups { get { return m_removedGroups; } }
        public List<Group> TouchedGroups { get { return m_touchedGroups; } }
        public List<Group> AliveGroups { get { return m_aliveGroups; } } 
        #endregion

        #region Private constructor
        private Surface()
		{
            m_gListeners = new List<ITuioObjectGestureListener>();
            m_addingCursors = new List<Cursor>();
            m_updatingCursors = new List<Cursor>();
            m_removingCursors = new List<Cursor>();
            m_resurrectableTraces = new List<Trace>();
            m_aliveGroups = new List<Group>();
            m_addedGroups = new List<Group>();
            m_removedGroups = new List<Group>();
            m_touchedGroups = new List<Group>();
            m_cursorTraceTable = new Dictionary<int, Trace>();
            m_lastProcessedRefreshTimeStamp = 0;
            m_startTime = DateTime.Now;

            Settings.Initialize();
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
        public void AddListener(ITuioObjectGestureListener listener)
        {
            m_gListeners.Add(listener);
        }
        public void RemoveListener(ITuioObjectGestureListener listener)
        {
            m_gListeners.Remove(listener);
            GestureEventManager.Instance.UnregisterAllHandlersOf(listener);
        }
        #endregion

        #region TuioListener, members of
        void TuioListener.addTuioObject(TuioObject obj) { }
        void TuioListener.updateTuioObject(TuioObject obj) { }
        void TuioListener.removeTuioObject(TuioObject obj) { }
        void TuioListener.addTuioCursor(TuioCursor c)
        {
            m_addingCursors.Add(new Cursor((int)(c.SessionId), c.X * SCREEN_RATIO, c.Y, Cursor.States.ADDED));
        }
        void TuioListener.updateTuioCursor(TuioCursor c)
        {
            m_updatingCursors.Add(new Cursor((int)(c.SessionId), c.X * SCREEN_RATIO, c.Y, Cursor.States.UPDATED));
        }
        void TuioListener.removeTuioCursor(TuioCursor c)
        {
            m_removingCursors.Add(new Cursor((int)(c.SessionId), c.X * SCREEN_RATIO, c.Y, Cursor.States.REMOVED));
        }
        void TuioListener.refresh(long timeStampAsLong)
        {
            m_addedGroups.Clear();
            m_removedGroups.Clear();
            m_touchedGroups.Clear();

            int timeStamp = (int)timeStampAsLong;

            RemoveNonResurrectableTraces(timeStamp);
            RemoveNonResurrectableGroups(timeStamp);

            ProcessCurrentRemovingCursors(timeStamp);
            ProcessCurrentUpdatingCursors(timeStamp);
            ProcessCurrentAddingCursors(timeStamp);

            foreach (Group group in m_touchedGroups)
                group.Process(timeStamp);

            m_addingCursors.Clear();
            m_updatingCursors.Clear();
            m_removingCursors.Clear();
        }
        #endregion

        #region Private methods
        private void RemoveNonResurrectableTraces(int timeStamp)
        {
            int i;
            for (i = 0; i < m_resurrectableTraces.Count && timeStamp - m_resurrectableTraces[i].Last.TimeStamp <= Settings.TRACE_TIME_GAP; i++) ;
            //Console.WriteLine("Removing {0} non resurrectable traces", m_resurrectableTraces.Count - i);
            m_resurrectableTraces.RemoveRange(i, m_resurrectableTraces.Count - i);
        }
        private void RemoveNonResurrectableGroups(int timeStamp)
        {
            m_aliveGroups.RemoveAll(delegate(Group group)
            {
                if (!group.Alive && timeStamp - group.LastTimeStamp > Settings.TRACE_TIME_GAP)
                {
                    m_removedGroups.Add(group);
                    //Console.WriteLine("Removed non resurrectable group");
                    return true;
                }
                else
                    return false;
            });
        }
        private void ProcessCurrentAddingCursors(int timeStamp)
        {
            foreach (Cursor cursor in m_addingCursors)
            {
                // set timestamp
                cursor.TimeStamp = timeStamp;
                
                Group group;
                Trace trace;

                // try finding a resurrecting trace
                // TODO: don't give priority to the first added cursors
                trace = TryResurrectTrace(cursor);

                if (trace != null)
                {
                    // resurrect the trace
                    group = trace.Group;
                    trace.UpdateCursor(cursor, ListTargetsAt(cursor.X, cursor.Y));
                }
                else
                {
                    // create a new trace
                    group = GetMatchingGroup(cursor);
                    trace = new Trace(cursor, group, ListTargetsAt(cursor.X, cursor.Y));
                }

                // index the cursor
                m_cursorTraceTable[cursor.SessionId] = trace;

                // refresh touched group list
                if (!m_touchedGroups.Contains(group))
                    m_touchedGroups.Add(group);
            }
        }
        private void ProcessCurrentUpdatingCursors(int timeStamp)
        {
            foreach (Cursor cursor in m_updatingCursors)
            {
                // set timestamp
                cursor.TimeStamp = timeStamp;
                
                // determine belonging trace
                Trace trace = m_cursorTraceTable[cursor.SessionId];

                // update trace
                trace.UpdateCursor(cursor, ListTargetsAt(cursor.X, cursor.Y));

                // refresh touched group list
                if (!m_touchedGroups.Contains(trace.Group))
                    m_touchedGroups.Add(trace.Group); 
            }
        }
        private void ProcessCurrentRemovingCursors(int timeStamp)
        {
            foreach (Cursor cursor in m_removingCursors)
            {
                // set timestamp
                cursor.TimeStamp = timeStamp;

                // determine trace
                Trace trace = m_cursorTraceTable[cursor.SessionId];

                // update trace
                trace.RemoveCursor(cursor, ListTargetsAt(cursor.X, cursor.Y));

                // add trace to resurrectable traces list
                m_resurrectableTraces.Insert(0, trace);

                // remove index
                m_cursorTraceTable.Remove(cursor.SessionId);

                // refresh touched group list
                if (!m_touchedGroups.Contains(trace.Group))
                    m_touchedGroups.Add(trace.Group);
            }
        }
        private Trace TryResurrectTrace(Cursor cursor)
        {
            Trace resurrectingTrace = null;
            float minDist = Settings.TRACE_SPACE_GAP * Settings.TRACE_SPACE_GAP;
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
        private Group GetMatchingGroup(Cursor cursor)
        {
            Group matchingGroup = null;
            float minDist = Settings.GROUPING_SPACE * Settings.GROUPING_SPACE;
            float tempDist;

            foreach (Group group in m_aliveGroups)
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
            m_addedGroups.Add(group);
            m_aliveGroups.Add(group);
            return group;
        }
        private List<ITuioObjectGestureListener> ListTargetsAt(float x, float y)
        {
            List<ITuioObjectGestureListener> targets = new List<ITuioObjectGestureListener>();
            foreach (ITuioObjectGestureListener listener in m_gListeners)
            {
                if (listener.Contains(x, y))
                    targets.Add(listener);
            }

            // TODO: optimize
            targets.Sort(new Comparison<ITuioObjectGestureListener>(
                delegate(ITuioObjectGestureListener a, ITuioObjectGestureListener b)
                {
                    // BUG: objects updated during statement execution (?)
                    int d = (int)((a.GetSquareDistance(x, y) - b.GetSquareDistance(x, y)) * 1000000000);
                    if (a == b && d != 0)
                        Console.WriteLine("List<ITuioObjectGestureListener> Surface.ListTargetsAt(float, float) {0}", d); // breakpoint
                    return d;
                }
            ));

            return targets;
        }
        #endregion  
    }
}
