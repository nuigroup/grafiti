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
    /// Manages the registration of gesture event handlers.
    /// </summary>
    public class GestureEventManager
    {
        #region Private or internal members
        private static GestureEventManager s_instance = null;
        private static readonly object s_lock = new object();
        private GestureEventRegistry s_grRegistry;
        private GRConfigurator m_defaultGRConfigurator;
        private DoubleDictionary<Type, GRConfigurator, int> m_priorityNumbersTable = new DoubleDictionary<Type,GRConfigurator,int>();
        internal GestureEventRegistry GRRegistry { get { return s_grRegistry; } } 
        #endregion

        #region Private constructor
        private GestureEventManager()
        {
            s_grRegistry = GestureEventRegistry.Instance;
            m_defaultGRConfigurator = new GRConfigurator(false);
        } 
        #endregion

        #region Singleton
        public static GestureEventManager Instance
        {
            get
            {
                lock (s_lock)
                {
                    if (s_instance == null)
                        s_instance = new GestureEventManager();
                    return s_instance;
                }
            }
        } 
        #endregion

        #region Client's interface
        /// <summary>
        /// Sets the priority number to associate with the given gesture recognizer class and its default
        /// configurator. However note that when registering a gesture event handler with the same GR type,
        /// no configurator must be passed as parameter in order to associate it with the priority number 
        /// specified.
        /// </summary>
        /// <param name="grType"></param>
        /// <param name="priorityNumber"></param>
        public void SetPriorityNumber(Type grType, int priorityNumber)
        {
            SetPriorityNumber(grType, GestureRecognizer.DefaultConfigurator, priorityNumber);
        }
        /// <summary>
        /// Sets the priority number to associate with the given gesture recognizer class and the
        /// given configurator.
        /// </summary>
        /// <param name="grType">Type of the gesture recognizer.</param>
        /// <param name="configurator">Configurator of the gesture recognizer.</param>
        /// <param name="priorityNumber">Priority number.</param>
        public void SetPriorityNumber(Type grType, GRConfigurator configurator, int priorityNumber)
        {
            lock (s_lock)
            {
                if (!m_priorityNumbersTable.ContainsKeys(grType, configurator))
                    m_priorityNumbersTable[grType, configurator] = priorityNumber;
                else
                    System.Diagnostics.Debug.Assert(m_priorityNumbersTable[grType, configurator] == priorityNumber,
                        "Attempting to reset a priority number to a different value than the one previously set.");
            }
        }
        /// <summary>
        /// Registers a handler for a gesture event. The GR will be will be configured by default.
        /// </summary>
        /// <param name="grType">Type of the gesture recognizer.</param>
        /// <param name="e">The event as string.</param>
        /// <param name="handler">The listener's function that will be called when the event is raised.</param>
        public void RegisterHandler(Type grType, string ev, GestureEventHandler handler)
        {
            RegisterHandler(grType, GestureRecognizer.DefaultConfigurator, ev, handler);
        }
        /// <summary>
        /// Registers a handler for a gesture event. The GR will be configured with the given configurator.
        /// </summary>
        /// <param name="grType">Type of the gesture recognizer.</param>
        /// <param name="grConf">The GR's configurator.</param>
        /// <param name="e">The event as string.</param>
        /// <param name="handler">The listener's function that will be called when the event is raised.</param>
        public void RegisterHandler(Type grType, GRConfigurator grConf, string ev, GestureEventHandler handler)
        {
            lock (s_lock)
            {
                System.Diagnostics.Debug.Assert(handler.Target is IGestureListener,
                    "Attempting to register a handler for an instance of class " +
                    handler.Target.GetType().ToString() +
                    " which doesn't implement the interface IGestureListener.");

                if (!m_priorityNumbersTable.ContainsKeys(grType, grConf))
                    m_priorityNumbersTable[grType, grConf] = 0;

                s_grRegistry.RegisterHandler(grType, grConf, m_priorityNumbersTable[grType, grConf], ev, handler);
            }
        }
        /// <summary>
        /// Unregisters all registered handlers for the given listener.
        /// </summary>
        /// <param name="listener">The listener</param>
        public void UnregisterAllHandlersOf(IGestureListener listener)
        {
            lock (s_lock)
            {
                s_grRegistry.UnregisterAllHandlers(listener);
            }
        }
        #endregion
    }
}