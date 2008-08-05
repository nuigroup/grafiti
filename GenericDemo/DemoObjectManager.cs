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
    public class DemoObjectLink
    {
        private DemoObject m_fromObject, m_toObject;
        private float m_xm, m_ym; // mean point
        private int m_nFingers;
        private Pen m_pen;

        public float Xm { get { return m_xm; } }
        public float Ym { get { return m_ym; } }

        public DemoObjectLink(DemoObject from, DemoObject to, int nFingers)
        {
            m_fromObject = from;
            m_toObject = to;
            m_nFingers = nFingers;
            Update();
            m_pen = new Pen(Color.Black);
            m_pen.Width = nFingers;
            from.m_links.Add(this);
            to.m_links.Add(this);
        }

        public void Update()
        {
            m_xm = (m_fromObject.X + m_toObject.X) / 2;
            m_ym = (m_fromObject.Y + m_toObject.Y) / 2;
        }

        public void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            float screen = MainForm.height;
            g.DrawLine(m_pen, new PointF(m_fromObject.X * screen, m_fromObject.Y * screen),
                new PointF(m_toObject.X * screen, m_toObject.Y * screen));
            g.FillEllipse(Brushes.Black, m_xm * screen - 2, m_ym * screen - 2, 4, 4);
        }
    }
    public class DemoObjectManager : TuioListener
    {
        private const float MAX_DISTANCE_FOR_LINKING = 0.3f;
        private const float SQUARE_MAX_DISTANCE = MAX_DISTANCE_FOR_LINKING * MAX_DISTANCE_FOR_LINKING;
        private Form m_parentForm;  
        private List<TuioObject> m_tuioObjectAddedList, m_tuioObjectUpdatedList, m_tuioObjectRemovedList;
        private Dictionary<long, DemoObject> m_idDemoObjectTable;
        private List<DemoObjectLink> m_links;
        private PinchingGRConfigurator m_pinchingConfigurator = new PinchingGRConfigurator(true, false);

        public List<DemoObjectLink> Links { get { return m_links; } }
        internal PinchingGRConfigurator PinchingConfigurator { get { return m_pinchingConfigurator; } }

        public DemoObjectManager(Form parentForm)
        {
            m_parentForm = parentForm;
            m_tuioObjectAddedList = new List<TuioObject>();
            m_tuioObjectUpdatedList = new List<TuioObject>();
            m_tuioObjectRemovedList = new List<TuioObject>();
            m_idDemoObjectTable = new Dictionary<long,DemoObject>();
            m_links = new List<DemoObjectLink>();

            GestureEventManager.Instance.SetPriorityNumber(-2);
            GestureEventManager.Instance.RegisterHandler(typeof(RemovingLinkGR), new RemovingLinkGRConfigurator(this), "RemoveLinks", OnRemoveLinks);

            GestureEventManager.Instance.SetPriorityNumber(-1);
            GestureEventManager.Instance.RegisterHandler(typeof(MultiTraceGR), "MultiTraceFromTo", OnMultiTraceFromTo);
            
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
            foreach (TuioObject o in m_tuioObjectAddedList)
            {
                DemoObject demoObject = new DemoObject(this, m_parentForm, (int)o.getSessionID(), o.getX() * Surface.SCREEN_RATIO, o.getY(), o.getAngle());
                m_idDemoObjectTable[o.getSessionID()] = demoObject;
                Surface.Instance.AddListener(demoObject);
            }
            foreach (TuioObject o in m_tuioObjectUpdatedList)
            {
                m_idDemoObjectTable[o.getSessionID()].Update(o.getX() * Surface.SCREEN_RATIO, o.getY(), o.getAngle());
            }
            foreach (TuioObject o in m_tuioObjectRemovedList)
            {
                DemoObject demoObject = m_idDemoObjectTable[o.getSessionID()];
                m_idDemoObjectTable.Remove(o.getSessionID());
                Surface.Instance.RemoveListener(demoObject);
                foreach (DemoObjectLink link in demoObject.m_links)
                    m_links.Remove(link);
            }

            foreach (DemoObjectLink link in m_links)
                link.Update();

            m_tuioObjectAddedList.Clear();
            m_tuioObjectUpdatedList.Clear();
            m_tuioObjectRemovedList.Clear();
        }

        #endregion

        public void OnRemoveLinks(object obj, GestureEventArgs args)
        {
            RemovingLinkGREventArgs cArgs = (RemovingLinkGREventArgs)args;
            foreach(DemoObjectLink link in cArgs.Links)
                m_links.Remove(link);
        }
        public void OnMultiTraceFromTo(object obj, GestureEventArgs args)
        {
            MultiTraceFromToEventArgs cArgs = (MultiTraceFromToEventArgs)args;
            DemoObject fromObj = (DemoObject)cArgs.FromTarget;
            DemoObject toObj = (DemoObject)cArgs.ToTarget;
            if (fromObj != toObj &&
                fromObj.GetSquareDistance(cArgs.InitialCentroidX, cArgs.InitialCentroidY) <= SQUARE_MAX_DISTANCE &&
                toObj.GetSquareDistance(cArgs.FinalCentroidX, cArgs.FinalCentroidY) <= SQUARE_MAX_DISTANCE)
                m_links.Add(new DemoObjectLink(fromObj, toObj, cArgs.NOfFingers));
        }

        public void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            foreach (DemoObjectLink link in m_links)
                link.OnPaint(e);

            foreach (DemoObject demoObject in m_idDemoObjectTable.Values)
                demoObject.OnPaint(e);
        }
    }
}
