/*
	grafiti library

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

using System.Reflection;

using TUIO;

namespace grafiti
{

    /// <summary>

    /// Derived classes will include gesture event data

    /// </summary>

    public class GestureEventArgs : EventArgs { }

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

        public bool recognizing;

        public bool interpreting;

        public GestureEventArgs gestureEventArgs;

        public float probability;



        public GestureRecognitionResult(bool recognizing, bool interpreting, GestureEventArgs gestureEventArgs, float probability)
        {

            this.recognizing = recognizing;

            this.interpreting = interpreting;

            this.gestureEventArgs = gestureEventArgs;

            this.probability = probability;
        }
	}



    public delegate void GestureEventHandler(object gestureRecognizer, GestureEventArgs args);

	public abstract class GestureRecognizer
	{

        // The associated group

        private Group m_group;



        // Properties

        public Group Group { get { return m_group; } internal set { m_group = value; } }



        public GestureRecognizer() { }



        //void SetFirstFittingMode(bool firstFitting);



        //GestureRecognitionResult Process(List<bool> updateTraces);

        //GestureRecognitionResult Process(List<int> totalNumberOfFrames);

        public abstract GestureRecognitionResult Process(Trace trace);
	}







    public abstract class GlobalGestureRecognizer : GestureRecognizer

    {

        private DoubleDictionary<EventInfo, IGestureListener, List<GestureEventHandler>> m_handlerTable;

        private DoubleDictionary<TargetList, EventInfo, List<GestureEventHandler>> m_temporaryHandlerTable;



        private enum TargetList

        {

            ENTERING,

            CURRENT,

            LEAVING,

            INITIAL,

            NEWINITIAL,

            FINAL,

            INTERSECT,

            UNION,

            DEFAULT

        }





        // GR developers have to declare through these lists, the events associated to the relativr target list.

        // For example, to implement a multi-trace gesture recognizer there should be declared somthing like

        // <code>m_enteringEvents = new Enum[] { Events.MultiTraceEnter };</code>

        // where before the client specified the Events enumeration, including  MultiTraceEnter

        // The last list, i.e. m_defaultEvents will include other events that are supposed to be sent to

        // every control that subscribed to them, disregarding whether such control appears in some predefined

        // list or not.

        protected Enum[] EnteringEvents     = new Enum[0] { };

        protected Enum[] CurrentEvents      = new Enum[0] { };

        protected Enum[] LeavingEvents      = new Enum[0] { };

        protected Enum[] InitialEvents      = new Enum[0] { };

        protected Enum[] NewInitialEvents   = new Enum[0] { };

        protected Enum[] FinalEvents        = new Enum[0] { };

        protected Enum[] IntersectionEvents = new Enum[0] { };

        protected Enum[] UnionEvents        = new Enum[0] { };

        protected Enum[] DefaultEvents      = new Enum[0] { };





        // GR developers can use this (in association with the predefined lists), to manage the handlers

        // of events when the predefined lists don't give a sufficient support.

        // It associates a <event, listener> with a list of listener's handlers.

        // TODO: return as readonly

        protected DoubleDictionary<EventInfo, IGestureListener, List<GestureEventHandler>> HandlerTable{

            get { return m_handlerTable; }

        }



        public GlobalGestureRecognizer() : base()

        {

            m_handlerTable = new DoubleDictionary<EventInfo, IGestureListener, List<GestureEventHandler>>();

            m_temporaryHandlerTable = new DoubleDictionary<TargetList, EventInfo, List<GestureEventHandler>>();

        }



        // This is called by GGRProvider

        internal void AddHandler(Enum e, IGestureListener listener, GestureEventHandler handler)

        {

            // Check id it's a default event

            bool isDefaultEvent = false;

            for (int i = 0; i < DefaultEvents.Length; i++)

            {

                if (DefaultEvents[i] == e)

                {

                    isDefaultEvent = true;

                    break;

                }

            }



            EventInfo eventInfo = GetEventInfo(e);



            if (isDefaultEvent)

            {

                //GetEventInfo(e).GetAddMethod().Invoke(this, new object[] { handler });

                eventInfo.AddEventHandler(this, handler);

            }

            else

            {

                if (!m_handlerTable.ContainsKeys(eventInfo, listener))

                    m_handlerTable[eventInfo, listener] = new List<GestureEventHandler>();

                m_handlerTable[eventInfo, listener].Add(handler);

            }

                

        }



        /// <summary>

        /// Update handlers to the events for targets appearing in the predefined target lists

        /// </summary>

        internal void UpdateHandlers(bool initial, bool final, bool entering, bool current, bool leaving, bool intersect, bool union)

        {

            if (initial)

            {

                UpdateHandlers(TargetList.INITIAL);

                UpdateHandlers(TargetList.NEWINITIAL);

            }

            if (final)

                UpdateHandlers(TargetList.FINAL);

            if (entering)

                UpdateHandlers(TargetList.ENTERING);

            if (current)

                UpdateHandlers(TargetList.CURRENT);

            if (leaving)

                UpdateHandlers(TargetList.LEAVING);

            if (intersect)

                UpdateHandlers(TargetList.INTERSECT);

            if (union)

                UpdateHandlers(TargetList.UNION);

        }



        private void UpdateHandlers(TargetList targetList)

        {

            List<IGestureListener> groupTargetList;

            Enum[] eventList;



            if (targetList == TargetList.ENTERING)

            {

                groupTargetList = Group.EnteringTargets;

                eventList = EnteringEvents;

            }

            else if (targetList == TargetList.CURRENT)

            {

                groupTargetList = Group.CurrentTargets;

                eventList = CurrentEvents;

            }

            else if (targetList == TargetList.LEAVING)

            {

                groupTargetList = Group.LeavingTargets;

                eventList = LeavingEvents;

            }

            else if (targetList == TargetList.INITIAL)

            {

                groupTargetList = Group.IntialTargets;

                eventList = InitialEvents;

            }

            else if (targetList == TargetList.NEWINITIAL)

            {

                groupTargetList = Group.NewIntialTargets;

                eventList = NewInitialEvents;

            }

            else if (targetList == TargetList.FINAL)

            {

                groupTargetList = Group.FinalTargets;

                eventList = FinalEvents;

            }

            else if (targetList == TargetList.INTERSECT)

            {

                groupTargetList = Group.IntersectionTargets;

                eventList = IntersectionEvents;

            }

            else if (targetList == TargetList.UNION)

            {

                groupTargetList = Group.UnionTargets;

                eventList = UnionEvents;

            }

            else

                throw new Exception("Invalid parameter.");





            EventInfo eventInfo;

            for (int i = 0; i < eventList.Length; i++)

            {

                eventInfo = GetEventInfo(eventList[i]);



                // Remove old handlers

                if (targetList != TargetList.UNION) // UNION adds only

                {

                    if (m_temporaryHandlerTable.ContainsKeys(targetList, eventInfo))

                        foreach (Delegate handler in m_temporaryHandlerTable[targetList, eventInfo])

                        {

                            eventInfo.RemoveEventHandler(this, handler);

                            //Console.WriteLine("REMOVED HANDLER " + handler.Target + "." + eventInfo);

                        }

                }



                // Add new handlers

                if (targetList != TargetList.INTERSECT) // INTERSECT removes only

                {

                    m_temporaryHandlerTable[targetList, eventInfo] = new List<GestureEventHandler>();

                    foreach (IGestureListener target in groupTargetList)

                        if (m_handlerTable.ContainsKeys(eventInfo, target))

                            foreach (GestureEventHandler handler in m_handlerTable[eventInfo, target])

                            {

                                m_temporaryHandlerTable[targetList, eventInfo].Add(handler);

                                //eventInfo.GetAddMethod().Invoke(this, new object[] { handler });

                                eventInfo.AddEventHandler(this, handler);

                                //Console.WriteLine("ADDED HANDLER " + handler.Target + "." + eventInfo);

                            }

                }

            }

        }



        private System.Reflection.EventInfo GetEventInfo(Enum e)

        {

            return GetType().GetEvent(e.ToString());

        }

    }
}
