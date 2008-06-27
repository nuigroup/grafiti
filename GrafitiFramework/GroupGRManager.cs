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
        private DoubleDictionary<Type, IGestureListener, Dictionary<object, LocalGestureRecognizer>> m_lgrInstanceTable;
        private Dictionary<IGestureListener, List<GestureRecognizer>> m_targetLGRListTable;

        // Registered GRs that haven't recognized a gesture yet
        private List<GestureRecognizer> m_grs;

        // Subset of the previous list. These GRs have all the same PN and thus are currently to be processed.
        private List<GestureRecognizer> m_currentGRs;

        // GRs that have already successfully recognized a gesture and are currently interpreting further inputs.
        private List<GestureRecognizer> m_interpreting;

        // Auxiliary temp lists.
        private List<GestureRecognizer> m_toRemove;
        private List<GestureRecognizer> m_succedingGRs;
        private List<GestureRecognitionResult> m_succedingResults;

        // Current PN. It determines the current GRs to be processed.
        private int m_currentPN;

        // Flag indicating if there are no more GRs to process.
        private bool m_taskCompleted;


        internal DoubleDictionary<Type, object, GlobalGestureRecognizer> GGRInstanceTable { get { return m_ggrInstanceTable; } }
        internal DoubleDictionary<Type, IGestureListener, Dictionary<object, LocalGestureRecognizer>> LGRInstanceTable { get { return m_lgrInstanceTable; } }

        internal bool TaskCompleted { get { return m_taskCompleted; } }


        public GroupGRManager(Group group)
        {
            m_group = group;
            m_grRegistry = GestureEventManager.Instance.GRRegistry;

            m_ggrInstanceTable = new DoubleDictionary<Type, object, GlobalGestureRecognizer>();
            m_lgrInstanceTable = new DoubleDictionary<Type, IGestureListener, Dictionary<object, LocalGestureRecognizer>>();
            m_targetLGRListTable = new Dictionary<IGestureListener, List<GestureRecognizer>>();
            
            m_grs = new List<GestureRecognizer>();
            m_currentGRs = new List<GestureRecognizer>();
            m_interpreting = new List<GestureRecognizer>();

            m_toRemove = new List<GestureRecognizer>();
            m_succedingGRs = new List<GestureRecognizer>();
            m_succedingResults = new List<GestureRecognitionResult>();

            m_currentPN = 0;
            m_taskCompleted = false;

            m_grRegistry.Subscribe(this);
        }

        public void AddLocalTarget(IGestureListener localTarget)
        {
            if (!m_targetLGRListTable.ContainsKey(localTarget))
                m_targetLGRListTable[localTarget] = new List<GestureRecognizer>();

            UpdateLGRs(localTarget);
        }

        private void UpdateLGRs(IGestureListener localTarget)
        {
            Type grType;
            object grConf;
            LocalGestureRecognizer lgr;
            Dictionary<object, LocalGestureRecognizer> lgrDict;

            foreach (GestureEventRegistry.RegistrationInfo lgrInfo in m_grRegistry.LGRRegistry)
            {
                grType = lgrInfo.GRType;
                grConf = lgrInfo.GRConfiguration;

                if (lgrInfo.Handler.Target == localTarget)
                {
                    if (!m_lgrInstanceTable.TryGetValue(grType, localTarget, out lgrDict)) //localTarget, out lgr))
                    {
                        lgrDict = new Dictionary<object, LocalGestureRecognizer>();
                        m_lgrInstanceTable[grType, localTarget] = lgrDict;
                    }

                    if(!lgrDict.TryGetValue(grConf, out lgr))
                    {
                        lgr = (LocalGestureRecognizer)grType.GetConstructor(new Type[] { typeof(GRConfiguration) }).Invoke(new Object[] { grConf });
                        lgrDict[grConf] = lgr;

                        m_targetLGRListTable[localTarget].Add(lgr);

                        // Add LGR
                        lgr.Group = m_group;
                        lgr.PriorityNumber = lgrInfo.PriorityNumber;
                        AddGR(lgr);
                        // end Add
                    }

                    lgr.AddHandler(lgrInfo.Event, lgrInfo.Handler);
                }
            }
        }

        public void RemoveLocalTarget(IGestureListener localTarget)
        {
            if (m_targetLGRListTable.ContainsKey(localTarget))
            {
                foreach (GestureRecognizer lgr in m_targetLGRListTable[localTarget])
                {
                    m_grs.Remove(lgr);
                    m_lgrInstanceTable.Remove(lgr.GetType(), localTarget);
                }
                m_targetLGRListTable.Remove(localTarget);

                //Console.WriteLine("Removed local target {0}", localTarget);
            }
        }

        /// <summary>
        /// Called by GRRegistry. Accordingly to ggrInfos, updates the GGRs already present 
        /// by registering new handlers, or creates new instances with handlers.
        /// </summary>
        /// <param name="ggrInfos">The informations of the registrations of the GGRs.</param>
        internal void UpdateGGRs(List<GestureEventRegistry.RegistrationInfo> ggrInfos)
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
                    m_ggrInstanceTable[grType, grParam] = ggr;

                    // Add GGR
                    ggr.Group = m_group;
                    ggr.PriorityNumber = ggrInfo.PriorityNumber;
                    AddGR(ggr);
                    // end Add

                }
                // Add event handler
                ggr.AddHandler(ggrInfo.Event, ggrInfo.Handler);
            }
        }

        public void UpdateGGRHandlers(bool initial, bool final, bool entering, bool current, bool leaving,
            bool intersect, bool union, bool newClosestEnt, bool newClosestCur, bool newClosestLvn)
        {
            foreach (GestureRecognizer gr in m_interpreting)
                if (gr is GlobalGestureRecognizer)
                    ((GlobalGestureRecognizer)gr).UpdateHandlers(initial, final, entering, current, leaving, 
                        intersect, union, newClosestEnt, newClosestCur, newClosestLvn);
            
            foreach (GestureRecognizer gr in m_grs)
                if(gr is GlobalGestureRecognizer)
                    ((GlobalGestureRecognizer)gr).UpdateHandlers(initial, final, entering, current, leaving, 
                        intersect, union, newClosestEnt, newClosestCur, newClosestLvn);
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
            foreach (GestureRecognizer gr in m_interpreting)
            {
                result = gr.Process(traces);
                if (!result.Interpreting)
                    m_toRemove.Add(gr);
            }
            foreach (GestureRecognizer gr in m_toRemove)
                m_interpreting.Remove(gr);
            m_toRemove.Clear();



            /****************
                 current
            /****************/

            bool someIsRecognizing = false;
            bool exclusiveWinner = false;
            while (UpdateCurrents())
            {
                m_succedingGRs.Clear();
                m_succedingResults.Clear();

                foreach (GestureRecognizer gr in m_currentGRs)
                {
                    if (m_toRemove.Contains(gr))
                        continue;

                    result = gr.Process(traces);
                    if (!result.Recognizing)
                    {
                        if (result.Successful)
                        {
                            m_succedingGRs.Add(gr);
                            m_succedingResults.Add(result);
                        }
                        if (gr is GlobalGestureRecognizer)
                            m_toRemove.Add(gr);
                        else
                            m_toRemove.AddRange(FindAllLGRsOfType(gr.GetType()));
                    }
                    else
                        someIsRecognizing = true;
                }

                // TODO: sort m_succedingGRs
                // they're already ordered by distance of the relative target regarding the LGR list (by now it's
                // referred at the time of the creation of that list: the order is not the actual one
                // since it's not updated!), but if there are two GRs with the same target, we must see 
                // which has the higher probability of success.

                for (int i = 0; i < m_succedingGRs.Count; i++)
                {
                    m_succedingGRs[i].Armed = true;
                    m_succedingGRs[i].ProcessPendlingEvents();
                    if (m_succedingResults[i].Interpreting)
                        m_interpreting.Add(m_succedingGRs[i]);
                    if (m_succedingGRs[i].Configuration.Exclusive)
                    {
                        exclusiveWinner = true;
                        break;
                    }
                }

                if (exclusiveWinner)
                    m_grs.Clear();
                else
                    foreach (GestureRecognizer gr in m_toRemove)
                        m_grs.Remove(gr);
                m_toRemove.Clear();

                if (someIsRecognizing || exclusiveWinner)
                    break;
            }


            if (!(m_grs.Count > 0 || m_interpreting.Count > 0))// || !trace.Group.Alive) // ...
            {
                m_grRegistry.Unsubscribe(this);
                m_taskCompleted = true;
            }

            return m_taskCompleted;
        }

        private List<GestureRecognizer> FindAllLGRsOfType(Type grType)
        {
            List<GestureRecognizer> list = new List<GestureRecognizer>();
            foreach (GestureRecognizer gr in m_grs)
                if (gr.GetType() == grType)
                    list.Add(gr);
            return list;
        }

        private void AddGR(GestureRecognizer gr)
        {
            // insert gr in a position such that
            // 1. the list is ordered by priority number (decreasing)
            // 2. if same priority, LGRs come before GGRs
            // 3. if two GRs, belonging to the same supertype (LGR or GGR), have the same PN, the first added comes before the second
            int i;
            if (gr is GlobalGestureRecognizer)
                for (i = m_grs.Count - 1; i >= 0 && gr.PriorityNumber < m_grs[i].PriorityNumber; i--) ;
            else
                for (i = m_grs.Count - 1; i >= 0 && 
                    ((gr.PriorityNumber <  m_grs[i].PriorityNumber) || 
                     (gr.PriorityNumber == m_grs[i].PriorityNumber  && m_grs[i] is GlobalGestureRecognizer));
                     i--) ;
            m_grs.Insert(i + 1, gr);
        }

        private bool UpdateCurrents()
        {
            m_currentGRs.Clear();
            if (m_grs.Count > 0)
            {
                m_currentPN = m_grs[0].PriorityNumber;
                foreach (GestureRecognizer gr in m_grs)
                    if (gr.PriorityNumber == m_currentPN)
                        m_currentGRs.Add(gr);
            }
            return m_currentGRs.Count > 0;
        }
    }
}