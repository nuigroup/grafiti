﻿/*
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
        /// Registry of GRs
        /// </summary>
        private readonly GestureEventRegistry m_grRegistry = GestureEventRegistry.Instance;

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
        private Dictionary<IGestureListener, List<GestureRecognizer>> m_targetLgrListTable;

        // Auxiliary temp lists.
        private List<GestureRecognizer> m_toRemove = new List<GestureRecognizer>();
        private List<GestureRecognizer> m_succedingGRs = new List<GestureRecognizer>();

        internal bool Processing { get { return m_processing; } } 
        #endregion

        #region Constructor
        internal GroupGRManager(Group group)
        {
            m_group = group;

            m_ggrInstanceTable = new DoubleDictionary<Type, object, GlobalGestureRecognizer>();
            m_lgrInstanceTable = new DoubleDictionary<Type, IGestureListener, Dictionary<object, LocalGestureRecognizer>>();
            m_targetLgrListTable = new Dictionary<IGestureListener, List<GestureRecognizer>>();

            m_currentPN = 0;
            m_pns = new List<int>();
            m_processing = true;

            m_armedGRs = new List<GestureRecognizer>();
            m_unarmedLGRs = new Dictionary<int, List<LocalGestureRecognizer>>();
            m_unarmedGGRs = new Dictionary<int, List<GlobalGestureRecognizer>>();

            m_grRegistry.Subscribe(this);
        }      
        #endregion

        #region Group methods
        internal void AddLocalTarget(IGestureListener localTarget)
        {
            if (!m_targetLgrListTable.ContainsKey(localTarget))
                m_targetLgrListTable[localTarget] = new List<GestureRecognizer>();

            AddOrUpdateLGRs(localTarget);
        }
        
        internal void RemoveLocalTarget(IGestureListener localTarget)
        {
            if (m_targetLgrListTable.ContainsKey(localTarget))
            {
                foreach (GestureRecognizer lgr in m_targetLgrListTable[localTarget])
                {
                    // Notify lgr
                    ((LocalGestureRecognizer)lgr).OnTargetRemoved1();

                    // reset exclusive local target if this is removed
                    if (localTarget == m_group.ExclusiveLocalTarget)
                        ResetExclusiveLocalTarget();
                    RemoveUnarmedGR(lgr);
                    m_armedGRs.Remove(lgr);
                    m_lgrInstanceTable.Remove(lgr.GetType(), localTarget);
                }
                m_targetLgrListTable.Remove(localTarget);

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

        internal bool Process(List<Trace> traces, bool terminating)
        {
            if (!m_processing)
                return false;

            #region Armed and processing GRs
            if (!terminating)
            {
                m_toRemove.Clear();
                foreach (GestureRecognizer gr in m_armedGRs)
                {
                    gr.Process1(traces);
                    if (!gr.Processing)
                        m_toRemove.Add(gr);
                }
                foreach (GestureRecognizer gr in m_toRemove)
                    m_armedGRs.Remove(gr);
            }
            #endregion

            #region Unarmed GRs

            bool someIsRecognizing = false;
            bool exclusiveWinner = false;
            m_toRemove.Clear();

            if(!(m_group.OnSingleGUIControl) && !terminating)
                SortUnarmedLGRs();

            int pn;
            for (int pnIdx = 0; pnIdx < m_pns.Count; ) // iterate through the ordered list of priority numbers
            {
                pn = m_pns[pnIdx];

                m_succedingGRs.Clear();

                #region process GRs, find successfuls and remove unnecessary ones
                foreach (GestureRecognizer gr in AppendGRs(m_unarmedGGRs[pn], m_unarmedLGRs[pn], true))
                {
                    if (gr is LocalGestureRecognizer && m_toRemove.Contains(gr))
                        continue;

                    if (gr.Processing && !terminating)
                        gr.Process1(traces);

                    if (!gr.Recognizing)
                    {
                        if (pn == m_currentPN || terminating)
                        {
                            if (gr.Successful)
                                m_succedingGRs.Add(gr);
                            m_toRemove.Add(gr);
                        }
                        else
                            if (!gr.Successful)
                                m_toRemove.Add(gr);

                        if (gr is LocalGestureRecognizer)
                            m_toRemove.AddRange(FindAllOtherEquivalentLGRs((LocalGestureRecognizer)gr));
                    }
                    else
                        if (someIsRecognizing == false)
                            someIsRecognizing = true;
                }
                #endregion

                // Process succeding GRs
                if (m_succedingGRs.Count > 0)
                {
                    #region sort succeding GRs by probability of success
                    {
                        // sort m_succedingGRs in order of decreasing probability of success.
                        // if two grs have the same probability, the original order is unchanged,
                        // such that the order by increasing distance to its target is preserved.

                        // list of sorted GRs, as it is filled, the source one is emptied
                        List<GestureRecognizer> sortedSucceding = new List<GestureRecognizer>();

                        // to optimize, considering 1 as the most used probability, fill the list
                        // with the relative GRs linearly
                        for (int i = 0; i < m_succedingGRs.Count; )
                        {
                            if (m_succedingGRs[i].Probability == 1)
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
                            maxP = m_succedingGRs[0].Probability;
                            idxMaxP = 0;
                            for (int i = 1; i < m_succedingGRs.Count; i++)
                            {
                                currentP = m_succedingGRs[i].Probability;
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

                                // remove from succeding list all LGRs with different target
                                for (int j = i + 1; j < m_succedingGRs.Count; )
                                {
                                    if (m_succedingGRs[j] is LocalGestureRecognizer &&
                                        ((LocalGestureRecognizer)m_succedingGRs[j]).Target != m_group.ExclusiveLocalTarget)
                                        m_succedingGRs.RemoveAt(j);
                                    else
                                        j++;
                                }

                                // append for successive removal from unArmed LGRs
                                m_toRemove.AddRange(FindAllLGRsWithDifferentTarget(m_group.ExclusiveLocalTarget));

                                break;
                            }
                    }
                    #endregion

                    #region arm and process events iteratively until an exclusive is found
                    foreach (GestureRecognizer winning in m_succedingGRs)
                    {
                        winning.Armed = true;
                        winning.ProcessPendlingEvents();

                        if (winning.Processing)
                            m_armedGRs.Add(winning);
                        if (winning.Configurator.Exclusive)
                        {
                            exclusiveWinner = true;

                            // Set max number of fingers allowed
                            m_group.MaxNumberOfFingersAllowed = winning.MaxNumberOfFingersAllowed;

                            // If a GGR wins exclusively and no LGR has set the exclusive local target,
                            // then reset it in order to clear LGRtargetList
                            if (winning is GlobalGestureRecognizer && m_group.ExclusiveLocalTarget == null)
                                ResetExclusiveLocalTarget();
                            break;
                        }
                    }
                    #endregion
                }


                if (exclusiveWinner)
                    ClearUnarmed();
                else
                    foreach (GestureRecognizer gr in m_toRemove)
                        RemoveUnarmedGR(gr);

                m_toRemove.Clear();

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
            foreach (GestureRecognizer gr in m_armedGRs)
                gr.OnGroupRemoval1();
            Process(null, true);
            Unsubscribe();
        }
        #endregion

        #region GRRegistry methods
        /// <summary>
        /// Called by GRRegistry. Accordingly to ggrInfos, updates the GGRs already present 
        /// by registering new handlers, or creates new instances with handlers.
        /// </summary>
        /// <param name="ggrInfos">The informations of the registrations of the GGRs.</param>
        internal void AddOrUpdateGGRs(List<GestureEventRegistry.RegistrationInfo> ggrInfos)
        {
            Type grType;
            object grParam;
            GlobalGestureRecognizer ggr;

            foreach (GestureEventRegistry.RegistrationInfo ggrInfo in ggrInfos)
            {
                grType = ggrInfo.GRType;
                grParam = ggrInfo.GRConfigurator;
                if (!m_ggrInstanceTable.TryGetValue(grType, grParam, out ggr))
                {
                    ggr = (GlobalGestureRecognizer)grType.GetConstructor(new Type[] { typeof(GRConfigurator) }).Invoke(new Object[] { grParam });
                    ggr.Group = m_group;
                    ggr.PriorityNumber = ggrInfo.PriorityNumber;
                    m_ggrInstanceTable[grType, grParam] = ggr;
                    AddUnarmedGR(ggr);
                }
                // Add event handler
                ggr.AddHandler(ggrInfo.Event, ggrInfo.Handler);
            }
        } 
        #endregion

        #region Private members
        private void AddOrUpdateLGRs(IGestureListener localTarget)
        {
            Type grType;
            object grConf;
            LocalGestureRecognizer lgr;
            Dictionary<object, LocalGestureRecognizer> lgrDict;

            foreach (GestureEventRegistry.RegistrationInfo lgrInfo in m_grRegistry.LGRRegistry)
            {
                grType = lgrInfo.GRType;
                grConf = lgrInfo.GRConfigurator;

                if (lgrInfo.Handler.Target == localTarget)
                {
                    if (!m_lgrInstanceTable.TryGetValue(grType, localTarget, out lgrDict))
                    {
                        lgrDict = new Dictionary<object, LocalGestureRecognizer>();
                        m_lgrInstanceTable[grType, localTarget] = lgrDict;
                    }

                    if (!lgrDict.TryGetValue(grConf, out lgr))
                    {
                        lgr = (LocalGestureRecognizer)grType.GetConstructor(new Type[] { typeof(GRConfigurator) }).Invoke(new Object[] { grConf });
                        lgr.Group = m_group;
                        lgr.PriorityNumber = lgrInfo.PriorityNumber;
                        lgr.Target = (ITangibleGestureListener)lgrInfo.Handler.Target;
                        lgrDict[grConf] = lgr;
                        m_targetLgrListTable[localTarget].Add(lgr);
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
            GRConfigurator conf = lgr.Configurator;
            foreach (int pn in m_pns)
                if (pn >= lgrPN)
                    foreach (LocalGestureRecognizer g in m_unarmedLGRs[pn])
                        if (g != lgr && g.GetType() == grType && g.Configurator == conf)
                            yield return g;
        }

        private IEnumerable<GestureRecognizer> FindAllLGRsWithDifferentTarget(IGestureListener target)
        {
            foreach (int pn in m_pns)
                foreach (LocalGestureRecognizer lgr in m_unarmedLGRs[pn])
                    if (lgr.Target != target)
                        yield return lgr;
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

        private void RemoveUnarmedGR(GestureRecognizer gr)
        {
            int pn = gr.PriorityNumber;

            if (m_pns.Contains(pn))
            {
                if (gr is LocalGestureRecognizer)
                    m_unarmedLGRs[pn].Remove((LocalGestureRecognizer)gr);

                if (gr is GlobalGestureRecognizer)
                    m_unarmedGGRs[pn].Remove((GlobalGestureRecognizer)gr);

                if (m_unarmedLGRs[pn].Count == 0 && m_unarmedGGRs[pn].Count == 0)
                {
                    m_unarmedLGRs.Remove(pn);
                    m_unarmedGGRs.Remove(pn);
                    m_pns.Remove(pn);
                }
            }
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

        private void ClearUnarmed()
        {
            m_unarmedLGRs.Clear();
            m_unarmedGGRs.Clear();
            m_pns.Clear();
        }

        private void Unsubscribe()
        {
            m_grRegistry.Unsubscribe(this);
        } 
        #endregion
    }
}