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
    /// <summary>
    /// Defines the main singleton instance that processes Tuio cursor messages and
    /// retrieves informations from the client about the tuio objects and the
    /// GUI components in the surface.
    /// </summary>
    public class Surface : TuioListener
    {
        #region Private members
        // The instance object
        private static Surface s_instance = null;
        // Locking object
        private static readonly object s_lock = new object();

        // Client's GUI manager used for hit tests.
        private IGrafitiClientGUIManager m_clientGUIManager;

        /// <summary>
        /// Ratio of the camera resolutions (width resolution over height resolution).
        /// </summary>
        private readonly float CAMERA_RESOLUTION_RATIO;

        /// <summary>
        /// Offset for x coordinates
        /// </summary>
        private readonly float OFFSET_X;

        // Accumulators for adding, updating and removing cursors.
        // These lists will be cleared at the end of every TuioListener.refresh() call,
        // thus their purpose is merely private to this class
        private List<CursorPoint> m_addingCursors, m_updatingCursors, m_removingCursors;

        // List of recently died traces. A trace dies when a REMOVED cursor is added to their path,
        // (that is when the user lifts the finger from the table). Within a time specified by
        // Settings.TRACE_TIME_GAP and a space specified by Settings.TRACE_SPACE_GAP the trace can resurrect,
        // that is a new cursor can be added to the path of the trace so that this reborns.
        private List<Trace> m_resurrectableTraces;

        // List of groups which contain some alive or resurrectable traces.
        private List<Group> m_activeGroups;

        // Accumulators for added, removed or modified alive groups (this includes also adding and removing).
        // These lists will be cleared at the beginning of every TuioListener.refresh() call, so
        // that they will be accessible by the client through the relative properties.
        private List<Group> m_addedGroups, m_removedGroups, m_touchedGroups;

        // A dictionary to associate tuio cursors' ids with the relative trace
        private Dictionary<int, Trace> m_cursorTraceTable;

        // Debugging variables
        private int m_lastProcessedRefreshTimeStamp = 0;
        private DateTime m_startTime;
        
        #endregion

        #region Public properties
        // The following can be used to create a visual feedback of Grafiti.
        /// <summary>
        /// Offset for Tuio's x coordinates.
        /// </summary>
        /// <returns>The offset for Tuio's x coordinates.</returns>
        public float OffsetX { get { return OFFSET_X; } }
        /// <summary>
        /// List of groups that have been added during the last refresh() call.
        /// </summary>
        public List<Group> AddedGroups { get { return m_addedGroups; } }
        /// <summary>
        /// List of groups that have been removed during the last refresh() call.
        /// </summary>
        public List<Group> RemovedGroups { get { return m_removedGroups; } }
        /// <summary>
        /// List of groups that have been added, updated or removed during the last refresh() call.
        /// </summary>
        public List<Group> TouchedGroups { get { return m_touchedGroups; } }
        /// <summary>
        /// List of groups that are currently alive or that have been recently removed so that
        /// they still can 'resurrect'.
        /// </summary>
        public List<Group> ActiveGroups { get { return m_activeGroups; } }
        #endregion

        #region Private constructor
        private Surface()
        {
            m_addingCursors = new List<CursorPoint>();
            m_updatingCursors = new List<CursorPoint>();
            m_removingCursors = new List<CursorPoint>();
            m_resurrectableTraces = new List<Trace>();
            m_activeGroups = new List<Group>();
            m_addedGroups = new List<Group>();
            m_removedGroups = new List<Group>();
            m_touchedGroups = new List<Group>();
            m_cursorTraceTable = new Dictionary<int, Trace>();
            m_lastProcessedRefreshTimeStamp = 0;
            m_startTime = DateTime.Now;

            Settings.Initialize();
            CAMERA_RESOLUTION_RATIO = Settings.INPUT_DEV_RESO_RATIO;
            if (Settings.RECTANGULAR_TABLE)
                OFFSET_X = 0;
            else
                OFFSET_X = - (1f - 1f / CAMERA_RESOLUTION_RATIO) / 2;
        }
        #endregion

        #region Public members
        /// <summary>
        /// Initializes the surface and set the client's GUI manager.
        /// </summary>
        /// <param name="guiManager">Client's GUI manager to set.</param>
        public static Surface Initialize(IGrafitiClientGUIManager guiManager)
        {
            lock (s_lock)
            {
                if (s_instance == null)
                {
                    s_instance = new Surface();
                    s_instance.m_clientGUIManager = guiManager;
                }
                else
                    throw new Exception("Attempting to reinitialize Surface.");
            }
            return s_instance;
        }
        /// <summary>
        /// The instance.
        /// </summary>
        public static Surface Instance { get { return s_instance; } }
        /// <summary>
        /// Takes a GUI control and point specified in Grafiti-coordinate-system and 
        /// returns the point relative to the GUI component in client's coordinates.
        /// </summary>
        /// <param name="target">The GUI component.</param>
        /// <param name="x">X coordinate of the point to convert.</param>
        /// <param name="y">Y coordinate of the point to convert.</param>
        /// <param name="cx">X coordinate of the converted point.</param>
        /// <param name="cy">Y coordinate of the converted point.</param>
        public void PointToClient(IGestureListener target, float x, float y, out float cx, out float cy)
        {
            m_clientGUIManager.PointToClient(target, x, y, out cx, out cy);
        }
        #endregion

        #region TuioListener, members of
        public void addTuioObject(TuioObject obj) { /*Console.WriteLine("ADD");*/}
        public void updateTuioObject(TuioObject obj) { /*Console.WriteLine("UPDATE");*/ }
        public void removeTuioObject(TuioObject obj) { /*Console.WriteLine("REMOVE");*/ }
        public void addTuioCursor(TuioCursor c)
        {
            m_addingCursors.Add(
                new CursorPoint((int)(c.getSessionID()),
                (c.getX() + OFFSET_X) * CAMERA_RESOLUTION_RATIO, 
                c.getY(),
                CursorPoint.States.ADDED));
        }
        public void updateTuioCursor(TuioCursor c)
        {
            m_updatingCursors.Add(
                new CursorPoint((int)(c.getSessionID()),
                (c.getX() + OFFSET_X) * CAMERA_RESOLUTION_RATIO,
                c.getY(),
                CursorPoint.States.UPDATED));
        }
        public void removeTuioCursor(TuioCursor c)
        {
            m_removingCursors.Add(
                new CursorPoint((int)(c.getSessionID()),
                (c.getX() + OFFSET_X) * CAMERA_RESOLUTION_RATIO,
                c.getY(),
                CursorPoint.States.REMOVED));
        }
        public void refresh(long timeStampAsLong)
        {
			m_lastProcessedRefreshTimeStamp++;
			
            m_addedGroups.Clear();
            m_removedGroups.Clear();
            m_touchedGroups.Clear();

            int timeStamp = (int)timeStampAsLong;

            //Console.WriteLine(timeStamp - m_lastProcessedRefreshTimeStamp);
            //m_lastProcessedRefreshTimeStamp = timeStamp;

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
            for (int j = i; j < m_resurrectableTraces.Count - i; j++)
                m_resurrectableTraces[j].Terminate();
            m_resurrectableTraces.RemoveRange(i, m_resurrectableTraces.Count - i);
        }
        private void RemoveNonResurrectableGroups(int timeStamp)
        {
            m_activeGroups.RemoveAll(delegate(Group group)
            {
                if (!group.IsAlive && timeStamp - group.CurrentTimeStamp > Settings.TRACE_TIME_GAP)
                {
                    group.Terminate();
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
            // TODO: don't give priority to the first added cursors!

            foreach (CursorPoint cursor in m_addingCursors)
            {
                // set timestamp
                cursor.TimeStamp = timeStamp;

                // Targeting
                List<IGestureListener> targets;
                bool isZControl = GetTargetsAt(cursor.X, cursor.Y, out targets);

                // Grouping
                Group group;
                Trace trace;
                if(TryResurrectTrace(cursor, out trace)) // try finding a resurrecting trace
                {
                    // resurrect the trace
                    group = trace.Group;
                    trace.AppendAddingOrUpdatingCursor(cursor, targets, isZControl);
                }
                else
                {
                    // create a new trace
                    group = GetMatchingGroup(cursor, targets, isZControl);
                    trace = new Trace(cursor, group, targets, isZControl);
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
            foreach (CursorPoint cursor in m_updatingCursors)
            {
                // set timestamp
                cursor.TimeStamp = timeStamp;

                // determine belonging trace
                Trace trace = m_cursorTraceTable[cursor.SessionId];

                // update trace
                List<IGestureListener> targets;
                bool isZControl = GetTargetsAt(cursor.X, cursor.Y, out targets);
                trace.AppendAddingOrUpdatingCursor(cursor, targets, isZControl);

                // refresh touched group list
                if (!m_touchedGroups.Contains(trace.Group))
                    m_touchedGroups.Add(trace.Group);
            }
        }
        private void ProcessCurrentRemovingCursors(int timeStamp)
        {
            foreach (CursorPoint cursor in m_removingCursors)
            {
                // set timestamp
                cursor.TimeStamp = timeStamp;

                // determine trace
                Trace trace = m_cursorTraceTable[cursor.SessionId];

                // update trace
                List<IGestureListener> targets;
                bool isZControl = GetTargetsAt(cursor.X, cursor.Y, out targets);
                trace.AppendRemovingCursor(cursor, targets, isZControl);

                // add trace to resurrectable traces list
                m_resurrectableTraces.Insert(0, trace);

                // remove index
                m_cursorTraceTable.Remove(cursor.SessionId);

                // refresh touched group list
                if (!m_touchedGroups.Contains(trace.Group))
                    m_touchedGroups.Add(trace.Group);
            }
        }
        private bool TryResurrectTrace(CursorPoint cursor, out Trace resurrectingTrace)
        {
            resurrectingTrace = null;
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
                return true;
            }
            else
                return false;
        }
        /// <summary>
        /// Clustering method. Determine the best matching group to add the cursor's trace to.
        /// If none existing groups are suitable, a new one is created.
        /// </summary>
        /// <param name="cursor">The cursor to cluster</param>
        /// <param name="targets">The given cursor's targets</param>
        /// <param name="guiTargets">Flag indicating whether the given targets are GUI controls</param>
        /// <returns>The matching group where to add the given cursor-s trace.</returns>
        private Group GetMatchingGroup(CursorPoint cursor, List<IGestureListener> targets, bool isZControl)
        {
            Group matchingGroup = null;
            float minDist = Settings.GROUPING_SPACE * Settings.GROUPING_SPACE;
            float tempDist;

            foreach (Group group in m_activeGroups)
            {
                // filter out groups that don't accept the trace
                if (!group.AcceptNewCursor(cursor, targets, isZControl))
                    continue;

                // group all traces on the same zControl
                if (isZControl && group.OnZControl && targets[0] == group.ClosestCurrentTarget)
                {
                    matchingGroup = group;
                    break;
                }

                // find the closest
                tempDist = group.SquareDistanceToNearestTrace(cursor, Settings.CLUSTERING_ONLY_WITH_ALIVE_TRACES);
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
            m_activeGroups.Add(group);
            return group;
        }
        /// <summary>
        /// Get targets for a point and returns true if such point is over a Z-control.
        /// </summary>
        private bool GetTargetsAt(float x, float y, out List<IGestureListener> targets)
        {
            targets = new List<IGestureListener>();

            IGestureListener zListener;
            object zControl;
            List<ITangibleGestureListener> tangibleListeners;
            m_clientGUIManager.GetVisualsAt(x, y, out zListener, out zControl, out tangibleListeners);

            if (zListener != null)
            {
                targets.Add(zListener);
                return true;
            }
            if (zControl != null)
            {
                return true;
            }
            foreach(ITangibleGestureListener tangibleListener in tangibleListeners)
                targets.Add(tangibleListener);
            return false;
        }
        #endregion
    }
}