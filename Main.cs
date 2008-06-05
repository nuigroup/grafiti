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
using TUIO;
using grafiti;

using Client;

namespace grafiti
{
	public class grafitiListener : ITuioListener
	{
		private Surface m_surface;

        private List<IGestureListener> m_gListeners;

        private DoubleDictionary<int, int, TuioObjectGestureListener> m_gListenersTable;
		
		public grafitiListener()
		{

            m_gListeners = new List<IGestureListener>();

            m_gListenersTable = new DoubleDictionary<int, int, TuioObjectGestureListener>();

            m_surface = new Surface(m_gListeners);
		}

        public void AddTuioObj(long s_id, int f_id, float x, float y, float a)

        {

            Console.WriteLine("add obj " + f_id + " " + s_id + " " + x + " " + y + " " + a);

            TuioObjectGestureListener listener = new TuioObjectGestureListener(m_surface, (int) s_id, f_id, x, y);

            m_gListeners.Add(listener);

            m_gListenersTable[(int) s_id, f_id] = listener;

        }

        public void UpdateTuioObj(long s_id, int f_id, float x, float y, float a, float X, float Y, float A, float m, float r)

        {

            //Console.WriteLine("set obj " + f_id + " " + s_id + " " + x + " " + y + " " + a + " " + (float)Math.Sqrt(X * X + Y * Y) + " " + A + " " + m + " " + r);

            TuioObjectGestureListener listener = (TuioObjectGestureListener)m_gListenersTable[(int) s_id, f_id];

            listener.UpdatePosition(x, y);

        }

        public void RemoveTuioObj(long s_id, int f_id)

        {

            Console.WriteLine("del obj " + f_id + " " + s_id);

            TuioObjectGestureListener listener = (TuioObjectGestureListener)m_gListenersTable[(int) s_id, f_id];

            m_gListeners.Remove(listener);

            m_gListenersTable.Remove((int) s_id, f_id);

            m_surface.UnregisterAllHandlers(listener);

        }



        public void AddTuioCur(TuioCursor cursor)

        {

            //Console.WriteLine("add \\  cur " + s_id + " " + x + " " + y);

            m_surface.AddCursor(cursor);

        }

        public void UpdateTuioCur(TuioCursor cursor)

        {

            //Console.WriteLine("set  | cur " + s_id + " " + x + " " + y + " " + (float)Math.Sqrt(X * X + Y * Y) + " " + m);

            m_surface.UpdateCursor(cursor);

        }

        public void RemoveTuioCur(TuioCursor cursor)

        {

            //Console.WriteLine("del /  cur " + s_id);

            m_surface.RemoveCursor(cursor);

        }

        public void Refresh()

        {

            //Console.WriteLine("refresh");	

        }









        public static void Main(String[] argv)

        {

            grafitiListener listener = new grafitiListener();



            TuioClient client = null;



            switch (argv.Length)

            {

                case 1:

                    int port = 0;

                    port = int.Parse(argv[0], null);

                    if (port > 0) client = new TuioClient(port);

                    break;

                case 0:

                    client = new TuioClient();

                    break;

            }





            if (client != null)

            {

                client.addTuioListener(listener);

                client.connect();

                Console.WriteLine("listening to TUIO messages at port " + client.getPort());



            }

            else

                Console.WriteLine("usage: java TuioDump [port]");

        }
	}
}