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

using grafiti;



namespace grafiti

{



    public class MultiTraceEventArgs : GestureEventArgs

    {

        private string m_message;

        public string Message { get { return m_message; } }



        public MultiTraceEventArgs(string message)

        {

            m_message = message;

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



        // These are public only to make reflection work.

        // They're not intended to be accessed directly from clients.

        public event GestureEventHandler MultiTraceStarted;

        public event GestureEventHandler MultiTraceDown;

        public event GestureEventHandler MultiTraceMove;

        public event GestureEventHandler MultiTraceUp;

        public event GestureEventHandler MultiTraceEnter;

        public event GestureEventHandler MultiTraceLeave;

        public event GestureEventHandler MultiTraceEnd;



        public MultiTraceGR() : base()

        {

            NewInitialEvents = new Enum[] { Events.MultiTraceStarted };

            EnteringEvents   = new Enum[] { Events.MultiTraceEnter };

            LeavingEvents    = new Enum[] { Events.MultiTraceLeave };

            CurrentEvents    = new Enum[] { Events.MultiTraceDown, Events.MultiTraceMove, Events.MultiTraceUp };

            FinalEvents      = new Enum[] { Events.MultiTraceEnd };

        }





        private void OnMultiTraceStart()

        {

            if (MultiTraceStarted != null)

            {

                MultiTraceStarted(this, new MultiTraceEventArgs("START EVENT"));

            }

        }

        private void OnMultiTraceEnd()

        {

            if (MultiTraceEnd != null)

            {

                MultiTraceEnd(this, new MultiTraceEventArgs("END EVENT"));

            }

        }

        private void OnMultiTraceGestureDown()

        {

            if (MultiTraceDown != null)

            {

                MultiTraceDown(this, new MultiTraceEventArgs("DOWN EVENT"));

            }

        }

        private void OnMultiTraceGestureMove()

        {

            if (MultiTraceMove != null)

            {

                MultiTraceMove(this, new MultiTraceEventArgs("MOVE EVENT"));

            }

        }

        private void OnMultiTraceGestureUp()

        {

            if (MultiTraceUp != null)

            {

                MultiTraceUp(this, new MultiTraceEventArgs("UP EVENT"));

            }

        }

        private void OnMultiTraceGestureEnter()

        {

            if (MultiTraceEnter != null)

            {

                MultiTraceEnter(this, new MultiTraceEventArgs("ENTER EVENT"));

            }

        }

        private void OnMultiTraceGestureLeave()

        {

            if (MultiTraceLeave != null)

            {

                MultiTraceLeave(this, new MultiTraceEventArgs("LEAVE EVENT"));

            }

        }



        public override GestureRecognitionResult Process(Trace trace)

        {

            MultiTraceEventArgs data = (MultiTraceEventArgs)null;



            OnMultiTraceGestureEnter();

            OnMultiTraceStart();

            OnMultiTraceEnd();

            OnMultiTraceGestureLeave();



            if (trace.Last.State == (int)TUIO.TuioCursor.States.set)

            {

                OnMultiTraceGestureMove();

                return new GestureRecognitionResult(false, true, data, 1);

            }

            else if (trace.Last.State == (int)TUIO.TuioCursor.States.add)

            {

                OnMultiTraceGestureDown();

                return new GestureRecognitionResult(false, true, data, 1);

            }

            else

            {

                OnMultiTraceGestureUp();

                //if (!Group.Alive)

                  //  OnMultiTraceEnd();

                return new GestureRecognitionResult(false, false, data, 1);

            }

        }

    }



}