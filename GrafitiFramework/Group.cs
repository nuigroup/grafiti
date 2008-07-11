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
        private int m_id;                           // id
        private List<Trace> m_traces;               // the traces of which the group is composed
        private int m_nOfAliveTraces;               // number of alive traces
        private float m_x0, m_y0;                   // initial coordinates of the first added trace
        private long m_t0, m_currentTimeStamp;      // initial and last time stamp
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

        // Target lists
        private TargetList m_initialTargets, m_newInitialTargets, m_finalTargets;
        private TargetList m_intersectionTargets, m_unionTargets;
        private TargetList m_enteringTargets, m_currentTargets, m_leavingTargets;
        private IGestureListener m_closestEnteringTarget, m_closestCurrentTarget, m_closestLeavingTarget;

        private SimmetricDoubleDictionary<Trace, float> m_traceSpaceCouplingTable;
        private SimmetricDoubleDictionary<Trace, long> m_traceTimeCouplingTable;

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
        public long LastTimeStamp { get { return m_currentTimeStamp; } }

        // Centroid coordinates (calculated on the last point of the living and the resurrectable traces)
        public float CentroidX { get { return m_centroidX; } }
        public float CentroidY { get { return m_centroidY; } }
        // Centroid coordinates (calculated on the last point of the living traces)
        public float CentroidLivingX { get { return m_centroidLivingX; } }
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
            m_traceTimeCouplingTable = new SimmetricDoubleDictionary<Trace, long>(10);

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

            if (Surface.LGR_TARGET_LIST == Surface.LGRTargetLists.INITIAL_TARGET_LIST)
                m_initialTargets = new TargetList(m_groupGRManager);
            else if (Surface.LGR_TARGET_LIST == Surface.LGRTargetLists.INTERSECTION_TARGET_LIST)
                m_intersectionTargets = new TargetList(m_groupGRManager);
            //else if (Surface.LGR_TARGET_LIST == Surface.LGRTargetLists.FINAL_TARGET_LIST)
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

            TuioCursor firstPoint = trace.First;
            long firstPointTimeStamp = firstPoint.TimeStamp;

            // set initial (referential) point of the group
            if (m_traces.Count == 1)
            {
                m_x0 = firstPoint.X;
                m_y0 = firstPoint.Y;
                m_t0 = firstPointTimeStamp;
            }

            // update starting sequence
            if (firstPointTimeStamp - m_t0 < Surface.GROUPING_SYNCH_TIME)
            {
                m_startingSequence.Add(trace);
                m_currentInitialTraces.Add(trace);
            }

            // compute and index trace-couplings
            foreach (Trace t in m_traces)
                if (t != trace)
                {
                    TuioCursor current = t[t.Count - 1];
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
        internal void Process(long timeStamp)
        {
            // update last time stamp
            m_currentTimeStamp = timeStamp;

            // update centroid
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
            int n = 0;            // element counter
            float sx = 0, sy = 0; // coordinate accumulators

            // Living traces
            foreach (Trace trace in m_traces)
                if (trace.Alive)
                {
                    sx += trace.Last.X;
                    sy += trace.Last.Y;
                    n++;
                }

            if (n != 0)
            {
                m_centroidLivingX = sx / n;
                m_centroidLivingY = sy / n;
            }

            // Died yet resurrectable traces
            // TODO: make parametrizable (enable/disable + time threshold switchable between
            //       TRACE_RESURRECTION_TIME and GROUPING_INITIAL_AND_FINAL_TIME).
            foreach (Trace deadTrace in m_endingSequenceReversed)
            {
                // Filter out traces that are died permanently.
                if (m_currentTimeStamp - deadTrace.Last.TimeStamp > Surface.GROUPING_SYNCH_TIME)
                    break;

                sx += deadTrace.Last.X;
                sy += deadTrace.Last.Y;
                n++;
            }

            if (n != 0)
            {
                m_centroidX = sx / n;
                m_centroidY = sy / n;
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



            // Clear volatile lists
            if (m_newInitialTargets.Count > 0)
            {
                m_newInitialTargets.Clear();
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

            List<List<IGestureListener>> currentInitialTargetLists = new List<List<IGestureListener>>();
            foreach (Trace t in m_currentInitialTraces)
                currentInitialTargetLists.Add(t.InitialTargets);
            List<IGestureListener> intersectionInitialList = Intersect(currentInitialTargetLists);
            List<IGestureListener> unionInitialList = Union(currentInitialTargetLists);

            List<List<IGestureListener>> currentStartingAndResettingTargetLists = new List<List<IGestureListener>>();
            foreach (Trace t in m_currentStartingTraces)
                currentStartingAndResettingTargetLists.Add(t.InitialTargets);
            foreach (Trace t in m_currentResettingTraces)
                currentStartingAndResettingTargetLists.Add(t.CurrentTargets);
            List<IGestureListener> intersectionStartingAndResettingList = Intersect(currentStartingAndResettingTargetLists);
            List<IGestureListener> unionStartingAndResettingList = Union(currentStartingAndResettingTargetLists);


            if (m_initializing)
            {
                m_initializing = false;

                // The first added traces determine INITIAL, NEWINITIAL and INTERSECTION lists
                if (Surface.INTERSECTION_MODE)
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
                if (Surface.INTERSECTION_MODE)
                {
                    // INITIAL list is the intersection of the recently born traces' INITIAL lists.
                    if (currentInitialTargetLists.Count > 0)
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
                else
                {
                    // INITIAL list is the union of the recently born traces' INITIAL lists.
                    foreach (IGestureListener target in unionInitialList)
                    {
                        if (!m_initialTargets.Contains(target))
                        {
                            m_initialTargets.Add(target);
                            m_newInitialTargets.Add(target);
                            newInitial = true;
                        }
                    }

                    // update INTERSECTION list
                    m_intersectionTargets.RemoveAll(delegate(IGestureListener target)
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

            List<List<IGestureListener>> currentEnteringTargetLists = new List<List<IGestureListener>>();
            foreach (Trace t in m_allCurrentTraces)
                currentEnteringTargetLists.Add(t.EnteringTargets);
            List<IGestureListener> enteringTargets = Union(currentEnteringTargetLists);

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
                    bool trueForAll = true;
                    if (Surface.INTERSECTION_MODE)
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

            List<List<IGestureListener>> currentLeavingTargetLists = new List<List<IGestureListener>>();
            foreach (Trace t in m_allCurrentTraces)
                currentLeavingTargetLists.Add(t.LeavingTargets);
            List<IGestureListener> leavingTargets = Union(currentLeavingTargetLists);

            // Note: if the following condition is satisfied then traceLeavingTargets is empty
            foreach (IGestureListener leavingTarget in leavingTargets)
            {
                if (m_currentTargets.Contains(leavingTarget))
                {
                    if (m_intersectionTargets.Remove(leavingTarget))
                        newIntersect = true;

                    bool falseForAll = true;
                    if (!Surface.INTERSECTION_MODE)
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
                long finalTimeStamp = m_allCurrentTraces[0].Last.TimeStamp;
                List<List<IGestureListener>> finalTargetLists = new List<List<IGestureListener>>();
                foreach (Trace deadTrace in m_endingSequenceReversed)
                {
                    if (finalTimeStamp - deadTrace.Last.TimeStamp > Surface.GROUPING_SYNCH_TIME)
                        break;
                    finalTargetLists.Add(deadTrace.FinalTargets);
                }

                if (Surface.INTERSECTION_MODE)
                    m_finalTargets.AddRange(Intersect(finalTargetLists));
                else
                    m_finalTargets.AddRange(Union(finalTargetLists));

                if (m_finalTargets.Count > 0)
                    newFinal = true;
            }
            else
            {
                // In INTERSECTION mode, if any trace has been removed 
                // then some target may be added to the CURRENT (and ENTERING) list
                if (m_currentEndingTraces.Count > 0 && Surface.INTERSECTION_MODE)
                {
                    List<List<IGestureListener>> currentCurrentTargetLists = new List<List<IGestureListener>>();
                    foreach (Trace t in m_traces)
                        if (t.Alive)
                            currentCurrentTargetLists.Add(t.CurrentTargets);
                    List<IGestureListener> currentTargets = Intersect(currentCurrentTargetLists);

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


            #region closest target

            float minDist = Surface.GROUPING_SPACE * Surface.GROUPING_SPACE + 1;
            float tempDist;
            IGestureListener closestTarget = null;

            // Find closest target
            foreach (IGestureListener target in m_currentTargets)
            {
                tempDist = target.GetSquareDistance(m_centroidX, m_centroidY);
                if (tempDist < minDist)
                {
                    minDist = tempDist;
                    closestTarget = target;
                }
            }

            // Update entering/current/leaving
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

            #endregion


            if (newCurrent | newIntersect | newUnion | newClosestCur | newClosestEnt | newClosestLvn |
                newInitial | newFinal | newEntering | newLeaving)
            {
                m_groupGRManager.UpdateGGRHandlers(newInitial, newFinal, newEntering, newCurrent, newLeaving,
                    newIntersect, newUnion, newClosestEnt, newClosestCur, newClosestLvn);

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

        // Return the distance between a given point and the nearest current finger.
        internal float SquareMinDist(TuioCursor cursor)
        {
            float minDist = Surface.GROUPING_SPACE * Surface.GROUPING_SPACE + 1;
            foreach (Trace trace in m_traces)
            {
                if (trace.Alive)
                    minDist = Math.Min(trace.Last.SquareDistance(cursor), minDist);
            }

            return minDist;
        }
        internal bool AcceptNewCursor(TuioCursor cursor)
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
        private string Dump()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.Append("Group " + m_id + "\nInitial targets:");
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

            return sb.ToString();

            //    float x, y;
            //    Console.WriteLine("-------------------------------------------");
            //    Console.WriteLine("Group {0}", m_id);
            //    Console.WriteLine("-------------------------------------------");
            //    foreach (Trace trc in m_traces)
            //    {
            //        Console.WriteLine("Trace {0} normalized history:", trc.SessionId);
            //        foreach (TuioCursor cur in trc.History)
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
