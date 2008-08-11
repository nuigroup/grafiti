﻿/*
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
            CLOSEST_LEAVING,
            CLOSEST_INITIAL,
            CLOSEST_NEWINITIAL,
            CLOSEST_FINAL
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
        protected string[] ClosestInitialEvents = new string[0] { };
        protected string[] ClosestNewInitialEvents = new string[0] { };
        protected string[] ClosestFinalEvents = new string[0] { };

        private bool m_freezeEventHandlerLists = false;


        // GR developers can use this (in association with the predefined lists), to manage the handlers
        // of events when the predefined lists don't give a sufficient support.
        // It associates a <event, listener> with a list of listener's handlers.
        // TODO: return as readonly
        protected DoubleDictionary<EventInfo, object, List<GestureEventHandler>> HandlerTable
        {
            get { return m_handlerTable; }
        }

        // Auxiliar list used instead of creating a new instance each time.
        List<IGestureListener> m_temporaryTargetSingletonList;

        public GlobalGestureRecognizer(GRConfigurator configurator) : base(configurator)
        {
            m_handlerTable = new DoubleDictionary<EventInfo, object, List<GestureEventHandler>>();
            m_temporaryHandlerTable = new DoubleDictionary<TargetList, EventInfo, List<GestureEventHandler>>();
            m_temporaryTargetSingletonList = new List<IGestureListener>(1);
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

        protected void FreezeEventHandlerLists()
        {
            m_freezeEventHandlerLists = true;
        }
        protected void UnFreezeEventHandlerLists()
        {
            m_freezeEventHandlerLists = false;
        }


        /// <summary>
        /// Update handlers to the events for targets appearing in the predefined target lists
        /// </summary>
        internal void UpdateHandlers(
            bool initial, bool final, bool entering, bool current, bool leaving,
            bool intersect, bool union, 
            bool newClosestEnt, bool newClosestCur, bool newClosestLvn, bool newClosestIni, bool newClosestFin)
        {
            OnPreUpdateHandlers(initial, final, entering, current, leaving,
                intersect, union, newClosestEnt, newClosestCur, newClosestLvn, newClosestIni, newClosestFin);

            if (!m_freezeEventHandlerLists)
            {

                if (initial)
                {
                    UpdateHandlers(TargetList.INITIAL);
                    UpdateHandlers(TargetList.NEWINITIAL);
                    UpdateHandlers(TargetList.CLOSEST_NEWINITIAL);
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
                if (newClosestIni)
                    UpdateHandlers(TargetList.CLOSEST_INITIAL);
                if (newClosestFin)
                    UpdateHandlers(TargetList.CLOSEST_FINAL);
            }
            OnPostUpdateHandlers(initial, final, entering, current, leaving,
                intersect, union, newClosestEnt, newClosestCur, newClosestLvn, newClosestIni, newClosestFin);
        }
        protected virtual void OnPreUpdateHandlers(bool initial, bool final, bool entering, bool current, bool leaving,
            bool intersect, bool union, bool newClosestEnt, bool newClosestCur, bool newClosestLvn, bool newClosestIni, bool newClosestFin) { }

        protected virtual void OnPostUpdateHandlers(bool initial, bool final, bool entering, bool current, bool leaving,
            bool intersect, bool union, bool newClosestEnt, bool newClosestCur, bool newClosestLvn, bool newClosestIni, bool newClosestFin) { }

        private void UpdateHandlers(TargetList targetList)
        {
            List<IGestureListener> groupTargetList;
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
            else // Using temporary singleton list
            {
                // Assign groupTargetList to the cleared temp list
                if (m_temporaryTargetSingletonList.Count > 0)
                    m_temporaryTargetSingletonList.RemoveAt(0);
                groupTargetList = m_temporaryTargetSingletonList;

                if (targetList == TargetList.CLOSEST_ENTERING)
                {
                    if (Group.ClosestEnteringTarget != null)
                        groupTargetList.Add(Group.ClosestEnteringTarget);
                    eventList = this.ClosestEnteringEvents;
                }
                else if (targetList == TargetList.CLOSEST_CURRENT)
                {
                    if (Group.ClosestCurrentTarget != null)
                        groupTargetList.Add(Group.ClosestCurrentTarget);
                    eventList = this.ClosestCurrentEvents;
                }
                else if (targetList == TargetList.CLOSEST_LEAVING)
                {
                    if (Group.ClosestLeavingTarget != null)
                        groupTargetList.Add(Group.ClosestLeavingTarget);
                    eventList = this.ClosestLeavingEvents;
                }
                else if (targetList == TargetList.CLOSEST_INITIAL)
                {
                    if (Group.ClosestInitialTarget != null)
                        groupTargetList.Add(Group.ClosestInitialTarget);
                    eventList = this.ClosestInitialEvents;
                }
                else if (targetList == TargetList.CLOSEST_NEWINITIAL)
                {
                    if (Group.ClosestNewInitialTarget != null)
                        groupTargetList.Add(Group.ClosestNewInitialTarget);
                    eventList = this.ClosestNewInitialEvents;
                }
                else if (targetList == TargetList.CLOSEST_FINAL)
                {
                    if (Group.ClosestFinalTarget != null)
                        groupTargetList.Add(Group.ClosestFinalTarget);
                    eventList = this.ClosestFinalEvents;
                }
                else
                    throw new Exception("Invalid parameter.");
            }


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
                    if (m_temporaryHandlerTable.ContainsKeys(targetList, eventInfo))
                        m_temporaryHandlerTable[targetList, eventInfo].Clear();
                    else
                        m_temporaryHandlerTable[targetList, eventInfo] = new List<GestureEventHandler>();
                    foreach (IGestureListener target in groupTargetList)
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