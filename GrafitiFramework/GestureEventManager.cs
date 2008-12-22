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
    public static class GestureEventManager
    {
        #region Private members
        private static readonly object s_lock = new object();
        private static GRConfiguration s_defaultGRConfiguration = new GRConfiguration(false);
        #endregion

        #region Client's interface
        /// <summary>
        /// Sets the priority number to associate with the given gesture recognizer class and its default
        /// configuration. However note that when registering a gesture event handler with the same GR type,
        /// no configuration must be passed as parameter in order to associate it with the priority number 
        /// specified.
        /// Note that once a priority number is set it can't be changed.
        /// </summary>
        /// <param name="grType"></param>
        /// <param name="priorityNumber"></param>
        public static void SetPriorityNumber(Type grType, int priorityNumber)
        {
            SetPriorityNumber(grType, GestureRecognizer.DefaultConfiguration, priorityNumber);
        }
        /// <summary>
        /// Sets the priority number to associate with the given gesture recognizer class and the
        /// given configuration.
        /// Note that once a priority number is set it can't be changed.
        /// </summary>
        /// <param name="grType">Type of the gesture recognizer.</param>
        /// <param name="configuration">Configuration of the gesture recognizer.</param>
        /// <param name="priorityNumber">Priority number.</param>
        public static void SetPriorityNumber(Type grType, GRConfiguration configuration, int priorityNumber)
        {
            lock (s_lock)
            {
                GestureEventRegistry.SetPriorityNumber(grType, configuration, priorityNumber);
            }
        }
        /// <summary>
        /// Registers a handler for a gesture event. The GR will be will be configured by default.
        /// </summary>
        /// <param name="grType">Type of the gesture recognizer.</param>
        /// <param name="e">The event as string.</param>
        /// <param name="handler">The listener's function that will be called when the event is raised.</param>
        public static void RegisterHandler(Type grType, string ev, GestureEventHandler handler)
        {
            RegisterHandler(grType, GestureRecognizer.DefaultConfiguration, ev, handler);
        }
        /// <summary>
        /// Registers a handler for a gesture event. The GR will be configured with the given configuration.
        /// </summary>
        /// <param name="grType">Type of the gesture recognizer.</param>
        /// <param name="grConf">The GR's configuration.</param>
        /// <param name="e">The event as string.</param>
        /// <param name="handler">The listener's function that will be called when the event is raised.</param>
        public static void RegisterHandler(Type grType, GRConfiguration grConf, string ev, GestureEventHandler handler)
        {
            lock (s_lock)
            {
                GestureEventRegistry.RegisterHandler(grType, grConf, ev, handler);
            }
        }

        public static void UnregisterHandler(Type grType, string ev, GestureEventHandler handler)
        {
            UnregisterHandler(grType, GestureRecognizer.DefaultConfiguration, ev, handler);
        }
        
        public static void UnregisterHandler(Type grType, GRConfiguration grConf, string ev, GestureEventHandler handler)
        {
            lock (s_lock)
            {
                GestureEventRegistry.UnregisterHandler(grType, grConf, ev, handler);
            }
        }
        /// <summary>
        /// Unregisters all registered handlers for the given listener.
        /// </summary>
        /// <param name="listener">The listener</param>
        public static void UnregisterAllHandlersOf(IGestureListener listener)
        {
            lock (s_lock)
            {
                GestureEventRegistry.UnregisterAllHandlers(listener);
            }
        }
        #endregion
    }
}