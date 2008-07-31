/*
	GenericDemo, Grafiti demo application

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
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections;
using TUIO;
using Grafiti;

namespace GenericDemo
{
    public class DemoObjectManager : TuioListener
    {
        private Form m_parentForm;
        private List<TuioObject> m_tuioObjectAddedList, m_tuioObjectUpdatedList, m_tuioObjectRemovedList;
        private Dictionary<long, DemoObject> m_idDemoObjectTable;

        public DemoObjectManager(Form parentForm)
        {
            m_parentForm = parentForm;
            m_tuioObjectAddedList = new List<TuioObject>();
            m_tuioObjectUpdatedList = new List<TuioObject>();
            m_tuioObjectRemovedList = new List<TuioObject>();
            m_idDemoObjectTable = new Dictionary<long,DemoObject>();
        }

        #region TuioListener interface

        public void addTuioObject(TuioObject o)
        {
            m_tuioObjectAddedList.Add(o);
        }

        public void updateTuioObject(TuioObject o)
        {
            m_tuioObjectUpdatedList.Add(o);
        }

        public void removeTuioObject(TuioObject o)
        {
            m_tuioObjectRemovedList.Add(o);
        }

        public void addTuioCursor(TuioCursor c) { }

        public void updateTuioCursor(TuioCursor c) { }

        public void removeTuioCursor(TuioCursor c) { }

        public void refresh(long timestamp)
        {
            lock (this)
            {
                foreach (TuioObject o in m_tuioObjectAddedList)
                {
                    DemoObject demoObject = new DemoObject(m_parentForm, (int)o.getSessionID(), o.getX() * Surface.SCREEN_RATIO, o.getY(), o.getAngle());
                    m_idDemoObjectTable[o.getSessionID()] = demoObject;
                    Surface.Instance.AddListener(demoObject);
                }
                foreach (TuioObject o in m_tuioObjectUpdatedList)
                {
                    m_idDemoObjectTable[o.getSessionID()].Update(o.getX() * Surface.SCREEN_RATIO, o.getY(), o.getAngle());
                }
                foreach (TuioObject o in m_tuioObjectRemovedList)
                {
                    Surface.Instance.RemoveListener(m_idDemoObjectTable[o.getSessionID()]);
                    m_idDemoObjectTable.Remove(o.getSessionID());
                }
            }
            m_tuioObjectAddedList.Clear();
            m_tuioObjectUpdatedList.Clear();
            m_tuioObjectRemovedList.Clear();
        }

        #endregion



        public void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            lock (this)
            {
                foreach (DemoObject demoObject in m_idDemoObjectTable.Values)
                {
                    demoObject.OnPaint(e);
                }
            }
        }
    }
}
