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
    /// Base class for gesture event data.
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
    }

    /// <summary>
    /// Default event handler for gestures.
    /// </summary>
    /// <param name="obj">The object that raised the event</param>
    /// <param name="args">The object containing the arguments data of the event.</param>
    public delegate void GestureEventHandler(object obj, GestureEventArgs args);

    /// <summary>
    /// Base class for gesture recognizers' configuration objects. A configuration contains all the
    /// informations to parametrize the behaviour of the GR, or to make it access some resources.
    /// </summary>
    public class GRConfiguration
    {
        protected readonly bool m_exclusive;

        // If the following is set to true, a successful recognition of the Process function will block
        // the callings to successive GRs (in the calling order).
        // On the other hand, GRs that are not exclusive they allow successive GRs to process data and
        // to send events as well.
        public bool Exclusive { get { return m_exclusive; } }

        public GRConfiguration() : this(false) { }
        
        public GRConfiguration(bool exclusive)
        {
            m_exclusive = exclusive;
        }
    }
    
    /// <summary>
    /// Base class of gesture recognizers (GRs). A GR, that is dynamically instantiated by Grafiti,
    /// processes data of a single instance of the class Group for the recognition of a gesture.
    /// In case of successful recognition the GR can be enabled to send gesture events to the 
    /// registered listeners. A GR can be parametrized through a configuration at the time
    /// of registration of a handler.
    /// </summary>
    public abstract class GestureRecognizer
    {
        #region Private or internal members
        internal static readonly GRConfiguration DefaultConfiguration = new GRConfiguration();

        private GRConfiguration m_configuration;
        private int m_priorityNumber;
        private Group m_group;
        private bool m_armed;
        private List<GestureEventHandler> m_bufferedHandlers;
        private List<GestureEventArgs> m_bufferedArgs;
        private int m_maxNumberOfFingersAllowed = -1;
        private int m_debug_NProcessCalls = 0;

        // The recognition state, in base of which the GRs are coordinated by the group GR manager,
        // is composed by the following four parameters.
        /// <summary>
        /// Still attempting to recognize the gesture.
        /// </summary>
        private bool m_recognizing;
        /// <summary>
        /// Gesture successfully recognized.
        /// </summary>
        private bool m_successful;
        /// <summary>
        /// Confidence of successful recognition (0 = plain failure, 1 = plain success).
        /// </summary>
        private float m_confidence;
        /// <summary>
        /// Will process further incoming input.
        /// </summary>
        private bool m_processing;

        // These are public but intended to be internal
        public bool Recognizing { get { return m_recognizing; } }
        public bool Successful { get { return m_successful; } }
        public float Confidence { get { return m_confidence; } }
        public bool Processing { get { return m_processing; } }

        internal int PriorityNumber { get { return m_priorityNumber; } set { m_priorityNumber = value; } }
        internal bool Armed { get { return m_armed; } set { m_armed = value; } } 
        #endregion

        #region Public members
        /// <summary>
        /// Object that will be passed as parameter to the constructor. It can be used to configure
        /// the GR's behaviour and/or to give it access to some resources.
        /// It should be set once in the constructor.
        /// </summary>
        public GRConfiguration Configuration { get { return m_configuration; } protected set { m_configuration = value; } }

        /// <summary>
        /// The associated group to process 
        /// </summary>
        public Group Group { get { return m_group; } internal set { m_group = value; } }

        /// <summary>
        /// Max number of finger allowed. When the GR has successfully recognized a gesture, it is exclusive and
        /// it is armed, then the group will be limited by this number of fingers. However if at the moment
        /// of the arming there is a higher number of fingers they won't be removed: the value is
        /// significative only when adding new fingers (i.e. addings will be inhibited).
        /// </summary>
        public int MaxNumberOfFingersAllowed 
        { 
            get { return m_maxNumberOfFingersAllowed; } 
            set { m_maxNumberOfFingersAllowed = value; } 
        }
        #endregion

        #region Constructor
        public GestureRecognizer(GRConfiguration configuration)
        {
            m_configuration = configuration;
            m_armed = false;
            m_bufferedHandlers = new List<GestureEventHandler>();
            m_bufferedArgs = new List<GestureEventArgs>();

            m_recognizing = true;
            m_successful = false;
            m_confidence = 1;
            m_processing = true;
        }
        #endregion

        #region Private or internal methods
        internal abstract void AddHandler(string ev, GestureEventHandler handler);
        internal System.Reflection.EventInfo GetEventInfo(string ev)
        {
            Debug.Assert(GetType().GetEvent(ev) != null, "Attempting to access unexisting event named " + ev +
                " for class " + GetType().ToString());
            return GetType().GetEvent(ev);
        }
        internal void Process1(List<Trace> traces)
        {
            //m_debug_NProcessCalls++;
            //Debug.WriteLine("N Process calls: " + m_debug_NProcessCalls + ", in " + this);
            Process(traces);
        }
        internal void RaisePendlingEvents()
        {
            Debug.Assert(m_armed);

            for (int i = 0; i < m_bufferedHandlers.Count; i++)
                m_bufferedHandlers[i](this, m_bufferedArgs[i]);
            m_bufferedHandlers.Clear();
            m_bufferedArgs.Clear();
        }
        internal void OnTerminating1()
        {
            //Console.WriteLine("Terminating GR " + this);
            OnTerminating();
        } 
        #endregion

        #region Public or protected methods
        /// <summary>
        /// The main function that will process the user input. It will be called on every refresh
        /// of the TUIO messages.
        /// </summary>
        /// <param name="traces">The list of the updated traces, to which one element has been added to their cursor list.</param>
        public abstract void Process(List<Trace> traces);

        #region Recognition state-transition methods
        protected void ValidateGesture()
        {
            ValidateGesture(1f);
        }
        protected void ValidateGesture(float confidence)
        {
            Debug.WriteLine(m_recognizing, "Warning: GestureHasBeenRecognized has been called more than once (class " 
                + GetType().ToString() +").");

            if (m_recognizing)
            {
                m_recognizing = false;
                m_successful = true;
                m_confidence = confidence;
            }
        }
        protected void Terminate(bool successful)
        {
            Terminate(successful, 1f);
        }
        protected void Terminate(bool successful, float confidence)
        {
            Debug.WriteLine(m_processing, "Warning: Terminate has been called more than once (class "
                + GetType().ToString() + ").");

            if (m_processing)
            {
                if (m_recognizing)
                {
                    m_recognizing = false;
                    m_successful = successful;
                    m_confidence = confidence;
                }
                m_processing = false;
            }
        }
        #endregion


        /// <summary>
        /// Use this method to send events. If the GR is not armed (e.g. it's in competition
        /// with other GRs), events will be scheduled in a queue and raised as soon as the GR 
        /// will be armed. If the GR is already armed, events are raised immediately.
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
                    m_bufferedArgs.Add(args);
                }
            }
        }

        /// <summary>
        /// Called when the GR is going to be removed from the list of active GRs and thus it won't
        /// be called anymore. Override this to handle the finalization of the GR if needed,
        /// like terminating threads, freeing resources, send terminating events...
        /// From now on, recognition state transitions will be in fact ignored.
        /// Note: at least one of the following reasons causes this method to be called:
        /// - the GR has been explicitly put in the 'terminated' state through one of the relative 
        ///   methods called in its Process function
        /// - the group ceased to be active i.e. all the fingers have been removed for a sufficient
        ///   time, such that the traces can't be reset anymore
        /// - an exclusive GR with precedence has won
        /// In case of an LGR the following reasons are also possible:
        /// - its target has been removed from the group's LGR-list
        /// - an LGR with different target has the precedence and has won
        /// </summary>
        protected virtual void OnTerminating() { } 
        #endregion
    }
}
