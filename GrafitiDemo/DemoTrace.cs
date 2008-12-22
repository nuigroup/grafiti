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
    public class DemoTrace
    {
        const int MAX_TAIL_LENGTH = 15; // number of refresh() calls
        const float PEN_WIDTH = 2f;
        Trace m_trace;
        int m_startPath = 0;
        MyColor m_color;
        float m_pointerSize;

        internal Trace Trace { get { return m_trace; } }

        public DemoTrace(DemoGroup demoGroup, Trace trace)
        {
            m_color = demoGroup.Color;
            m_trace = trace;
            m_pointerSize = 0.01f;
        }

        public void Update(long timestamp)
        {
            m_startPath = Math.Max(m_startPath, m_trace.Count - MAX_TAIL_LENGTH);
            if (m_trace.Last.State == CursorPoint.States.REMOVED)
            {//
                if (m_startPath < m_trace.Count - 1)
                    m_startPath++;
            }//
            else
                m_startPath = Math.Max(0, m_trace.Count - MAX_TAIL_LENGTH);
        }

        // layer 1 is for transparent stuff, layer 2 is for the rest
        public void Draw(int layer)
        {
            CursorPoint last = m_trace.Last;

            if (layer == 2)
            {
                // draw path
                Gl.glLineWidth(1);
                Gl.glColor3d(m_color.R, m_color.G, m_color.B);
                Gl.glBegin(Gl.GL_LINES);
                for (int i = m_startPath; i < m_trace.Count - 1; i++)
                {
                    Gl.glVertex2f(m_trace[i].X, m_trace[i].Y);
                    Gl.glVertex2f(m_trace[i + 1].X, m_trace[i + 1].Y);
                }
                Gl.glEnd();
            }

            // draw pointer and id
            if (!(m_startPath == m_trace.Count - 1 && last.State == CursorPoint.States.REMOVED))
            {

                if (last.State == CursorPoint.States.REMOVED)
                    Gl.glColor3d(0.5, 0.5, 0.5);
                else
                    Gl.glColor3d(1, 1, 1);

                Gl.glPushMatrix();
                    Gl.glTranslatef(last.X, last.Y, 0);

                    if (layer == 2)
                    {
                        Utilities.DrawPlainCircle(m_pointerSize);
                    }

                    //g.DrawString(m_trace.Id.ToString(), m_idFont, Brushes.White,
                        //    last.X * screen - m_pointerSize / 2,
                    //    last.Y * screen - m_pointerSize / 2);

                    if (layer == 1)
                    {
                        Gl.glEnable(Gl.GL_BLEND);
                        Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
                        // draw grouping area
                        if (m_trace.Group.MaxNumberOfActiveTraces < 0 && !m_trace.Group.OnZControl)
                        {
                            Gl.glColor4d(m_color.R, m_color.G, m_color.B, 0.2);
                            Utilities.DrawPlainCircle(Settings.GroupingSpace);
                        }
                        Gl.glDisable(Gl.GL_BLEND);
                    }
                Gl.glPopMatrix();
            }
        }
    }
}
