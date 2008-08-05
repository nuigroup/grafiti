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

namespace Grafiti
{
    public class FingerEventArgs : GestureEventArgs
    { 
        private long m_sessionId;
        private float m_x, m_y;
        private float m_centroidX, m_centroidY;

        public long SessionId { get { return m_sessionId; } }
        public float X { get { return m_x; } }
        public float Y { get { return m_y; } }
        public float CentroidX { get { return m_centroidX; } }
        public float CentroidY { get { return m_centroidY; } }

        public FingerEventArgs(string eventId, int groupId, long sessionId, float x, float y,
            float centroidX, float centroidY)
            : base(eventId, groupId)
        {
            m_sessionId = sessionId;
            m_x = x;
            m_y = y;
            m_centroidX = centroidX;
            m_centroidY = centroidY;
        }
    }
    public class PinchBeginEventArgs : GestureEventArgs
    {
        public PinchBeginEventArgs(string eventId, int groupId)
            : base(eventId, groupId) { }
    }
    public class PinchEventArgs : GestureEventArgs
    {
        private List<long> m_ids;
        private float m_scale = 0;
        private float m_scaleSpeed = 0;
        private float m_traslationX = 0, m_traslationY = 0;
        private float m_traslationXSpeed = 0, m_traslationYSpeed = 0;
        private float m_rotation = 0;
        private float m_rotationSpeed = 0;
        private float m_centroidX = 0, m_centroidY = 0;

        public List<long> Ids { get { return m_ids; } }
        public float Scaling { get { return m_scale; } }
        public float ScalingSpeed { get { return m_scaleSpeed; } }
        public float TraslationX { get { return m_traslationX; } }
        public float TraslationY { get { return m_traslationY; } }
        public float TraslationXSpeed { get { return m_traslationXSpeed; } }
        public float TraslationYSpeed { get { return m_traslationYSpeed; } }
        public float Rotation { get { return m_rotation; } }
        public float RotationSpeed { get { return m_rotationSpeed; } }
        public float CentroidX { get { return m_centroidX; } }
        public float CentroidY { get { return m_centroidY; } }

        public PinchEventArgs(string eventId, int groupId, float value, float speed,
            float centroidX, float centroidY, List<long> ids)
            : base(eventId, groupId)
        {
            if (eventId == "Scale")
            {
                m_scale = value;
                m_scaleSpeed = speed;
            } 
            else if (eventId == "Rotate")
            {
                m_rotation = value;
                m_rotationSpeed = speed;
            }
            else
            {
                throw new Exception("Attempting to call PinchEventArgs constructor for an event which is not Scale nor Rotate");
            }
            m_centroidX = centroidX;
            m_centroidY = centroidY;
            m_ids = ids;
        }
        public PinchEventArgs(string eventId, int groupId, float valueX, float valueY, float speedX, float speedY,
            float centroidX, float centroidY, List<long> ids)
            : base(eventId, groupId)
        {
            if (eventId == "Translate")
            {
                m_traslationX = valueX;
                m_traslationY = valueY;
                m_traslationXSpeed = speedX;
                m_traslationYSpeed = speedY;
            }
            else
            {
                throw new Exception("Attempting to call PinchEventArgs constructor for an event which is not Translate.");
            }
            m_centroidX = centroidX;
            m_centroidY = centroidY;
            m_ids = ids;
        }
        public PinchEventArgs(string eventId, int groupId, float scale, float scaleSpeed,
            float traslationX, float traslationY, float traslationXSpeed, float traslationYSpeed,
            float rotation, float rotationSpeed, float centroidX, float centroidY, List<long> ids)
            : base(eventId, groupId)
        {
            m_scale = scale;
            m_scaleSpeed = scaleSpeed;
            m_traslationX = traslationX;
            m_traslationY = traslationY;
            m_traslationXSpeed = traslationXSpeed;
            m_traslationYSpeed = traslationYSpeed;
            m_rotation = rotation;
            m_rotationSpeed = rotationSpeed;
            m_centroidX = centroidX;
            m_centroidY = centroidY;
            m_ids = ids;
        }
    }

    public class PinchingGRConfigurator : GRConfigurator
    {
        public static readonly PinchingGRConfigurator DEFAULT_CONFIGURATOR = new PinchingGRConfigurator();

        private const bool EXCLUSIVE_DEFAULT = false;

        public readonly bool IS_ONE_FINGER_SCALING_ENABLED;
        public const bool DEFAULT_IS_ONE_FINGER_SCALING_ENABLED = true;

        public readonly float SCALE_FACTOR; // no
        public const float DEFAULT_SCALE_FACTOR = 1;

        public readonly float TRASLATE_FACTOR; // no
        public const float DEFAULT_TRASLATE_FACTOR = 1;

        public PinchingGRConfigurator()
            : this(DEFAULT_SCALE_FACTOR, DEFAULT_TRASLATE_FACTOR) { }

        public PinchingGRConfigurator(bool exclusive, bool isOneFingerScalingEnabled)
            : this(exclusive, DEFAULT_SCALE_FACTOR, DEFAULT_TRASLATE_FACTOR, isOneFingerScalingEnabled) { }

        public PinchingGRConfigurator(float scaleFactor, float translateFactor)
            : base(EXCLUSIVE_DEFAULT)
        {
            SCALE_FACTOR = scaleFactor;
            TRASLATE_FACTOR = translateFactor;
            IS_ONE_FINGER_SCALING_ENABLED = DEFAULT_IS_ONE_FINGER_SCALING_ENABLED;
        }

        public PinchingGRConfigurator(bool exclusive, float scaleFactor, float translateFactor, bool isOneFingerScalingEnabled)
            : base(exclusive)
        {
            SCALE_FACTOR = scaleFactor;
            TRASLATE_FACTOR = translateFactor;
            IS_ONE_FINGER_SCALING_ENABLED = isOneFingerScalingEnabled;
        }

    }

    public class PinchingGR : LocalGestureRecognizer
    {
        // Configurator
        private PinchingGRConfigurator m_conf;

        // Last timestamp value
        private long m_lastTimeStamp;

        // List of currently living cursors ids
        private List<long> m_ids;

        // Sum of distances between centroid and cursors: reference for scaling
        private float m_centroidDistanceRef;
        
        // Centroid of living traces (last cursors): reference for traslation
        private float m_centroidXRef, m_centroidYRef;
        
        // Angle between two fingers: reference for rotation
        private float m_angleRef;
        
        // Traces on which the angle is calculated
        Trace m_traceA, m_traceB;

        // Scaling factors and speed
        private float m_scale;
        private float m_scaleSpeed;

        // Traslation factors and speed
        private float m_translateX, m_translateY;
        private float m_translateXSpeed, m_translateYSpeed;

        // Rotation factors and speed
        private float m_rotation;
        private float m_rotationSpeed;

        public PinchingGR(GRConfigurator configurator)
            : base(configurator)
        {
            if (!(configurator is PinchingGRConfigurator))
                Configurator = PinchingGRConfigurator.DEFAULT_CONFIGURATOR;

            m_conf = (PinchingGRConfigurator)Configurator;

            m_lastTimeStamp = 0;
            m_ids = new List<long>();

            m_centroidDistanceRef = 0;
            m_centroidXRef = m_centroidYRef = 0;
            m_angleRef = 0;
            m_traceA = m_traceB = null;

            m_scale = 0;
            m_scaleSpeed = 0;
            m_translateX = m_translateY = 0;
            m_translateXSpeed = m_translateYSpeed = 0;
            m_rotation = 0;
            m_rotationSpeed = 0;
        }

        public event GestureEventHandler Down;
        public event GestureEventHandler Up;
        public event GestureEventHandler Move;
        public event GestureEventHandler Scale;
        public event GestureEventHandler Rotate;
        public event GestureEventHandler Translate;
        public event GestureEventHandler Pinch;
        public event GestureEventHandler TranslateOrScaleBegin;
        public event GestureEventHandler RotateBegin;

        protected void OnDown(long sessionId, float x, float y)
        {
            AppendEvent(Down, new FingerEventArgs(
                "Down", Group.Id, sessionId, x, y, Group.CentroidLivingX, Group.CentroidLivingY));
        }
        protected void OnUp(long sessionId, float x, float y)
        {
            AppendEvent(Up, new FingerEventArgs(
                "Up", Group.Id, sessionId, x, y, Group.CentroidLivingX, Group.CentroidLivingY));
        }
        protected void OnMove(long sessionId, float x, float y)
        {
            AppendEvent(Move, new FingerEventArgs(
                "Move", Group.Id, sessionId, x, y, Group.CentroidLivingX, Group.CentroidLivingY));
        }
        protected void OnTranslateOrScaleBegin()
        {
            AppendEvent(TranslateOrScaleBegin, new PinchBeginEventArgs("TranslateOrScaleBegin", Group.Id));
        }
        protected void OnRotateBegin()
        {
            AppendEvent(RotateBegin, new PinchBeginEventArgs("RotateBegin", Group.Id));
        }
        protected void OnScale()
        {
            AppendEvent(Scale, new PinchEventArgs(
                "Scale", Group.Id, m_scale, m_scaleSpeed,
                Group.CentroidLivingX, Group.CentroidLivingY, m_ids)); 
        }
        protected void OnTranslate() 
        {
            AppendEvent(Translate, new PinchEventArgs(
                "Translate", Group.Id, m_translateX, m_translateY, m_translateXSpeed, m_translateYSpeed,
                Group.CentroidLivingX, Group.CentroidLivingY, m_ids));
        }
        protected void OnRotate() 
        {
            AppendEvent(Rotate, new PinchEventArgs(
                "Rotate", Group.Id, m_rotation, m_rotationSpeed,
                Group.CentroidLivingX, Group.CentroidLivingY, m_ids)); 
        }
        protected void OnPinch()
        {
            AppendEvent(Pinch, new PinchEventArgs(
                "Pinch", Group.Id, m_scale, m_scaleSpeed,
                m_translateX, m_translateY, m_translateXSpeed, m_translateYSpeed, m_rotation, m_rotationSpeed,
                Group.CentroidLivingX, Group.CentroidLivingY, m_ids));
        }

        public override void Process(List<Trace> traces)
        {
            // While there has been only one trace, if scaling with one finger is disabled then
            // return a 'recognizing' result
            if (Recognizing)
                if (m_conf.IS_ONE_FINGER_SCALING_ENABLED || Group.Traces.Count > 1)
                    GestureHasBeenRecognized();
                else
                    if (!Group.Alive)
                        GestureHasBeenRecognized(false);


            // If a cursor has been added or removed this flag is set to true
            // so that references for computing scaling, translation and rotation
            // are updated
            bool allFingersHaveBeenUpdated = true;

            Cursor cursor;

            // Update list of cursor ids.
            foreach (Trace trace in traces)
            {
                cursor = trace.Last;
                if (cursor.State == Cursor.States.ADDED)
                    m_ids.Add(cursor.SessionId);
                else if (cursor.State == Cursor.States.REMOVED)
                    m_ids.Remove(cursor.SessionId);
            }

            // Send basic finger events (down, up, move)
            // and set allFingersHaveBeenUpdated flag
            foreach (Trace trace in traces)
            {
                cursor = trace.Last;
                if (cursor.State == Cursor.States.UPDATED)
                    OnMove(cursor.SessionId, cursor.X, cursor.Y);
                else
                {
                    allFingersHaveBeenUpdated = false;
                    if (cursor.State == Cursor.States.ADDED)
                        OnDown(cursor.SessionId, cursor.X, cursor.Y);
                    else if (cursor.State == Cursor.States.REMOVED)
                        OnUp(cursor.SessionId, cursor.X, cursor.Y);
                }
            }
            
            // Delta of time between the two last Process()
            float dt = traces[0].Last.TimeStamp - m_lastTimeStamp;
            m_lastTimeStamp = traces[0].Last.TimeStamp;

            // If at least a finger has been either added or removed
            // then update references and exit
            if (!allFingersHaveBeenUpdated)
            {
                UpdateCentroidRef();
                UpdateDistanceFromCentroidRef();
                OnTranslateOrScaleBegin();

                // If there are two fingers then recalculate reference angle for rotation
                if (Group.NOfAliveTraces == 2)
                {
                    UpdateAngleRef();
                    OnRotateBegin();
                }
                
                return;
            }

            // Compute scaling
            if (Group.NOfAliveTraces > 1)
            {
                float distanceFromCentroid = GetMeanDistanceFromCentroid();
                float newScale = (distanceFromCentroid - m_centroidDistanceRef) * m_conf.SCALE_FACTOR;
                m_scaleSpeed = (newScale - m_scale) / dt * 1000;
                m_scale = newScale;
                OnScale(); 
            }

            // Compute translation
            float newTranslateX = (Group.CentroidLivingX - m_centroidXRef) * m_conf.TRASLATE_FACTOR;
            float newTranslateY = (Group.CentroidLivingY - m_centroidYRef) * m_conf.TRASLATE_FACTOR;
            m_translateXSpeed = (newTranslateX - m_translateX) / dt * 1000;
            m_translateYSpeed = (newTranslateY - m_translateY) / dt * 1000;
            m_translateX = newTranslateX;
            m_translateY = newTranslateY;
            OnTranslate();

            // If there are two fingers compute rotation
            if(Group.NOfAliveTraces == 2)
            {
                float newRotation = GetAngleBetween2Traces() - m_angleRef;
                m_rotationSpeed = (newRotation - m_rotation) / dt * 1000;
                m_rotation = newRotation;
                OnRotate();
            }

            OnPinch();

            return;
        }

        // set centroid reference to the current centroid (calculated on the living traces)
        private void UpdateCentroidRef()
        {
            m_centroidXRef = Group.CentroidLivingX;
            m_centroidYRef = Group.CentroidLivingY;
        }
        // set distance-to-centroid reference to the current distance to centroid
        private void UpdateDistanceFromCentroidRef()
        {
            m_centroidDistanceRef = GetMeanDistanceFromCentroid();
        }
        // calculate distance to current centroid
        private float GetMeanDistanceFromCentroid()
        {
            float dx = 0, dy = 0;
            foreach (Trace trace in Group.Traces)
            {
                if (trace.Alive)
                {
                    dx += Math.Abs(trace.Last.X - Group.CentroidLivingX);
                    dy += Math.Abs(trace.Last.Y - Group.CentroidLivingY);
                }
            }
            dx /= Group.NOfAliveTraces;
            dy /= Group.NOfAliveTraces;

            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
        // store the only two living traces on the relative variables and
        // update angle reference to the current angle defined by those traces
        private void UpdateAngleRef()
        {
            System.Diagnostics.Debug.Assert(Group.NOfAliveTraces == 2);
            m_traceA = m_traceB = null;
            foreach (Trace trace in Group.Traces)
            {
                if (trace.Alive)
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

            m_angleRef = GetAngleBetween2Traces();
        }
        // calculate the current angle between the two living traces
        private float GetAngleBetween2Traces()
        {
            float dx = m_traceB.Last.X - m_traceA.Last.X;
            float dy = m_traceB.Last.Y - m_traceA.Last.Y;
            return (float) Math.Atan2(dy, dx);
        }
    }
}