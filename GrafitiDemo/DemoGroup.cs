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
using System.Text;
using Tao.FreeGlut;
using Tao.OpenGl;
using Grafiti;


namespace GrafitiDemo
{
    public class DemoGroup
    {
        private Group m_group;
        private MyColor m_color;
        private List<DemoTrace> m_demoTraces = new List<DemoTrace>();
        private static Random s_random = new Random();

        public Group Group { get { return m_group; } }
        public MyColor Color { get { return m_color; } }
        
        public DemoGroup(Group group)
        {
            m_group = group;
            m_color = new MyColor(s_random.NextDouble(), s_random.NextDouble(), s_random.NextDouble());
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
                    m_demoTraces.Add(new DemoTrace(this, trace));
            }

            foreach (DemoTrace demoTrace in m_demoTraces)
                demoTrace.Update(timestamp);
        }

        public void Draw1()
        {
            // draw demo traces
            foreach (DemoTrace demoTrace in m_demoTraces)
                demoTrace.Draw(1);

            Gl.glColor3d(m_color.R, m_color.G, m_color.B);

            // draw lines from last cursor to centroid
            foreach (Trace trace in m_group.Traces)
            {
                if (trace.IsAlive)
                {
                    Gl.glLineWidth(1);
                    Gl.glBegin(Gl.GL_LINES);
                        Gl.glVertex2f(m_group.ActiveCentroidX, m_group.ActiveCentroidY);
                        Gl.glVertex2f(trace.Last.X, trace.Last.Y);
                    Gl.glEnd();
                }
            }

            // draw lines to target(s)
            if (m_group.NumberOfAliveTraces >= 0 && !m_group.OnZControl)
            {
                if (m_group.ExclusiveLocalTarget != null)
                {
                    // draw lines from centroid to exclusive local target
                    Gl.glLineWidth(2);
                    DrawLineToTarget(m_group.ExclusiveLocalTarget);
                }
                else
                {
                    // draw lines from centroid to all possible local targets
                    Gl.glLineWidth(1);
                    foreach (IGestureListener target in m_group.LGRTargets)
                        DrawLineToTarget(target);
                }
            }
        }
        public void Draw2()
        {
            // draw demo traces
            foreach (DemoTrace demoTrace in m_demoTraces)
                demoTrace.Draw(2);
        }

        private void DrawLineToTarget(object obj)
        {
            // have to check whether the object is a DemoObject because it could be the GUI control
            // where the gesture have started from (if Settings.LGR_TARGET_LIST == INITIAL_TARGET_LIST)
            if (obj is DemoObject)
            {
                DemoObject demoObj = (DemoObject)obj;
                Gl.glBegin(Gl.GL_LINES);
                    Gl.glVertex2f(m_group.ActiveCentroidX, m_group.ActiveCentroidY);
                    Gl.glVertex2f(demoObj.X, demoObj.Y);
                Gl.glEnd();
            }
        }
    }
}
