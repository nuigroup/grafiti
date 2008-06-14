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
        //private static GestureEventManager m_instance = null;

        private GRRegistry m_grRegistry;
        private object m_defaultGgrParam;
        private int m_grPriorityNumber;

        internal GRRegistry GRRegistry { get { return m_grRegistry; } }

        public GestureEventManager()
        {
            m_grRegistry = new GRRegistry();
            m_defaultGgrParam = new object();
            m_grPriorityNumber = 0;
        }

        //public static GestureEventManager Instance
        //{
        //    get
        //    {
        //        if (m_instance == null)
        //            m_instance = new GestureEventManager();
        //        return m_instance;
        //    }
        //}

        public void SetPriorityNumber(int pn)
        {
            m_grPriorityNumber = pn;
        }

        public void RegisterHandler(
            Type grType,                    // gesture recognizer's type
            Enum e,                         // gesture recognizer's event
            GestureEventHandler handler     // listener's handler
            )
        {
            RegisterHandler(grType, m_defaultGgrParam, e, handler);
        }

        /// <summary>
        /// Clients can use this function to register a handler for a gesture event.
        /// </summary>
        /// <param name="grType">Type of the gesture recognizer.</param>
        /// <param name="e">The event (as specified in the proper enumeration in the GR class).</param>
        /// <param name="listener">The listener object.</param>
        /// <param name="handler">The listener's function that serves as a handler.</param>
        public void RegisterHandler(
            Type grType,                    // gesture recognizer's type
            object grParam,                 // gesture recognizer's ctor's param
            Enum e,                         // gesture recognizer's event
            GestureEventHandler handler     // listener's handler
            )
        {
            m_grRegistry.RegisterHandler(grType, grParam, m_grPriorityNumber, e, handler);
        }

        internal void UnregisterAllHandlers(IGestureListener listener)
        {
            m_grRegistry.UnregisterAllHandlers(listener);
        }
    }
}