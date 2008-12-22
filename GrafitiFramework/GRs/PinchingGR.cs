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
using System.Threading;
using Grafiti;

namespace Grafiti.GestureRecognizers
{
    public class PinchEventArgs : GestureEventArgs
    {
        private float m_x = 0, m_y = 0;
        private float m_size = 0;
        private float m_rotation = 0;

        public float X{get {return m_x;}}
        public float Y{get {return m_y;}}
        public float Size { get { return m_size; } }
        public float Rotation { get { return m_rotation; } }

        public PinchEventArgs() 
            : base() { }

        public PinchEventArgs(string eventId, int groupId, 
            float x, float y, float size, float rotation)
            : base(eventId, groupId)
        {
            m_x = x;
            m_y = y;
            m_size = size;
            m_rotation = rotation;
        }
    }

    public delegate void PinchEventHandler(object obj, PinchEventArgs args);


    public class PinchingGRConfiguration : GRConfiguration
    {
        private readonly IPinchable m_clientWindow;
        private readonly bool m_relativeScaling;

        public IPinchable ClientWindow { get { return m_clientWindow; } }
        public bool RelativeScaling { get { return m_relativeScaling; } }

        public PinchingGRConfiguration(bool exclusive, IPinchable clientWindow, bool relativeScaling)
            : base(exclusive)
        {
            m_clientWindow = clientWindow;
            m_relativeScaling = relativeScaling;
        }
    }

    public interface IPinchable
    { 
        void GetPinchReference(out float x, out float y, out float size, out float rotation);
    }

    public class PinchingGR : LocalGestureRecognizer
    {
        // Configuration
        private IPinchable m_clientWindow;
        private bool m_relativeScaling;

        // referential traces
        private Trace m_traceA, m_traceB;

        // reference values
        private float m_A0x, m_A0y, m_B0x, m_B0y;
        private float m_lengthA0B0;
        private float m_angleA0B0;
        private float m_xRef, m_yRef, m_sizeRef, m_rotationRef;
        private float m_scaleFactor;


        public PinchingGR()
            : base(null) { }

        public PinchingGR(GRConfiguration configuration)
            : base(configuration)
        {
            PinchingGRConfiguration conf = (PinchingGRConfiguration)configuration;
            m_clientWindow = conf.ClientWindow;
            m_relativeScaling = conf.RelativeScaling;
        }

        public event GestureEventHandler Pinch;

        public override void Process(List<Trace> traces)
        {
            if (Group.NumberOfAliveTraces > 2)
            {
                Terminate(false);
                return;
            }
            else if (Group.NumberOfAliveTraces != 2)
                return;

            // If some cursor has been added or removed this flag is set to true
            // so that references for computing scaling, translation and rotation
            // are updated
            if (traces.Exists(delegate(Trace trace)
            {
                return trace.State == Trace.States.ADDED ||
                    trace.State == Trace.States.REMOVED ||
                    trace.State == Trace.States.RESET;
            }))
            {
                ValidateGesture();

                // Set traces A and B
                m_traceA = m_traceB = null;
                foreach (Trace trace in Group.Traces)
                {
                    if (trace.IsAlive)
                        if (m_traceA == null)
                        {
                            m_traceA = trace;
                        }
                        else
                        {
                            m_traceB = trace;
                            break;
                        }
                }

                m_A0x = m_traceA.Last.X;
                m_A0y = m_traceA.Last.Y;
                m_B0x = m_traceB.Last.X;
                m_B0y = m_traceB.Last.Y;

                m_lengthA0B0 = GetDistanceAB();
                m_angleA0B0 = GetAngleAB();

                float x, y, size, rotation;
                m_clientWindow.GetPinchReference(out x, out y, out size, out rotation);
                m_xRef = x;
                m_yRef = y;
                m_sizeRef = size;
                m_rotationRef = rotation;
                m_scaleFactor = m_sizeRef / m_lengthA0B0;
            }
            else
            {
                // Compute scaling
                if (Group.NumberOfAliveTraces == 2)
                {
                    // Scale
                    float newLength = GetDistanceAB();
                    float deltaSize = (newLength - m_lengthA0B0);
                    if (m_relativeScaling)
                        deltaSize *= m_scaleFactor;
                    float size = m_sizeRef + deltaSize;

                    // rotation
                    float newAngle = GetAngleAB();
                    float deltaAngle = newAngle - m_angleA0B0;
                    float rotation = m_rotationRef - deltaAngle;

                    // translate
                    // How did 'a' get there (from a0 to a)? Let's do all the steps:
                    // translate a0 of -R (a is now relative to R, the reference point)
                    float A1x = m_A0x - m_xRef;
                    float A1y = m_A0y - m_yRef;
                    // scale a0 (from R) of ratioLength
                    float ratioLength = newLength / m_lengthA0B0;
                    A1x *= ratioLength;
                    A1y *= ratioLength;
                    // rotate a0 (around c0) of rotation
                    float tempX = (float)((double)A1x * Math.Cos(deltaAngle) - (double)A1y * Math.Sin(deltaAngle));
                    float tempY = (float)((double)A1x * Math.Sin(deltaAngle) + (double)A1y * Math.Cos(deltaAngle));
                    #region Step by step
                    //// back to c0 (now it's in absolute coordinates)
                    //A1x = tempX + m_xRef;
                    //A1y = tempY + m_yRef;

                    //// Now the point obtained is just a translation back from the current 'a'
                    //// so calculate the translation
                    //float xt = m_traceA.Last.X - A1x;
                    //float yt = m_traceA.Last.Y - A1y;

                    //// and apply it to the reference point
                    //float x = xt + m_xRef;
                    //float y = yt + m_yRef; 
                    #endregion
                    float x = m_traceA.Last.X - tempX;
                    float y = m_traceA.Last.Y - tempY;

                    AppendEvent(Pinch, new PinchEventArgs("Pinch", Group.Id, x, y, size, rotation));
                }
            }
        }

        //// set centroid reference to the current centroid (calculated on the living traces)
        //private void UpdateCentroidRef()
        //{
        //    m_centroidXRef = Group.LivingCentroidX;
        //    m_centroidYRef = Group.LivingCentroidY;
        //}
        //// set distance-to-centroid reference to the current distance to centroid
        //private void UpdateDistanceFromCentroidRef()
        //{
        //    m_centroidDistanceRef = GetMeanDistanceFromCentroid();
        //}
        //// calculate distance to current centroid
        //private float GetMeanDistanceFromCentroid()
        //{
        //    float dx = 0, dy = 0;
        //    foreach (Trace trace in Group.Traces)
        //    {
        //        if (trace.IsAlive)
        //        {
        //            dx += Math.Abs(trace.Last.X - Group.LivingCentroidX);
        //            dy += Math.Abs(trace.Last.Y - Group.LivingCentroidY);
        //        }
        //    }
        //    dx /= Group.NumberOfAliveTraces;
        //    dy /= Group.NumberOfAliveTraces;

        //    return (float)Math.Sqrt(dx * dx + dy * dy);
        //}
        
        // get distance between A and B
        private float GetDistanceAB()
        {
            float dx = Math.Abs(m_traceA.Last.X - m_traceB.Last.X);
            float dy = Math.Abs(m_traceA.Last.Y - m_traceB.Last.Y);
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
        // get the current angle between A and B
        private float GetAngleAB()
        {
            float dx = m_traceB.Last.X - m_traceA.Last.X;
            float dy = m_traceB.Last.Y - m_traceA.Last.Y;
            return (float) Math.Atan2(dy, dx);
        }
        
    }
}