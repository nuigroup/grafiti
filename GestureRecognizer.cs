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
        private Enum m_eventId;

        public Enum EventId { get { return m_eventId; } }

        public GestureEventArgs()
        {
            m_eventId = null;
        }
        public GestureEventArgs(Enum id)
        {
            m_eventId = id;
        }

        // public abstract Clone() ?
    }


    /// <summary>
    /// WORK IN PROGRESS...
    /// Output delievered by a gesture recognizer to the caller of the function Recognize(), that is:
    /// bool recognizing: true if the GR is still attempting to recognize the gesture
    /// bool interpreting: true if (the gesture is recognized and) the GR will process incoming input
    /// GestureEventArgs gestureEventArgs: gesture event data
    /// float probability: probability of recognition
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


    public delegate void GestureEventHandler(object gestureRecognizer, GestureEventArgs args);




    public abstract class GestureRecognizer
    {
        private int m_priorityNumber;

        private readonly object m_ctorParam;

        private Group m_group;

        private bool m_exclusive;
        private bool m_armed;
        private List<GestureEventHandler> m_loadedHandlers;
        private List<GestureEventArgs> m_loadedArgs;

        internal int PriorityNumber { get { return m_priorityNumber; } set { m_priorityNumber = value; } }
        
        internal bool Armed { get { return m_armed; } set { m_armed = value; } }


        #region CLIENT-RELATED PARAMETERS

        // If the following is set to true, a successful recognition of the Process function will block
        // the callings to successive GRs (in the calling order).
        // On the other hand, GRs that are not exclusive they allow successive GRs to process data and
        // to send events as well.
        // Should be defined only in the constructor.
        public bool Exclusive { get { return m_exclusive; } set { m_exclusive = value; } }

        // This object will be passed as parameter to the constructor. It can be used to configure its
        // behaviour and/or to give it access to some resource.
        protected object CtorParam { get { return m_ctorParam; } }


        // The associated group to process
        public Group Group { get { return m_group; } internal set { m_group = value; } }

        #endregion


        public GestureRecognizer(object ctorParam)
        {
            m_ctorParam = ctorParam;
            m_exclusive = true;
            m_armed = false;
            m_loadedHandlers = new List<GestureEventHandler>();
            m_loadedArgs = new List<GestureEventArgs>();
        }

        internal abstract void AddHandler(Enum e, object listener, GestureEventHandler handler);

        internal System.Reflection.EventInfo GetEventInfo(Enum e)
        {
            Debug.Assert(GetType().GetEvent(e.ToString()) != null);
            return GetType().GetEvent(e.ToString());
        }

        //GestureRecognitionResult Process(List<bool> updateTraces);
        //GestureRecognitionResult Process(List<int> totalNumberOfFrames);

        public abstract GestureRecognitionResult Process(Trace trace);

        /// <summary>
        /// Use this method to send events. If the GRs is not armed (e.g. it's in competition with another
        /// GR), events will be scheduled and raised as soon as the GR will be armed.
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="args"></param>
        protected void AppendEvent(GestureEventHandler handler, GestureEventArgs args)
        {
            if (handler != null)
            {
                if (m_armed)
                    handler(this, args);
                else
                {
                    m_loadedHandlers.Add((GestureEventHandler)handler.Clone());
                    m_loadedArgs.Add(args); // or clone?
                }
            }
        }

        internal void ProcessPendlingEvents()
        {
            Debug.Assert(m_armed);

            for (int i = 0; i < m_loadedHandlers.Count; i++)
                m_loadedHandlers[i](this, m_loadedArgs[i]);
            m_loadedHandlers.Clear();
            m_loadedArgs.Clear();
        }
    }
}
