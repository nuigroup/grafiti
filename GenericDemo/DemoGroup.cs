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
        MainForm m_mainForm;
        Group m_group;
        Color m_color;
        Pen m_pen;
        List<DemoTrace> m_demoTraces = new List<DemoTrace>();

        public Group Group { get { return m_group; } }
        public Color Color { get { return m_color; } }

        public DemoGroup(MainForm mainForm, Group group, Color color)
        {
            m_mainForm = mainForm;
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
                    m_demoTraces.Add(new DemoTrace(m_mainForm, this, trace));
            }

            foreach (DemoTrace demoTrace in m_demoTraces)
                demoTrace.Update(timestamp);
        }

        public void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            foreach (DemoTrace demoTrace in m_demoTraces)
                demoTrace.OnPaint(e);

            Graphics g = e.Graphics;

            // draw lines from cursor to centroid
            foreach (Trace trace in m_group.Traces)
            {
                if (trace.Alive)
                {
                    Cursor cursor = trace.Last;
                    g.DrawLine(m_pen,
                        m_group.CentroidX * MainForm.height, m_group.CentroidY * MainForm.height,
                        cursor.X * MainForm.height, cursor.Y * MainForm.height);
                }
            }

            if (m_group.NOfAliveTraces >= 0)
            {
                if (m_group.ExclusiveLocalTarget != null)
                {
                    // draw lines from centroid to exclusive local target
                    m_pen.Width = 2;
                    DrawLineToTarget(g, (DemoObject)m_group.ExclusiveLocalTarget);
                }
                else
                {
                    // draw lines from centroid to all possible local targets
                    m_pen.Width = 1;
                    foreach (ITuioObjectGestureListener target in m_group.LGRTargets)
                        DrawLineToTarget(g, (DemoObject)target);
                }
            }
        }

        private void DrawLineToTarget(Graphics g, DemoObject demoObj)
        {
            g.DrawLine(m_pen,
                m_group.CentroidX * MainForm.height, m_group.CentroidY * MainForm.height,
                demoObj.X * MainForm.height, demoObj.Y * MainForm.height);
        }
    }
}
