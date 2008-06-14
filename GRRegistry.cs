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
using TUIO;
using Grafiti;


namespace Grafiti
{
    /// <summary>
    /// Provides global gesture recognizer instances for groups, and manage the registration 
    /// of event handlers.
    /// </summary>
    internal class GRRegistry
    {
        /// <summary>
        /// Represents a subscription of a gesture recognizer's event, by a listener that 
        /// declared the relative handler.
        /// </summary>
        internal class GRRegistrationInfo
        {
            public Type m_grType;
            public object m_grParam;
            public int m_priorityNumber; // priority number
            public Enum m_event;
            public object m_listener;
            public GestureEventHandler m_handler;

            public GRRegistrationInfo(Type grType, object grParam, int pn, Enum e, object listener, GestureEventHandler handler)
            {
                m_grType = grType;
                m_grParam = grParam;
                m_priorityNumber = pn;
                m_event = e;
                m_listener = listener;
                m_handler = handler;
            }
        }

        // Registries of listeners' registrations
        private List<GRRegistrationInfo> m_ggrRegistry; // global
        private List<GRRegistrationInfo> m_lgrRegistry; // local

        // List of GroupGRManager's to be updated when a new handler is registered
        private List<GroupGRManager> m_subscribedGRManagers;

        internal List<GRRegistrationInfo> LGRRegistry { get { return m_lgrRegistry; } }


        internal GRRegistry()
        {
            m_ggrRegistry = new List<GRRegistrationInfo>();
            m_lgrRegistry = new List<GRRegistrationInfo>();
            m_subscribedGRManagers = new List<GroupGRManager>();
        }

        internal void RegisterHandler(Type grType, object grParam, int priorityNumber, Enum e, GestureEventHandler handler)
        {
            GRRegistrationInfo grInfo = new GRRegistrationInfo(grType, grParam, priorityNumber, e, handler.Target, handler);
            
            if (grType.IsSubclassOf(typeof(GlobalGestureRecognizer)))
            {
                m_ggrRegistry.Add(grInfo);

                // dinamically update subscribed GRManagers
                List<GRRegistrationInfo> ggrInfoList = new List<GRRegistrationInfo>();
                ggrInfoList.Add(grInfo);
                foreach (GroupGRManager grManager in m_subscribedGRManagers)
                    grManager.UpdateGGRs(ggrInfoList);
            }
            else
            {
                m_lgrRegistry.Add(grInfo);

                // if LGRs are associated to the group's FINAL target list then dynamic update should be done
            }

        }

        internal void UnregisterAllHandlers(object listener)
        {
            m_ggrRegistry.RemoveAll(delegate(GRRegistrationInfo ggrInfo) { return ggrInfo.m_listener == listener; });
            m_lgrRegistry.RemoveAll(delegate(GRRegistrationInfo ggrInfo) { return ggrInfo.m_listener == listener; });
        }


        internal void Subscribe(GroupGRManager grManager)
        {
            m_subscribedGRManagers.Add(grManager);
            grManager.UpdateGGRs(m_ggrRegistry);
        }

        internal void Unsubscribe(GroupGRManager grManager)
        {
            m_subscribedGRManagers.Remove(grManager);
        }

    }
}