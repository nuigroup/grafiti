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
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using TUIO;

namespace Grafiti
{
    /// <summary>
    /// Derived classes will include gesture event data
    /// </summary>
    public class GestureEventArgs : EventArgs
    {
        private string m_eventId;
        private int m_groupId;

        public string EventId { get { return m_eventId; } }
        public int GroupId { get { return m_groupId; } }

        public GestureEventArgs()
        {
            m_eventId = "";
            m_groupId = -1;
        }
        public GestureEventArgs(string eventId, int groupId)
        {
            m_eventId = eventId;
            m_groupId = groupId;
        }

        // public abstract Clone() ?
    }


    /// <summary>
    /// Output delievered by a gesture recognizer to the caller of the function Recognize(), that is:
    /// bool recognizing: true iff the GR is still attempting to recognize the gesture
    /// successful: true iff the GR has recognized successfully a gesture
    /// bool interpreting: true iff (the gesture is recognized and) the GR will process further incoming input
    /// float probability: probability of successful recognition (0=plain failure, 1=plain success)
    /// </summary>
    public class GestureRecognitionResult
    {
        private bool m_recognizing;
        private bool m_successful;
        private bool m_interpreting;
        private float m_probability;

        internal bool Recognizing { get { return m_recognizing; } }
        internal bool Successful { get { return m_successful; } }
        internal bool Interpreting { get { return m_interpreting; } }
        internal float Probability { get { return m_probability; } }

        public GestureRecognitionResult(bool recognizing, bool successful, bool interpreting) :
            this(recognizing, successful, interpreting, 1) { }

        public GestureRecognitionResult(bool recognizing, bool successful, bool interpreting, float probability)
        {
            m_recognizing = recognizing;
            m_successful = successful;
            m_interpreting = interpreting;
            m_probability = probability;
        }
    }

    // Default event handler for gestures
    public delegate void GestureEventHandler(object gestureRecognizer, GestureEventArgs args);


    public class GRConfigurator
    {
        protected readonly bool m_exclusive;

        // If the following is set to true, a successful recognition of the Process function will block
        // the callings to successive GRs (in the calling order).
        // On the other hand, GRs that are not exclusive they allow successive GRs to process data and
        // to send events as well.
        public bool Exclusive { get { return m_exclusive; } }

        public GRConfigurator() : this(false) { }
        
        public GRConfigurator(bool exclusive)
        {
            m_exclusive = exclusive;
        }
    }

    public abstract class GestureRecognizer
    {
        private static GRConfigurator s_defaultConfigurator = null;
        private GRConfigurator m_configurator;
        private int m_priorityNumber;
        private Group m_group;
        internal bool m_recognizing = true, m_successful = false, m_interpreting = true;
        internal float m_probabilityOfSuccess = 1;
        private bool m_armed;
        private List<GestureEventHandler> m_bufferedHandlers;
        private List<GestureEventArgs> m_bufferedArgs;

        private int m_debug_NProcessCalls = 0;

        internal static GRConfigurator DefaultConfigurator
        {
            get
            {
                if (s_defaultConfigurator == null)
                    s_defaultConfigurator = new GRConfigurator();
                return s_defaultConfigurator; 
            }
        }
        internal int PriorityNumber { get { return m_priorityNumber; } set { m_priorityNumber = value; } }
        internal bool Armed { get { return m_armed; } set { m_armed = value; } }


        #region CLIENT-RELATED PARAMETERS
        // This object will be passed as parameter to the constructor. It can be used to configure its
        // behaviour and/or to give it access to some resources.
        // Should be set once in the constructor.
        public GRConfigurator Configurator { get { return m_configurator; } protected set { m_configurator = value; } }

        // The associated group to process
        public Group Group { get { return m_group; } internal set { m_group = value; } }

        #endregion


        public GestureRecognizer(GRConfigurator configurator)
        {
            m_configurator = configurator;
            m_armed = false;
            m_bufferedHandlers = new List<GestureEventHandler>();
            m_bufferedArgs = new List<GestureEventArgs>();
        }

        internal abstract void AddHandler(string ev, GestureEventHandler handler);

        internal System.Reflection.EventInfo GetEventInfo(string ev)
        {
            Debug.Assert(GetType().GetEvent(ev) != null);
            return GetType().GetEvent(ev);
        }

        /// <summary>
        /// The main function that will process the user input. It will be called on every refresh
        /// of the TUIO messages.
        /// </summary>
        /// <param name="traces">The list of the updated traces, to which one element has been added to their cursor list.</param>
        /// <returns></returns>
        public abstract GestureRecognitionResult Process(List<Trace> traces);

        internal GestureRecognitionResult Process1(List<Trace> traces)
        {
            m_debug_NProcessCalls++;
            Debug.WriteLine("N Process calls: " + m_debug_NProcessCalls + ", in " + this);
            return Process(traces);
        }

        /// <summary>
        /// Use this method to send events. If the GRs is not armed (e.g. it's in competition with another
        /// GR), events will be scheduled and raised as soon as the GR will be armed.
        /// </summary>
        /// <param name="ev">The event</param>
        /// <param name="args">The event's arguments</param>
        protected void AppendEvent(GestureEventHandler ev, GestureEventArgs args)
        {
            if (ev != null)
            {
                if (m_armed)
                    ev(this, args);
                else
                {
                    m_bufferedHandlers.Add((GestureEventHandler)ev.Clone());
                    m_bufferedArgs.Add(args); // or clone?
                }
            }
        }

        internal void ProcessPendlingEvents()
        {
            Debug.Assert(m_armed);

            for (int i = 0; i < m_bufferedHandlers.Count; i++)
                m_bufferedHandlers[i](this, m_bufferedArgs[i]);
            m_bufferedHandlers.Clear();
            m_bufferedArgs.Clear();
        }
    }
}
