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
        private Group m_group;
        private GRRegistry m_grRegistry;

        private DoubleDictionary<Type, object, GlobalGestureRecognizer> m_ggrInstanceTable;
        private DoubleDictionary<Type, IGestureListener, LocalGestureRecognizer> m_lgrInstanceTable;
        private Dictionary<IGestureListener, List<GestureRecognizer>> m_targetLGRListTable;

        private List<GestureRecognizer> m_grs;
        private List<GestureRecognizer> m_currentGRs;
        private List<GestureRecognizer> m_interpreting;

        // temp lists
        private List<GestureRecognizer> m_toRemove; // temp list
        private List<GestureRecognizer> m_succedingGRs;
        private List<GestureRecognitionResult> m_succedingResults;

        private int m_currentPN;

        private bool m_taskCompleted;


        internal DoubleDictionary<Type, object, GlobalGestureRecognizer> GGRInstanceTable { get { return m_ggrInstanceTable; } }
        internal DoubleDictionary<Type, IGestureListener, LocalGestureRecognizer> LGRInstanceTable { get { return m_lgrInstanceTable; } }

        internal bool TaskCompleted { get { return m_taskCompleted; } }

        public GroupGRManager(Group group, GRRegistry registry)
        {
            m_group = group;
            m_grRegistry = registry;
            m_ggrInstanceTable = new DoubleDictionary<Type, object, GlobalGestureRecognizer>();
            m_lgrInstanceTable = new DoubleDictionary<Type, IGestureListener, LocalGestureRecognizer>();
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
            LocalGestureRecognizer lgr;
            foreach (GRRegistry.GRRegistrationInfo lgrInfo in m_grRegistry.LGRRegistry)
            {
                grType = lgrInfo.m_grType;
                if (lgrInfo.m_listener == localTarget)
                {
                    if (!m_lgrInstanceTable.TryGetValue(grType, localTarget, out lgr))
                    {
                        lgr = (LocalGestureRecognizer) m_grRegistry.LGRPrototypes[grType].Copy();
                        m_lgrInstanceTable[grType, localTarget] = lgr;

                        m_targetLGRListTable[localTarget].Add(lgr);

                        // Add LGR
                        lgr.Group = m_group;
                        lgr.PriorityNumber = lgrInfo.m_priorityNumber;
                        AddGR(lgr);
                        // end Add

                    }

                    lgr.AddHandler(lgrInfo.m_event, (IGestureListener) lgrInfo.m_listener, lgrInfo.m_handler);
                }
            }
        }

        public void RemoveLocalTarget(IGestureListener localTarget)
        {
            foreach (GestureRecognizer lgr in m_targetLGRListTable[localTarget])
            {
                m_grs.Remove(lgr);
                m_lgrInstanceTable.Remove(lgr.GetType(), localTarget);
            }
            m_targetLGRListTable.Remove(localTarget);
        }

        /// <summary>
        /// Called by GRFactory. Accordingly to ggrInfos, updates the GGRs already present 
        /// by registering new handlers, or creates new instances with handlers.
        /// </summary>
        /// <param name="ggrInfos">The informations about the GGRs.</param>
        internal void UpdateGGRs(List<GRRegistry.GRRegistrationInfo> ggrInfos)
        {
            Type grType;
            object ggrParam;
            GlobalGestureRecognizer ggr;
            
            foreach (GRRegistry.GRRegistrationInfo ggrInfo in ggrInfos)
            {
                grType = ggrInfo.m_grType;
                ggrParam = ggrInfo.m_grParam;
                if (!m_ggrInstanceTable.TryGetValue(grType, ggrParam, out ggr))
                {
                    ggr = (GlobalGestureRecognizer)m_grRegistry.GGRPrototypes[grType, ggrParam].Copy();
                    m_ggrInstanceTable[grType, ggrParam] = ggr;

                    // Add GGR
                    ggr.Group = m_group;
                    ggr.PriorityNumber = ggrInfo.m_priorityNumber;
                    AddGR(ggr);
                    // end Add

                }
                // Add event handler
                ggr.AddHandler(ggrInfo.m_event, ggrInfo.m_listener, ggrInfo.m_handler);
            }
        }

        public void UpdateGGRHandlers(bool initial, bool final, bool entering, bool current, bool leaving, bool intersect, bool union)
        {
            foreach (GestureRecognizer gr in m_interpreting)
                if (gr is GlobalGestureRecognizer)
                    ((GlobalGestureRecognizer)gr).UpdateHandlers(initial, final, entering, current, leaving, intersect, union);
            
            foreach (GestureRecognizer gr in m_grs)
                if(gr is GlobalGestureRecognizer)
                    ((GlobalGestureRecognizer)gr).UpdateHandlers(initial, final, entering, current, leaving, intersect, union);
        }



        public bool Process(Trace trace)
        {
            if (m_taskCompleted)
                return true;

            GestureRecognitionResult rs;


            /****************
               interpreting
            /****************/

            m_toRemove.Clear();
            foreach (GestureRecognizer gr in m_interpreting)
            {
                rs = gr.Process(trace);
                if (!rs.interpreting)
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

                    rs = gr.Process(trace);
                    if (!rs.recognizing)
                    {
                        if (rs.successful)
                        {
                            m_succedingGRs.Add(gr);
                            m_succedingResults.Add(rs);
                            m_toRemove.Add(gr);
                        }
                        else
                            m_toRemove.AddRange(FindAllGRsLike(gr));
                    }
                    else
                        someIsRecognizing = true;
                }

                //// TODO: sort
                //if (m_succedingGRs.Count >= 2)
                //{
                //    for (int i = 0; i < m_succedingGRs.Count; i++)
                //        m_targetLGRListTable
                //    foreach (GestureRecognizer gr in m_succedingGRs)
                //    {
                //        // sono già in ordine di target list position (...)
                //        // però se ce ne sono 2 vincenti nella stessa posizione
                //        // bisogna vedere chi ha la probabilità maggiore
                //    }
                //}

                for (int i = 0; i < m_succedingGRs.Count; i++)
                {
                    m_succedingGRs[i].Armed = true;
                    m_succedingGRs[i].ProcessPendlingEvents();
                    if (m_succedingResults[i].interpreting)
                        m_interpreting.Add(m_succedingGRs[0]);
                    if (m_succedingGRs[i].Exclusive)
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


            if (!(m_grs.Count > 0 || m_interpreting.Count > 0) || !trace.Group.Alive) // ...
            {
                m_grRegistry.Unsubscribe(this);
                m_taskCompleted = true;
            }

            return m_taskCompleted;
        }

        private List<GestureRecognizer> FindAllGRsLike(GestureRecognizer sample)
        {
            List<GestureRecognizer> list = new List<GestureRecognizer>();
            Type sampleType = sample.GetType();
            if (sample is LocalGestureRecognizer)
                foreach (GestureRecognizer gr in m_grs)
                {
                    if (gr.GetType() == sampleType)
                        list.Add(gr);
                }
            else
            {
                object sampleCtorParam = ((GlobalGestureRecognizer)sample).CtorParam;
                foreach (GestureRecognizer gr in m_grs)
                    if (gr is GlobalGestureRecognizer &&
                         gr.GetType() == sampleType &&
                         ((GlobalGestureRecognizer)gr).CtorParam == sampleCtorParam
                       )
                    {
                        list.Add(gr);
                    }
            }
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