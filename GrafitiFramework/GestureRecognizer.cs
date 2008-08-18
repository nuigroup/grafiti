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
    /// Default event handler for gestures
    /// </summary>
    /// <param name="gestureRecognizer">Instance of a class deriving from GestureRecognizer that called 
    /// the event</param>
    /// <param name="args">The object containing the arguments data of the gesture event.</param>
    public delegate void GestureEventHandler(object obj, GestureEventArgs args);

    /// <summary>
    /// Base class for gesture recognizers' configurator objects. A configurator contains all the
    /// informations to parametrize or the behaviour of the GR, or to make it access to some resource.
    /// </summary>
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
    
    /// <summary>
    /// Base class of gesture recognizers. Instances of this class will be created dynamically by
    /// Grafiti. The constructor optionally expects a configurator object. A gesture recognizer 
    /// will process data of a single instance of the class Group.
    /// </summary>
    public abstract class GestureRecognizer
    {
        #region Private or internal members
        internal static readonly GRConfigurator DefaultConfigurator = new GRConfigurator();

        private GRConfigurator m_configurator;
        private int m_priorityNumber;
        private Group m_group;
        private bool m_armed;
        private List<GestureEventHandler> m_bufferedHandlers;
        private List<GestureEventArgs> m_bufferedArgs;
        private int m_maxNumberOfFingersAllowed = -1;
        private int m_debug_NProcessCalls = 0;

        // The result state, in base of which the GRs are coordinated by the group GR manager,
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
        /// Probability of successful recognition (0 = plain failure, 1 = plain success).
        /// </summary>
        private float m_probability;
        /// <summary>
        /// Will process further incoming input.
        /// </summary>
        private bool m_processing;

        // These are public but intended to be internal
        public bool Recognizing { get { return m_recognizing; } }
        public bool Successful { get { return m_successful; } }
        public bool Processing { get { return m_processing; } }
        public float Probability { get { return m_probability; } }

        internal int PriorityNumber { get { return m_priorityNumber; } set { m_priorityNumber = value; } }
        internal bool Armed { get { return m_armed; } set { m_armed = value; } } 
        #endregion

        #region Public members
        // This object will be passed as parameter to the constructor. It can be used to configure its
        // behaviour and/or to give it access to some resources.
        // Should be set once in the constructor.
        public GRConfigurator Configurator { get { return m_configurator; } protected set { m_configurator = value; } }

        // The associated group to process
        public Group Group { get { return m_group; } internal set { m_group = value; } }

        /// <summary>
        /// Max number of finger allowed. When the GR has successfully recognized a gesture, it is exclusive and
        /// it is armed, then the group will be limited by this number of fingers. However if at the moment
        /// of the arming there are a higher number of fingers they won't be removed: the value is
        /// significative only when adding new fingers.
        /// </summary>
        public int MaxNumberOfFingersAllowed 
        { 
            get { return m_maxNumberOfFingersAllowed; } 
            set { m_maxNumberOfFingersAllowed = value; } 
        }
        #endregion

        #region Constructor
        public GestureRecognizer(GRConfigurator configurator)
        {
            m_configurator = configurator;
            m_armed = false;
            m_bufferedHandlers = new List<GestureEventHandler>();
            m_bufferedArgs = new List<GestureEventArgs>();

            m_recognizing = true;
            m_successful = false;
            m_probability = 1;
            m_processing = true;
        }
        #endregion

        #region Private or internal methods
        internal abstract void AddHandler(string ev, GestureEventHandler handler);
        internal System.Reflection.EventInfo GetEventInfo(string ev)
        {
            Debug.Assert(GetType().GetEvent(ev) != null);
            return GetType().GetEvent(ev);
        }
        internal void Process1(List<Trace> traces)
        {
            m_debug_NProcessCalls++;
            Debug.WriteLine("N Process calls: " + m_debug_NProcessCalls + ", in " + this);
            Process(traces);
        }
        internal void ProcessPendlingEvents()
        {
            Debug.Assert(m_armed);

            for (int i = 0; i < m_bufferedHandlers.Count; i++)
                m_bufferedHandlers[i](this, m_bufferedArgs[i]);
            m_bufferedHandlers.Clear();
            m_bufferedArgs.Clear();
        }
        internal void OnGroupRemoval1()
        {
            OnGroupRemoval();
        } 
        #endregion

        #region Public or protected methods
        /// <summary>
        /// The main function that will process the user input. It will be called on every refresh
        /// of the TUIO messages.
        /// </summary>
        /// <param name="traces">The list of the updated traces, to which one element has been added to their cursor list.</param>
        /// <returns></returns>
        public abstract void Process(List<Trace> traces);

        #region Changing state methods
        protected void GestureHasBeenRecognized()
        {
            GestureHasBeenRecognized(true, 1f);
        }
        protected void GestureHasBeenRecognized(bool successful)
        {
            GestureHasBeenRecognized(successful, 1f);
        }
        protected void GestureHasBeenRecognized(bool successful, float probability)
        {
            Debug.Assert(m_recognizing || (m_successful == successful && m_probability == probability));
            if (m_recognizing)
            {
                m_recognizing = false;
                m_successful = successful;
                m_probability = probability;
                if (!successful)
                    Terminate();
            }
        }
        protected void Terminate()
        {
            Terminate(false);
        }
        protected void Terminate(bool successfulRecognition)
        {
            if (m_recognizing)
                GestureHasBeenRecognized(successfulRecognition);
            m_processing = false;
        }
        #endregion


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

        /// <summary>
        /// Called when the group has been definetly removed from the surface, this happens
        /// after Settings.TRACE_TIME_GAP ms from when all the traces have been removed
        /// Override this to handle the finalization of the lgr if needed.
        /// </summary>
        protected virtual void OnGroupRemoval() { } 
        #endregion
    }
}
