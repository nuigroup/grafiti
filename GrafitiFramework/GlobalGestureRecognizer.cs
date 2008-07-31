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
    public abstract class GlobalGestureRecognizer : GestureRecognizer
    {
        private DoubleDictionary<EventInfo, object, List<GestureEventHandler>> m_handlerTable;
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
            DEFAULT,
            CLOSEST_ENTERING,
            CLOSEST_CURRENT,
            CLOSEST_LEAVING
        }


        // GR developers have to declare through these lists, the events associated to the relative target list.
        // For example, to implement a multi-trace gesture recognizer there should be declared something like
        // <code>m_enteringEvents = new string[] { "MultiTraceEnter };</code>
        // where the client previously specified the Events enumeration, including  MultiTraceEnter.
        // The DefaultEvents list will include other events that are supposed to be sent to
        // every control that subscribed to them, disregarding whether such control appears in some predefined
        // list or not.
        protected string[] EnteringEvents = new string[0] { };
        protected string[] CurrentEvents = new string[0] { };
        protected string[] LeavingEvents = new string[0] { };
        protected string[] InitialEvents = new string[0] { };
        protected string[] NewInitialEvents = new string[0] { };
        protected string[] FinalEvents = new string[0] { };
        protected string[] IntersectionEvents = new string[0] { };
        protected string[] UnionEvents = new string[0] { };
        protected string[] DefaultEvents = new string[0] { };
        protected string[] ClosestEnteringEvents = new string[0] { };
        protected string[] ClosestCurrentEvents = new string[0] { };
        protected string[] ClosestLeavingEvents = new string[0] { };


        // GR developers can use this (in association with the predefined lists), to manage the handlers
        // of events when the predefined lists don't give a sufficient support.
        // It associates a <event, listener> with a list of listener's handlers.
        // TODO: return as readonly
        protected DoubleDictionary<EventInfo, object, List<GestureEventHandler>> HandlerTable
        {
            get { return m_handlerTable; }
        }

        public GlobalGestureRecognizer(GRConfiguration configuration) : base(configuration)
        {
            m_handlerTable = new DoubleDictionary<EventInfo, object, List<GestureEventHandler>>();
            m_temporaryHandlerTable = new DoubleDictionary<TargetList, EventInfo, List<GestureEventHandler>>();
        }

        internal override sealed void AddHandler(string ev, GestureEventHandler handler)
        {
            // Check id it's a default event
            bool isDefaultEvent = false;
            for (int i = 0; i < DefaultEvents.Length; i++)
            {
                if (DefaultEvents[i] == ev)
                {
                    isDefaultEvent = true;
                    break;
                }
            }

            EventInfo eventInfo = GetEventInfo(ev);
            object listener = handler.Target;

            if (isDefaultEvent)
                eventInfo.AddEventHandler(this, handler);
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
        internal void UpdateHandlers(bool initial, bool final, bool entering, bool current, bool leaving,
            bool intersect, bool union, bool newClosestEnt, bool newClosestCur, bool newClosestLvn)
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
            if (newClosestEnt)
                UpdateHandlers(TargetList.CLOSEST_ENTERING);
            if (newClosestCur)
                UpdateHandlers(TargetList.CLOSEST_CURRENT);
            if (newClosestLvn)
                UpdateHandlers(TargetList.CLOSEST_LEAVING);

            OnUpdateHandlers(initial, final, entering, current, leaving,
                intersect, union, newClosestEnt, newClosestCur, newClosestLvn);
        }

        protected virtual void OnUpdateHandlers(bool initial, bool final, bool entering, bool current, bool leaving,
            bool intersect, bool union, bool newClosestEnt, bool newClosestCur, bool newClosestLvn) { }

        private void UpdateHandlers(TargetList targetList)
        {
            List<ITuioObjectGestureListener> groupTargetList;
            string[] eventList;

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
                groupTargetList = Group.InitialTargets;
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
            else if (targetList == TargetList.CLOSEST_ENTERING)
            {
                groupTargetList = new List<ITuioObjectGestureListener>();
                if (Group.ClosestEnteringTarget != null)
                    groupTargetList.Add(Group.ClosestEnteringTarget);
                eventList = this.ClosestEnteringEvents;
            }
            else if (targetList == TargetList.CLOSEST_CURRENT)
            {
                groupTargetList = new List<ITuioObjectGestureListener>();
                if (Group.ClosestCurrentTarget != null)
                    groupTargetList.Add(Group.ClosestCurrentTarget);
                eventList = this.ClosestCurrentEvents;
            }
            else if (targetList == TargetList.CLOSEST_LEAVING)
            {
                groupTargetList = new List<ITuioObjectGestureListener>();
                if (Group.ClosestLeavingTarget != null)
                    groupTargetList.Add(Group.ClosestLeavingTarget);
                eventList = this.ClosestLeavingEvents;
            }
            else
                throw new Exception("Invalid parameter.");

            // TODO: optimize
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
                    foreach (ITuioObjectGestureListener target in groupTargetList)
                        if (m_handlerTable.ContainsKeys(eventInfo, target))
                            foreach (GestureEventHandler handler in m_handlerTable[eventInfo, target])
                            {
                                m_temporaryHandlerTable[targetList, eventInfo].Add(handler);
                                eventInfo.AddEventHandler(this, handler);
                                //Console.WriteLine("ADDED HANDLER " + handler.Target + "." + eventInfo);
                            }
                }
            }
        }
    } 
}