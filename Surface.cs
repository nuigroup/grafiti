/*
	grafiti library

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
using grafiti;


namespace grafiti
{
	public class Surface
	{
		private const float CLUSTERING_THRESHOLD = 0.2f;

        private const bool DEFAULT_INTERSECTION_MODE = true;

        private readonly bool m_intersectionMode;

        private List<Trace> m_traces;
		private List<Group> m_groups;

        private Dictionary<long, Trace> m_cursorTraceTable;

        private List<IGestureListener> m_listeners;

        private GGRProvider m_ggrProvider;



        public Surface(List<IGestureListener> listeners) : this(listeners, DEFAULT_INTERSECTION_MODE) { }



        public Surface(List<IGestureListener> listeners, bool intersectionMode)
		{

            m_traces = new List<Trace>(100);
			m_groups = new List<Group>(100);

            m_cursorTraceTable = new Dictionary<long, Trace>(100);

            m_listeners = listeners;

            m_intersectionMode = intersectionMode;

            m_ggrProvider = new GGRProvider();
		}
		
		public void AddCursor(TuioCursor cursor)
		{

            float x = cursor.XPos;

            float y = cursor.YPos;

            // find the best matching group

            Group group = FindOrCreateMatchingGroup(x, y);

            

            // create a new trace and index it

            Trace trace = new Trace(cursor, group);

            m_traces.Add(trace);

            m_cursorTraceTable[cursor.SessionId] = trace;

            // set/update trace and group targets

            trace.UpdateTargets(cursor.State, ListTargetsAt(x, y));



            // processing

            group.Process(trace);
		}



        private Group FindOrCreateMatchingGroup(float x, float y)

        {

            Group matchingGroup = null;



            float minDist = CLUSTERING_THRESHOLD * CLUSTERING_THRESHOLD;

            float dist;


            foreach (Group group in m_groups)

            {

                // filter out groups that don't accept the trace

                if (!group.AcceptNewTrace(x, y))

                    continue;



                // find the closest

                dist = group.SquareMinDist(x, y);

                if (dist < minDist)

                {

                    minDist = dist;

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

            Group group = new Group(m_intersectionMode);

            m_groups.Add(group);

            m_ggrProvider.ProvideGGRs(group);

            return group;

        }



        private void RemoveGroup(Group group)

        {

            m_groups.Remove(group);

            m_ggrProvider.Dispose(group);

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

            Enum e,                         // gesture recognizer's event

            IGestureListener listener,      // listener

            GestureEventHandler handler     // listener's handler

            )

        {

            GGRProvider.GGRInfoEntry ggrInfo = m_ggrProvider.RegisterHandler(grType, e, listener, handler);



            // Attach dynamically to existing groups

            List<GGRProvider.GGRInfoEntry> ggrInfoList = new List<GGRProvider.GGRInfoEntry>();

            ggrInfoList.Add(ggrInfo);

            foreach (Group group in m_groups)

            {

                // TODO: condition checking whether a group has already a definitive gr

                m_ggrProvider.UpdateOrProvideGGRs(group, ggrInfoList);

            }

        }

        public void UnregisterAllHandlers(IGestureListener listener)

        {

            m_ggrProvider.UnregisterAllHandlers(listener);

        }



        public void UpdateCursor(TuioCursor cursor)
		{

            // update trace

            Trace trace = m_cursorTraceTable[cursor.SessionId];

            trace.Update(cursor);



            // update trace and group targets

            trace.UpdateTargets(cursor.State, ListTargetsAt(cursor.XPos, cursor.YPos));



            // processing

            trace.Group.Process(trace);
		}



        public void RemoveCursor(TuioCursor cursor)

        {

            // update trace

            Trace trace = m_cursorTraceTable[cursor.SessionId];

            trace.RemoveCursor(cursor);



            // update trace and group targets

            trace.UpdateTargets(cursor.State, ListTargetsAt(cursor.XPos, cursor.YPos));



            // processing

            trace.Group.Process(trace);



            // remove index

            m_cursorTraceTable.Remove(cursor.SessionId);



            // remove group if it has no more traces (assuming that it has been completely processed!)

            // TODO: implement Group.ProcessingTerminated (like 'disposable') and periodically check for

            // groups that have this property set to true (among the dead ones) to remove them.

            // In this way groups with a working thread in some of their GR can stay alive.

            if (!trace.Group.Alive)

                RemoveGroup(trace.Group);
		}



        private List<IGestureListener> ListTargetsAt(float x, float y)

        {

            List<IGestureListener> targets = new List<IGestureListener>();

            foreach (IGestureListener listener in m_listeners)

            {

                if (listener.Contains(x, y))

                    targets.Add(listener);

            }

            // TODO: optimize (or is it done by CLR?)

            targets.Sort(new Comparison<IGestureListener>(

                delegate (IGestureListener a, IGestureListener b)

                {

                    float ax, ay, bx, by;

                    a.GetPosition(out ax, out ay);

                    b.GetPosition(out bx, out by);

                    float dax = ax - x;

                    float day = ay - y;

                    float dbx = bx - x;

                    float dby = by - y;

                    return (int) (((dax * dax + day * day) - (dbx * dbx + dby * dby)) * 10000000);

                }

            ));

            return targets;

        }
	}
}
