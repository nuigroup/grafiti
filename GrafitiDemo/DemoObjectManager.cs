/*
	GrafitiDemo, Grafiti demo application

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
using System.Collections;
using Tao.FreeGlut;
using Tao.OpenGl;
using TUIO;
using Grafiti;
using Grafiti.GestureRecognizers;

namespace GrafitiDemo
{
    public class DemoObjectLink
    {
        private Viewer m_viewer;
        private DemoObject m_fromObject, m_toObject;
        private float m_xm, m_ym; // mean point
        private int m_nFingers;

        public float Xm { get { return m_xm; } }
        public float Ym { get { return m_ym; } }
        public DemoObject FromObject { get { return m_fromObject; } }
        public DemoObject ToObject { get { return m_toObject; } }

        public DemoObjectLink(Viewer viewer, DemoObject from, DemoObject to, int nFingers)
        {
            m_viewer = viewer;
            m_fromObject = from;
            m_toObject = to;
            m_nFingers = nFingers;
            Update();
            from.Links.Add(this);
            to.Links.Add(this);
        }

        public void Update()
        {
            m_xm = (m_fromObject.X + m_toObject.X) / 2f;
            m_ym = (m_fromObject.Y + m_toObject.Y) / 2f;
        }

        public void Draw()
        {
            Gl.glLineWidth(m_nFingers);
            Gl.glBegin(Gl.GL_LINES);
            Gl.glVertex2f(m_fromObject.X, m_fromObject.Y);
            Gl.glVertex2f(m_toObject.X, m_toObject.Y);
            Gl.glEnd();

            Gl.glPointSize(m_nFingers * 4);
            Gl.glBegin(Gl.GL_POINTS);
            Gl.glVertex2f(m_xm, m_ym);
            Gl.glEnd();
        }
    }
 
    public class DemoObjectManager : TuioListener, IGestureListener
    {
        #region Variables
        private readonly float CAM_RESO_RATIO = Settings.GetCameraResolutionRatio();
        private readonly float OFFSET_X = Surface.Instance.OffsetX;
        private const float MAX_SQUARE_DISTANCE_FOR_LINKING = 0.25f * 0.25f;
        private Viewer m_viewer;
        private List<ITangibleGestureListener> m_tempTangibleList = new List<ITangibleGestureListener>();
        private List<TuioObject> m_tuioObjectAddedList, m_tuioObjectUpdatedList, m_tuioObjectRemovedList;
        private List<DemoObject> m_currentTuioObjects = new List<DemoObject>();
        private Dictionary<long, DemoObject> m_idDemoObjectTable;
        private List<DemoObjectLink> m_links, m_pendingLinks;
        
        private PinchingGRConfigurator m_pinchingGRConf = new PinchingGRConfigurator(true, 2, -1);
        private BasicMultiFingerGRConfigurator m_basicMultiFingerGRConf = new BasicMultiFingerGRConfigurator();
        private Dictionary<int, List<DemoObject>> m_linkRequests = new Dictionary<int, List<DemoObject>>();
        private static object m_LinkageLock = new object();
        private static object m_lock = new object();

        internal List<DemoObjectLink> Links { get { return m_links; } }
        internal PinchingGRConfigurator PinchingConf { get { return m_pinchingGRConf; } }
        internal BasicMultiFingerGRConfigurator BasicMultiFingerGRConf { get { return m_basicMultiFingerGRConf; } }
        
        #endregion

        public DemoObjectManager(Viewer viewer)
        {
            m_viewer = viewer;
            m_tuioObjectAddedList = new List<TuioObject>();
            m_tuioObjectUpdatedList = new List<TuioObject>();
            m_tuioObjectRemovedList = new List<TuioObject>();
            m_idDemoObjectTable = new Dictionary<long,DemoObject>();
            m_links = new List<DemoObjectLink>();
            m_pendingLinks = new List<DemoObjectLink>();

            RemovingLinkGRConfigurator removingLinkGRConf = new RemovingLinkGRConfigurator(this);
            GestureEventManager.SetPriorityNumber(typeof(RemovingLinkGR), removingLinkGRConf, 1);
            GestureEventManager.RegisterHandler(typeof(RemovingLinkGR), removingLinkGRConf, "RemoveLinks", OnRemoveLinks);

            MultiTraceGRConfigurator m_multiTraceGRConf = new MultiTraceGRConfigurator(true, MAX_SQUARE_DISTANCE_FOR_LINKING, true, false);
            GestureEventManager.SetPriorityNumber(typeof(MultiTraceGR), m_multiTraceGRConf, 2);
            GestureEventManager.RegisterHandler(typeof(MultiTraceGR), m_multiTraceGRConf, "MultiTraceFromTo", OnMultiTraceFromTo);

            LazoGRConfigurator lazoGRConf = new LazoGRConfigurator(m_currentTuioObjects, 1f / 20f);
            GestureEventManager.SetPriorityNumber(typeof(LazoGR), lazoGRConf, 1);
            GestureEventManager.RegisterHandler(typeof(LazoGR), lazoGRConf, "Lazo", OnLazo);
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
            lock (m_lock)
            {
                // Add links that have been added asynchronously (by hovering)
                foreach (DemoObjectLink link in m_pendingLinks)
                    m_links.Add(link);
                m_pendingLinks.Clear();

                foreach (TuioObject o in m_tuioObjectAddedList)
                {
                    DemoObject demoObject = new DemoObject(this, m_viewer, (int)o.getSessionID(), (o.getX() + OFFSET_X) * Settings.GetCameraResolutionRatio(), o.getY(), o.getAngle());
                    m_idDemoObjectTable[o.getSessionID()] = demoObject;
                    m_currentTuioObjects.Add(demoObject);
                }
                foreach (TuioObject o in m_tuioObjectUpdatedList)
                {
                    m_idDemoObjectTable[o.getSessionID()].Update((o.getX() + OFFSET_X) * Settings.GetCameraResolutionRatio(), o.getY(), o.getAngle());
                }
                foreach (TuioObject o in m_tuioObjectRemovedList)
                {
                    DemoObject demoObject = m_idDemoObjectTable[o.getSessionID()];
                    demoObject.Remove((o.getX() + OFFSET_X) * Settings.GetCameraResolutionRatio(), o.getY(), o.getAngle());
                    m_idDemoObjectTable.Remove(o.getSessionID());
                    m_currentTuioObjects.Remove(demoObject);
                    foreach (DemoObjectLink link in demoObject.Links)
                        m_links.Remove(link);
                }

                foreach (DemoObjectLink link in m_links)
                    link.Update();
            }

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
            //refresh
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
                if (fromObj != toObj)
                {
                    DemoObjectLink link = MakeLink(fromObj, toObj, cArgs.NOfFingers);
                    if (link != null)
                    {
                        m_links.Add(link);
                        //refresh
                        //Console.WriteLine("Link added");
                    }
                }
            }
        }

        public void OpenLinkRequest(int channel, DemoObject demoObject)
        {
            lock (m_LinkageLock)
            {
                if (!m_linkRequests.ContainsKey(channel))
                    m_linkRequests[channel] = new List<DemoObject>();

                // this is asynchronous, so for efficiency the link will be actually added later 
                DemoObjectLink link;
                foreach (DemoObject req in m_linkRequests[channel])
                { 
                    link = MakeLink(req, demoObject, channel);
                    if (link != null)
                        m_pendingLinks.Add(link);
                }

                m_linkRequests[channel].Add(demoObject);
            }
        }

        private DemoObjectLink MakeLink(DemoObject from, DemoObject to, int n)
        {
            if (m_currentTuioObjects.Contains(from) && m_currentTuioObjects.Contains(to))
                return new DemoObjectLink(m_viewer, from, to, n);
            else
                return null;
        }
        public void CloseLinkRequest(int channel, DemoObject demoObject)
        {
            lock (m_LinkageLock)
            {
                m_linkRequests[channel].Remove(demoObject);
            }
        }

        public List<ITangibleGestureListener> HitTestTangibles(float x, float y)
        {
            m_tempTangibleList.Clear();
            foreach (DemoObject t in m_currentTuioObjects)
                if (t.ContainsPoint(x, y))
                    m_tempTangibleList.Add(t);
            return m_tempTangibleList;
        }

        public void Draw()
        {
            Gl.glColor3f(1f, 1f, 1f);

            lock (m_lock)
            {
                foreach (DemoObjectLink link in m_links)
                    link.Draw();

                foreach (DemoObject demoObject in m_idDemoObjectTable.Values)
                    demoObject.Draw(); 
            }
        }
    }
}
