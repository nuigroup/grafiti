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
using System.Collections.Generic;
using System.Threading;
using Grafiti;
using TUIO;

namespace GenericDemo
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
            : base(true) // Default is exclusive
        {
            m_demoObjectManager = demoObjectManager;
        }
    }

    public class RemovingLinkGR : GlobalGestureRecognizer
    {
        private DemoObjectManager m_demoObjectManager;
        private List<DemoObjectLink> m_linksToRemove;
        
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

            if (Group.Traces.Count != 1 || cursor.State == CursorPoint.States.REMOVED)
            {
                GestureHasBeenRecognized(false);
                return;
            }

            foreach (DemoObjectLink link in m_demoObjectManager.Links)
                if (cursor.SquareDistance(link.Xm, link.Ym) <= 0.0015)
                    m_linksToRemove.Add(link);

            if (m_linksToRemove.Count > 0)
            {
                AppendEvent(RemoveLinks, new RemovingLinkGREventArgs("RemoveLink", Group.Id, m_linksToRemove));
                Terminate(true);
            }
        }
    }
}