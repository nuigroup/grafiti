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

namespace Grafiti
{
    public class MultiTraceEventArgs : GestureEventArgs
    {
        private int m_nOfFingers;

        public int NOfFingers { get { return m_nOfFingers; } }

        public MultiTraceEventArgs(Enum id, int nFingers) : base(id) 
        {
            m_nOfFingers = nFingers;
        }
    }

    public class MultiTraceGR : GlobalGestureRecognizer
    {
        public enum Events
        {
            MultiTraceStarted,
            MultiTraceEnter,
            MultiTraceLeave,
            MultiTraceDown,
            MultiTraceMove,
            MultiTraceUp,
            MultiTraceEnd
        }

        private int m_nOfFingers;

        // These are public only to make reflection to work.
        // They're not intended to be accessed directly from clients.
        public event GestureEventHandler MultiTraceStarted;
        public event GestureEventHandler MultiTraceDown;
        public event GestureEventHandler MultiTraceMove;
        public event GestureEventHandler MultiTraceUp;
        public event GestureEventHandler MultiTraceEnter;
        public event GestureEventHandler MultiTraceLeave;
        public event GestureEventHandler MultiTraceEnd;

        public MultiTraceGR(GRConfiguration configuration) : base(configuration)
        {
            NewInitialEvents = new Enum[] { Events.MultiTraceStarted };
            EnteringEvents = new Enum[] { Events.MultiTraceEnter };
            LeavingEvents = new Enum[] { Events.MultiTraceLeave };
            CurrentEvents = new Enum[] { Events.MultiTraceDown, Events.MultiTraceMove, Events.MultiTraceUp };
            FinalEvents = new Enum[] { Events.MultiTraceEnd };

            m_nOfFingers = 0;
        }

        private void OnMultiTraceStart()       { AppendEvent(MultiTraceStarted, new MultiTraceEventArgs(Events.MultiTraceStarted, m_nOfFingers)); }
        private void OnMultiTraceEnd()         { AppendEvent(MultiTraceEnd,     new MultiTraceEventArgs(Events.MultiTraceEnd,     m_nOfFingers)); }
        private void OnMultiTraceGestureDown() { AppendEvent(MultiTraceDown,    new MultiTraceEventArgs(Events.MultiTraceDown,    m_nOfFingers)); }
        private void OnMultiTraceGestureMove() { AppendEvent(MultiTraceMove,    new MultiTraceEventArgs(Events.MultiTraceMove,    m_nOfFingers)); }
        private void OnMultiTraceGestureUp()   { AppendEvent(MultiTraceUp,      new MultiTraceEventArgs(Events.MultiTraceUp,      m_nOfFingers)); }
        private void OnMultiTraceGestureEnter(){ AppendEvent(MultiTraceEnter,   new MultiTraceEventArgs(Events.MultiTraceEnter,   m_nOfFingers)); }
        private void OnMultiTraceGestureLeave(){ AppendEvent(MultiTraceLeave,   new MultiTraceEventArgs(Events.MultiTraceLeave,   m_nOfFingers)); }

        public override GestureRecognitionResult Process(List<Trace> traces)
        {
            System.Diagnostics.Debug.Assert(traces.Count > 0);

            GestureRecognitionResult result = null;
            m_nOfFingers = Group.NOfAliveTraces;

            OnMultiTraceGestureEnter();
            OnMultiTraceStart();

            foreach (Trace trace in traces)
            {
                if (trace.Last.State == TUIO.TuioCursor.UPDATED)
                {
                    OnMultiTraceGestureMove();
                    result = new GestureRecognitionResult(false, true, true);
                }
                else if (trace.Last.State == TUIO.TuioCursor.ADDED)
                {
                    OnMultiTraceGestureDown();
                    result = new GestureRecognitionResult(false, true, true);
                }
                else
                {
                    OnMultiTraceGestureUp();
                    if (!Group.Alive)
                    {
                        OnMultiTraceEnd();
                        result = new GestureRecognitionResult(false, true, false);
                    }
                    else
                        result = new GestureRecognitionResult(false, true, true);
                }
            }

            OnMultiTraceGestureLeave();

            return result;
        }
    }

}