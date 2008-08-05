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
    public class Group
    {
        #region Private and internal members
        /// <summary>
        /// A list of IGestureListener that at every update will notify a GroupGRManager object 
        /// (if specified in the constructor)
        /// </summary>
        private class TargetList : List<ITuioObjectGestureListener>
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
            public new void Add(ITuioObjectGestureListener target)
            {
                if (m_groupGRManager != null)
                    m_groupGRManager.AddLocalTarget(target);
                base.Add(target);
            }
            public new void AddRange(IEnumerable<ITuioObjectGestureListener> targets)
            {
                if (m_groupGRManager != null)
                    foreach (ITuioObjectGestureListener target in targets)
                        m_groupGRManager.AddLocalTarget(target);
                base.AddRange(targets);
            }
            public new bool Remove(ITuioObjectGestureListener target)
            {
                if (m_groupGRManager != null)
                    m_groupGRManager.RemoveLocalTarget(target);
                return base.Remove(target);
            }

        }

        private static int s_counter = 0;           // id counter
        private int m_id;                           // id
        private List<Trace> m_traces;               // the traces of which the group is composed
        private int m_nOfAliveTraces;               // number of alive traces
        private float m_x0, m_y0;                   // initial coordinates of the first added trace
        private int m_t0, m_currentTimeStamp;       // initial and last time stamp
        private float m_centroidX, m_centroidY;     // centroid coordinates, calculated on the last points of the
        private float m_centroidLivingX, m_centroidLivingY;

        private GroupGRManager m_groupGRManager;    // gesture recognition manager
        private bool m_initializing;
        private bool m_processingTerminated;

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
        private ITuioObjectGestureListener m_closestEnteringTarget, m_closestCurrentTarget, m_closestLeavingTarget;
        private ITuioObjectGestureListener m_closestInitialTarget, m_closestNewInitialTarget, m_closestFinalTraget;

        // One-element target list. When there is a running and armed LGR, this variable points to its target.
        // If the variable is (re)set (by GroupGRManager), the change is reflected in m_lgrTargets.
        private ITuioObjectGestureListener m_exclusiveLocalTarget = null;

        private SimmetricDoubleDictionary<Trace, float> m_traceSpaceCouplingTable;
        private SimmetricDoubleDictionary<Trace, int> m_traceTimeCouplingTable;

        // temp auxiliary variables
        private List<List<ITuioObjectGestureListener>> m_currentInitialTargetLists = new List<List<ITuioObjectGestureListener>>();
        private List<List<ITuioObjectGestureListener>> m_currentStartingAndResettingTargetLists = new List<List<ITuioObjectGestureListener>>();
        private List<List<ITuioObjectGestureListener>> m_currentEnteringTargetLists = new List<List<ITuioObjectGestureListener>>();
        private List<List<ITuioObjectGestureListener>> m_currentLeavingTargetLists = new List<List<ITuioObjectGestureListener>>();
        private List<List<ITuioObjectGestureListener>> m_currentCurrentTargetLists = new List<List<ITuioObjectGestureListener>>();
        private List<List<ITuioObjectGestureListener>> m_finalTargetLists = new List<List<ITuioObjectGestureListener>>();

        internal GroupGRManager GRManager { get { return m_groupGRManager; } }
        internal bool ProcessingTerminated { get { return m_processingTerminated; } }
        #endregion

        #region Public properties
        // Id
        public int Id { get { return m_id; } }

        // Traces of which the group is composed
        public List<Trace> Traces { get { return m_traces; } }

        // Number of alive traces
        public int NOfAliveTraces { get { return m_nOfAliveTraces; } }

        // Flag indicating iff the group is alive, i.e. at least one trace is alive (a cursor is on the surface)
        public bool Alive { get { return m_nOfAliveTraces > 0; } }

        // Last time stamp, that is the current, corresponding to when the last refresh was called
        public int LastTimeStamp { get { return m_currentTimeStamp; } }

        // Centroid coordinates (calculated on the last point of the living and the resurrectable traces)
        public float CentroidX { get { return m_centroidX; } }
        public float CentroidY { get { return m_centroidY; } }
        // Centroid coordinates (calculated on the last point of the living traces)
        public float CentroidLivingX { get { return m_centroidLivingX; } }
        public float CentroidLivingY { get { return m_centroidLivingY; } }

        // Target lists
        public List<ITuioObjectGestureListener> InitialTargets { get { return (List<ITuioObjectGestureListener>)m_initialTargets; } }
        public List<ITuioObjectGestureListener> NewIntialTargets { get { return (List<ITuioObjectGestureListener>)m_newInitialTargets; } }
        public List<ITuioObjectGestureListener> FinalTargets { get { return (List<ITuioObjectGestureListener>)m_finalTargets; } }
        public List<ITuioObjectGestureListener> IntersectionTargets { get { return (List<ITuioObjectGestureListener>)m_intersectionTargets; } }
        public List<ITuioObjectGestureListener> UnionTargets { get { return (List<ITuioObjectGestureListener>)m_unionTargets; } }
        public List<ITuioObjectGestureListener> EnteringTargets { get { return (List<ITuioObjectGestureListener>)m_enteringTargets; } }
        public List<ITuioObjectGestureListener> CurrentTargets { get { return (List<ITuioObjectGestureListener>)m_currentTargets; } }
        public List<ITuioObjectGestureListener> LeavingTargets { get { return (List<ITuioObjectGestureListener>)m_leavingTargets; } }
        public ITuioObjectGestureListener ClosestEnteringTarget { get { return m_closestEnteringTarget; } }
        public ITuioObjectGestureListener ClosestCurrentTarget { get { return m_closestCurrentTarget; } }
        public ITuioObjectGestureListener ClosestLeavingTarget { get { return m_closestLeavingTarget; } }
        public ITuioObjectGestureListener ClosestInitialTarget { get { return m_closestInitialTarget; } }
        public ITuioObjectGestureListener ClosestNewInitialTarget { get { return m_closestNewInitialTarget; } }
        public ITuioObjectGestureListener ClosestFinalTarget { get { return m_closestFinalTraget; } }

        // List of targets of the currently active LGRs
        public List<ITuioObjectGestureListener> LGRTargets { get { return (List<ITuioObjectGestureListener>)m_lgrTargets; } }

        // Target of the winning and exclusive LGR
        public ITuioObjectGestureListener ExclusiveLocalTarget
        {
            get { return m_exclusiveLocalTarget; }
            internal set
            {
                m_exclusiveLocalTarget = value;
                // Reset lgr target list to the only actual target
                m_lgrTargets.Clear();
                if (value != null)
                    ((List<ITuioObjectGestureListener>)m_lgrTargets).Add(value); // by-passing the groupGRManager call
            }
        }
        #endregion

        #region Internal constructor
        internal Group()
        {
            m_groupGRManager = new GroupGRManager(this);
            m_processingTerminated = false;

            m_id = s_counter++;
            m_traces = new List<Trace>();
            m_nOfAliveTraces = 0;
            m_initializing = true;

            m_centroidX = m_centroidY = 0;
            m_centroidLivingX = m_centroidLivingY = 0;
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
            m_nOfAliveTraces++;

            Cursor firstPoint = trace.First;
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
                    Cursor current = t[t.Count - 1];
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
                m_nOfAliveTraces++;
                m_endingSequenceReversed.Remove(trace);
                m_currentResettingTraces.Add(trace);
            }

            m_currentUpdatingTraces.Add(trace);
            m_allCurrentTraces.Add(trace);
        }
        internal void EndTrace(Trace trace)
        {
            m_nOfAliveTraces--;

            // update ending sequence
            m_endingSequenceReversed.Insert(0, trace);

            m_currentEndingTraces.Add(trace);
            m_allCurrentTraces.Add(trace);
        }
        internal void Process(int timeStamp)
        {
            // update current time stamp
            m_currentTimeStamp = timeStamp;

            // update centroids
            UpdateCentroids();

            // update target lists and gesture event handlers
            UpdateTargetsAndHandlers();

            // process
            if (!m_processingTerminated)
                m_processingTerminated = m_groupGRManager.Process(m_allCurrentTraces);

            // clear temp lists
            m_currentStartingTraces.Clear();
            m_currentInitialTraces.Clear();
            m_currentUpdatingTraces.Clear();
            m_currentEndingTraces.Clear();
            m_allCurrentTraces.Clear();
        }
        private void UpdateCentroids()
        {
            int n = 0;            // element counter for living traces
            int nd = 0;           // element counter for recently dead traces
            float sx = 0, sy = 0; // coordinate accumulators for living traces
            float sxd = 0, syd = 0; // coordinate accumulators for recently dead traces

            Cursor cursor;
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
                    if (m_currentTimeStamp - cursor.TimeStamp <= Settings.GetTraceTimeGap())
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
            List<ITuioObjectGestureListener> intersectionInitialList = Intersect(m_currentInitialTargetLists);
            List<ITuioObjectGestureListener> unionInitialList = Union(m_currentInitialTargetLists);

            foreach (Trace t in m_currentStartingTraces)
                m_currentStartingAndResettingTargetLists.Add(t.InitialTargets);
            foreach (Trace t in m_currentResettingTraces)
                m_currentStartingAndResettingTargetLists.Add(t.CurrentTargets);
            List<ITuioObjectGestureListener> intersectionStartingAndResettingList = Intersect(m_currentStartingAndResettingTargetLists);
            List<ITuioObjectGestureListener> unionStartingAndResettingList = Union(m_currentStartingAndResettingTargetLists);


            if (m_initializing)
            {
                m_initializing = false;

                // The first added traces determine INITIAL, NEWINITIAL and INTERSECTION lists
                if (Settings.INTERSECTION_MODE)
                {
                    m_initialTargets.AddRange(intersectionInitialList);
                    m_newInitialTargets.AddRange(intersectionInitialList);
                }
                else
                {
                    m_initialTargets.AddRange(unionInitialList);
                    m_newInitialTargets.AddRange(unionInitialList);
                }
                m_intersectionTargets.AddRange(intersectionInitialList);

                if (m_initialTargets.Count > 0)
                {
                    newInitial = true;
                    newIntersect = true;
                }
            }
            else
            {
                if (Settings.INTERSECTION_MODE)
                {
                    // INITIAL list is the intersection of the recently born traces' INITIAL lists.
                    if (m_currentInitialTargetLists.Count > 0)
                    {
                        foreach (ITuioObjectGestureListener target in m_initialTargets)
                            if (!intersectionInitialList.Contains(target))
                                m_leavingTargets.Add(target);

                        foreach (ITuioObjectGestureListener target in m_leavingTargets)
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
                        foreach (ITuioObjectGestureListener target in m_currentTargets)
                        {
                            if (!intersectionStartingAndResettingList.Contains(target))
                                m_leavingTargets.Add(target);
                        }
                        foreach (ITuioObjectGestureListener target in m_leavingTargets)
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
                else
                {
                    // INITIAL list is the union of the recently born traces' INITIAL lists.
                    foreach (ITuioObjectGestureListener target in unionInitialList)
                    {
                        if (!m_initialTargets.Contains(target))
                        {
                            m_initialTargets.Add(target);
                            m_newInitialTargets.Add(target);
                            newInitial = true;
                        }
                    }

                    // update INTERSECTION list
                    m_intersectionTargets.RemoveAll(delegate(ITuioObjectGestureListener target)
                    {
                        if (!intersectionStartingAndResettingList.Contains(target))
                        {
                            newIntersect = true;
                            return true;
                        }
                        else
                            return false;
                    });
                }
            }
            #endregion


            #region all modified traces

            m_currentEnteringTargetLists.Clear();
            foreach (Trace t in m_allCurrentTraces)
                m_currentEnteringTargetLists.Add(t.EnteringTargets);
            List<ITuioObjectGestureListener> enteringTargets = Union(m_currentEnteringTargetLists);

            foreach (ITuioObjectGestureListener enteringTarget in enteringTargets)
            {
                // in this way the group's union list is the union of the traces' union lists
                if (!m_unionTargets.Contains(enteringTarget))
                {
                    m_unionTargets.Add(enteringTarget);
                    newUnion = true;
                }


                if (!m_currentTargets.Contains(enteringTarget))
                {
                    bool trueForAll = true;
                    if (Settings.INTERSECTION_MODE)
                    {
                        foreach (Trace t in m_traces)
                        {
                            if (t.Alive && !t.CurrentTargets.Contains(enteringTarget))
                            {
                                trueForAll = false;
                                break;
                            }
                        }
                    }
                    if (trueForAll)
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
            List<ITuioObjectGestureListener> leavingTargets = Union(m_currentLeavingTargetLists);

            // Note: if the following condition is satisfied then traceLeavingTargets is empty
            foreach (ITuioObjectGestureListener leavingTarget in leavingTargets)
            {
                if (m_currentTargets.Contains(leavingTarget))
                {
                    if (m_intersectionTargets.Remove(leavingTarget))
                        newIntersect = true;

                    bool falseForAll = true;
                    if (!Settings.INTERSECTION_MODE)
                    {
                        foreach (Trace t in m_traces)
                            if (t.Alive && t.CurrentTargets.Contains(leavingTarget))
                            {
                                falseForAll = false;
                                break;
                            }
                    }
                    if (falseForAll)
                    {
                        m_currentTargets.Remove(leavingTarget);
                        m_leavingTargets.Add(leavingTarget);
                        newCurrent = true;
                        newLeaving = true;

                        //Console.WriteLine("Removed {0} from CURRENT", traceLeavingTarget.ToString());
                    }
                }
            }

            #endregion


            #region removed traces

            if (!Alive) // if all traces have been removed then compute FINAL targets
            {
                int finalTimeStamp = m_allCurrentTraces[0].Last.TimeStamp;
                m_finalTargetLists.Clear();
                foreach (Trace deadTrace in m_endingSequenceReversed)
                {
                    if (finalTimeStamp - deadTrace.Last.TimeStamp > Settings.GROUPING_SYNCH_TIME)
                        break;
                    m_finalTargetLists.Add(deadTrace.FinalTargets);
                }

                if (Settings.INTERSECTION_MODE)
                    m_finalTargets.AddRange(Intersect(m_finalTargetLists));
                else
                    m_finalTargets.AddRange(Union(m_finalTargetLists));

                if (m_finalTargets.Count > 0)
                    newFinal = true;
            }
            else
            {
                // In INTERSECTION mode, if any trace has been removed 
                // then some target may be added to the CURRENT (and ENTERING) list
                if (m_currentEndingTraces.Count > 0 && Settings.INTERSECTION_MODE)
                {
                    m_currentCurrentTargetLists.Clear();
                    foreach (Trace t in m_traces)
                        if (t.Alive)
                            m_currentCurrentTargetLists.Add(t.CurrentTargets);
                    List<ITuioObjectGestureListener> currentTargets = Intersect(m_currentCurrentTargetLists);

                    foreach (ITuioObjectGestureListener currentTarget in currentTargets)
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


            #region closest targets

            float minDist = Settings.GROUPING_SPACE * Settings.GROUPING_SPACE + 1;
            float tempDist;
            ITuioObjectGestureListener closestTarget = null;

            // Find closest target
            foreach (ITuioObjectGestureListener target in m_currentTargets)
            {
                tempDist = target.GetSquareDistance(m_centroidX, m_centroidY);
                if (tempDist < minDist)
                {
                    minDist = tempDist;
                    closestTarget = target;
                }
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
        private List<ITuioObjectGestureListener> Intersect(List<List<ITuioObjectGestureListener>> targetLists)
        {
            List<ITuioObjectGestureListener> output = new List<ITuioObjectGestureListener>();
            if (targetLists.Count == 0)
                return output;

            output.AddRange(targetLists[0]);
            if (targetLists.Count == 1)
                return output;

            List<ITuioObjectGestureListener> firstElement = targetLists[0];
            targetLists.RemoveAt(0);
            int updatedOutputCount = output.Count;
            for (int i = 0; i < updatedOutputCount; )
            {
                if (!targetLists.TrueForAll(delegate(List<ITuioObjectGestureListener> targetList)
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
        private List<ITuioObjectGestureListener> Union(List<List<ITuioObjectGestureListener>> targetLists)
        {
            List<ITuioObjectGestureListener> output = new List<ITuioObjectGestureListener>();

            if (targetLists.Count == 0)
                return output;

            output.AddRange(targetLists[0]);
            if (targetLists.Count == 1)
                return output;

            for (int i = 1; i < targetLists.Count; i++)
                foreach (ITuioObjectGestureListener target in targetLists[i])
                    if (!output.Contains(target))
                        output.Add(target);

            return output;
        }

        // Return the distance between a given point and the nearest current finger.
        internal float SquareMinDist(Cursor cursor)
        {
            float minDist = Settings.GROUPING_SPACE * Settings.GROUPING_SPACE + 1;
            foreach (Trace trace in m_traces)
            {
                if (trace.Alive)// || 
                    //(m_currentTimeStamp - trace.Last.TimeStamp <= Settings.GetTraceTimeGap())) // recently dead
                    minDist = Math.Min(trace.Last.SquareDistance(cursor), minDist);
            }

            return minDist;
        }
        internal bool AcceptNewCursor(Cursor cursor)
        {
            // 1. ask GR if is currently interpreting and accepting the new trace

            // 2. (optional) check that the intersection of the targets is not empty.
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


            //sb.Append("\nInitial targets:");
            //foreach (ITuioObjectGestureListener target in m_initialTargets)
            //    sb.Append(target.ToString() + ", ");

            //sb.Append("\nNew Initial targets:");
            //foreach (ITuioObjectGestureListener target in m_newInitialTargets)
            //    sb.Append(target.ToString() + ", ");

            //sb.Append("\nIntersection targets:");
            //foreach (ITuioObjectGestureListener target in m_intersectionTargets)
            //    sb.Append(target.ToString() + ", ");

            //sb.Append("\nUnion targets:");
            //foreach (ITuioObjectGestureListener target in m_unionTargets)
            //    sb.Append(target.ToString() + ", ");

            //sb.Append("\nEntering targets:");
            //foreach (ITuioObjectGestureListener target in m_enteringTargets)
            //    sb.Append(target.ToString() + ", ");

            //sb.Append("\nCurrent targets:");
            //foreach (ITuioObjectGestureListener target in m_currentTargets)
            //    sb.Append(target.ToString() + ", ");

            //sb.Append("\nLeaving targets:");
            //foreach (ITuioObjectGestureListener target in m_leavingTargets)
            //    sb.Append(target.ToString() + ", ");

            //sb.Append("\nFinal targets:");
            //foreach (ITuioObjectGestureListener target in m_finalTargets)
            //    sb.Append(target.ToString() + ", ");

            //sb.Append("\nClosest entering target:");
            //if (m_closestEnteringTarget != null)
            //    sb.Append(m_closestEnteringTarget.ToString());
            //else
            //    sb.Append("null");

            //sb.Append("\nClosest current target:");
            //if (m_closestCurrentTarget != null)
            //    sb.Append(m_closestCurrentTarget.ToString());
            //else
            //    sb.Append("null");

            //sb.Append("\nClosest leaving target:");
            //if (m_closestLeavingTarget != null)
            //    sb.Append(m_closestLeavingTarget.ToString());
            //else
            //    sb.Append("null");

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
