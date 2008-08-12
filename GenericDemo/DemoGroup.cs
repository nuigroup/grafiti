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
using System.Drawing;
using System.Text;
using Grafiti;


namespace GenericDemo
{
    class DemoGroup
    {
        MainForm m_form;
        Group m_group;
        Color m_color;
        Pen m_pen;
        List<DemoTrace> m_demoTraces = new List<DemoTrace>();

        public Group Group { get { return m_group; } }
        public Color Color { get { return m_color; } }

        public DemoGroup(MainForm mainForm, Group group, Color color)
        {
            m_form = mainForm;
            m_group = group;
            m_color = color;
            m_pen = new Pen(m_color);
        }

        public void Update(long timestamp)
        {
            // Add new demo traces
            foreach (Trace trace in m_group.Traces)
            {
                if (!m_demoTraces.Exists(delegate(DemoTrace demoTrace)
                {
                    return (demoTrace.Trace == trace);
                }))
                    m_demoTraces.Add(new DemoTrace(m_form, this, trace));
            }

            foreach (DemoTrace demoTrace in m_demoTraces)
                demoTrace.Update(timestamp);
        }

        public void Draw(Graphics g, float screen)
        {
            // draw demo traces
            foreach (DemoTrace demoTrace in m_demoTraces)
                demoTrace.Draw(g, screen);

            // draw lines from cursor to centroid
            foreach (Trace trace in m_group.Traces)
            {
                if (trace.Alive)
                {
                    Cursor cursor = trace.Last;
                    g.DrawLine(m_pen,
                        m_group.CentroidX * screen, m_group.CentroidY * screen,
                        cursor.X * screen, cursor.Y * screen);
                }
            }

            // draw lines to target(s)
            if (m_group.NumberOfPresentTraces >= 0 && !m_group.OnSingleGUIControl)
            {
                if (m_group.ExclusiveLocalTarget != null)
                {
                    // draw lines from centroid to exclusive local target
                    m_pen.Width = 2;
                    DrawLineToTarget(g, (DemoObject)m_group.ExclusiveLocalTarget, screen);
                }
                else
                {
                    // draw lines from centroid to all possible local targets
                    m_pen.Width = 1;
                    foreach (IGestureListener target in m_group.LGRTargets)
                        DrawLineToTarget(g, (object)target, screen);
                }
            }            
        }

        private void DrawLineToTarget(Graphics g, object obj, float screen)
        {
            // have to check whether the object is a DemoObject because it could be the GUI control
            // where the gesture have started from (if Settings.LGR_TARGET_LIST == INITIAL_TARGET_LIST)
            if (obj is DemoObject)
            {
                DemoObject demoObj = (DemoObject)obj;
                g.DrawLine(m_pen,
                    m_group.CentroidX * screen, m_group.CentroidY * screen,
                    demoObj.X * screen, demoObj.Y * screen);
            }
        }
    }
}
