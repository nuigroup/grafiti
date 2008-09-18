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
using Grafiti;

namespace Grafiti.GestureRecognizers
{
    public class MultiTraceEventArgs : GestureEventArgs
    {
        protected int m_nOfFingers;
        protected float m_centroidX, m_centroidY;

        public int NOfFingers  { get { return m_nOfFingers; } }
        public float CentroidX { get { return m_centroidX; } }
        public float CentroidY { get { return m_centroidY; } }

        public MultiTraceEventArgs() 
            : base() { }

        public MultiTraceEventArgs(string eventId, int groupId, int nFingers, float centroidX, float centroidY)
            : base(eventId, groupId)
        {
            m_nOfFingers = nFingers;
            m_centroidX = centroidX;
            m_centroidY = centroidY;
        }
    }

    public delegate void MultiTraceEventHandler(object obj, MultiTraceEventArgs args);
 

    public class MultiTraceFromToEventArgs : MultiTraceEventArgs
    {
        protected IGestureListener m_fromTarget, m_toTarget;
        protected float m_initialCentroidX, m_initialCentroidY;

        public IGestureListener FromTarget { get { return m_fromTarget; } }
        public IGestureListener ToTarget { get { return m_toTarget; } }
        public float InitialCentroidX { get { return m_initialCentroidX; } }
        public float InitialCentroidY { get { return m_initialCentroidY; } }
        public float FinalCentroidX { get { return m_centroidX; } }
        public float FinalCentroidY { get { return m_centroidY; } }

        public MultiTraceFromToEventArgs() 
            : base() { }

        public MultiTraceFromToEventArgs(string eventId, int groupId, int nFingers, 
            IGestureListener fromTarget, IGestureListener toTarget,
            float initialCentroidX, float initialCentroidY, float finalCentroidX, float finalCentroidY)
            : base(eventId, groupId, nFingers, finalCentroidX, finalCentroidY)
        {
            m_fromTarget = fromTarget;
            m_toTarget = toTarget;
            m_initialCentroidX = initialCentroidX;
            m_initialCentroidY = initialCentroidY;
        }
    }

    public delegate void MultiTraceFromToEventHandler(object obj, MultiTraceFromToEventArgs args);


    public class MultiTraceGRConfigurator : GRConfigurator
    {
        public static readonly MultiTraceGRConfigurator DEFAULT_CONFIGURATOR = new MultiTraceGRConfigurator();

        public readonly float MAX_SQUARE_DISTANCE;
        private const float DEFAULT_MAX_SQUARE_DISTANCE = -1;

        public readonly bool RECOGNIZE_WHEN_ENDED;
        private const bool DEFAULT_RECOGNIZE_WHEN_ENDED = true;

        public readonly bool ALLOW_AUTOLINK;
        private const bool DEFAULT_ALLOW_AUTOLINK = true;

        public MultiTraceGRConfigurator()
            : this(DEFAULT_MAX_SQUARE_DISTANCE, DEFAULT_RECOGNIZE_WHEN_ENDED, DEFAULT_ALLOW_AUTOLINK) { }

        public MultiTraceGRConfigurator(bool exclusive)
            : this(exclusive, DEFAULT_MAX_SQUARE_DISTANCE, DEFAULT_RECOGNIZE_WHEN_ENDED, DEFAULT_ALLOW_AUTOLINK) { }

        public MultiTraceGRConfigurator(float maxSquareDistance, bool recognizeWhenEnded, bool allowAutolink)
            : base()
        {
            MAX_SQUARE_DISTANCE = maxSquareDistance;
            RECOGNIZE_WHEN_ENDED = recognizeWhenEnded;
            ALLOW_AUTOLINK = allowAutolink;
        }

        public MultiTraceGRConfigurator(bool exclusive, float maxSquareDistance, bool recognizeWhenEnded, bool allowAutolink)
            : base(exclusive)
        {
            MAX_SQUARE_DISTANCE = maxSquareDistance;
            RECOGNIZE_WHEN_ENDED = recognizeWhenEnded;
            ALLOW_AUTOLINK = allowAutolink;
        }
    }


    public class MultiTraceGR : GlobalGestureRecognizer
    {
        private MultiTraceGRConfigurator m_conf;
        private int m_startingTime = -1;
        private List<Trace> m_startingTraces;
        private int m_nOfFingers;
        private float m_initialCentroidX = -1, m_initialCentroidY = -1;

        // These are public only to make reflection to work.
        // They're not intended to be accessed directly from clients.
        public event GestureEventHandler MultiTraceStarted;
        public event GestureEventHandler MultiTraceDown;
        public event GestureEventHandler MultiTraceMove;
        public event GestureEventHandler MultiTraceUp;
        public event GestureEventHandler MultiTraceEnter;
        public event GestureEventHandler MultiTraceLeave;
        public event GestureEventHandler MultiTraceEnd;
        public event GestureEventHandler MultiTraceFromTo;

        public MultiTraceGR(GRConfigurator configurator) : base(configurator)
        {
            if (!(configurator is MultiTraceGRConfigurator))
                Configurator = MultiTraceGRConfigurator.DEFAULT_CONFIGURATOR;

            m_conf = (MultiTraceGRConfigurator)Configurator;
            
            ClosestInitialEvents  = new string[] { "MultiTraceStarted" };
            ClosestEnteringEvents = new string[] { "MultiTraceEnter" };
            ClosestLeavingEvents  = new string[] { "MultiTraceLeave" };
            ClosestCurrentEvents  = new string[] { "MultiTraceDown", "MultiTraceMove", "MultiTraceUp" };
            ClosestFinalEvents    = new string[] { "MultiTraceEnd" };
            DefaultEvents         = new string[] { "MultiTraceFromTo" };

            m_startingTime = -1;
            m_startingTraces = new List<Trace>();
            m_nOfFingers = 0;
        }

        private void OnMultiTraceStart()       { AppendEvent(MultiTraceStarted, new MultiTraceEventArgs("MultiTraceStarted", Group.Id, m_nOfFingers, Group.ActiveCentroidX, Group.ActiveCentroidY)); }
        private void OnMultiTraceEnd()         { AppendEvent(MultiTraceEnd,     new MultiTraceEventArgs("MultiTraceEnd",     Group.Id, m_nOfFingers, Group.ActiveCentroidX, Group.ActiveCentroidY)); }
        private void OnMultiTraceGestureDown() { AppendEvent(MultiTraceDown,    new MultiTraceEventArgs("MultiTraceDown",    Group.Id, m_nOfFingers, Group.ActiveCentroidX, Group.ActiveCentroidY)); }
        private void OnMultiTraceGestureMove() { AppendEvent(MultiTraceMove,    new MultiTraceEventArgs("MultiTraceMove",    Group.Id, m_nOfFingers, Group.ActiveCentroidX, Group.ActiveCentroidY)); }
        private void OnMultiTraceGestureUp()   { AppendEvent(MultiTraceUp,      new MultiTraceEventArgs("MultiTraceUp",      Group.Id, m_nOfFingers, Group.ActiveCentroidX, Group.ActiveCentroidY)); }
        private void OnMultiTraceGestureEnter(){ AppendEvent(MultiTraceEnter,   new MultiTraceEventArgs("MultiTraceEnter",   Group.Id, m_nOfFingers, Group.ActiveCentroidX, Group.ActiveCentroidY)); }
        private void OnMultiTraceGestureLeave(){ AppendEvent(MultiTraceLeave,   new MultiTraceEventArgs("MultiTraceLeave",   Group.Id, m_nOfFingers, Group.ActiveCentroidX, Group.ActiveCentroidY)); }
        private void OnMultiTraceFromTo()      { AppendEvent(MultiTraceFromTo,  new MultiTraceFromToEventArgs(
            "MultiTraceFromTo",  Group.Id, m_nOfFingers, Group.ClosestInitialTarget, Group.ClosestFinalTarget,
            m_initialCentroidX, m_initialCentroidY, Group.ActiveCentroidX, Group.ActiveCentroidY)); }

        public override void Process(List<Trace> traces)
        {
            if (m_startingTime == -1)
                m_startingTime = traces[0].Last.TimeStamp;

            if (!m_conf.RECOGNIZE_WHEN_ENDED)
                GestureHasBeenRecognized();

            m_nOfFingers = Group.NumberOfPresentTraces;

            OnMultiTraceGestureLeave();
            OnMultiTraceGestureEnter();

            foreach (Trace trace in traces)
            {
                if (trace.State == Trace.States.UPDATED)
                {
                    OnMultiTraceGestureMove();
                }
                else if (trace.State == Trace.States.ADDED)
                {
                    if (trace.Last.TimeStamp - m_startingTime <= Settings.GroupingSynchTime)
                        m_startingTraces.Add(trace);
                    
                    OnMultiTraceGestureDown();
                }
                else
                {
                    OnMultiTraceGestureUp();
                }
            }

            if (!Group.IsPresent)
            {
                m_nOfFingers = 0;
                int endTime = Group.CurrentTimeStamp;
                foreach (Trace startingTrace in m_startingTraces)
                {
                    if (endTime - startingTrace.Last.TimeStamp <= Settings.GroupingSynchTime)
                        m_nOfFingers++;
                }
                OnMultiTraceStart();
                OnMultiTraceEnd();

                // If both initial and final target exist and their distance to the initial and final group's centroids are less or equal to the threshold,
                if (Group.ClosestInitialTarget != null && Group.ClosestFinalTarget != null &&
                    (m_conf.ALLOW_AUTOLINK || Group.ClosestInitialTarget != Group.ClosestFinalTarget) &&
                    (!(Group.ClosestInitialTarget is ITangibleGestureListener) || ((ITangibleGestureListener)(Group.ClosestInitialTarget)).GetSquareDistance(m_initialCentroidX, m_initialCentroidY) <= m_conf.MAX_SQUARE_DISTANCE) &&
                    (!(Group.ClosestFinalTarget is ITangibleGestureListener) || ((ITangibleGestureListener)(Group.ClosestFinalTarget)).GetSquareDistance(Group.ActiveCentroidX, Group.ActiveCentroidY) <= m_conf.MAX_SQUARE_DISTANCE))
                {
                    OnMultiTraceFromTo();
                    Terminate(true);
                }
                else
                    Terminate(false);
            }
        }

        protected override void UpdateEventHandlers(bool initial, bool final, bool entering, bool current, bool leaving, 
            bool intersect, bool union, bool newClosestEnt, bool newClosestCur, bool newClosestLvn, 
            bool newClosestIni, bool newClosestFin)
        {
            base.UpdateEventHandlers(initial, final, entering, current, leaving,
                intersect, union, newClosestEnt, newClosestCur, newClosestLvn, newClosestIni, newClosestFin);

            if (newClosestIni)
            {
                if (Group.ClosestInitialTarget != null)
                {
                    m_initialCentroidX = Group.ActiveCentroidX;
                    m_initialCentroidY = Group.ActiveCentroidY;
                }
            }
        }
    }

}