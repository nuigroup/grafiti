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

using TUIO;



namespace SimpleGRNS

{

    //public class SimpleGREventArgs : GestureEventArgs

    //{

    //    public float x, y;



    //    public SimpleGREventArgs(float x, float y)

    //    {

    //        this.x = x;

    //        this.y = y;

    //    }

    //}





    public class SimpleGR : GestureRecognizer

    {

        public SimpleGR() : base() { }



        public delegate void SimpleGestureHandler(object simpleGR, GestureEventArgs args);



        public event SimpleGestureHandler SimpleGesture;



        protected virtual void OnSimpleGesture(GestureEventArgs args)

        {

            if (SimpleGesture != null)

            {

                SimpleGesture(this, args);

            }

        }





        public override GestureRecognitionResult Process(Trace trace)

        {

            GestureEventArgs data;



            // if the gesture has been alive for 2 seconds

            if (trace.Last.TimeStamp - trace.First.TimeStamp >= 2000)

            {

                data = new GestureEventArgs();

                OnSimpleGesture(data);

                return new GestureRecognitionResult(false, false, data, 1);

            } else {

                data = (GestureEventArgs)null;

                return new GestureRecognitionResult(true, false, data, 1);

            }  

        }

    }

}