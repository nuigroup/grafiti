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
        public MultiTraceEventArgs(Enum id) : base(id)
        {

        }
    }


    public class MultiTraceGR : GlobalGestureRecognizer
    {
        private object m_ctorParam;

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

        // These are public only to make reflection work.
        // They're not intended to be accessed directly from clients.
        public event GestureEventHandler MultiTraceStarted;
        public event GestureEventHandler MultiTraceDown;
        public event GestureEventHandler MultiTraceMove;
        public event GestureEventHandler MultiTraceUp;
        public event GestureEventHandler MultiTraceEnter;
        public event GestureEventHandler MultiTraceLeave;
        public event GestureEventHandler MultiTraceEnd;

        public MultiTraceGR(object ctorParam) : base(ctorParam)
        {
            m_ctorParam = ctorParam;
            NewInitialEvents = new Enum[] { Events.MultiTraceStarted };
            EnteringEvents = new Enum[] { Events.MultiTraceEnter };
            LeavingEvents = new Enum[] { Events.MultiTraceLeave };
            CurrentEvents = new Enum[] { Events.MultiTraceDown, Events.MultiTraceMove, Events.MultiTraceUp };
            FinalEvents = new Enum[] { Events.MultiTraceEnd };

            Exclusive = false;
        }

        private void OnMultiTraceStart()
        {
            AppendEvent(MultiTraceStarted, new MultiTraceEventArgs(Events.MultiTraceStarted));
        }
        private void OnMultiTraceEnd()
        {
            AppendEvent(MultiTraceEnd, new MultiTraceEventArgs(Events.MultiTraceEnd));
        }
        private void OnMultiTraceGestureDown()
        {
            AppendEvent(MultiTraceDown, new MultiTraceEventArgs(Events.MultiTraceDown));
        }
        private void OnMultiTraceGestureMove()
        {
            AppendEvent(MultiTraceMove, new MultiTraceEventArgs(Events.MultiTraceMove));
        }
        private void OnMultiTraceGestureUp()
        {
            AppendEvent(MultiTraceUp, new MultiTraceEventArgs(Events.MultiTraceUp));
        }
    
        private void OnMultiTraceGestureEnter()
        {
            AppendEvent(MultiTraceEnter, new MultiTraceEventArgs(Events.MultiTraceEnter));
        }
        private void OnMultiTraceGestureLeave()
        {
            AppendEvent(MultiTraceLeave, new MultiTraceEventArgs(Events.MultiTraceLeave));
        }

        public override GestureRecognitionResult Process(Trace trace)
        {
            GestureRecognitionResult result;

            OnMultiTraceGestureEnter();
            OnMultiTraceStart();

            if (trace.Last.State == (int)TUIO.TuioCursor.States.set)
            {
                OnMultiTraceGestureMove();
                result = new GestureRecognitionResult(false, true, true);
            }
            else if (trace.Last.State == (int)TUIO.TuioCursor.States.add)
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

            OnMultiTraceGestureLeave();

            return result;
        }

        public override GestureRecognizer Copy()
        {
            return new MultiTraceGR(m_ctorParam);
        }
    }

}