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
        const int PATH_LENGTH_MILLISECONDS = 1000;
        const float PEN_WIDTH = 2f;

        MainForm m_mainForm;

        Trace m_trace;
        int m_startPath = 0;

        Pen m_pen;
        Font m_idFont = new Font("Arial", 9.5f);
        float m_pointerSize;

        Thread m_thread = null;
        DateTime m_lastUpdateTime = DateTime.Now;
        long m_lastTimestamp = 0;

        internal Trace Trace { get { return m_trace; } }

        public DemoTrace(MainForm mainForm, DemoGroup demoGroup, Trace trace)
        {
            m_mainForm = mainForm;
            m_trace = trace;
            m_pen = new Pen(new SolidBrush(demoGroup.Color), PEN_WIDTH);
            m_pointerSize = MainForm.height / 25;
            m_thread = new Thread(new ThreadStart(TailLoop));
            m_thread.Start();
        }

        public void Update(long timestamp)
        {
            lock (this)
            {
                m_lastUpdateTime = DateTime.Now;
                m_lastTimestamp = timestamp;
            }
        }

        public void TailLoop()
        {
            while (true)
            {
                long virtualCurrentTimestamp;
                lock (this)
                {
                    long deltaMs = (long)(DateTime.Now - m_lastUpdateTime).TotalMilliseconds;
                    virtualCurrentTimestamp = deltaMs + m_lastTimestamp;
                }

                int oldStartPath = m_startPath;

                while (m_startPath < m_trace.Path.Count - 1 &&
                    virtualCurrentTimestamp - m_trace.Path[m_startPath].TimeStamp > PATH_LENGTH_MILLISECONDS)
                    m_startPath++;
                
                if (m_startPath != oldStartPath)
                    m_mainForm.Invalidate();

                Thread.Sleep(5);
            }
        }

        public void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // draw path
            for (int i = m_startPath; i < m_trace.Path.Count - 1; i++)
            {
                g.DrawLine(m_pen, Utilities.GetScreenPoint(m_trace.Path[i]), Utilities.GetScreenPoint(m_trace.Path[i + 1]));
            }

            // draw pointer and id
            Cursor last = m_trace.Path[m_trace.Path.Count - 1];
            if (!(m_startPath == m_trace.Path.Count - 1 && last.State == Cursor.States.REMOVED))
            {
                Brush pointerBrush;
                if (last.State == Cursor.States.REMOVED)
                    pointerBrush = Brushes.Black;
                else
                    pointerBrush = Brushes.Gray;
                g.FillEllipse(pointerBrush, last.X * MainForm.height - m_pointerSize / 2, last.Y * MainForm.height - m_pointerSize / 2, m_pointerSize, m_pointerSize);
                g.DrawString(m_trace.Id.ToString(), m_idFont, Brushes.White, new PointF(last.X * MainForm.height - m_pointerSize / 2, last.Y * MainForm.height - m_pointerSize / 2));
            }
        }
    }
}
