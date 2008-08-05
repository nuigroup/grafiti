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
using System.Text;
using System.Drawing;
using System.Threading;
using Grafiti;

namespace GenericDemo
{
    class DemoTrace
    {
        const int MAX_TAIL_LENGTH = 15; // number of refresh() calls
        const float PEN_WIDTH = 2f;
        MainForm m_mainForm;
        Trace m_trace;
        int m_startPath = 0;
        Pen m_pen;
        SolidBrush m_brush;
        Font m_idFont = new Font("Arial", 9.5f);
        float m_pointerSize;

        internal Trace Trace { get { return m_trace; } }

        public DemoTrace(MainForm mainForm, DemoGroup demoGroup, Trace trace)
        {
            Color color = demoGroup.Color;
            m_mainForm = mainForm;
            m_trace = trace;
            m_brush = new SolidBrush(Color.FromArgb(10, color.R, color.G, color.B));
            m_pen = new Pen(m_brush, PEN_WIDTH);
            m_pointerSize = MainForm.height / 25;
        }

        public void Update(long timestamp)
        {
            m_startPath = Math.Max(m_startPath, m_trace.Count - MAX_TAIL_LENGTH);
            if (m_trace.Last.State == Cursor.States.REMOVED)
            {//
                if (m_startPath < m_trace.Count - 1)
                    m_startPath++;
            }//
            else
                m_startPath = Math.Max(0, m_trace.Count - MAX_TAIL_LENGTH);
        }
        
        public void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Cursor last = m_trace.Last;
            float screen = (float)MainForm.height;

            // draw path
            for (int i = m_startPath; i < m_trace.Count - 1; i++)
                g.DrawLine(m_pen, Utilities.GetScreenPoint(m_trace[i]), Utilities.GetScreenPoint(m_trace[i + 1]));

            // draw pointer and id
            if (!(m_startPath == m_trace.Count - 1 && last.State == Cursor.States.REMOVED))
            {
                Brush pointerBrush;
                if (last.State == Cursor.States.REMOVED)
                    pointerBrush = Brushes.Black;
                else
                    pointerBrush = Brushes.Gray;
                g.FillEllipse(pointerBrush, last.X * screen - m_pointerSize / 2, last.Y * screen - m_pointerSize / 2, m_pointerSize, m_pointerSize);
                g.DrawString(m_trace.Id.ToString(), m_idFont, Brushes.White, last.X * screen - m_pointerSize / 2, last.Y * screen - m_pointerSize / 2);

                // draw grouping area
                g.FillEllipse(m_brush, last.X * screen - Settings.GetGroupingSpace() * screen, last.Y * screen - Settings.GetGroupingSpace() * screen, Settings.GetGroupingSpace() * 2 * screen, Settings.GetGroupingSpace() * 2 * screen);
            }
        }
    }
}
