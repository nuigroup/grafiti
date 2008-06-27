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
    public class GestureEventManager
    {
        #region Private or internal members
        private static GestureEventManager s_instance = null;
        private static readonly object s_lock = new object();
        private GestureEventRegistry s_grRegistry;
        private GRConfiguration m_defaultGRConfiguration;
        private int m_grPriorityNumber;
        internal GestureEventRegistry GRRegistry { get { return s_grRegistry; } } 
        #endregion

        #region Private constructor
        private GestureEventManager()
        {
            s_grRegistry = GestureEventRegistry.Instance;
            m_defaultGRConfiguration = new GRConfiguration(false);
            m_grPriorityNumber = 0;
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
        /// Set the priority number for the next GR registrations. GRs with a lower priority number will
        /// have precedence over those with a higher priority number.
        /// Note that if two or more statements declaring gesture event handlers are executed with 
        /// the same GR class and the same GR configuration, but different priority numbers are used, then only
        /// the last priority number set before the first of those statements will be considered.
        /// </summary>
        /// <param name="pn">The priority number.</param>
        public void SetPriorityNumber(int pn)
        {
            m_grPriorityNumber = pn;
        }
        
        /// <summary>
        /// Register a handler for a gesture event. The GR will be will be associated with the default 
        /// configuration.
        /// </summary>
        /// <param name="grType">Type of the gesture recognizer.</param>
        /// <param name="e">The event (as specified in the proper enumeration in the GR class).</param>
        /// <param name="handler">The listener's function that will be called when the event is raised.</param>
        
        public void RegisterHandler(Type grType, Enum e, GestureEventHandler handler)
        {
            RegisterHandler(grType, GestureRecognizer.DefaultConfiguration, e, handler);
        }
        /// <summary>
        /// Register a handler for a gesture event. A configuration for the GR can be specified.
        /// </summary>
        /// <param name="grType">Type of the gesture recognizer.</param>
        /// <param name="grParam">The GR's configuration.</param>
        /// <param name="e">The event (as specified in the proper enumeration in the GR class).</param>
        /// <param name="handler">The listener's function that will be called when the event is raised.</param>
        public void RegisterHandler(Type grType, GRConfiguration grConf, Enum e, GestureEventHandler handler)
        {
            s_grRegistry.RegisterHandler(grType, grConf, m_grPriorityNumber, e, handler);
        }
        #endregion

        #region Private or internal methods
        internal void UnregisterAllHandlersOf(IGestureListener listener)
        {
            s_grRegistry.UnregisterAllHandlers(listener);
        }
        #endregion
    }
}