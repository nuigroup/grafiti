/*
	grafiti library

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
using TUIO;
using grafiti;





namespace grafiti

{

    /// <summary>

    /// Provides global gesture recognizer instances for groups, and manage the registration 

    /// of event handlers.

    /// </summary>

    internal class GGRProvider

    { 

        /// <summary>

        /// Represents a subscription of a gesture recognizer's event, by a listener that 

        /// declared the relative handler.

        /// </summary>

        internal class GGRInfoEntry

        {

            public Type m_grType;

            public Enum m_event;

            public IGestureListener m_listener;

            public GestureEventHandler m_handler;



            public GGRInfoEntry(Type grType, Enum e, IGestureListener listener, GestureEventHandler handler)

            {

                m_grType = grType;

                m_event = e;

                m_listener = listener;

                m_handler = handler;

            }

        }



        // list of all subscriptions

        private List<GGRInfoEntry> m_ggrInfos;



        // each group has an instance table used to associate each type of gr with only one instance

        private Dictionary<Group, Dictionary<Type, GlobalGestureRecognizer>> m_groupGGRInstanceTable;





        internal GGRProvider()

        {

            m_ggrInfos = new List<GGRInfoEntry>();

            m_groupGGRInstanceTable = new Dictionary<Group, Dictionary<Type, GlobalGestureRecognizer>>();

        }



        internal GGRInfoEntry RegisterHandler(Type grType, Enum e, IGestureListener listener, GestureEventHandler handler)

        {

            GGRInfoEntry ggrInfo = new GGRInfoEntry(grType, e, listener, handler);

            m_ggrInfos.Add(ggrInfo);

            return ggrInfo;

        }

        

        internal void UnregisterAllHandlers(IGestureListener listener)

        {

            m_ggrInfos.RemoveAll(delegate(GGRInfoEntry entry){ return entry.m_listener == listener; });

        }



        internal void ProvideGGRs(Group group)

        {

            m_groupGGRInstanceTable[group] = new Dictionary<Type, GlobalGestureRecognizer>();

            UpdateOrProvideGGRs(group, m_ggrInfos);

        }

        

        /// <summary>

        /// Accordingly to ggrInfos, updates the GGRs already present in the group by registering

        /// new handlers, or creates new instances of GGRs (with handlers).

        /// </summary>

        /// <param name="group">A group's GGR instance table.</param>

        /// <param name="ggrInfos">The informations about the GGRs.</param>

        internal void UpdateOrProvideGGRs(Group group, List<GGRInfoEntry> ggrInfos)

        {

            Dictionary<Type, GlobalGestureRecognizer> instanceTable = m_groupGGRInstanceTable[group];



            GlobalGestureRecognizer ggr;

            Type ggrType;

            foreach (GGRInfoEntry ggrInfo in ggrInfos)

            {

                ggrType = ggrInfo.m_grType;

                if (!instanceTable.TryGetValue(ggrType, out ggr))

                {

                    ggr = (GlobalGestureRecognizer)ggrType.GetConstructor(new Type[] { }).Invoke(new Object[] { });

                    instanceTable[ggrType] = ggr;

                    group.AddGGR(ggr);

                    ggr.Group = group;

                }

                // Add the gesture event handler

                //entry.m_eventInfo.GetAddMethod().Invoke(mtgr, new object[] { entry.m_handler }); // old

                ggr.AddHandler(ggrInfo.m_event, ggrInfo.m_listener, ggrInfo.m_handler);

            }

        }



        internal void Dispose(Group group)

        {

            m_groupGGRInstanceTable.Remove(group);

        }

    }

}