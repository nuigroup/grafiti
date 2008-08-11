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
        private Form m_form;
        private DemoObject m_fromObject, m_toObject;
        private float m_xm, m_ym; // mean point
        private int m_nFingers;
        private Pen m_pen;

        public float Xm { get { return m_xm; } }
        public float Ym { get { return m_ym; } }

        public DemoObjectLink(Form form, DemoObject from, DemoObject to, int nFingers)
        {
            m_form = form;
            m_fromObject = from;
            m_toObject = to;
            m_nFingers = nFingers;
            Update();
            m_pen = new Pen(Color.Black);
            m_pen.Width = nFingers;
            from.Links.Add(this);
            to.Links.Add(this);
        }

        public void Update()
        {
            m_xm = (m_fromObject.X + m_toObject.X) / 2;
            m_ym = (m_fromObject.Y + m_toObject.Y) / 2;
        }

        public void Draw(Graphics g, float screen)
        {
            g.DrawLine(m_pen, new PointF(m_fromObject.X * screen, m_fromObject.Y * screen),
                new PointF(m_toObject.X * screen, m_toObject.Y * screen));
            g.FillEllipse(Brushes.Black, m_xm * screen - 2, m_ym * screen - 2, 4, 4);
        }
    }
 
    public class DemoObjectManager : TuioListener, IGestureListener
    {
        #region Variables
        private const float MAX_DISTANCE_FOR_LINKING = 0.3f;
        private const float SQUARE_MAX_DISTANCE = MAX_DISTANCE_FOR_LINKING * MAX_DISTANCE_FOR_LINKING;
        private Form m_form;
        private List<TuioObject> m_tuioObjectAddedList, m_tuioObjectUpdatedList, m_tuioObjectRemovedList;
        private List<DemoObject> m_currentTuioObjects = new List<DemoObject>();
        private Dictionary<long, DemoObject> m_idDemoObjectTable;
        private List<DemoObjectLink> m_links, m_pendingLinks;
        private PinchingGRConfigurator m_pinchingConf = new PinchingGRConfigurator(true, false);
        private BasicMultiFingerGRConfigurator m_basicMultiFingerGRConf = new BasicMultiFingerGRConfigurator();
        private Dictionary<int, List<DemoObject>> m_linkRequests = new Dictionary<int, List<DemoObject>>();
        private static object m_lock = new object();

        internal List<DemoObjectLink> Links { get { return m_links; } }
        internal PinchingGRConfigurator PinchingConf { get { return m_pinchingConf; } }
        internal BasicMultiFingerGRConfigurator BasicMultiFingerGRConf { get { return m_basicMultiFingerGRConf; } }
        
        #endregion

        public DemoObjectManager(Form parentForm)
        {
            m_form = parentForm;
            m_tuioObjectAddedList = new List<TuioObject>();
            m_tuioObjectUpdatedList = new List<TuioObject>();
            m_tuioObjectRemovedList = new List<TuioObject>();
            m_idDemoObjectTable = new Dictionary<long,DemoObject>();
            m_links = new List<DemoObjectLink>();
            m_pendingLinks = new List<DemoObjectLink>();

            RemovingLinkGRConfigurator removingLinkGRConf = new RemovingLinkGRConfigurator(this);
            GestureEventManager.Instance.SetPriorityNumber(typeof(RemovingLinkGR), removingLinkGRConf, -2);
            GestureEventManager.Instance.RegisterHandler(typeof(RemovingLinkGR), removingLinkGRConf, "RemoveLinks", OnRemoveLinks);

            GestureEventManager.Instance.SetPriorityNumber(typeof(MultiTraceGR), -1);
            GestureEventManager.Instance.RegisterHandler(typeof(MultiTraceGR), "MultiTraceFromTo", OnMultiTraceFromTo);

            LazoGRConfigurator lazoGRConf = new LazoGRConfigurator(m_currentTuioObjects);
            GestureEventManager.Instance.SetPriorityNumber(typeof(LazoGR), lazoGRConf, 5);
            GestureEventManager.Instance.RegisterHandler(typeof(LazoGR), lazoGRConf, "Lazo", OnLazo);
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
                DemoObject demoObject = new DemoObject(this, m_form, (int)o.getSessionID(), o.getX() * Surface.SCREEN_RATIO, o.getY(), o.getAngle());
                m_idDemoObjectTable[o.getSessionID()] = demoObject;
                m_currentTuioObjects.Add(demoObject);
            }
            foreach (TuioObject o in m_tuioObjectUpdatedList)
            {
                m_idDemoObjectTable[o.getSessionID()].Update(o.getX() * Surface.SCREEN_RATIO, o.getY(), o.getAngle());
            }
            foreach (TuioObject o in m_tuioObjectRemovedList)
            {
                DemoObject demoObject = m_idDemoObjectTable[o.getSessionID()];
                demoObject.RemoveFromSurface();
                m_idDemoObjectTable.Remove(o.getSessionID());
                m_currentTuioObjects.Remove(demoObject);
                foreach (DemoObjectLink link in demoObject.Links)
                    m_links.Remove(link);
            }

            // Add links that have been added asynchronously (by hovering)
            foreach (DemoObjectLink link in m_pendingLinks)
                m_links.Add(link);
            m_pendingLinks.Clear();

            foreach (DemoObjectLink link in m_links)
                link.Update();

            m_tuioObjectAddedList.Clear();
            m_tuioObjectUpdatedList.Clear();
            m_tuioObjectRemovedList.Clear();
        }
        #endregion

        public void OnLazo(object obj, GestureEventArgs args)
        {
            LazoGREventArgs cArgs = (LazoGREventArgs)args;
            foreach (DemoObject demoObject in cArgs.Selected)
                demoObject.Selected = true;
            m_form.Invalidate();
        }

        public void OnRemoveLinks(object obj, GestureEventArgs args)
        {
            RemovingLinkGREventArgs cArgs = (RemovingLinkGREventArgs)args;
            foreach (DemoObjectLink link in cArgs.Links)
            {
                m_links.Remove(link);
                //Console.WriteLine("Link removed");
            } 
        }
        public void OnMultiTraceFromTo(object obj, GestureEventArgs args)
        {
            MultiTraceFromToEventArgs cArgs = (MultiTraceFromToEventArgs)args;
            if (cArgs.FromTarget is DemoObject && cArgs.ToTarget is DemoObject)
            {
                DemoObject fromObj = (DemoObject)cArgs.FromTarget;
                DemoObject toObj = (DemoObject)cArgs.ToTarget;
                if (fromObj != toObj &&
                    fromObj.GetSquareDistance(cArgs.InitialCentroidX, cArgs.InitialCentroidY) <= SQUARE_MAX_DISTANCE &&
                    toObj.GetSquareDistance(cArgs.FinalCentroidX, cArgs.FinalCentroidY) <= SQUARE_MAX_DISTANCE)
                {
                    m_links.Add(new DemoObjectLink(m_form, fromObj, toObj, cArgs.NOfFingers));
                    m_form.Invalidate();
                    //Console.WriteLine("Link added");
                }
            }
        }

        public void OpenLinkRequest(int channel, DemoObject demoObject)
        {
            lock (m_lock)
            {
                if (!m_linkRequests.ContainsKey(channel))
                    m_linkRequests[channel] = new List<DemoObject>();

                foreach (DemoObject req in m_linkRequests[channel])
                    m_pendingLinks.Add(new DemoObjectLink(m_form, req, demoObject, channel));

                m_linkRequests[channel].Add(demoObject);
            }
        }
        public void CloseLinkRequest(int channel, DemoObject demoObject)
        {
            lock (m_lock)
            {
                m_linkRequests[channel].Remove(demoObject);
            }
        }

        public IEnumerable<ITuioObjectGestureListener> HitTestTangibles(float x, float y)
        {
            foreach (DemoObject t in m_currentTuioObjects)
                if (t.ContainsPoint(x, y))
                    yield return t;
        }

        public void Draw(Graphics g, float screen)
        {
            foreach (DemoObjectLink link in m_links)
                link.Draw(g, screen);

            foreach (DemoObject demoObject in m_idDemoObjectTable.Values)
                demoObject.Draw(g, screen);
        }
    }
}
