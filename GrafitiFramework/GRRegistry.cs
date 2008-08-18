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
    /// Stores the registrations of gesture event handlers and notifies changes to the 
    /// subscribed groups' GR managers.
    /// </summary>
    internal class GestureEventRegistry
    {
        #region Declarations
        /// <summary>
        /// Represents a subscription for a gesture recognizer's event, made by a listener that 
        /// declared the relative handler.
        /// </summary>
        internal class RegistrationInfo
        {
            private readonly Type m_grType;                 // type of the GR
            private readonly GRConfigurator m_grConf;       // configurator of the GR
            private readonly int m_priorityNumber;          // priority number of the GR
            private readonly string m_event;                // event as string
            private readonly GestureEventHandler m_handler; // listener's handler

            internal Type GRType { get { return m_grType; } }
            internal GRConfigurator GRConfigurator { get { return m_grConf; } }
            internal int PriorityNumber { get { return m_priorityNumber; } }
            internal string Event { get { return m_event; } }
            internal GestureEventHandler Handler { get { return m_handler; } }

            internal RegistrationInfo(Type grType, GRConfigurator grConf, int pn, string ev, GestureEventHandler handler)
            {
                m_grType = grType;
                m_grConf = grConf;
                m_priorityNumber = pn;
                m_event = ev;
                m_handler = handler;
            }
        }

        // The only instance
        private static GestureEventRegistry m_instance = null;
        private static readonly object m_lock = new object();

        // Registry of priority numbers. Once an entry is stored it can't be changed, nor removed.
        private DoubleDictionary<Type, GRConfigurator, int> m_priorityNumbersTable = new DoubleDictionary<Type, GRConfigurator, int>();

        // Registries of listeners' registrations
        private List<RegistrationInfo> m_ggrRegistry; // global
        private List<RegistrationInfo> m_lgrRegistry; // local

        // List of GroupGRManager's to be updated when a new handler is registered
        private List<GroupGRManager> m_subscribedGRManagers;

        internal List<RegistrationInfo> LGRRegistry { get { return m_lgrRegistry; } }
        #endregion

        #region Private constructor
        private GestureEventRegistry()
        {
            m_ggrRegistry = new List<RegistrationInfo>();
            m_lgrRegistry = new List<RegistrationInfo>();
            m_subscribedGRManagers = new List<GroupGRManager>();
        }   
        #endregion

        #region Singleton
        internal static GestureEventRegistry Instance
        {
            get
            {
                lock (m_lock)
                {
                    if (m_instance == null)
                        m_instance = new GestureEventRegistry();
                    return m_instance;
                }
            }
        }
        #endregion


        #region GroupGRManager subscription methods
        internal void Subscribe(GroupGRManager grManager)
        {
            m_subscribedGRManagers.Add(grManager);
            grManager.AddOrUpdateGGRs(m_ggrRegistry);
        }
        internal void Unsubscribe(GroupGRManager grManager)
        {
            m_subscribedGRManagers.Remove(grManager);
        }
        #endregion

        #region GestureEventManager methods
        internal void SetPriorityNumber(Type grType, GRConfigurator configurator, int priorityNumber)
        {
            if (!m_priorityNumbersTable.ContainsKeys(grType, configurator))
                m_priorityNumbersTable[grType, configurator] = priorityNumber;
            else
                System.Diagnostics.Debug.Assert(m_priorityNumbersTable[grType, configurator] == priorityNumber,
                    "Attempting to reset a priority number to a different value than the one previously set.");
        }
        internal void RegisterHandler(Type grType, GRConfigurator grConf, string ev, GestureEventHandler handler)
        {
            System.Diagnostics.Debug.Assert(handler.Target is IGestureListener,
                "Attempting to register a handler for an instance of class " +
                handler.Target.GetType().ToString() +
                " which doesn't implement the interface IGestureListener.");

            int priorityNumber;
            if (!m_priorityNumbersTable.ContainsKeys(grType, grConf))
            {
                priorityNumber = 0;
                m_priorityNumbersTable[grType, grConf] = priorityNumber;
            }
            else
                priorityNumber = m_priorityNumbersTable[grType, grConf];


            RegistrationInfo grInfo = new RegistrationInfo(grType, grConf, priorityNumber, ev, handler);

            if (grType.IsSubclassOf(typeof(GlobalGestureRecognizer)))
            {
                m_ggrRegistry.Add(grInfo);

                // update subscribed GRManagers
                List<RegistrationInfo> ggrInfoList = new List<RegistrationInfo>();
                ggrInfoList.Add(grInfo);
                foreach (GroupGRManager grManager in m_subscribedGRManagers)
                    grManager.AddOrUpdateGGRs(ggrInfoList);
            }
            else
            {
                m_lgrRegistry.Add(grInfo);

                // TODO: if LGRs are associated to the group's FINAL target list then dynamic update should be done
            }
        }
        internal void UnregisterAllHandlers(object listener)
        {
            m_ggrRegistry.RemoveAll(delegate(RegistrationInfo ggrInfo) { return ggrInfo.Handler.Target == listener; });
            m_lgrRegistry.RemoveAll(delegate(RegistrationInfo ggrInfo) { return ggrInfo.Handler.Target == listener; });
        }
        #endregion
    }
}