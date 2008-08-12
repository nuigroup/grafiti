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

namespace Grafiti
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

        // maximum threshold, relative to the circle's radius for...
        private float m_threshold;
        private const float THRESHOLD_DEFAULT = 0.5f;

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
    /// Recognize a circle, made by one or more traces (starting and ending in a synchronized way,
    /// relatively to GROUPING_SYNCH_TIME). In order to be correctly recognized, the circle must
    /// be produced with a relatively constant speed.
    /// </summary>
    public class CircleGR : GlobalGestureRecognizer
    {
        CircleGRConfigurator m_configurator;
        private int m_startingTime = -1;
        private List<float> m_centroidXs = new List<float>();
        private List<float> m_centroidYs = new List<float>();


        // These are public only to make reflection to work.
        // They're not intended to be accessed directly from clients.
        public event GestureEventHandler Circle;

        public CircleGR(GRConfigurator configurator)
            : base(configurator)
        {
            if (!(configurator is CircleGRConfigurator))
                Configurator = CircleGRConfigurator.DEFAULT_CONFIGURATOR;

            m_configurator = (CircleGRConfigurator)Configurator;
            m_startingTime = -1;

            DefaultEvents = new string[] { "Circle" };

        }

        private void OnCircle(int n, float x, float y, float r)
        {
            AppendEvent(Circle, new CircleGREventArgs("Circle", Group.Id, n, x, y, r)); 
        }

        public override void Process(List<Trace> traces)
        {
            if (m_startingTime == -1)
                m_startingTime = traces[0].Last.TimeStamp;

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


                m_centroidXs.Add(Group.CentroidX);
                m_centroidYs.Add(Group.CentroidY);
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

                float xs = 0, ys = 0; // sum of centroids
                for (int i = 0; i < m_centroidXs.Count; i++)
                {
                    xs += m_centroidXs[i];
                    ys += m_centroidYs[i];
                }

                float xm, ym; // mean centroid
                xm = xs / m_centroidXs.Count;
                ym = ys / m_centroidYs.Count;

                float dx, dy, d; // distances
                float ds = 0; // sum of distances from mean centroid
                float maxDistance = 0; // maximum distance between a centroid and the mean centroid
                for (int i = 0; i < m_centroidXs.Count; i++)
                {
                    dx = m_centroidXs[i] - xm;
                    dy = m_centroidYs[i] - ym;
                    d = (float)Math.Sqrt(dx * dx + dy * dy);
                    maxDistance = Math.Max(maxDistance, d);
                    ds += d;
                }

                float meanRadius = ds / m_centroidXs.Count;

                if (maxDistance - meanRadius > meanRadius * m_configurator.Threshold)
                {
                    Terminate(false);
                    return;
                }

                // distance between first and last point
                dx = m_centroidXs[0] - m_centroidXs[m_centroidXs.Count - 1];
                dy = m_centroidYs[0] - m_centroidYs[m_centroidYs.Count - 1];
                if (Math.Sqrt(dx * dx + dy * dy) > meanRadius * m_configurator.Threshold)
                {
                    Terminate(false);
                    return;
                }

                OnCircle(Group.Traces.Count, xm, ym, meanRadius);
                Terminate(true);
            }
        }
    }

}