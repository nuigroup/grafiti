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
	public class Surface
    {
        internal const bool INTERSECTION_MODE = true;

        // The maximum time in milliseconds between cursors to determine the group's INITIAL and FINAL list
        internal const int GROUPING_TIMESPAN = 600;
        internal const float CLUSTERING_THRESHOLD = 0.2f;

        // Used e.g. for double tap
        internal const int TRACE_RESURRECTION_TIME = 500;
        internal const float TRACE_RESURRECTION_SPACE = 0.05f;


		private List<Group> m_groups;
        private Dictionary<long, Trace> m_cursorTraceTable;
        private List<IGestureListener> m_listeners;

        private GestureEventManager m_gestureEventManager;

        //private GRRegistry m_grRegistry;
        //private object m_defaultGgrParam;
        //private int m_grPriorityNumber;


        public Surface()
		{
			m_groups = new List<Group>();
            m_cursorTraceTable = new Dictionary<long, Trace>();
            m_listeners = new List<IGestureListener>();

            m_gestureEventManager = new GestureEventManager();
            
            //m_grRegistry = new GRRegistry();
            //m_defaultGgrParam = new object();
            //m_grPriorityNumber = 0;


		}

        public void AddListener(IGestureListener listener)
        {
            m_listeners.Add(listener);
        }

        public void RemoveListener(IGestureListener listener)
        {
            m_listeners.Remove(listener);
            m_gestureEventManager.UnregisterAllHandlers(listener);
        }

        public GestureEventManager GetGestureEventManager()
        {
            return m_gestureEventManager;
        }

		
		internal void AddCursor(TuioCursor cursor)
        {
            // get the best matching group and possibly its resurrecting trace
            Trace trace;
            Group group = GetMatchingGroup(cursor, out trace);

            if (trace != null)
                trace.UpdateCursor(cursor); // trace has resurrected
            else
                trace = new Trace(cursor, group);
            
            // index the cursor
            m_cursorTraceTable[cursor.SessionId] = trace;

            // set/update trace and group targets
            trace.UpdateTargets(ListTargetsAt(cursor.XPos, cursor.YPos));

            // processing
            group.Process(trace);
        }

        internal void UpdateCursor(TuioCursor cursor) // TODO: check if already completely processed
        {
            // determine belonging trace
            Trace trace = m_cursorTraceTable[cursor.SessionId];
            
            // update trace, its targets and its group's targets
            trace.UpdateCursor(cursor);
            trace.UpdateTargets(ListTargetsAt(cursor.XPos, cursor.YPos));

            // processing
            trace.Group.Process(trace);
        }

        internal void RemoveCursor(TuioCursor cursor)
        {
            // determine trace
            Trace trace = m_cursorTraceTable[cursor.SessionId];

            // update trace, its targets and its group's targets
            trace.RemoveCursor(cursor);
            trace.UpdateTargets(ListTargetsAt(cursor.XPos, cursor.YPos));

            // processing
            trace.Group.Process(trace);


            // remove index
            m_cursorTraceTable.Remove(cursor.SessionId);

            // TODO
            // remove group if it has no more traces
            //if (!trace.Group.Alive)
            //    RemoveGroup(trace.Group);
        }

        internal void Refresh()
        {
                
        }


        private Group GetMatchingGroup(TuioCursor cursor, out Trace resurrectingTrace)
        {
            Group matchingGroup = null;
            resurrectingTrace = null;
            Trace tempResurrectingTrace = null;

            float minDist = CLUSTERING_THRESHOLD * CLUSTERING_THRESHOLD;
            float tempDist;

            foreach (Group group in m_groups)
            {
                // filter out groups that don't accept the trace
                if (!group.AcceptNewCursor(cursor))
                    continue;

                // find the closest
                tempDist = group.Candidate(cursor, out tempResurrectingTrace);
                if (tempDist < minDist)
                {
                    minDist = tempDist;
                    matchingGroup = group;
                    resurrectingTrace = tempResurrectingTrace;
                }
            }

            // if no group is found, create a new one
            if (matchingGroup == null)
                matchingGroup = CreateGroup();
            
            return matchingGroup;
        }

        private Group CreateGroup()
        {
            Group group = new Group(INTERSECTION_MODE, m_gestureEventManager.GetGRRegistry());
            m_groups.Add(group);
            return group;
        }

        private void RemoveGroup(Group group)
        {
            m_groups.Remove(group);
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
                delegate (IGestureListener a, IGestureListener b)
                {
                    return (int)((a.GetSquareDistance(x, y) - b.GetSquareDistance(x,y)) * 1000000000);
                }
            ));

            return targets;
        }


        /*
        public void SetPriorityNumber(int pn)
        {
            m_grPriorityNumber = pn;
        }


        public void RegisterHandler(
            Type grType,                    // gesture recognizer's type
            Enum e,                         // gesture recognizer's event
            GestureEventHandler handler     // listener's handler
            )
        {
            RegisterHandler(grType, m_defaultGgrParam, e, handler);
        }

        /// <summary>
        /// Clients can use this function to register a handler for a gesture event.
        /// </summary>
        /// <param name="grType">Type of the gesture recognizer.</param>
        /// <param name="e">The event (as specified in the proper enumeration in the GR class).</param>
        /// <param name="listener">The listener object.</param>
        /// <param name="handler">The listener's function that serves as a handler.</param>
        public void RegisterHandler(
            Type grType,                    // gesture recognizer's type
            object grParam,                 // gesture recognizer's ctor's param
            Enum e,                         // gesture recognizer's event
            GestureEventHandler handler     // listener's handler
            )
        {
            m_grRegistry.RegisterHandler(grType, grParam, m_grPriorityNumber, e, handler);
        }

        private void UnregisterAllHandlers(IGestureListener listener)
        {
            m_grRegistry.UnregisterAllHandlers(listener);
        }
        */
    }
}
