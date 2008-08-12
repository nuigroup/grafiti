using System;
using System.Collections.Generic;
using System.Text;

namespace Grafiti
{
    /// <summary>
    /// Represents a point in the surface where a finger is or was placed. It's directely
    /// related to the TuioCursor class.
    /// </summary>
    public class Cursor
    {
        public enum States
        {
            ADDED,
            UPDATED,
            REMOVED
        }

        protected int m_sessionId;
        protected float m_x, m_y;
        protected float m_xSpeed = 0, m_ySpeed = 0;
        protected float m_motionSpeed = 0, m_motionAccel = 0;
        protected int m_timestamp = -1;
        protected States m_state;
        
        public float X { get { return m_x; } }
        public float Y { get { return m_y; } }
        public float XSpeed { get { return m_xSpeed; } set { m_xSpeed = value; } }
        public float YSpeed { get { return m_ySpeed; } set { m_ySpeed = value; } }
        public float MotionSpeed { get { return m_motionSpeed; } set { m_motionSpeed = value; } }
        public float MotionAcceleration { get { return m_motionAccel; } set { m_motionAccel = value; } }
        public int SessionId { get { return m_sessionId; } }
        public int TimeStamp { get { return m_timestamp; } internal set { m_timestamp = value; } }
        public States State { get { return m_state; } internal set { m_state = value; } }

        public Cursor(int sessionId, float x, float y, States state)
        {
            m_x = x;
            m_y = y;
            m_sessionId = sessionId;
            m_xSpeed = 0;
            m_ySpeed = 0;
            m_state = state;
        }
        public Cursor(int sessionId, float x, float y, float xSpeed, float ySpeed, float motionSpeed, float motionAccel, int timestamp, States state)
        {
            m_x = x;
            m_y = y;
            m_sessionId = sessionId;
            m_xSpeed = xSpeed;
            m_ySpeed = ySpeed;
            m_motionSpeed = motionSpeed;
            m_motionAccel = motionAccel;
            m_timestamp = timestamp;
            m_state = state;
        }

        public float SquareDistance(Cursor cursor)
        {
            float dx = m_x - cursor.X;
            float dy = m_y - cursor.Y;
            return dx * dx + dy * dy;
        }

        public float SquareDistance(float x, float y)
        {
            float dx = m_x - x;
            float dy = m_y - y;
            return dx * dx + dy * dy;
        }
    }
}