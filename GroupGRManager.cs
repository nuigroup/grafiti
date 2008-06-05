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

        private List<IGestureListener> m_lgrTargets;
        private List<GestureRecognizer> m_lgrs;
        private List<GestureRecognizer> m_ggrs;
        private List<GestureRecognizer> m_grs;

        private List<GestureRecognizer> m_currentGRs;
        private List<GestureRecognizer> m_backgroundGRs;



        private int m_currentPN;
        private bool m_firstProcessCall;


        internal DoubleDictionary<Type, object, GlobalGestureRecognizer> GGRInstanceTable { get { return m_ggrInstanceTable; } }
        internal DoubleDictionary<Type, IGestureListener, LocalGestureRecognizer> LGRInstanceTable { get { return m_lgrInstanceTable; } }

        public GroupGRManager(Group group, GRRegistry registry)
        {
            m_group = group;
            m_grRegistry = registry;
            m_ggrInstanceTable = new DoubleDictionary<Type, object, GlobalGestureRecognizer>();
            m_lgrInstanceTable = new DoubleDictionary<Type, IGestureListener, LocalGestureRecognizer>();
            m_targetLGRListTable = new Dictionary<IGestureListener, List<GestureRecognizer>>();

            m_lgrs = new List<GestureRecognizer>();
            m_ggrs = new List<GestureRecognizer>();
            m_grs = new List<GestureRecognizer>();
            m_firstProcessCall = true;

            m_currentGRs = new List<GestureRecognizer>();
            m_backgroundGRs = new List<GestureRecognizer>();

            m_currentPN = 0;


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
                        m_lgrs.Add(lgr);
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
                m_lgrs.Remove(lgr);
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
                    m_ggrs.Add(ggr);
                    AddGR(ggr);
                    // end Add

                }
                // Add event handler
                ggr.AddHandler(ggrInfo.m_event, ggrInfo.m_listener, ggrInfo.m_handler);
            }
        }

        public void UpdateGGRHandlers(bool initial, bool final, bool entering, bool current, bool leaving, bool intersect, bool union)
        {
            foreach (GlobalGestureRecognizer ggr in m_ggrs)
                ggr.UpdateHandlers(initial, final, entering, current, leaving, intersect, union);
        }


        

        /*
        public void UpdateLGRs(List<IGestureListener> lgrTargets)
        {
            m_lgrTargets = lgrTargets;
            m_lgrs.Clear();
            foreach (IGestureListener target in lgrTargets)
            {
                m_lgrs.AddRange(target.GetLocalGRs());
            }
            MergeGRs();
        }

        public void MergeGRs()
        {
            m_grs.Clear();
            m_grs.AddRange(m_ggrs);
            m_grs.AddRange(m_lgrs);
            m_grs.Sort();
        }



        private void SetCurrent()
        {
            foreach (GestureRecognizer gr in m_grs)
            {
                if (gr.PriorityNumber == m_currentPN)
                {
                    m_current.Add(gr);
                }
                else
                    break;
            }
        }

        */

        public void Process(Trace trace)
        {
            GestureRecognitionResult rs;
            List<GestureRecognizer> toRemove = new List<GestureRecognizer>();

            foreach (GestureRecognizer gr in m_backgroundGRs)
            {
                rs = gr.Process(trace);
                gr.ProcessPendlingEvents();

                if (!rs.recognizing)
                    if (!rs.successful || !rs.interpreting)
                        toRemove.Add(gr);
            }
            foreach (GestureRecognizer gr in toRemove)
            {
                m_backgroundGRs.Remove(gr);
            }

            UpdateCurrents();
            List<GestureRecognizer> succeding = new List<GestureRecognizer>();
            List<GestureRecognitionResult> succedingResults = new List<GestureRecognitionResult>();
            GestureRecognizer winner;

            foreach (GestureRecognizer gr in m_currentGRs)
            {
                rs = gr.Process(trace);

                if (!rs.recognizing)
                    if (!rs.successful)
                        toRemove.Add(gr);
                    else
                    {
                        succeding.Add(gr);
                        succedingResults.Add(rs);
                    }
            }


            //if (succeding.Count >= 2)
            //{
            //    IGestureListener succedingTarget;

            //    foreach (GestureRecognizer gr in succeding)
            //    {
            //        // sono già in ordine di target list position
            //        // però se ce ne sono 2 vincenti nella stessa posizione
            //        // bisogna vedere chi ha la probabilità maggiore
            //    }
            //}

            if (succeding.Count > 0)
            {
                m_grs.Clear();
                succeding[0].Armed = true;
                succeding[0].ProcessPendlingEvents();
                if (succedingResults[0].interpreting)
                    m_backgroundGRs.Add(succeding[0]);
            }
            else
            {
                foreach (GestureRecognizer gr in toRemove)
                    m_grs.Remove(gr);
            }

            if (!(m_grs.Count > 0 || m_backgroundGRs.Count > 0) || !trace.Group.Alive) // finish
            {
                m_grRegistry.Unsubscribe(this);
                Console.WriteLine("unsubscribing " + this);
            }
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

        private void UpdateCurrents()
        {
            m_currentGRs.Clear();
            if (m_grs.Count > 0)
            {
                m_currentPN = m_grs[0].PriorityNumber;
                foreach (GestureRecognizer gr in m_grs)
                    if (gr.PriorityNumber == m_currentPN)
                        m_currentGRs.Add(gr);
            }
        }
    }
}