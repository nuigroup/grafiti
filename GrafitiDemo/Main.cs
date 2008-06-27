/*
	Grafiti Demo Application

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
using Grafiti;
using ClientNamespace;

namespace ClientNamespace
{
	public class ClientListener : TuioListener
	{
        private Dictionary<TuioObject, ClientGestureListener> m_gListenersTable;
		
		public ClientListener()
		{
            m_gListenersTable = new Dictionary<TuioObject, ClientGestureListener>();
		}

        #region TuioListener, Members of
        void TuioListener.addTuioObject(TuioObject tuioObject)
        {
            ClientGestureListener listener = new ClientGestureListener(tuioObject);
            m_gListenersTable[tuioObject] = listener;
            Surface.Instance.AddListener(listener);
        }
        void TuioListener.updateTuioObject(TuioObject tuioObject) { }
        void TuioListener.removeTuioObject(TuioObject tuioObject)
        {
            ClientGestureListener listener = (ClientGestureListener)m_gListenersTable[tuioObject];
            Surface.Instance.RemoveListener(listener);
        }
        void TuioListener.addTuioCursor(TuioCursor tuioCursor) { }
        void TuioListener.updateTuioCursor(TuioCursor tuioCursor) { }
        void TuioListener.removeTuioCursor(TuioCursor tuioCursor) { }
        void TuioListener.refresh(long timestamp) { }
        #endregion


        public static void Main(String[] argv)
        {
            ClientListener clientListener = new ClientListener();

            TuioClient tuioClient = null;

            switch (argv.Length)
            {
                case 1:
                    int port = 0;
                    try
                    {
                        port = int.Parse(argv[0], null);
                        if (port > 0)
                            tuioClient = new TuioClient(port);
                    }
                    catch (Exception e) { }
                    break;
                case 0:
                    tuioClient = new TuioClient();
                    break;
            }


            if (tuioClient != null)
            {
                tuioClient.addTuioListener(clientListener);
                tuioClient.addTuioListener(Surface.Instance);
                tuioClient.connect();
                Console.WriteLine("Listening to TUIO messages at port " + tuioClient.getPort());
            }
            else
                Console.WriteLine("Usage: [mono] GrafitiDemo [port]");
        }
    }
}
