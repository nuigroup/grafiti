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
    internal class GroupGRManager
    {
        #region Declarations
        /// <summary>
        /// The relative group to process.
        /// </summary>
        private readonly Group m_group;

        /// <summary>
        /// Current priority number. It determines the current GRs to be processed.
        /// </summary>
        private int m_currentPN;

        /// <summary>
        /// List of priority numbers, starting from the current one, that is, it is relative to unarmed GRs.
        /// </summary>
        private List<int> m_pns;

        /// <summary>
        /// Flag indicating if there are some processing GRs.
        /// </summary>
        private bool m_processing;

        /// <summary>
        /// Armed (raising events) and processing GRs
        /// </summary>
        private List<GestureRecognizer> m_armedGRs;

        /// <summary>
        /// Unarmed and active (processing or waiting to be armed) LGRs
        /// </summary>
        private Dictionary<int, List<LocalGestureRecognizer>> m_unarmedLGRs;
        /// <summary>
        /// Unarmed and active (processing or waiting to be armed) GGRs
        /// </summary>
        private Dictionary<int, List<GlobalGestureRecognizer>> m_unarmedGGRs;


        // Tables used to instantiate new GRs.
        private DoubleDictionary<Type, object, GlobalGestureRecognizer> m_ggrInstanceTable;
        private DoubleDictionary<Type, IGestureListener, Dictionary<object, LocalGestureRecognizer>> m_lgrInstanceTable;
        private Dictionary<IGestureListener, List<GestureRecognizer>> m_targetLgrInstancesTable;

        // Auxiliary temp lists.
        private List<ToRemoveGR> m_toRemove = new List<ToRemoveGR>();
        private List<GestureRecognizer> m_succedingGRs = new List<GestureRecognizer>();

        private bool m_firstCycle = true;

        internal bool Processing { get { return m_processing; } }
        #endregion

        #region Constructor
        internal GroupGRManager(Group group)
        {
            m_group = group;

            m_ggrInstanceTable = new DoubleDictionary<Type, object, GlobalGestureRecognizer>();
            m_lgrInstanceTable = new DoubleDictionary<Type, IGestureListener, Dictionary<object, LocalGestureRecognizer>>();
            m_targetLgrInstancesTable = new Dictionary<IGestureListener, List<GestureRecognizer>>();

            m_currentPN = 0;
            m_pns = new List<int>();
            m_processing = true;

            m_armedGRs = new List<GestureRecognizer>();
            m_unarmedLGRs = new Dictionary<int, List<LocalGestureRecognizer>>();
            m_unarmedGGRs = new Dictionary<int, List<GlobalGestureRecognizer>>();

            GestureEventRegistry.Subscribe(this);
        }      
        #endregion

        #region Called by Group
        internal void AddLocalTarget(IGestureListener localTarget)
        {
            if (!m_targetLgrInstancesTable.ContainsKey(localTarget))
                m_targetLgrInstancesTable[localTarget] = new List<GestureRecognizer>();

            InitializeLGRs(localTarget);
        }
        
        internal void RemoveLocalTarget(IGestureListener localTarget)
        {
            if (m_targetLgrInstancesTable.ContainsKey(localTarget))
            {
                foreach (GestureRecognizer lgr in m_targetLgrInstancesTable[localTarget])
                {
                    RemoveUnarmedGR(new ToRemoveGR(lgr, true));
                    RemoveArmedGR(lgr);

                    m_lgrInstanceTable.Remove(lgr.GetType(), localTarget);

                    // reset exclusive local target if this is removed
                    if (localTarget == m_group.ExclusiveLocalTarget)
                        ResetExclusiveLocalTarget();
                }
                m_targetLgrInstancesTable.Remove(localTarget);

                //Console.WriteLine("Removed local target {0}", localTarget);
            }
        }

        internal void UpdateGGRHandlers(
            bool initial, bool final, bool entering, bool current, bool leaving,
            bool intersect, bool union, 
            bool newClosestEnt, bool newClosestCur, bool newClosestLvn, bool newClosestIni, bool newClosestFin)
        {
            foreach (GestureRecognizer gr in m_armedGRs)
                if (gr is GlobalGestureRecognizer)
                    ((GlobalGestureRecognizer)gr).UpdateHandlers1(initial, final, entering, current, leaving, 
                        intersect, union, newClosestEnt, newClosestCur, newClosestLvn, newClosestIni, newClosestFin);
            
            foreach(int pn in m_pns)
                foreach (GlobalGestureRecognizer ggr in m_unarmedGGRs[pn])
                    ggr.UpdateHandlers1(initial, final, entering, current, leaving,
                        intersect, union, newClosestEnt, newClosestCur, newClosestLvn, newClosestIni, newClosestFin);
        }

        /// <summary>
        /// Coordinately process the active GRs with the modified traces. If the group is being terminated then
        /// the GRs are not processed, but instead they're iterated in the same order they were during the last call:
        /// succeding GRs that were waiting for higher priority GRs are armed until an exclusive is found.
        /// </summary>
        /// <param name="traces">The traces that have been modified during last refresh cycle.</param>
        /// <param name="groupIsActive">true iff the group is not being terminated.</param>
        /// <returns></returns>
        internal bool Process(List<Trace> traces, bool groupIsActive)
        {
            if (!m_processing)
                return false;

            #region Armed and processing GRs
            if (groupIsActive)
            {
                m_toRemove.Clear();
                foreach (GestureRecognizer gr in m_armedGRs)
                {
                    gr.Process1(traces);
                    if (!gr.Processing)
                        m_toRemove.Add(new ToRemoveGR(gr, true));
                }
                foreach (ToRemoveGR toRemoveGr in m_toRemove)
                    RemoveArmedGR(toRemoveGr.gr);
            }
            #endregion

            #region Unarmed GRs

            bool someIsRecognizing = false;
            m_toRemove.Clear();

            if(!(m_group.OnZControl) && groupIsActive && (Settings.SortLGRsDynamically || m_firstCycle))
                SortUnarmedLGRs();

            if (m_firstCycle)
                m_firstCycle = false;

            int pn;
            for (int pnIdx = 0; pnIdx < m_pns.Count; ) // iterate through the ordered list of priority numbers
            {
                pn = m_pns[pnIdx];

                m_succedingGRs.Clear();

                #region process GRs, find succeding ones and remove unnecessary ones
                foreach (GestureRecognizer gr in AppendGRs(m_unarmedGGRs[pn], m_unarmedLGRs[pn], Settings.PRECEDENCE_GGRS_OVER_LGRS))
                {
                    // If the GR has been added in the toRemove list (by another GR with precedence over it) then skip it
                    if (gr is LocalGestureRecognizer)
                    {
                        bool inToRemoveList = false;
                        foreach(ToRemoveGR toRemoveGR in m_toRemove)
                            if(toRemoveGR.gr == gr)
                            {
                                inToRemoveList = true;
                                break;
                            }
                        if (inToRemoveList)
                            continue;
                    }

                    if (gr.Processing && groupIsActive)
                        gr.Process1(traces);

                    if (!gr.Recognizing)
                    {
                        if (pn == m_currentPN || !groupIsActive)
                        {
                            if (gr.Successful)
                            {
                                m_succedingGRs.Add(gr);
                                m_toRemove.Add(new ToRemoveGR(gr, false));
                            }
                            else
                            {
                                m_toRemove.Add(new ToRemoveGR(gr, true));
                            }
                        }
                        else
                            if (!gr.Successful)
                                m_toRemove.Add(new ToRemoveGR(gr, true));

                        if (gr is LocalGestureRecognizer)
                            foreach(GestureRecognizer eqGR in FindAllOtherEquivalentLGRs((LocalGestureRecognizer)gr))
                                m_toRemove.Add(new ToRemoveGR(eqGR, true));
                    }
                    else
                        if (someIsRecognizing == false)
                            someIsRecognizing = true;
                }
                
                foreach (ToRemoveGR toRemoveGR in m_toRemove)
                    RemoveUnarmedGR(toRemoveGR);
                m_toRemove.Clear();  
                #endregion

                // At this point the unarmed list contains the active and non successfull GRs.
                // Some unarmed, i.e. the succeding ones, are in the successfull list.

                // Process succeding GRs
                if (m_succedingGRs.Count > 0)
                {
                    #region sort succeding GRs by confidence
                    {
                        // sort m_succedingGRs in order of decreasing confidence.
                        // if two grs have the same confidence, the original order is unchanged,
                        // such that the order by increasing distance to its target is preserved.

                        // list of sorted GRs, as it is filled, the source one is emptied
                        List<GestureRecognizer> sortedSucceding = new List<GestureRecognizer>();

                        // to optimize, considering 1 as the most used confidence, fill the list
                        // with the relative GRs linearly
                        for (int i = 0; i < m_succedingGRs.Count; )
                        {
                            if (m_succedingGRs[i].Confidence == 1)
                            {
                                sortedSucceding.Add(m_succedingGRs[i]);
                                m_succedingGRs.RemoveAt(i);
                            }
                            else
                                i++;
                        }

                        // selection sort the remaining
                        float maxP, currentP;
                        int idxMaxP;
                        while (m_succedingGRs.Count > 0)
                        {
                            maxP = m_succedingGRs[0].Confidence;
                            idxMaxP = 0;
                            for (int i = 1; i < m_succedingGRs.Count; i++)
                            {
                                currentP = m_succedingGRs[i].Confidence;
                                if (currentP > maxP)
                                {
                                    maxP = currentP;
                                    idxMaxP = i;
                                }

                            }
                            sortedSucceding.Add(m_succedingGRs[idxMaxP]);
                            m_succedingGRs.RemoveAt(idxMaxP);
                        }
                        m_succedingGRs = sortedSucceding;
                    }
                    #endregion

                    #region set exclusive target and remove LGRs with different target
                    if (m_group.ExclusiveLocalTarget == null)
                    {
                        // set exclusive local target (target of the first LGRs encountered in the succeding list)
                        int idxFirstLGR;
                        for (int i = 0; i < m_succedingGRs.Count; i++)
                            if (m_succedingGRs[i] is LocalGestureRecognizer)
                            {
                                idxFirstLGR = i;
                                m_group.ExclusiveLocalTarget = ((LocalGestureRecognizer)m_succedingGRs[i]).Target;

                                // remove from succeding list all LGRs (in the next positions) with different target
                                for (int j = i + 1; j < m_succedingGRs.Count; )
                                {
                                    if (m_succedingGRs[j] is LocalGestureRecognizer &&
                                        ((LocalGestureRecognizer)m_succedingGRs[j]).Target != m_group.ExclusiveLocalTarget)
                                    {
                                        m_succedingGRs[j].OnTerminating1();
                                        m_succedingGRs.RemoveAt(j);
                                    }
                                    else
                                        j++;
                                }

                                // append to toRemove list not-succeding (unarmed) LGRs with different targets
                                foreach (GestureRecognizer gr in FindAllLGRsWithDifferentTarget(m_group.ExclusiveLocalTarget))
                                    m_toRemove.Add(new ToRemoveGR(gr, true));
                                foreach (ToRemoveGR toRemoveGR in m_toRemove)
                                    RemoveUnarmedGR(toRemoveGR);
                                m_toRemove.Clear();

                                break;
                            }
                    }
                    #endregion

                    #region arm and process events iteratively until an exclusive is found
                    foreach (GestureRecognizer candidate in m_succedingGRs)
                    {
                        candidate.Armed = true;
                        candidate.RaisePendlingEvents();

                        if (candidate.Processing)
                            m_armedGRs.Add(candidate);

                        if (candidate.Configuration.Exclusive)
                        {
                            ClearAllUnarmed(); // clear all unarmed
                            if (Settings.EXCLUSIVE_BLOCK_INTERPRETING)
                                ClearNonNegativePNArmedBut(candidate); // clear armed with nonnegative PN but the winner

                            // Set max number of fingers allowed
                            m_group.MaxNumberOfActiveTraces = candidate.MaxNumberOfFingersAllowed;

                            // If a GGR wins exclusively and no LGR has set the exclusive local target,
                            // then reset it in order to clear LGRtargetList
                            if (candidate is GlobalGestureRecognizer && m_group.ExclusiveLocalTarget == null)
                                ResetExclusiveLocalTarget();

                            // Terminate successive candidates
                            for (int i = m_succedingGRs.IndexOf(candidate) + 1; i < m_succedingGRs.Count; i++)
                                m_succedingGRs[i].OnTerminating1();
                            break;
                        }
                    }
                    #endregion
                }

                // If all current GR failed in the recognition then pass to the next priority number
                if (!someIsRecognizing && m_pns.Count > 0)
                    m_currentPN = m_pns[0];
                else
                    pnIdx++;
            }
            #endregion

            if (m_pns.Count == 0 && m_armedGRs.Count == 0)
            {
                Unsubscribe();
                m_processing = false;
            }

            return m_processing;
        }

        internal void Terminate()
        {
            // Process
            Process(null, false);

            // Clear lists and terminate GRs
            ClearAllUnarmed();
            ClearAllArmed();

            // Unsubscribe from GRRegistry
            Unsubscribe();
        }
        #endregion

        #region Called by GRRegistry
        /// <summary>
        /// Called by GRRegistry. Accordingly to ggrInfos, updates the GGRs already present 
        /// by registering new handlers, or creates new instances with handlers.
        /// </summary>
        /// <param name="ggrInfos">The informations of the registrations of the GGRs.</param>
        internal void InitializeGGRs(List<GestureEventRegistry.RegistrationInfo> ggrInfos)
        {
            Type grType;
            object grParam;
            GlobalGestureRecognizer ggr;

            foreach (GestureEventRegistry.RegistrationInfo ggrInfo in ggrInfos)
            {
                grType = ggrInfo.GRType;
                grParam = ggrInfo.GRConfiguration;
                if (!m_ggrInstanceTable.TryGetValue(grType, grParam, out ggr))
                {
                    ggr = (GlobalGestureRecognizer)grType.GetConstructor(new Type[] { typeof(GRConfiguration) }).Invoke(new Object[] { grParam });
                    ggr.Group = m_group;
                    ggr.PriorityNumber = ggrInfo.PriorityNumber;
                    m_ggrInstanceTable[grType, grParam] = ggr;
                    AddUnarmedGR(ggr);
                }
                // Add event handler
                ggr.AddHandler(ggrInfo.Event, ggrInfo.Handler);
            }
        }
        internal bool UpdateGGR(GestureEventRegistry.RegistrationInfo ggrInfo)
        {
            Type grType = ggrInfo.GRType;
            object grParam = ggrInfo.GRConfiguration;
            GlobalGestureRecognizer ggr;

            // If the GR is registered then update it
            if (m_ggrInstanceTable.TryGetValue(grType, grParam, out ggr))
            {
                // Add event handler
                ggr.AddHandler(ggrInfo.Event, ggrInfo.Handler);
                return true;
            }
            return false;            
        } 
        #endregion

        #region Private members
        private void InitializeLGRs(IGestureListener localTarget)
        {
            Type grType;
            object grConf;
            LocalGestureRecognizer lgr;
            Dictionary<object, LocalGestureRecognizer> lgrDict;

            foreach (GestureEventRegistry.RegistrationInfo lgrInfo in GestureEventRegistry.LGRRegistry)
            {
                if (lgrInfo.Handler.Target == localTarget)
                {
                    grType = lgrInfo.GRType;
                    grConf = lgrInfo.GRConfiguration;

                    if (!m_lgrInstanceTable.TryGetValue(grType, localTarget, out lgrDict))
                    {
                        lgrDict = new Dictionary<object, LocalGestureRecognizer>();
                        m_lgrInstanceTable[grType, localTarget] = lgrDict;
                    }

                    if (!lgrDict.TryGetValue(grConf, out lgr))
                    {
                        lgr = (LocalGestureRecognizer)grType.GetConstructor(new Type[] { typeof(GRConfiguration) }).Invoke(new Object[] { grConf });
                        lgr.Group = m_group;
                        lgr.PriorityNumber = lgrInfo.PriorityNumber;
                        lgr.Target = (IGestureListener)lgrInfo.Handler.Target;
                        lgrDict[grConf] = lgr;
                        m_targetLgrInstancesTable[localTarget].Add(lgr);
                        AddUnarmedGR(lgr);
                    }

                    lgr.AddHandler(lgrInfo.Event, lgrInfo.Handler);
                }
            }
        }

        private void ResetExclusiveLocalTarget()
        {
            m_group.ExclusiveLocalTarget = null;
        }

        private IEnumerable<GestureRecognizer> AppendGRs(List<GlobalGestureRecognizer> ggrs, List<LocalGestureRecognizer> lgrs, bool firstGlobal)
        {
            List<GlobalGestureRecognizer>.Enumerator ggrEnum = ggrs.GetEnumerator();
            List<LocalGestureRecognizer>.Enumerator lgrEnum = lgrs.GetEnumerator();
            if (firstGlobal)
            {
                while (ggrEnum.MoveNext()) yield return (GestureRecognizer)ggrEnum.Current;
                while (lgrEnum.MoveNext()) yield return (GestureRecognizer)lgrEnum.Current;
            }
            else
            {
                while (lgrEnum.MoveNext()) yield return (GestureRecognizer)lgrEnum.Current;
                while (ggrEnum.MoveNext()) yield return (GestureRecognizer)ggrEnum.Current;
            }
        }

        private IEnumerable<GestureRecognizer> FindAllOtherEquivalentLGRs(LocalGestureRecognizer lgr)
        {
            int lgrPN = lgr.PriorityNumber;
            Type grType = lgr.GetType();
            GRConfiguration conf = lgr.Configuration;
            foreach (int pn in m_pns)
                if (pn >= lgrPN)
                    foreach (LocalGestureRecognizer g in m_unarmedLGRs[pn])
                        if (g != lgr && g.GetType() == grType && g.Configuration == conf)
                            yield return g;
        }

        private IEnumerable<GestureRecognizer> FindAllLGRsWithDifferentTarget(IGestureListener target)
        {
            foreach (int pn in m_pns)
                foreach (LocalGestureRecognizer lgr in m_unarmedLGRs[pn])
                    if (lgr.Target != target)
                        yield return lgr;
        }

        /// <summary>
        /// Sort unarmed LGRs by (increasing) distance from the group's centroid to their target
        /// </summary>
        private void SortUnarmedLGRs()
        {
            foreach (int pn in m_pns)
            {
                foreach (LocalGestureRecognizer lgr in m_unarmedLGRs[pn])
                    lgr.SquareDistanceFromTarget = ((ITangibleGestureListener)(lgr.Target)).GetSquareDistance(m_group.ActiveCentroidX, m_group.ActiveCentroidY);
                m_unarmedLGRs[pn].Sort(new Comparison<LocalGestureRecognizer>(
                delegate(LocalGestureRecognizer a, LocalGestureRecognizer b)
                {
                    return (int)((a.SquareDistanceFromTarget - b.SquareDistanceFromTarget) * 10000000);
                }
                ));
            }
        }

        

        private void AddUnarmedGR(GestureRecognizer gr)
        {
            int pn = gr.PriorityNumber;
            if (!(m_pns.Contains(pn)))
            {
                m_pns.Add(pn);
                m_pns.Sort();
                m_currentPN = m_pns[0];
                m_unarmedLGRs[pn] = new List<LocalGestureRecognizer>();
                m_unarmedGGRs[pn] = new List<GlobalGestureRecognizer>();
            }
            if (gr is LocalGestureRecognizer)
                m_unarmedLGRs[pn].Add((LocalGestureRecognizer)gr);
            else
                m_unarmedGGRs[pn].Add((GlobalGestureRecognizer)gr);
        }

        private struct ToRemoveGR
        {
            public GestureRecognizer gr;
            public bool terminating;
            public ToRemoveGR(GestureRecognizer gr, bool terminating)
            {
                this.gr = gr;
                this.terminating = terminating;
            }
        }

        private void RemoveUnarmedGR(ToRemoveGR toRemoveGR)
        {
            GestureRecognizer gr = toRemoveGR.gr;
            int pn = gr.PriorityNumber;

            bool removed = false;

            if (m_pns.Contains(pn))
            {
                if (gr is LocalGestureRecognizer)
                    removed = m_unarmedLGRs[pn].Remove((LocalGestureRecognizer)gr);
                else if (gr is GlobalGestureRecognizer)
                    removed = m_unarmedGGRs[pn].Remove((GlobalGestureRecognizer)gr);

                if (m_unarmedGGRs[pn].Count == 0 && m_unarmedLGRs[pn].Count == 0)
                {
                    m_unarmedLGRs.Remove(pn);
                    m_unarmedGGRs.Remove(pn);
                    m_pns.Remove(pn);
                }
            }

            if (removed && toRemoveGR.terminating)
                gr.OnTerminating1();
        }

        private void RemoveArmedGR(GestureRecognizer gr)
        {
            if (m_armedGRs.Remove(gr))
                gr.OnTerminating1();
        }

        private void ClearAllUnarmed()
        {
            // Terminate unarmed GRs
            Dictionary<int, List<GlobalGestureRecognizer>>.ValueCollection.Enumerator unarmedGGRsE;
            Dictionary<int, List<LocalGestureRecognizer>>.ValueCollection.Enumerator unarmedLGRsE;
            unarmedGGRsE = m_unarmedGGRs.Values.GetEnumerator();
            unarmedLGRsE = m_unarmedLGRs.Values.GetEnumerator();
            while (unarmedGGRsE.MoveNext())
                foreach (GlobalGestureRecognizer ggr in unarmedGGRsE.Current)
                    ggr.OnTerminating1();
            while (unarmedLGRsE.MoveNext())
                foreach (LocalGestureRecognizer lgr in unarmedLGRsE.Current)
                    lgr.OnTerminating1();
            m_unarmedGGRs.Clear();
            m_unarmedLGRs.Clear();
            m_pns.Clear();
        }

        private void ClearAllArmed()
        {
            // Terminate armed GRs
            foreach (GestureRecognizer gr in m_armedGRs)
                gr.OnTerminating1();
            m_armedGRs.Clear();
        }

        private void ClearNonNegativePNArmedBut(GestureRecognizer ex)
        {
            m_armedGRs.RemoveAll(delegate (GestureRecognizer gr)
            {
                if (gr.PriorityNumber >= 0 && gr != ex)
                {
                    gr.OnTerminating1();
                    return true;
                }
                else
                    return false;
            });
        }


        private void Unsubscribe()
        {
            GestureEventRegistry.Unsubscribe(this);
        } 
        #endregion
    }
}