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
    public class GroupGRManager
    {
        // The relative group to process.
        private Group m_group;

        // Registry of GRs
        private GestureEventRegistry m_grRegistry;

        // Tables used to instantiate new GRs.
        private DoubleDictionary<Type, object, GlobalGestureRecognizer> m_ggrInstanceTable;
        private DoubleDictionary<Type, ITuioObjectGestureListener, Dictionary<object, LocalGestureRecognizer>> m_lgrInstanceTable;
        private Dictionary<ITuioObjectGestureListener, List<GestureRecognizer>> m_targetLGRListTable;

        // Auxiliary temp lists.
        private List<GestureRecognizer> m_toRemove = new List<GestureRecognizer>();
        private List<GestureRecognizer> m_succedingGRs = new List<GestureRecognizer>();        

        // Current priority number. It determines the current GRs to be processed.
        private int m_currentPN;

        // Flag indicating if there are no more GRs to process.
        private bool m_taskCompleted;

        private List<int> m_pns;
        private List<GestureRecognizer> m_armedGRs;
        private Dictionary<int, List<LocalGestureRecognizer>> m_unarmedLGRs;
        private Dictionary<int, List<GlobalGestureRecognizer>> m_unarmedGGRs;


        internal DoubleDictionary<Type, object, GlobalGestureRecognizer> GGRInstanceTable { get { return m_ggrInstanceTable; } }
        internal DoubleDictionary<Type, ITuioObjectGestureListener, Dictionary<object, LocalGestureRecognizer>> LGRInstanceTable { get { return m_lgrInstanceTable; } }

        internal bool TaskCompleted { get { return m_taskCompleted; } }


        public GroupGRManager(Group group)
        {
            m_group = group;
            m_grRegistry = GestureEventManager.Instance.GRRegistry;

            m_ggrInstanceTable = new DoubleDictionary<Type, object, GlobalGestureRecognizer>();
            m_lgrInstanceTable = new DoubleDictionary<Type, ITuioObjectGestureListener, Dictionary<object, LocalGestureRecognizer>>();
            m_targetLGRListTable = new Dictionary<ITuioObjectGestureListener, List<GestureRecognizer>>();

            m_currentPN = 0;
            m_pns = new List<int>();
            m_taskCompleted = false;

            m_armedGRs = new List<GestureRecognizer>();
            m_unarmedLGRs = new Dictionary<int,List<LocalGestureRecognizer>>();
            m_unarmedGGRs = new Dictionary<int,List<GlobalGestureRecognizer>>();

            m_grRegistry.Subscribe(this);
        }

        public void AddLocalTarget(ITuioObjectGestureListener localTarget)
        {
            if (!m_targetLGRListTable.ContainsKey(localTarget))
                m_targetLGRListTable[localTarget] = new List<GestureRecognizer>();

            AddOrUpdateLGRs(localTarget);
        }

        private void AddOrUpdateLGRs(ITuioObjectGestureListener localTarget)
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

                    if(!lgrDict.TryGetValue(grConf, out lgr))
                    {
                        lgr = (LocalGestureRecognizer)grType.GetConstructor(new Type[] { typeof(GRConfigurator) }).Invoke(new Object[] { grConf });
                        lgrDict[grConf] = lgr;

                        m_targetLGRListTable[localTarget].Add(lgr);

                        // Add LGR
                        lgr.Group = m_group;
                        lgr.PriorityNumber = lgrInfo.PriorityNumber;
                        lgr.Target = (ITuioObjectGestureListener)lgrInfo.Handler.Target;
                        AddUnarmedGR(lgr);
                        // end Add
                    }

                    lgr.AddHandler(lgrInfo.Event, lgrInfo.Handler);
                }
            }
        }

        public void RemoveLocalTarget(ITuioObjectGestureListener localTarget)
        {
            if (m_targetLGRListTable.ContainsKey(localTarget))
            {
                foreach (GestureRecognizer lgr in m_targetLGRListTable[localTarget])
                {
                    // reset exclusive local target if this is removed
                    if (localTarget == m_group.ExclusiveLocalTarget)
                        ResetExclusiveLocalTarget();
                    RemoveUnarmed(lgr);
                    m_armedGRs.Remove(lgr);
                    m_lgrInstanceTable.Remove(lgr.GetType(), localTarget);
                }
                m_targetLGRListTable.Remove(localTarget);

                //Console.WriteLine("Removed local target {0}", localTarget);
            }
        }

        private void ResetExclusiveLocalTarget()
        {
            m_group.ExclusiveLocalTarget = null;
        }

        /// <summary>
        /// Called by GRRegistry. Accordingly to ggrInfos, updates the GGRs already present 
        /// by registering new handlers, or creates new instances with handlers.
        /// </summary>
        /// <param name="ggrInfos">The informations of the registrations of the GGRs.</param>
        internal void AddOrUpdateUpdateGGRs(List<GestureEventRegistry.RegistrationInfo> ggrInfos)
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
                    m_ggrInstanceTable[grType, grParam] = ggr;

                    // Add GGR
                    ggr.Group = m_group;
                    ggr.PriorityNumber = ggrInfo.PriorityNumber;
                    AddUnarmedGR(ggr);
                    // end Add

                }
                // Add event handler
                ggr.AddHandler(ggrInfo.Event, ggrInfo.Handler);
            }
        }

        public void UpdateGGRHandlers(
            bool initial, bool final, bool entering, bool current, bool leaving,
            bool intersect, bool union, 
            bool newClosestEnt, bool newClosestCur, bool newClosestLvn, bool newClosestIni, bool newClosestFin)
        {
            foreach (GestureRecognizer gr in m_armedGRs)
                if (gr is GlobalGestureRecognizer)
                    ((GlobalGestureRecognizer)gr).UpdateHandlers(initial, final, entering, current, leaving, 
                        intersect, union, newClosestEnt, newClosestCur, newClosestLvn, newClosestIni, newClosestFin);
            
            foreach(int pn in m_pns)
                foreach (GlobalGestureRecognizer ggr in m_unarmedGGRs[pn])
                    ggr.UpdateHandlers(initial, final, entering, current, leaving,
                        intersect, union, newClosestEnt, newClosestCur, newClosestLvn, newClosestIni, newClosestFin);
        }

        public bool Process(List<Trace> traces)
        {
            if (m_taskCompleted)
                return true;

            GestureRecognitionResult result;


            /****************
               interpreting
            /****************/

            m_toRemove.Clear();
            foreach (GestureRecognizer gr in m_armedGRs)
            {
                result = gr.Process1(traces);
                if (!result.Interpreting)
                    m_toRemove.Add(gr);
            }
            foreach (GestureRecognizer gr in m_toRemove)
                m_armedGRs.Remove(gr);
            m_toRemove.Clear();



            /****************
                 current
            /****************/

            bool someIsRecognizing = false;
            bool exclusiveWinner = false;

            SortUnarmedLGRs();
            int pn;
            for (int pnIdx = 0; pnIdx < m_pns.Count; )
            {
                pn = m_pns[pnIdx];

                m_succedingGRs.Clear();

                // process GGRs
                foreach (GlobalGestureRecognizer ggr in m_unarmedGGRs[pn])
                {
                    if (!ggr.m_interpreting)
                        continue;

                    result = ggr.Process1(traces);
                    if (!result.Recognizing)
                    {
                        if (pn == m_currentPN)
                        {
                            if (result.Successful)
                            {
                                ggr.m_interpreting = result.Interpreting;
                                ggr.m_probabilityOfSuccess = result.Probability;
                                m_succedingGRs.Add(ggr);
                            }
                            m_toRemove.Add(ggr);
                        }
                    }
                    else
                        if (someIsRecognizing == false)
                            someIsRecognizing = true;
                }

                // process LGRs
                foreach (LocalGestureRecognizer lgr in m_unarmedLGRs[pn])
                {
                    if (m_toRemove.Contains(lgr) || (!lgr.m_interpreting))
                        continue;

                    result = lgr.Process1(traces);
                    if (!result.Recognizing)
                    {
                        if (pn == m_currentPN)
                        {
                            if (result.Successful)
                            {
                                lgr.m_interpreting = result.Interpreting;
                                lgr.m_probabilityOfSuccess = result.Probability;
                                m_succedingGRs.Add(lgr);
                            }
                            m_toRemove.Add(lgr);
                        }
                        m_toRemove.AddRange(FindAllOtherEquivalentLGRs(lgr));
                    }
                    else
                        if (someIsRecognizing == false)
                            someIsRecognizing = true;
                }



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
                            if (m_succedingGRs[i].m_probabilityOfSuccess == 1)
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
                            maxP = m_succedingGRs[0].m_probabilityOfSuccess;
                            idxMaxP = 0;
                            for (int i = 1; i < m_succedingGRs.Count; i++)
                            {
                                currentP = m_succedingGRs[i].m_probabilityOfSuccess;
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

                    // For all the succeding GRs, arm them and process their pending events,
                    // until an exclusive winner is found.
                    GestureRecognizer winning;
                    for (int i = 0; i < m_succedingGRs.Count; i++)
                    {
                        winning = m_succedingGRs[i];
                        winning.Armed = true;
                        winning.ProcessPendlingEvents();

                        if (winning.m_interpreting)
                            m_armedGRs.Add(winning);
                        if (winning.Configurator.Exclusive)
                        {
                            exclusiveWinner = true;
                            // If a GGR wins exclusively and no LGR has set the exclusive local target,
                            // then reset it in order to clear LGRtargetList
                            if (winning is GlobalGestureRecognizer && m_group.ExclusiveLocalTarget == null)
                                ResetExclusiveLocalTarget();
                            break;
                        }
                    }
                }


                if (exclusiveWinner)
                    ClearUnarmed();
                else
                    foreach(GestureRecognizer gr in m_toRemove)
                        RemoveUnarmed(gr);

                m_toRemove.Clear();

                // If all current GR failed in the recognition then pass to the next priority number
                if (!someIsRecognizing && m_pns.Count > 0)
                    m_currentPN = m_pns[0];
                else
                    pnIdx++;
            }


            if (m_pns.Count == 0 && m_armedGRs.Count == 0) // || !trace.Group.Alive) // ...
            {
                m_grRegistry.Unsubscribe(this);
                m_taskCompleted = true;
            }

            return m_taskCompleted;
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

        private IEnumerable<GestureRecognizer> FindAllLGRsWithDifferentTarget(ITuioObjectGestureListener target)
        {
            foreach (int pn in m_pns)
                foreach (LocalGestureRecognizer lgr in m_unarmedLGRs[pn])
                    if (lgr.Target != target)
                        yield return lgr;
        }

        private void AddUnarmedGR(GestureRecognizer gr)
        {
            int pn = gr.PriorityNumber;
            if(!(m_pns.Contains(pn)))
            {
                m_pns.Add(pn);
                m_pns.Sort();
                m_currentPN = m_pns[0];
                m_unarmedLGRs[pn] = new List<LocalGestureRecognizer>();
                m_unarmedGGRs[pn] = new List<GlobalGestureRecognizer>();
            }
            if(gr is LocalGestureRecognizer)
                m_unarmedLGRs[pn].Add((LocalGestureRecognizer)gr);
            else
                m_unarmedGGRs[pn].Add((GlobalGestureRecognizer)gr);
        }

        /// <summary>
        /// Sort unarmed LGRs by increasing distance to their target
        /// </summary>
        private void SortUnarmedLGRs()
        {
            foreach(int pn in m_pns)
            {
                foreach(LocalGestureRecognizer lgr in m_unarmedLGRs[pn])
                    lgr.SquareDistanceFromTarget = lgr.Target.GetSquareDistance(m_group.CentroidX, m_group.CentroidY);
                m_unarmedLGRs[pn].Sort(new Comparison<LocalGestureRecognizer>(
                delegate(LocalGestureRecognizer a, LocalGestureRecognizer b)
                {
                    return (int)((a.SquareDistanceFromTarget - b.SquareDistanceFromTarget) * 10000000);
                }
                ));
            }
        }

        private void RemoveUnarmed(GestureRecognizer gr)
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

        private void ClearUnarmed()
        {
            m_unarmedLGRs.Clear();
            m_unarmedGGRs.Clear();
            m_pns.Clear();
        }
    }
}