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
        private GroupGRManager m_groupGRManager;
        private readonly bool m_intersectionMode;
        private static int s_counter = 0;
		private int m_id;
        private List<Trace> m_traces;
		private float m_x0, m_y0, m_t0;
        private int m_nOfAliveTraces;
        private bool m_alive;

        // The maximum time in milliseconds between cursors to determine the group's INITIAL and FINAL list
        private const int GROUPING_TIMESPAN = 600; // to put in Surface
        private List<Trace> m_startingSequence;
        private List<Trace> m_endingSequence;

        private List<IGestureListener> /*m_targets,*/ m_initialTargets, m_newInitialTargets, m_finalTargets;
        private List<IGestureListener> m_intersectionTargets, m_unionTargets;
        private List<IGestureListener> m_enteringTargets, m_currentTargets, m_leavingTargets;


        private SimmetricDoubleDictionary<Trace, float> m_traceSpaceCouplingTable;
        private SimmetricDoubleDictionary<Trace, long> m_traceTimeCouplingTable;



        public GroupGRManager GRManager { get { return m_groupGRManager; } }

        public List<IGestureListener> IntialTargets { get { return m_initialTargets; } }
        public List<IGestureListener> NewIntialTargets { get { return m_newInitialTargets; } }
        public List<IGestureListener> FinalTargets { get { return m_finalTargets; } }
        public List<IGestureListener> IntersectionTargets { get { return m_intersectionTargets; } }
        public List<IGestureListener> UnionTargets { get { return m_unionTargets; } }
        public List<IGestureListener> EnteringTargets { get { return m_enteringTargets; } }
        public List<IGestureListener> CurrentTargets { get { return m_currentTargets; } }
        public List<IGestureListener> LeavingTargets { get { return m_leavingTargets; } }


        //public List<IGestureListener> Targets {get { return m_targets; } }
        public List<Trace> Traces { get { return m_traces; } }

        public bool Alive { get { return m_alive; } }

        public Group(bool intersectionMode, GRRegistry registry)
		{
            m_intersectionMode = intersectionMode;
            m_groupGRManager = new GroupGRManager(this, registry);

            m_id = s_counter++;
            //m_targets = null;
            m_traces = new List<Trace>();
            m_nOfAliveTraces = 0;
            m_alive = true;

            m_startingSequence = new List<Trace>();
            m_endingSequence = new List<Trace>();

            m_traceSpaceCouplingTable = new SimmetricDoubleDictionary<Trace, float>(10);
            m_traceTimeCouplingTable = new SimmetricDoubleDictionary<Trace, long>(10);

            //m_targets = new List<IGestureListener>();
            m_initialTargets = new List<IGestureListener>();
            m_newInitialTargets = new List<IGestureListener>();
            m_finalTargets = new List<IGestureListener>();
            m_enteringTargets = new List<IGestureListener>();
            m_currentTargets = new List<IGestureListener>();
            m_leavingTargets = new List<IGestureListener>();
            m_intersectionTargets = new List<IGestureListener>();
            m_unionTargets = new List<IGestureListener>();
		}

        public void StartTrace(Trace trace)
        {
            m_traces.Add(trace);
            m_nOfAliveTraces++;

            TuioCursor firstPoint = trace[0];
            long firstPointTimeStamp = firstPoint.TimeStamp;
            
            // set reference point of the group
            if (m_traces.Count == 1)
            {
                m_x0 = firstPoint.XPos;
                m_y0 = firstPoint.YPos;
                m_t0 = firstPointTimeStamp;
            }

            // update starting sequence
            if (firstPointTimeStamp - m_t0 < GROUPING_TIMESPAN)
                m_startingSequence.Add(trace);

            // compute and index trace-couplings
            foreach (Trace t in m_traces)
                if (t != trace)
                {
                    TuioCursor current = t[t.Count - 1];
                    float dx = firstPoint.XPos - current.XPos;
                    float dy = firstPoint.YPos - current.YPos;
                    m_traceSpaceCouplingTable[t, trace] = dx * dx + dy * dy;
                    m_traceTimeCouplingTable[t, trace] = firstPointTimeStamp - current.TimeStamp;
                }
        }
		
        public void UpdateTrace(Trace trace)
        {
            // nada?
		}

        public void EndTrace(Trace trace)
        {
            m_nOfAliveTraces--;
            m_alive = m_nOfAliveTraces > 0;

            // update ending sequence
            m_endingSequence.Add(trace);

            if (!m_alive)
            {
                // remove not-recently-dead traces from endingSequence
                long lastTimeStamp = trace.Last.TimeStamp;
                int i;
                for (i = 0; 
                    i < m_endingSequence.Count - 1 && 
                     lastTimeStamp - m_endingSequence[i].Last.TimeStamp > GROUPING_TIMESPAN;
                    i++);
                m_endingSequence.RemoveRange(0, i);
            }
        }

        public bool Process(Trace trace)
        {
            return m_groupGRManager.Process(trace);

            //if (!Alive)
            //   Console.WriteLine(Dump());
        }

        // Return the distance between a given point and the nearest current finger.
        public float SquareMinDist(float x, float y)
        {
            float dist;
            float mindist = 999;
            foreach (Trace trace in m_traces)
            {
                if (!trace.Alive)
                    break;
                TuioCursor current = trace[trace.Count - 1];
                float dx = x - current.XPos;
                float dy = y - current.YPos;
                dist = dx * dx + dy * dy;
                mindist = Math.Min(dist, mindist);
            }
            return mindist;
        }

        // returns a normalized copy of the given TuioCursor 
        private TuioCursor Normalize(TuioCursor cur)
        {
            cur.XPos -= m_x0;
            cur.YPos -= m_y0;
            return cur;
        }
        
        public void UpdateTargets(Trace trace)
        {           
            bool newInitial     = false;
            bool newFinal       = false;
            bool newEntering    = false;
            bool newCurrent     = false;
            bool newLeaving     = false;
            bool newUnion       = false;
            bool newIntersect   = false;


            #region ADD

            if (trace.State == Trace.States.ADD)
            {
                List<IGestureListener> traceInitialTargets = trace.InitialTargets;

                if (m_traces.Count == 1) // group has just been created
                {
                    // The first added trace determine INITIAL, NEWINITIAL and INTERSECTION lists
                    m_initialTargets.AddRange(traceInitialTargets);
                    m_newInitialTargets.AddRange(traceInitialTargets);
                    m_intersectionTargets.AddRange(traceInitialTargets);
                    foreach (IGestureListener lgrTarget in traceInitialTargets)
                        m_groupGRManager.AddLocalTarget(lgrTarget);
                    newInitial = true;
                }
                else
                {
                    if (m_newInitialTargets.Count > 0)
                    {
                        m_newInitialTargets.Clear();
                        newInitial = true;
                    }
                    if (m_startingSequence.Contains(trace))
                    {
                        if (m_intersectionMode)
                        {
                            // INITIAL list is the intersection of the recently born traces' INITIAL lists.
                            m_leavingTargets.Clear();
                            foreach (IGestureListener target in m_initialTargets)
                            {
                                if (!traceInitialTargets.Contains(target))
                                {
                                    m_leavingTargets.Add(target);
                                }
                            }
                            foreach (IGestureListener target in m_leavingTargets)
                            {
                                m_initialTargets.Remove(target);
                                m_currentTargets.Remove(target);
                                if (m_intersectionTargets.Remove(target))
                                    m_groupGRManager.RemoveLocalTarget(target);
                            }
                            if (m_leavingTargets.Count > 0)
                            {
                                newInitial = true;
                                newCurrent = true;
                                newLeaving = true;
                                newIntersect = true;
                            }
                        }
                        else
                        {
                            // INITIAL list is the union of the recently born traces' INITIAL lists.
                            foreach (IGestureListener target in traceInitialTargets)
                            {
                                if (!m_initialTargets.Contains(target))
                                {
                                    m_initialTargets.Add(target);
                                    m_newInitialTargets.Add(target);
                                    newInitial = true;
                                }
                            }
                        }

                        //Console.WriteLine("INITIAL LIST CREATED (OR UPDATED)");
                    }
                }
            }
            #endregion


            #region UPDATE
            List<IGestureListener> traceEnteringTargets = trace.EnteringTargets;
            List<IGestureListener> traceLeavingTargets = trace.LeavingTargets;

            if (m_newInitialTargets.Count > 0 && trace.State != (int)TuioCursor.States.add)
            {
                m_newInitialTargets.Clear();
                newInitial = true;
            }

            if (m_enteringTargets.Count != 0)
                newEntering = true;

            m_enteringTargets.Clear();

            foreach (IGestureListener traceEnteringTarget in traceEnteringTargets)
            {
                // in this way the group's union list is the union of the traces' union lists
                if (!m_unionTargets.Contains(traceEnteringTarget))
                {
                    m_unionTargets.Add(traceEnteringTarget);
                    newUnion = true;
                }

                if (!m_currentTargets.Contains(traceEnteringTarget))
                {
                    bool trueForAll = true;
                    if (m_intersectionMode)
                    {
                        foreach (Trace t in m_traces)
                            if (!t.CurrentTargets.Contains(traceEnteringTarget))
                            {
                                trueForAll = false;
                                break;
                            }
                    }
                    if (trueForAll)
                    {
                        m_enteringTargets.Add(traceEnteringTarget);
                        m_currentTargets.Add(traceEnteringTarget);
                        newEntering = true;
                        newCurrent = true;


                        //// in this way the group's union list is the intersection of the traces' union lists
                        //if (!m_unionTargets.Contains(traceEnteringTarget))
                        //    m_unionTargets.Add(traceEnteringTarget);
                    }
                }
            }

            // Note: if the following condition is satisfied then traceLeavingTargets is empty
            if (m_leavingTargets.Count > 0 && trace.State != (int)TuioCursor.States.add)
            {
                m_leavingTargets.Clear();
                newLeaving = true;
            }
            foreach (IGestureListener traceLeavingTarget in traceLeavingTargets)
            {
                if (m_currentTargets.Contains(traceLeavingTarget))
                {
                    if (m_intersectionTargets.Remove(traceLeavingTarget))
                        m_groupGRManager.RemoveLocalTarget(traceLeavingTarget);
                    newIntersect = true;

                    bool falseForAll = true;
                    if (!m_intersectionMode)
                    {
                        foreach (Trace t in m_traces)
                            if (t.CurrentTargets.Contains(traceLeavingTarget))
                            {
                                falseForAll = false;
                                break;
                            }
                    }
                    if (falseForAll)
                    {
                        m_currentTargets.Remove(traceLeavingTarget);
                        m_leavingTargets.Add(traceLeavingTarget);
                        newCurrent = true;
                        newLeaving = true;

                        //Console.WriteLine("Removed {0} from CURRENT", traceLeavingTarget.ToString());
                    }
                }
            } 
            #endregion


            #region DEL

            if (trace.State == Trace.States.DEL)
            {
                List<IGestureListener> traceFinalTargets = trace.FinalTargets;

                if (!m_alive)
                {
                    m_finalTargets.AddRange(traceFinalTargets);

                    for (int i = 0; i < m_endingSequence.Count - 1; i++)
                    {
                        if (m_intersectionMode)
                        {
                            // FINAL list if the instersection of the recently dead traces' FINAL lists.
                            m_finalTargets.RemoveAll(delegate(IGestureListener finalTarget)
                            {
                                return !m_endingSequence[i].FinalTargets.Contains(finalTarget);
                            });
                        }
                        else
                        {
                            // FINAL list if the union of the recently dead traces' FINAL lists.
                            foreach (IGestureListener target in m_endingSequence[i].FinalTargets)
                            {
                                if (!m_finalTargets.Contains(target))
                                    m_finalTargets.Add(target);
                            }
                        }
                    }

                    newFinal = true;
                }
            }

            #endregion


            if (newCurrent | newIntersect | newUnion | newInitial | newFinal | newEntering | newLeaving)
            {
                m_groupGRManager.UpdateGGRHandlers(newInitial, newFinal, newEntering, newCurrent, newLeaving, newIntersect, newUnion);
                //Console.WriteLine(Dump());
            }
        }

        public bool AcceptNewTrace(float x, float y)
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

        public string Dump()
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
    }
}
