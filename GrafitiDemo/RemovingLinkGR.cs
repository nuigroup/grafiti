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
using System.Threading;
using Grafiti;
using TUIO;

namespace GrafitiDemo
{
    public class RemovingLinkGREventArgs : GestureEventArgs
    {
        private List<DemoObjectLink> m_links;

        public List<DemoObjectLink> Links { get { return m_links; } }

        public RemovingLinkGREventArgs(string eventId, int groupId, List<DemoObjectLink> links)
            : base(eventId, groupId)
        {
            m_links = links;
        }
    }

    public class RemovingLinkGRConfigurator : GRConfigurator
    {
        public static readonly RemovingLinkGRConfigurator DEFAULT_CONFIGURATOR = new RemovingLinkGRConfigurator();

        private DemoObjectManager m_demoObjectManager;

        public DemoObjectManager DemoObjectManager { get { return m_demoObjectManager; } }

        public RemovingLinkGRConfigurator()
            : this(null) { }

        public RemovingLinkGRConfigurator(DemoObjectManager demoObjectManager)
            : base(true) // exclusive
        {
            m_demoObjectManager = demoObjectManager;
        }
    }

    public class RemovingLinkGR : GlobalGestureRecognizer
    {
        private const float SQUARE_DISTANCE_THRESHOLD = 0.02f * 0.02f;

        private DemoObjectManager m_demoObjectManager;
        private List<DemoObjectLink> m_linksToRemove;
        private float x1, y1, x2, y2; // coordinates of the last two points

        public RemovingLinkGR(GRConfigurator configurator)
            : base(configurator)
        {
            if (!(configurator is RemovingLinkGRConfigurator))
                Configurator = RemovingLinkGRConfigurator.DEFAULT_CONFIGURATOR;
            RemovingLinkGRConfigurator conf = (RemovingLinkGRConfigurator)Configurator;
            m_demoObjectManager = conf.DemoObjectManager;
            DefaultEvents = new string[] { "RemoveLinks" };
            m_linksToRemove = new List<DemoObjectLink>();
        }

        public event GestureEventHandler RemoveLinks;

        public override void Process(List<Trace> traces)
        {
            CursorPoint cursor = traces[0].Last;

            if (Group.Traces.Count > 1)
            {
                Terminate(false);
                return;
            }

            if (cursor.State == CursorPoint.States.ADDED)
            {
                x2 = cursor.X;
                y2 = cursor.Y;

                // If the added finger is over the middle point of a link then
                // notify immediately about that link (the first found) and exit.
                float dx, dy;
                foreach (DemoObjectLink link in m_demoObjectManager.Links)
                {
                    dx = x2 - link.Xm;
                    dy = y2 - link.Ym;
                    if (dx * dx + dy * dy <= SQUARE_DISTANCE_THRESHOLD)
                    {
                        m_linksToRemove.Add(link);
                        AppendEvent(RemoveLinks, new RemovingLinkGREventArgs("RemoveLink", Group.Id, m_linksToRemove));
                        Terminate(true);
                        break;
                    }
                }
                return;
            }
            x1 = x2;
            y1 = y2;
            x2 = cursor.X;
            y2 = cursor.Y;


            foreach (DemoObjectLink link in m_demoObjectManager.Links)
            {
                if (m_linksToRemove.Contains(link))
                    continue;

                if (Intersect(x1, y1, x2, y2,
                    link.FromObject.X, link.FromObject.Y, link.ToObject.X, link.ToObject.Y))
                    m_linksToRemove.Add(link);
            }


            if (Group.NumberOfPresentTraces == 0)
            {
                if (m_linksToRemove.Count > 0)
                {
                    AppendEvent(RemoveLinks, new RemovingLinkGREventArgs("RemoveLink", Group.Id, m_linksToRemove));
                    Terminate(true);
                }
                else
                    Terminate(false);
            }
        }



        private bool Intersect(
            float l1x1, float l1y1, float l1x2, float l1y2,
            float l2x1, float l2y1, float l2x2, float l2y2)
        {
            return
                (Math.Max(l1x1, l1x2) >= Math.Min(l2x1, l2x2)) &&
                (Math.Max(l2x1, l2x2) >= Math.Min(l1x1, l1x2)) &&
                (Math.Max(l1y1, l1y2) >= Math.Min(l2y1, l2y2)) &&
                (Math.Max(l2y1, l2y2) >= Math.Min(l1y1, l1y2)) &&
                (Multiply(l1x2, l1y2, l2x1, l2y1, l1x1, l1y1) * 
                 Multiply(l2x2, l2y2, l1x2, l1y2, l1x1, l1y1) >= 0) &&
                (Multiply(l2x2, l2y2, l1x2, l1y2, l2x1, l2y1) * 
                 Multiply(l1x1, l1y1, l2x2, l2y2, l2x1, l2y1) >= 0);
        }

        private float Multiply(float x1, float y1, float x2, float y2, float x0, float y0)
        {
            return (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0);
        }

        #region Alternative algorithm
        // If the algorithm above gives problems there is also the following
        //
        //public bool Intersect(
        //    float l1x1, float l1y1, float l1x2, float l1y2,
        //    float l2x1, float l2y1, float l2x2, float l2y2)
        //{
        //    int sign1, sign2;

        //    if ((Math.Max(l1x1, l1x2) >= Math.Min(l2x1, l2x2)) &&
        //        (Math.Max(l2x1, l2x2) >= Math.Min(l1x1, l1x2)) &&
        //        (Math.Max(l1y1, l1y2) >= Math.Min(l2y1, l2y2)) &&
        //        (Math.Max(l2y1, l2y2) >= Math.Min(l1y1, l1y2)))
        //    {
        //        sign1 = Math.Sign(CrossProduct(l2x1 - l1x1, l2y1 - l1y1, l1x2 - l1x1, l1y2 - l1y1));
        //        sign2 = Math.Sign(CrossProduct(l2x2 - l1x1, l2y2 - l1y1, l1x2 - l1x1, l1y2 - l1y1));

        //        if (sign1 == sign2 || sign1 == 0 || sign2 == 0)
        //        {
        //            sign1 = Math.Sign(CrossProduct(l1x1 - l2x1, l1y1 - l2y1, l2x2 - l2x1, l2y2 - l2y1));
        //            sign2 = Math.Sign(CrossProduct(l1x2 - l2x1, l1y2 - l2y1, l2x2 - l2x1, l2y2 - l2y1));

        //            if (sign1 == sign2 || sign1 == 0 || sign2 == 0)
        //                return true;
        //        }
        //    }

        //    return false;
        //}

        //private float CrossProduct(float x1, float y1, float x2, float y2)
        //{
        //    return x1 * y2 - y1 * x2;
        //} 
        #endregion
    }
}