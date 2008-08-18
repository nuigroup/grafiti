/*
	Grafiti library

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
using Grafiti;

namespace Grafiti.GestureRecognizers
{
    public class CircleGREventArgs : GestureEventArgs
    {
        protected int m_nOfFingers;
        protected float m_meanCenterX, m_meanCenterY;
        protected float m_meanRadius;

        public int NOfFingers { get { return m_nOfFingers; } }
        public float MeanCenterX { get { return m_meanCenterX; } }
        public float MeanCenterY { get { return m_meanCenterY; } }
        public float MeanRadius { get { return m_meanRadius; } }

        public CircleGREventArgs() 
            : base() { }

        public CircleGREventArgs(string eventId, int groupId, int nFingers, float centroidX, float centroidY, float meanRadius)
            : base(eventId, groupId)
        {
            m_nOfFingers = nFingers;
            m_meanCenterX = centroidX;
            m_meanCenterY = centroidY;
            m_meanRadius = meanRadius;
        }
    }

    public delegate void CircleGREventHandler(object obj, CircleGREventArgs args);


    public class CircleGRConfigurator : GRConfigurator
    {
        public static readonly CircleGRConfigurator DEFAULT_CONFIGURATOR = new CircleGRConfigurator();

        /// <summary>
        /// Threshold value multiplier, that will refer to the radius length.
        /// Distance between first and last point must be less than radius * threshold.
        /// Distance between every point of the path and the center must be between radius +/-
        /// radius * threshold.
        /// </summary>
        private readonly float m_threshold;
        private const float THRESHOLD_DEFAULT = 0.25f;

        public float Threshold { get { return m_threshold; } }

        public CircleGRConfigurator()
            : this(THRESHOLD_DEFAULT) { }

        public CircleGRConfigurator(bool exclusive)
            : this(exclusive, THRESHOLD_DEFAULT) { }

        public CircleGRConfigurator(float threshold)
            : base()
        {
            m_threshold = threshold;
        }

        public CircleGRConfigurator(bool exclusive, float threshold)
            : base(exclusive)
        {
            m_threshold = threshold;
        }
    }


    /// <summary>
    /// Circle recognizer. The gesture can be made by one or more traces (starting and ending in a 
    /// synchronized way, relatively to GROUPING_SYNCH_TIME).
    /// The algorythm will indistinctly recognize round paths covering the shape of a circonference
    /// (even passing more times on the same section). However the first and the last point of the path 
    /// must be close to each other.
    /// A "bug" makes the GR to recognize as a circle also a 'C' if the path is closed enough (not much),
    /// and such that the extremes of the path are close to each other (you must draw the letter twice:
    /// forward and backward).
    /// </summary>
    public class CircleGR : GlobalGestureRecognizer
    {
        CircleGRConfigurator m_configurator;
        private int m_startingTime = -1;
        private readonly float m_threshold;
        private float m_left, m_right, m_top, m_bottom;
        private List<float> m_pathXs = new List<float>();
        private List<float> m_pathYs = new List<float>();


        // These are public only to make reflection to work.
        // They're not intended to be accessed directly from clients.
        public event GestureEventHandler Circle;

        public CircleGR(GRConfigurator configurator)
            : base(configurator)
        {
            if (!(configurator is CircleGRConfigurator))
                Configurator = CircleGRConfigurator.DEFAULT_CONFIGURATOR;

            m_configurator = (CircleGRConfigurator)Configurator;
            m_threshold = m_configurator.Threshold;
            m_startingTime = -1;

            DefaultEvents = new string[] { "Circle" };

            m_left = Surface.SCREEN_RATIO;
            m_right = 0;
            m_top = 1;
            m_bottom = 0;

        }

        private void OnCircle(int n, float x, float y, float r)
        {
            AppendEvent(Circle, new CircleGREventArgs("Circle", Group.Id, n, x, y, r)); 
        }

        public override void Process(List<Trace> traces)
        {
            if (m_startingTime == -1)
                m_startingTime = traces[0].Last.TimeStamp;

            float x, y;

            foreach (Trace trace in traces)
            {
                if (trace.State == Trace.States.ADDED)
                {
                    if (!(trace.Last.TimeStamp - m_startingTime <= Settings.GetGroupingSynchTime()))
                    {
                        Terminate(false);
                        return;
                    }
                }
                else if (trace.State == Trace.States.REMOVED && trace.Count < 3)
                {
                    Terminate(false);
                    return;
                }

                x = Group.CentroidX;
                y = Group.CentroidY;

                m_pathXs.Add(x);
                m_pathYs.Add(y);

                m_left = Math.Min(m_left, x);
                m_top = Math.Min(m_top, y);
                m_right = Math.Max(m_right, x);
                m_bottom = Math.Max(m_bottom, y);
            }

            if (!Group.IsPresent)
            {
                int endTime = Group.CurrentTimeStamp;
                if (!Group.Traces.TrueForAll(delegate(Trace t)
                {
                    return endTime - t.Last.TimeStamp <= Settings.GetGroupingSynchTime();
                }))
                {
                    Terminate(false);
                    return;
                }

                float leftRight = m_right - m_left;
                float topBottom = m_bottom - m_top;
                float centerX = m_left + leftRight / 2;
                float centerY = m_top + topBottom / 2;
                float radius = leftRight < topBottom ? leftRight / 2 : topBottom / 2;

                float dx, dy, d;

                // Distance between first and last point must be less than radius * threshold
                dx = m_pathXs[m_pathXs.Count - 1] - m_pathXs[0];
                dy = m_pathYs[m_pathYs.Count - 1] - m_pathYs[0];
                d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d > radius * m_threshold)
                {
                    Terminate(false);
                    return;
                }

                // Distance between every point and the center must be between radius +/-
                // radius * threshold
                for (int i = 0; i < m_pathXs.Count; i++)
                {
                    dx = m_pathXs[i] - centerX;
                    dy = m_pathYs[i] - centerY;
                    d = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (d > radius * (1 + m_threshold) || d < radius * (1 - m_threshold))
                    {
                        Terminate(false);
                        return;
                    }
                }

                OnCircle(Group.Traces.Count, centerX, centerY, radius);
                Terminate(true);
            }
        }
    }

}