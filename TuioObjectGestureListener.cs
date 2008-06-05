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

using System.Text;

using System.Collections.Generic;



using grafiti;

using SimpleGRNS;





namespace Client

{

    public class TuioObjectGestureListener : IGestureListener

    {

        private const float INTERACTION_RANGE = 0.2f;

        private Surface m_surface;

        private readonly int m_sessionId, m_fId;

        private float m_x, m_y;

        private SimpleGR m_simpleGR;

        private List<GestureRecognizer> m_grList;

        

        public TuioObjectGestureListener(Surface surface, int sessionId, int fId, float x, float y)

        {

            m_surface = surface;

            m_sessionId = sessionId;

            m_fId = fId;

            m_x = x;

            m_y = y;



            // WORK IN PROGRESS (registration should be done through surface)

            m_simpleGR = new SimpleGR();

            m_simpleGR.SimpleGesture += new SimpleGR.SimpleGestureHandler(SimpleGestureEventHandler);



            m_grList = new List<GestureRecognizer>();

            m_grList.Add(m_simpleGR);



            // registering multi-target GRs



            new MultiTraceGR(); // this will make the class to compile!



            m_surface.RegisterHandler(

                typeof(MultiTraceGR),

                MultiTraceGR.Events.MultiTraceEnter,

                this,

                new GestureEventHandler(OnMultiTraceEvent)

                );

            m_surface.RegisterHandler(

                typeof(MultiTraceGR),

                MultiTraceGR.Events.MultiTraceLeave,

                this,

                new GestureEventHandler(OnMultiTraceEvent)

                );

            //m_surface.RegisterGR(

            //    typeof(MultiTraceGR),

            //    MultiTraceGR.Events.MultiTraceMove,

            //    this,

            //    new GestureEventHandler(OnMultiTraceEvent)

            //    );

            m_surface.RegisterHandler(

                typeof(MultiTraceGR),

                MultiTraceGR.Events.MultiTraceDown,

                this,

                new GestureEventHandler(OnMultiTraceEvent)

                );

            m_surface.RegisterHandler(

                typeof(MultiTraceGR),

                MultiTraceGR.Events.MultiTraceUp,

                this,

                new GestureEventHandler(OnMultiTraceEvent)

                );

            m_surface.RegisterHandler(

                typeof(MultiTraceGR),

                MultiTraceGR.Events.MultiTraceStarted,

                this,

                new GestureEventHandler(OnMultiTraceEvent)

                );

            m_surface.RegisterHandler(

                typeof(MultiTraceGR),

                MultiTraceGR.Events.MultiTraceEnd,

                this,

                new GestureEventHandler(OnMultiTraceEvent)

                );

        }

        public List<GestureRecognizer> GetLocalGRs()

        {

            return m_grList;

        }

        public void OnMultiTraceEvent(object MultiTraceGR, GestureEventArgs args)

        {

            Console.WriteLine("{0} received the MultiTraceEvent {1}", ToString(), ((MultiTraceEventArgs)args).Message);

        }

        public void SimpleGestureEventHandler(object source, GestureEventArgs args)

        {

            Console.WriteLine("evento ricevuto!");

        }



        public bool Contains(float x, float y)

        {

            float dx = Math.Abs(x - m_x);

            float dy = Math.Abs(y - m_y);

            return (float)Math.Sqrt(dx * dx + dy * dy) <= INTERACTION_RANGE;

        }



        public void GetPosition(out float x, out float y)

        {

            x = m_x;

            y = m_y;

        }



        public void UpdatePosition(float x, float y)

        {

            m_x = x;

            m_y = y;

        }



        public override string ToString()

        {

            StringBuilder sb = new StringBuilder();

            sb.Append("TuioGL# ");

            sb.Append(m_fId);

            sb.Append("(");

            sb.Append(m_sessionId);

            sb.Append(")");

            return sb.ToString();

        }





        

    } 



    //public class RectGestureListener : IGestureListener

    //{

    //    private static int counter = 0;

    //    private readonly int id;

    //    private readonly float x, y, w, h;



    //    public RectGestureListener(float x, float y, float w, float h)

    //    {

    //        this.x = x;

    //        this.y = y;

    //        this.w = w;

    //        this.h = h;

    //        this.id = counter++;

    //    }



    //    public bool Contains(float x, float y)

    //    {

    //        return x >= this.x && x < this.x + this.w && y >= this.y && y < this.y + this.h;

    //    }



    //    public override string ToString()

    //    {

    //        return "RGL n." + id;

    //    }

    //} 

}