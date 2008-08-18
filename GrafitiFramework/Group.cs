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
    /// Represents a group of instances of the Trace class, i.e. a group of fingers. A group
    /// is expected to produce a gesture, and it will be associated with a set of gesture recognizers.
    /// </summary>
    public class Group
    {
        #region Private and internal members
        /// <summary>
        /// A list of IGestureListener that at every update will notify a GroupGRManager object 
        /// (if specified in the constructor)
        /// </summary>
        private class TargetList : List<IGestureListener>
        {
            private GroupGRManager m_groupGRManager;

            public TargetList()
                : base()
            {
                m_groupGRManager = null;
            }
            public TargetList(GroupGRManager groupGRManager)
                : base()
            {
                m_groupGRManager = groupGRManager;
            }
            public new void Add(IGestureListener target)
            {
                if (m_groupGRManager != null)
                    m_groupGRManager.AddLocalTarget(target);
                base.Add(target);
            }
            public new void AddRange(IEnumerable<IGestureListener> targets)
            {
                if (m_groupGRManager != null)
                    foreach (IGestureListener target in targets)
                        m_groupGRManager.AddLocalTarget(target);
                base.AddRange(targets);
            }
            public new bool Remove(IGestureListener target)
            {
                if (m_groupGRManager != null)
                    m_groupGRManager.RemoveLocalTarget(target);
                return base.Remove(target);
            }

        }

        private static int s_counter = 0;           // id counter
        private readonly int m_id;                  // id
        private List<Trace> m_traces;               // the traces of which the group is composed
        private int m_nOfPresentTraces;             // number of alive traces (fingers currently in the surface)
        private int m_nOfActiveTraces;              // number of alive (present) or resurrectable traces.
        private float m_x0, m_y0;                   // initial coordinates of the first added trace
        private int m_t0, m_currentTimeStamp;       // initial and last time stamp
        private float m_centroidX, m_centroidY;     // centroid coordinates, calculated on the last points of the
        private float m_centroidLivingX, m_centroidLivingY;

        private GroupGRManager m_groupGRManager;    // gesture recognition manager
        private bool m_initializing;
        private bool m_processing;

        private int m_maxNumberOfFingersAllowed = -1;

        private List<Trace> m_startingSequence;       // traces listed in order of adding
        private List<Trace> m_endingSequenceReversed; // traces listed in reversed order of dying

        private List<Trace> m_currentStartingTraces, m_currentInitialTraces,// temp volatile (changing 
            m_currentUpdatingTraces, m_currentResettingTraces,
            m_currentEndingTraces, m_allCurrentTraces;                      // at every refresh) lists

        // Target lists: calculated geometrically, from the traces' target lists.
        private TargetList m_lgrTargets; // this can be modified also by setting m_exclusiveLocalTarget (see below)
        private TargetList m_initialTargets, m_newInitialTargets, m_finalTargets;
        private TargetList m_intersectionTargets, m_unionTargets;
        private TargetList m_enteringTargets, m_currentTargets, m_leavingTargets;
        // One-element target lists
        private IGestureListener m_closestEnteringTarget, m_closestCurrentTarget, m_closestLeavingTarget;
        private IGestureListener m_closestInitialTarget, m_closestNewInitialTarget, m_closestFinalTraget;

        // One-element target list. When there is a running and armed LGR, this variable points to its target.
        // If the variable is (re)set (by GroupGRManager), the change is reflected in m_lgrTargets.
        private IGestureListener m_exclusiveLocalTarget = null;

        private bool m_onSingleGUIControl;

        private SimmetricDoubleDictionary<Trace, float> m_traceSpaceCouplingTable;
        private SimmetricDoubleDictionary<Trace, int> m_traceTimeCouplingTable;

        // temp auxiliary variables
        private List<List<IGestureListener>> m_currentInitialTargetLists = new List<List<IGestureListener>>();
        private List<List<IGestureListener>> m_currentStartingAndResettingTargetLists = new List<List<IGestureListener>>();
        private List<List<IGestureListener>> m_currentEnteringTargetLists = new List<List<IGestureListener>>();
        private List<List<IGestureListener>> m_currentLeavingTargetLists = new List<List<IGestureListener>>();
        private List<List<IGestureListener>> m_currentCurrentTargetLists = new List<List<IGestureListener>>();
        private List<List<IGestureListener>> m_finalTargetLists = new List<List<IGestureListener>>();

        internal GroupGRManager GRManager { get { return m_groupGRManager; } }
        public bool Processing { get { return m_processing; } }

        public int MaxNumberOfFingersAllowed
        {
            get { return m_maxNumberOfFingersAllowed; }
            internal set { m_maxNumberOfFingersAllowed = value; }
        }
        #endregion

        #region Public properties
        /// <summary>
        /// Identification number of the group.
        /// </summary>
        public int Id { get { return m_id; } }

        /// <summary>
        /// Traces of which the group is composed
        /// </summary>
        public List<Trace> Traces { get { return m_traces; } }

        /// <summary>
        /// Number of present traces
        /// </summary>
        public int NumberOfPresentTraces { get { return m_nOfPresentTraces; } }

        /// <summary>
        /// True iff there is at least a present trace (a finger is currently in the surface)
        /// </summary>
        public bool IsPresent { get { return m_nOfPresentTraces > 0; } }

        /// <summary>
        /// Number of active traces
        /// </summary>
        public int NumberOfActiveTraces { get { return m_nOfActiveTraces; } }

        /// <summary>
        /// The group is terminated iff all traces have been removed since a time not lesser than Settings.TRACE_TIME_GAP
        /// </summary>
        public bool IsTerminated { get { return m_nOfActiveTraces == 0; } }

        /// <summary>
        /// Current time stamp corresponding to when the last refresh was called
        /// </summary>
        public int CurrentTimeStamp { get { return m_currentTimeStamp; } }

        /// <summary>
        /// Centroid x coordinate (calculated on the last point of the living and the resurrectable traces)
        /// </summary>
        public float CentroidX { get { return m_centroidX; } }
        /// <summary>
        /// Centroid y coordinate (calculated on the last point of the living and the resurrectable traces)
        /// </summary>
        public float CentroidY { get { return m_centroidY; } }

        /// <summary>
        /// Centroid x coordinate (calculated on the last point of the living traces)
        /// </summary>
        public float CentroidLivingX { get { return m_centroidLivingX; } }
        /// <summary>
        /// Centroid y coordinate (calculated on the last point of the living traces)
        /// </summary>
        public float CentroidLivingY { get { return m_centroidLivingY; } }

        // Target lists
        public List<IGestureListener> InitialTargets { get { return (List<IGestureListener>)m_initialTargets; } }
        public List<IGestureListener> NewIntialTargets { get { return (List<IGestureListener>)m_newInitialTargets; } }
        public List<IGestureListener> FinalTargets { get { return (List<IGestureListener>)m_finalTargets; } }
        public List<IGestureListener> IntersectionTargets { get { return (List<IGestureListener>)m_intersectionTargets; } }
        public List<IGestureListener> UnionTargets { get { return (List<IGestureListener>)m_unionTargets; } }
        public List<IGestureListener> EnteringTargets { get { return (List<IGestureListener>)m_enteringTargets; } }
        public List<IGestureListener> CurrentTargets { get { return (List<IGestureListener>)m_currentTargets; } }
        public List<IGestureListener> LeavingTargets { get { return (List<IGestureListener>)m_leavingTargets; } }
        public IGestureListener ClosestEnteringTarget { get { return m_closestEnteringTarget; } }
        public IGestureListener ClosestCurrentTarget { get { return m_closestCurrentTarget; } }
        public IGestureListener ClosestLeavingTarget { get { return m_closestLeavingTarget; } }
        public IGestureListener ClosestInitialTarget { get { return m_closestInitialTarget; } }
        public IGestureListener ClosestNewInitialTarget { get { return m_closestNewInitialTarget; } }
        public IGestureListener ClosestFinalTarget { get { return m_closestFinalTraget; } }

        /// <summary>
        /// List of targets of the currently active LGRs
        /// </summary>
        public List<IGestureListener> LGRTargets { get { return (List<IGestureListener>)m_lgrTargets; } }

        /// <summary>
        /// Target of the winning and exclusive LGR
        /// </summary>
        public IGestureListener ExclusiveLocalTarget
        {
            get { return m_exclusiveLocalTarget; }
            internal set
            {
                m_exclusiveLocalTarget = value;
                #region commented out to keep coherence: m_lgrTargets is set geometrically
                //// Reset lgr target list to the only actual target
                //m_lgrTargets.Clear();
                //if (value != null)
                //    ((List<IGestureListener>)m_lgrTargets).Add(value); // by-passing the groupGRManager call 
                #endregion
            }
        }

        /// <summary>
        /// True iff all the traces are currently on the same GUI component.
        /// </summary>
        public bool OnSingleGUIControl
        {
            get { return m_onSingleGUIControl; }
            private set
            {
                if (m_onSingleGUIControl != value)
                {
                    m_onSingleGUIControl = value;
                }
            }
        }
        #endregion

        #region Internal constructor
        internal Group()
        {
            m_groupGRManager = new GroupGRManager(this);
            m_processing = true;

            m_id = s_counter++;
            m_traces = new List<Trace>();
            m_nOfActiveTraces = 0;
            m_nOfPresentTraces = 0;
            m_initializing = true;

            m_centroidX = m_centroidY = -1;
            m_centroidLivingX = m_centroidLivingY = -1;
            m_currentTimeStamp = -1;

            m_startingSequence = new List<Trace>();
            m_endingSequenceReversed = new List<Trace>();

            m_currentStartingTraces = new List<Trace>();
            m_currentInitialTraces = new List<Trace>();
            m_currentUpdatingTraces = new List<Trace>();
            m_currentEndingTraces = new List<Trace>();
            m_currentResettingTraces = new List<Trace>();
            m_allCurrentTraces = new List<Trace>();

            m_traceSpaceCouplingTable = new SimmetricDoubleDictionary<Trace, float>(10);
            m_traceTimeCouplingTable = new SimmetricDoubleDictionary<Trace, int>(10);

            m_initialTargets = new TargetList();
            m_newInitialTargets = new TargetList();
            m_finalTargets = new TargetList();
            m_enteringTargets = new TargetList();
            m_currentTargets = new TargetList();
            m_leavingTargets = new TargetList();
            m_intersectionTargets = new TargetList();
            m_unionTargets = new TargetList();
            m_closestEnteringTarget = null;
            m_closestCurrentTarget = null;
            m_closestLeavingTarget = null;
            m_closestInitialTarget = null;
            m_closestNewInitialTarget = null;
            m_closestFinalTraget = null;

            if (Settings.LGR_TARGET_LIST == Settings.LGRTargetLists.INITIAL_TARGET_LIST)
            {
                m_initialTargets = new TargetList(m_groupGRManager);
                m_lgrTargets = m_initialTargets;
            }
            else if (Settings.LGR_TARGET_LIST == Settings.LGRTargetLists.INTERSECTION_TARGET_LIST)
            {
                m_intersectionTargets = new TargetList(m_groupGRManager);
                m_lgrTargets = m_intersectionTargets;
            }
            //else if (Settings.LGR_TARGET_LIST == Settings.LGRTargetLists.FINAL_TARGET_LIST)
            //    m_finalTargets = new TargetList(m_groupGRManager);
        }
        #endregion

        #region Private and internal methods
        internal void StartTrace(Trace trace)
        {
            if (m_traces.Count == 0)
                m_initializing = true;

            m_traces.Add(trace);
            m_nOfActiveTraces++;
            m_nOfPresentTraces++;

            CursorPoint firstPoint = trace.First;
            int firstPointTimeStamp = firstPoint.TimeStamp;

            // set initial (referential) point of the group
            if (m_traces.Count == 1)
            {
                m_x0 = firstPoint.X;
                m_y0 = firstPoint.Y;
                m_t0 = firstPointTimeStamp;
            }

            // update starting sequence
            if (firstPointTimeStamp - m_t0 < Settings.GROUPING_SYNCH_TIME)
            {
                m_startingSequence.Add(trace);
                m_currentInitialTraces.Add(trace);
            }

            // compute and index trace-couplings
            foreach (Trace t in m_traces)
                if (t != trace)
                {
                    CursorPoint current = t[t.Count - 1];
                    float dx = firstPoint.X - current.X;
                    float dy = firstPoint.Y - current.Y;
                    m_traceSpaceCouplingTable[t, trace] = dx * dx + dy * dy;
                    m_traceTimeCouplingTable[t, trace] = firstPointTimeStamp - current.TimeStamp;
                }

            m_currentStartingTraces.Add(trace);
            m_allCurrentTraces.Add(trace);
        }
        internal void UpdateTrace(Trace trace)
        {
            if (trace.State == Trace.States.RESET)
            {
                m_nOfPresentTraces++;
                m_endingSequenceReversed.Remove(trace);
                m_currentResettingTraces.Add(trace);
            }

            m_currentUpdatingTraces.Add(trace);
            m_allCurrentTraces.Add(trace);
        }
        internal void EndTrace(Trace trace)
        {
            m_nOfPresentTraces--;

            // update ending sequence
            m_endingSequenceReversed.Insert(0, trace);

            m_currentEndingTraces.Add(trace);
            m_allCurrentTraces.Add(trace);
        }
        internal void TerminateTrace(Trace trace)
        {
            m_nOfActiveTraces--;
        }
        internal void Terminate()
        {
            m_groupGRManager.Terminate();
        }
        internal bool Process(int timeStamp)
        {
            if (m_processing)
            {
                // update current time stamp
                m_currentTimeStamp = timeStamp;

                // update centroids
                UpdateCentroids();

                // update target lists and gesture event handlers
                UpdateTargetsAndHandlers();

                // process
                if (!m_groupGRManager.Process(m_allCurrentTraces, false))
                    m_processing = false;

                // clear temp lists
                m_currentStartingTraces.Clear();
                m_currentInitialTraces.Clear();
                m_currentUpdatingTraces.Clear();
                m_currentEndingTraces.Clear();
                m_allCurrentTraces.Clear();
            }
            return m_processing;
        }
        private void UpdateCentroids()
        {
            int n = 0;            // element counter for living traces
            int nd = 0;           // element counter for recently dead traces
            float sx = 0, sy = 0; // coordinate accumulators for living traces
            float sxd = 0, syd = 0; // coordinate accumulators for recently dead traces

            CursorPoint cursor;
            foreach (Trace trace in m_traces)
            {
                cursor = trace.Last;
                if (trace.Alive)
                {
                    // Living trace
                    sx += cursor.X;
                    sy += cursor.Y;
                    n++;
                }
                else
                {
                    // Recently dead trace
                    if (m_currentTimeStamp - cursor.TimeStamp <= Settings.TRACE_TIME_GAP)
                    {
                        sxd += cursor.X;
                        syd += cursor.Y;
                        nd++;
                    }
                }
            }

            int sn = n + nd;
            if (sn != 0)
            {
                m_centroidX = (sx + sxd) / sn;
                m_centroidY = (sy + syd) / sn;
                if (n != 0)
                {
                    m_centroidLivingX = sx / n;
                    m_centroidLivingY = sy / n;
                }
            }

            //Console.WriteLine("Centroid: {0}; {1}", m_centroidX, m_centroidY);
        }
        private void UpdateTargetsAndHandlers()
        {
            bool newInitial = false;
            bool newFinal = false;
            bool newEntering = false;
            bool newCurrent = false;
            bool newLeaving = false;
            bool newUnion = false;
            bool newIntersect = false;
            bool newClosestEnt = false;
            bool newClosestCur = false;
            bool newClosestLvn = false;
            bool newClosestIni = false;
            bool newClosestFin = false;


            // Clear volatile lists
            if (m_newInitialTargets.Count > 0)
            {
                m_newInitialTargets.Clear();
                m_closestNewInitialTarget = null;
                newInitial = true;
            }
            if (m_enteringTargets.Count > 0)
            {
                m_enteringTargets.Clear();
                newEntering = true;
            }
            if (m_leavingTargets.Count > 0)
            {
                m_leavingTargets.Clear();
                newLeaving = true;
            }
            if (m_finalTargets.Count > 0) // for group resurrection
            {
                m_finalTargets.Clear();
                newFinal = true;
            }


            #region added and reset traces

            m_currentInitialTargetLists.Clear();
            m_currentStartingAndResettingTargetLists.Clear();

            foreach (Trace t in m_currentInitialTraces)
                m_currentInitialTargetLists.Add(t.InitialTargets);
            List<IGestureListener> intersectionInitialList = Intersect(m_currentInitialTargetLists);

            foreach (Trace t in m_currentStartingTraces)
                m_currentStartingAndResettingTargetLists.Add(t.InitialTargets);
            foreach (Trace t in m_currentResettingTraces)
                m_currentStartingAndResettingTargetLists.Add(t.CurrentTargets);
            List<IGestureListener> intersectionStartingAndResettingList = Intersect(m_currentStartingAndResettingTargetLists);


            if (m_initializing)
            {
                m_initializing = false;

                // The first added traces determine INITIAL, NEWINITIAL and INTERSECTION lists
                m_initialTargets.AddRange(intersectionInitialList);
                m_newInitialTargets.AddRange(intersectionInitialList);
                m_intersectionTargets.AddRange(intersectionInitialList);

                if (m_initialTargets.Count > 0)
                {
                    newInitial = true;
                    newIntersect = true;
                }
            }
            else
            {

                // INITIAL list is the intersection of the recently born traces' INITIAL lists.
                if (m_currentInitialTargetLists.Count > 0)
                {
                    foreach (IGestureListener target in m_initialTargets)
                        if (!intersectionInitialList.Contains(target))
                            m_leavingTargets.Add(target);

                    foreach (IGestureListener target in m_leavingTargets)
                    {
                        m_initialTargets.Remove(target);
                        m_currentTargets.Remove(target);
                        m_intersectionTargets.Remove(target);
                    }
                    if (m_leavingTargets.Count > 0)
                    {
                        newLeaving = true;
                        newInitial = true;
                        newCurrent = true;
                        newIntersect = true;
                    }
                }

                // Remove targets that don't appear in the added/reset traces' lists
                if (m_currentStartingTraces.Count + m_currentResettingTraces.Count > 0)
                {
                    foreach (IGestureListener target in m_currentTargets)
                    {
                        if (!intersectionStartingAndResettingList.Contains(target))
                            m_leavingTargets.Add(target);
                    }
                    foreach (IGestureListener target in m_leavingTargets)
                    {
                        m_currentTargets.Remove(target);
                        m_intersectionTargets.Remove(target);
                    }
                    if (m_leavingTargets.Count > 0)
                    {
                        newLeaving = true;
                        newCurrent = true;
                        newIntersect = true;
                    }
                }
            }
            #endregion


            #region all modified traces

            m_currentEnteringTargetLists.Clear();
            foreach (Trace t in m_allCurrentTraces)
                m_currentEnteringTargetLists.Add(t.EnteringTargets);
            List<IGestureListener> enteringTargets = Union(m_currentEnteringTargetLists);

            foreach (IGestureListener enteringTarget in enteringTargets)
            {
                // in this way the group's union list is the union of the traces' union lists
                if (!m_unionTargets.Contains(enteringTarget))
                {
                    m_unionTargets.Add(enteringTarget);
                    newUnion = true;
                }

                if (!m_currentTargets.Contains(enteringTarget))
                {
                    if (m_traces.TrueForAll(delegate(Trace t)
                    {
                        return !t.Alive || t.CurrentTargets.Contains(enteringTarget);
                    }))
                    {
                        m_enteringTargets.Add(enteringTarget);
                        m_currentTargets.Add(enteringTarget);
                        newEntering = true;
                        newCurrent = true;


                        //// in this way the group's union list is the intersection of the traces' union lists
                        //if (!m_unionTargets.Contains(enteringTarget))
                        //    m_unionTargets.Add(enteringTarget);
                    }
                }
            }

            m_currentLeavingTargetLists.Clear();
            foreach (Trace t in m_allCurrentTraces)
                m_currentLeavingTargetLists.Add(t.LeavingTargets);
            List<IGestureListener> leavingTargets = Union(m_currentLeavingTargetLists);

            // Note: if the following condition is satisfied then traceLeavingTargets is empty
            foreach (IGestureListener leavingTarget in leavingTargets)
            {
                if (m_currentTargets.Contains(leavingTarget))
                {
                    if (m_intersectionTargets.Remove(leavingTarget))
                        newIntersect = true;

                    m_currentTargets.Remove(leavingTarget);
                    m_leavingTargets.Add(leavingTarget);
                    newCurrent = true;
                    newLeaving = true;

                    //Console.WriteLine("Removed {0} from CURRENT", traceLeavingTarget.ToString());
                }
            }

            #endregion


            #region removed traces

            if (!IsPresent) // if all traces have been removed then compute FINAL targets
            {
                int finalTimeStamp = m_allCurrentTraces[0].Last.TimeStamp;
                m_finalTargetLists.Clear();
                foreach (Trace deadTrace in m_endingSequenceReversed)
                {
                    if (finalTimeStamp - deadTrace.Last.TimeStamp > Settings.GROUPING_SYNCH_TIME)
                        break;
                    m_finalTargetLists.Add(deadTrace.FinalTargets);
                }

                m_finalTargets.AddRange(Intersect(m_finalTargetLists));

                if (m_finalTargets.Count > 0)
                    newFinal = true;
            }
            else
            {
                // If any trace has been removed then some target may be added to the CURRENT (and ENTERING) list
                if (m_currentEndingTraces.Count > 0)
                {
                    m_currentCurrentTargetLists.Clear();
                    foreach (Trace t in m_traces)
                        if (t.Alive)
                            m_currentCurrentTargetLists.Add(t.CurrentTargets);
                    List<IGestureListener> currentTargets = Intersect(m_currentCurrentTargetLists);

                    foreach (IGestureListener currentTarget in currentTargets)
                    {
                        if (!m_currentTargets.Contains(currentTarget))
                        {
                            m_enteringTargets.Add(currentTarget);
                            m_currentTargets.Add(currentTarget);
                            newEntering = true;
                            newCurrent = true;
                        }
                    }
                }
            }

            #endregion

            
            OnSingleGUIControl = m_currentTargets.Count > 0 && !(m_currentTargets[0] is ITangibleGestureListener);


            #region closest targets
            IGestureListener closestTarget = null;
            float minDist = Settings.GROUPING_SPACE * Settings.GROUPING_SPACE + 1;
            float tempDist;
            if (!(OnSingleGUIControl))
            {
                foreach (IGestureListener target in m_currentTargets)
                {
                    tempDist = ((ITangibleGestureListener)target).GetSquareDistance(m_centroidX, m_centroidY);
                    if (tempDist < minDist)
                    {
                        minDist = tempDist;
                        closestTarget = target;
                    }
                }
            }
            else
            {
                closestTarget = m_currentTargets[0];
            }

            // Update closest entering/current/leaving
            if (m_closestCurrentTarget != closestTarget)
            {
                if (m_closestCurrentTarget != null)
                {
                    m_closestLeavingTarget = m_closestCurrentTarget;
                    newClosestLvn = true;
                }
                else
                    if (m_closestLeavingTarget != null)
                    {
                        m_closestLeavingTarget = null;
                        newClosestLvn = true;
                    }

                m_closestCurrentTarget = closestTarget;
                m_closestEnteringTarget = closestTarget;
                newClosestCur = true;
                newClosestEnt = true;
            }
            else
            {
                if (m_closestEnteringTarget != null)
                {
                    m_closestEnteringTarget = null;
                    newClosestEnt = true;
                }
                if (m_closestLeavingTarget != null)
                {
                    m_closestLeavingTarget = null;
                    newClosestLvn = true;
                }
            }

            // update closest initial and new initial
            if (newInitial)
                if (m_closestInitialTarget != closestTarget)
                {
                    m_closestInitialTarget = closestTarget;
                    m_closestNewInitialTarget = m_closestInitialTarget;
                    newClosestIni = true;
                }

            // update closest final
            if (newFinal)
                if (m_closestFinalTraget != closestTarget)
                {
                    m_closestFinalTraget = closestTarget;
                    newClosestFin = true;
                }

            #endregion


            if (newCurrent | newIntersect | newUnion | newClosestCur | newClosestEnt | newClosestLvn |
                newInitial | newFinal | newEntering | newLeaving)
            {
                m_groupGRManager.UpdateGGRHandlers(newInitial, newFinal, newEntering, newCurrent, newLeaving,
                    newIntersect, newUnion, newClosestEnt, newClosestCur, newClosestLvn, newClosestIni, newClosestFin);

                //Console.WriteLine(Dump());
                //Console.WriteLine("newCurrent {0} | newIntersect {1} | newUnion {2} | newClosestCur" +
                //" {3} | newClosestEnt {4} | newClosestLvn {5} | newInitial {6} | newFinal {7} | newEntering {8} | newLeaving {9} ",
                //newCurrent, newIntersect, newUnion, newClosestCur, newClosestEnt, newClosestLvn,
                //newInitial, newFinal, newEntering, newLeaving);
            }
        }
        private List<IGestureListener> Intersect(List<List<IGestureListener>> targetLists)
        {
            List<IGestureListener> output = new List<IGestureListener>();
            if (targetLists.Count == 0)
                return output;

            output.AddRange(targetLists[0]);
            if (targetLists.Count == 1)
                return output;

            List<IGestureListener> firstElement = targetLists[0];
            targetLists.RemoveAt(0);
            int updatedOutputCount = output.Count;
            for (int i = 0; i < updatedOutputCount; )
            {
                if (!targetLists.TrueForAll(delegate(List<IGestureListener> targetList)
                {
                    return targetList.Contains(output[i]);
                }))
                {
                    output.RemoveAt(i);
                    updatedOutputCount--;
                }
                else
                    i++;
            }
            targetLists.Insert(0, firstElement);

            return output;
        }
        private List<IGestureListener> Union(List<List<IGestureListener>> targetLists)
        {
            List<IGestureListener> output = new List<IGestureListener>();

            if (targetLists.Count == 0)
                return output;

            output.AddRange(targetLists[0]);
            if (targetLists.Count == 1)
                return output;

            for (int i = 1; i < targetLists.Count; i++)
                foreach (IGestureListener target in targetLists[i])
                    if (!output.Contains(target))
                        output.Add(target);

            return output;
        }

        /// <summary>
        /// Calculate the distance between the given cursor and the nearest cursor belonging
        /// to an existing trace. This is the geometrical test for the clusterization.
        /// </summary>
        /// <param name="cursor">The cursor</param>
        /// <param name="onlyAliveTraces">If this is set to true, the distance will be relative
        /// only to alive traces (i.e. the finger is still present). If set to false there
        /// will be considered also the resurrectable (i.e. recently died) traces.</param>
        /// <returns>The minimum distance between the given cursor and the nearest trace,
        /// among the considered ones (depending on the parameter onlyAliveTraces)</returns>
        internal float SquareDistanceToNearestTrace(CursorPoint cursor, bool onlyAliveTraces)
        {
            float minDist = Settings.GROUPING_SPACE * Settings.GROUPING_SPACE + 1;
            foreach (Trace trace in m_traces)
            {
                if (trace.Alive || 
                    (!onlyAliveTraces && cursor.TimeStamp - trace.Last.TimeStamp <= Settings.GetTraceTimeGap())) // recently dead
                    minDist = Math.Min(trace.Last.SquareDistance(cursor), minDist);
            }

            return minDist;
        }
        /// <summary>
        /// Test whether a cursor can be added to the group. This is the non-geometrical test
        /// for the clusterization.
        /// </summary>
        /// <param name="cursor">The cursor that is supposed to be added to the group.</param>
        /// <param name="targets">Targets of the given cursor.</param>
        /// <param name="guiTargets">This flag must be true iff the targets are GUI controls</param>
        /// <returns>true iff the cursor can be added to the group</returns>
        internal bool AcceptNewCursor(CursorPoint cursor, List<IGestureListener> targets, bool guiTargets)
        {
            // If both the group and the cursor are on a GUI control, then such control must be the same
            // If only one of them is on a GUI control than don't accept the cursor
            if (guiTargets != OnSingleGUIControl ||
                (guiTargets && targets[0] != m_currentTargets[0]))
                return false;

            if (m_maxNumberOfFingersAllowed > 0 &&
                m_nOfActiveTraces >= m_maxNumberOfFingersAllowed)
                return false;


            // TODO maybe (?)
            // - ask GR if is currently interpreting and accepting the new trace

            // - (optional) check that the intersection of the targets is not empty.
            // Although this could be good for some purposes, if the actual target of the gesture
            // is the global surface, the right clusterizing will not be done. So that such gestures
            // will have to be done outside every controls' areas.
            // So this check should be enabled explicitly by the client if he/she wants.
            //if (Intersect(trace.Targets, targets).Count == 0)
            //    return false;

            return true;
        }
        internal string Dump()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();


            //sb.AppendLine("Group " + m_id);
            //sb.Append("Alive = ");
            //sb.Append(Alive);
            //sb.Append("\nN of Traces: ");
            //sb.Append(m_traces.Count);
            //sb.Append(", living: ");
            //sb.Append(m_nOfAliveTraces);

            //foreach (Trace trace in m_traces)
            //{
            //    sb.Append("\nTrace :");
            //    sb.Append(trace.Id);
            //    sb.Append(":");
            //    foreach (Cursor cursor in trace.Path)
            //        sb.Append(cursor.State + ", "); 
            //}


            sb.Append("\nInitial targets:");
            foreach (IGestureListener target in m_initialTargets)
                sb.Append(target.ToString() + ", ");

            sb.Append("\nNew Initial targets:");
            foreach (IGestureListener target in m_newInitialTargets)
                sb.Append(target.ToString() + ", ");

            sb.Append("\nIntersection targets:");
            foreach (IGestureListener target in m_intersectionTargets)
                sb.Append(target.ToString() + ", ");

            sb.Append("\nUnion targets:");
            foreach (IGestureListener target in m_unionTargets)
                sb.Append(target.ToString() + ", ");

            sb.Append("\nEntering targets:");
            foreach (IGestureListener target in m_enteringTargets)
                sb.Append(target.ToString() + ", ");

            sb.Append("\nCurrent targets:");
            foreach (IGestureListener target in m_currentTargets)
                sb.Append(target.ToString() + ", ");

            sb.Append("\nLeaving targets:");
            foreach (IGestureListener target in m_leavingTargets)
                sb.Append(target.ToString() + ", ");

            sb.Append("\nFinal targets:");
            foreach (IGestureListener target in m_finalTargets)
                sb.Append(target.ToString() + ", ");

            sb.Append("\nClosest entering target:");
            if (m_closestEnteringTarget != null)
                sb.Append(m_closestEnteringTarget.ToString());
            else
                sb.Append("null");

            sb.Append("\nClosest current target:");
            if (m_closestCurrentTarget != null)
                sb.Append(m_closestCurrentTarget.ToString());
            else
                sb.Append("null");

            sb.Append("\nClosest leaving target:");
            if (m_closestLeavingTarget != null)
                sb.Append(m_closestLeavingTarget.ToString());
            else
                sb.Append("null");

            sb.Append("\nLGR targets:");
            foreach (IGestureListener target in m_lgrTargets)
                sb.Append(target.ToString() + ", ");

            return sb.ToString();

            //    float x, y;
            //    Console.WriteLine("-------------------------------------------");
            //    Console.WriteLine("Group {0}", m_id);
            //    Console.WriteLine("-------------------------------------------");
            //    foreach (Trace trc in m_traces)
            //    {
            //        Console.WriteLine("Trace {0} normalized history:", trc.SessionId);
            //        foreach (Cursor cur in trc.History)
            //        {
            //            x = cur.XPos - m_x0;
            //            y = cur.YPos - m_y0;
            //            Console.WriteLine("({0},{1}), timestamp:{2}, state:{3}", x, y, cur.TimeStamp, cur.State);
            //        }
            //        Console.WriteLine();
            //    }
        }
        #endregion
    }
}
