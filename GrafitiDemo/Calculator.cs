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
using Grafiti.GestureRecognizers;
using System.Timers;


namespace GrafitiDemo
{
    public class Calculator : IGestureListener, IPinchable
    {
        private List<CalcButton> m_buttons = new List<CalcButton>(18);
        private LCD m_lcd;

        private List<int> m_display = new List<int>(12);

        private float m_x = 0.5f, m_y = 0.5f; // position in tuio coordinates
        private float m_theta = 0; // orientation
        private float m_size = 0.5f; // size (width = height)
        private float m_gridSize = 33f; // grid coordinates (33 x 33 units, a button is usually 6x6)

        private Stack<float> m_operands = new Stack<float>();
        private StringBuilder m_digits = new StringBuilder();
        private bool m_point = false;
        private bool m_canTypeNewOperand = true;
        private bool m_error = false;

        public static readonly string DECIMAL_SEPARATOR = 
            System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;


        public float X { get { return m_x; } set { m_x = value; } }
        public float Y { get { return m_y; } set { m_y = value; } }
        public float Size { get { return m_size; } set { m_size = value; } }

        public enum Items
        {
            Digit0 = 0,
            Digit1,
            Digit2,
            Digit3,
            Digit4,
            Digit5,
            Digit6,
            Digit7,
            Digit8,
            Digit9,
            Point,
            OpPlus,
            OpMinus,
            OpMult,
            OpDiv,
            Enter,
            Back,
            ClearAll
        }


        public Calculator()
        {
            m_buttons.Add(new CalcButton(this, Items.Digit0, 1, 26, 12, 6)); // topleft, size
            m_buttons.Add(new CalcButton(this, Items.Digit1, 1, 20, 6, 6));
            m_buttons.Add(new CalcButton(this, Items.Digit2, 7, 20, 6, 6));
            m_buttons.Add(new CalcButton(this, Items.Digit3, 13, 20, 6, 6));
            m_buttons.Add(new CalcButton(this, Items.Digit4, 1, 14, 6, 6));
            m_buttons.Add(new CalcButton(this, Items.Digit5, 7, 14, 6, 6));
            m_buttons.Add(new CalcButton(this, Items.Digit6, 13, 14, 6, 6));
            m_buttons.Add(new CalcButton(this, Items.Digit7, 1, 8, 6, 6));
            m_buttons.Add(new CalcButton(this, Items.Digit8, 7, 8, 6, 6));
            m_buttons.Add(new CalcButton(this, Items.Digit9, 13, 8, 6, 6));
            m_buttons.Add(new CalcButton(this, Items.Point, 13, 26, 6, 6));
            m_buttons.Add(new CalcButton(this, Items.Enter, 20, 26, 12, 6));
            m_buttons.Add(new CalcButton(this, Items.OpPlus, 20, 20, 6, 6));
            m_buttons.Add(new CalcButton(this, Items.OpMinus, 20, 14, 6, 6));
            m_buttons.Add(new CalcButton(this, Items.OpMult, 26, 20, 6, 6));
            m_buttons.Add(new CalcButton(this, Items.OpDiv, 26, 14, 6, 6));
            m_buttons.Add(new CalcButton(this, Items.Back, 20, 8, 6, 6));
            m_buttons.Add(new CalcButton(this, Items.ClearAll, 26, 8, 6, 6));

            m_lcd = new LCD(1, 32, 9);

            PinchingGRConfiguration m_pinchingConf = new PinchingGRConfiguration(true, this, true);
            GestureEventManager.RegisterHandler(typeof(PinchingGR), m_pinchingConf, "Pinch", OnPinch);
        }

        #region IPinchable Members
        public void GetPinchReference(out float x, out float y, out float size, out float rotation)
        {
            x = m_x;
            y = m_y;
            size = m_size;
            rotation = (float)((double)m_theta / 180.0d * Math.PI);
        }
        #endregion

        public void OnPinch(object obj, GestureEventArgs args)
        {
            PinchEventArgs cArgs = (PinchEventArgs)args;
            m_x = cArgs.X;
            m_y = cArgs.Y;
            m_size = cArgs.Size;
            m_theta = (float)((double)cArgs.Rotation / Math.PI * 180.0d);
        }


        public void GetTargetAt(float x, float y,
            out IGestureListener zGestureListener,
            out object zControl)
        {
            zGestureListener = null;
            zControl = null;

            // to center
            x = x - m_x;// +m_size / 2;
            y = y - m_y;// +m_size / 2;

            // rotate
            double radiants = Math.PI / 180 * m_theta;
            //Console.WriteLine(m_theta/180 + ", " + radiants/Math.PI);

            float tempX, tempY;

            tempX = (float)((double)x * Math.Cos(radiants) - (double)y * Math.Sin(radiants));
            tempY = (float)((double)x * Math.Sin(radiants) + (double)y * Math.Cos(radiants));

            x = tempX;
            y = tempY;

            // to top left
            x += m_size / 2;
            y += m_size / 2;

            if (x < 0 || x > m_size || y < 0 || y > m_size) // outside calculator
                return;

            zGestureListener = this;
            zControl = this;

            // to grid coordinates
            x = x * m_gridSize / m_size;
            y = y * m_gridSize / m_size;

            foreach (CalcButton button in m_buttons)
            {
                if (button.InsideTargetArea(x - button.X, y - button.Y))
                {
                    zGestureListener = button;
                    break;
                }                
            }
        }

        public void Draw()
        {
            Gl.glPushMatrix();
                Gl.glTranslatef(m_x, m_y, 0); // translate to center
                Gl.glRotated(-m_theta, 0, 0, 1);
                Gl.glScalef(m_size / m_gridSize, m_size / m_gridSize, 1); // use grid coord

                Gl.glColor3d(0, 0, 0); // black
                Utilities.DrawPlainSquare(m_gridSize); // black background
                
                Gl.glTranslatef(-m_gridSize / 2, -m_gridSize / 2, 0); // translate to topleft
                foreach (CalcButton button in m_buttons)
                    button.Draw();
                
                m_lcd.Draw();
                
                Gl.glColor3f(1, 1, 1); // white
                Gl.glBegin(Gl.GL_LINE_LOOP); // white borders
                Gl.glVertex2f(0, 0);
                Gl.glVertex2f(0, m_gridSize);
                Gl.glVertex2f(m_gridSize, m_gridSize);
                Gl.glVertex2f(m_gridSize, 0);
                Gl.glEnd();
                Gl.glBegin(Gl.GL_LINES); // lcd / buttons separator
                Gl.glVertex2f(0, 7);
                Gl.glVertex2f(m_gridSize, 7);
                Gl.glEnd();

            Gl.glPopMatrix();
        }


        internal void ProcessItem(Items item, bool hover)
        {
            if (m_error && item != Items.ClearAll)
                return;

            if ((int)item <= 9)
                ProcessDigit((int)item);
            else
                ProcessNonDigit(item, hover);
        }

        internal void ProcessNonDigit(Items item, bool hover)
        {
            //float n = 99999999f * 99999999999f;
            //m_digits.Append(n.ToString());
            //m_lcd.UpdateText(m_digits.ToString());
            //return;

            switch (item)
            {
                case Items.Enter:
                    if (m_digits.ToString().Length != 0)
                    {
                        PushOperand();
                        m_point = false;
                        m_canTypeNewOperand = true;
                    }
                    break;

                case Items.Back:
                    int sLength = m_digits.ToString().Length;
                    if (sLength > 0)
                    {
                        if (hover) // back all
                        {
                            m_digits = new StringBuilder();
                            m_point = false;
                        }
                        else
                        {
                            if (m_digits.ToString()[sLength - 1] == ',')
                                m_point = false;
                            m_digits.Remove(sLength - 1, 1);
                        }
                        m_lcd.UpdateText(m_digits.ToString());
                    }
                    break;

                case Items.ClearAll:
                    Reset();
                    break;

                case Items.Point:
                    if (!m_point)
                    {
                        m_point = true;
                        if (m_digits.Length == 0)
                            m_digits.Append("0");
                        m_digits.Append(DECIMAL_SEPARATOR);
                        m_lcd.UpdateText(m_digits.ToString());
                    }
                    break;

                case Items.OpPlus: 
                case Items.OpMinus:
                case Items.OpMult:
                case Items.OpDiv:
                    if (m_operands.Count < 2)
                        return;

                    float b = m_operands.Pop();
                    float a = m_operands.Pop();
                    float result;
                    switch (item)
                    {
                        case Items.OpPlus:
                            result = a + b;
                            break;
                        case Items.OpMinus:
                            result = a - b;
                            break;
                        case Items.OpMult:
                            result = a * b;
                            break;
                        case Items.OpDiv:
                            result = a / b;
                            break;
                        default:
                            throw new Exception("?");
                    }
                    m_operands.Push(result);
                    if (float.IsInfinity(result) || float.IsNaN(result))
                    {
                        Reset();
                        m_lcd.UpdateText("E");
                        m_error = true;
                    }
                    else
                        m_lcd.UpdateText(result.ToString());
                    m_digits = new StringBuilder();
                    m_point = false;
                    m_canTypeNewOperand = true;
                    break;

                default:
                    throw new Exception("Invalid operation");
            }
        }

        internal void ProcessDigit(int digit)
        {
            if (m_digits.ToString() == "0" && !m_point)
                return;

            if (!m_lcd.IsFull || m_canTypeNewOperand)
            {
                m_digits.Append(digit);
                m_lcd.UpdateText(m_digits.ToString());
                m_canTypeNewOperand = false;
            }
        }

        private void PushOperand()
        {
            m_operands.Push(float.Parse(m_digits.ToString()));
            m_digits = new StringBuilder();
        }
        
        private void Reset()
        {
            m_digits = new StringBuilder();
            m_operands.Clear();
            m_point = false;
            m_lcd.UpdateText("");
            m_error = false;
            m_canTypeNewOperand = true;
        }

    }

    public class CalcButton : TouchButton
    {
        Calculator m_calculator;
        Calculator.Items m_item;

        private Timer m_blinkingTimer = new Timer();
        private float m_colorValue = 1f, m_colorIncrement = 0.3f;
        private double m_blinkingTimerInterval = 50;

        public CalcButton(Calculator calculator, Calculator.Items item, float x, float y, float w, float h)
            : base(x, y, w, h) // position is topleft
        {
            m_calculator = calculator;
            m_item = item;
            Tap += new Grafiti.GestureRecognizers.BasicMultiFingerEventHandler(CalcButton_Tap);
            Hover += new BasicMultiFingerEventHandler(CalcButton_Hover);

            m_blinkingTimer.Elapsed += new ElapsedEventHandler(m_blinkingTimer_Elapsed);
            m_blinkingTimer.Interval = m_blinkingTimerInterval;
        }

        private void CalcButton_Tap(object obj, BasicMultiFingerEventArgs args)
        {
            for (int i = 0; i < args.NFingers; i++)
                m_calculator.ProcessItem(m_item, Hovering);

            StartBlinking();
        }

        void CalcButton_Hover(object obj, BasicMultiFingerEventArgs args)
        {
            
        }

        private void StartBlinking()
        {
            m_colorValue = 0.3f;
            m_blinkingTimer.Enabled = true;
        }

        void m_blinkingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (m_colorValue < 1)
            {
                m_colorValue += m_colorIncrement;
            }
            if (m_colorValue >= 1)
            {
                m_colorValue = 1;
                m_blinkingTimer.Enabled = false;
            }
        }

        public void Draw()
        {
            Gl.glPushMatrix();
            Gl.glTranslatef(X + Width / 2, Y + Height / 2, 0); // go to button's center

            if (IsPressed)
            {
                if(Hovering && m_item == Calculator.Items.Back)
                    Gl.glColor3d(0.45, 0.45, 0.65); // dark grey
                else
                    Gl.glColor3d(0.5, 0.5, 0.7); // grey

            }
            else
                Gl.glColor3d(m_colorValue, m_colorValue, 1); // white

            // Background area
            if (Width == Height)
                Utilities.DrawPlainCircle(Width / 2);
            else
            {
                if (Width > Height)
                {
                    Utilities.DrawPlainRectangle(Width - Height, Height);
                    Gl.glTranslatef(- (Width - Height)/ 2, 0, 0);
                    Utilities.DrawPlainCircle(Height / 2);
                    Gl.glTranslatef(Width - Height, 0, 0);
                    Utilities.DrawPlainCircle(Height / 2);
                    Gl.glTranslatef(- (Width - Height) / 2, 0, 0);
                }
                else
                {
                    Utilities.DrawPlainRectangle(Width, Height - Width);
                    Gl.glTranslatef(0, - (Height - Width) / 2, 0);
                    Utilities.DrawPlainCircle(Width / 2);
                    Gl.glTranslatef(0, Height - Width, 0);
                    Utilities.DrawPlainCircle(Width / 2);
                    Gl.glTranslatef(0, - (Height - Width) / 2, 0);
                }
            }

            Gl.glColor3d(0, 0, 0); // black
            Gl.glLineWidth(3);
            if ((int)m_item <= 9)
                LCD.DrawDigit((int)m_item);
            else
                switch (m_item)
                { 
                    case Calculator.Items.Enter:
                        Gl.glBegin(Gl.GL_LINE_STRIP);
                        Gl.glVertex2f(0, -1);
                        Gl.glVertex2f(2, -1);
                        Gl.glVertex2f(2, 1);
                        Gl.glVertex2f(-2, 1);
                        Gl.glEnd();
                        Gl.glBegin(Gl.GL_LINE_STRIP);
                        Gl.glVertex2f(-1, 0);
                        Gl.glVertex2f(-2, 1);
                        Gl.glVertex2f(-1, 2);
                        Gl.glEnd();
                        break;
                    case Calculator.Items.OpDiv:
                        Gl.glBegin(Gl.GL_LINES);
                        Gl.glVertex2f(-1, 1);
                        Gl.glVertex2f(1, -1);
                        Gl.glEnd();
                        break;
                    case Calculator.Items.OpMinus:
                        Gl.glBegin(Gl.GL_LINES);
                        Gl.glVertex2f(-1, 0);
                        Gl.glVertex2f(1, 0);
                        Gl.glEnd();
                        break;
                    case Calculator.Items.OpMult:
                        Gl.glBegin(Gl.GL_LINES);
                        Gl.glVertex2f(-1, -1);
                        Gl.glVertex2f(1, 1);
                        Gl.glEnd();
                        Gl.glBegin(Gl.GL_LINES);
                        Gl.glVertex2f(-1, 1);
                        Gl.glVertex2f(1, -1);
                        Gl.glEnd();
                        break;
                    case Calculator.Items.OpPlus:
                        Gl.glBegin(Gl.GL_LINES);
                        Gl.glVertex2f(0, 1);
                        Gl.glVertex2f(0, -1);
                        Gl.glEnd();
                        Gl.glBegin(Gl.GL_LINES);
                        Gl.glVertex2f(-1, 0);
                        Gl.glVertex2f(1, 0);
                        Gl.glEnd();
                        break;
                    case Calculator.Items.Point:
                        Gl.glBegin(Gl.GL_LINES);
                        Gl.glVertex2f(-0.25f, 0);
                        Gl.glVertex2f(0.25f, 0);
                        Gl.glEnd();
                        break;
                    case Calculator.Items.Back:
                        Gl.glBegin(Gl.GL_LINES);
                        Gl.glVertex2f(1, 0);
                        Gl.glVertex2f(-1, 0);
                        Gl.glEnd();
                        Gl.glBegin(Gl.GL_LINE_STRIP);
                        Gl.glVertex2f(0, -1);
                        Gl.glVertex2f(-1, 0);
                        Gl.glVertex2f(0, 1);
                        Gl.glEnd();
                        break;
                    case Calculator.Items.ClearAll:
                        Gl.glBegin(Gl.GL_LINE_STRIP);
                        Gl.glVertex2f(1, -1);
                        Gl.glVertex2f(-1, -1);
                        Gl.glVertex2f(-1, 1);
                        Gl.glVertex2f(1, 1);
                        Gl.glEnd();
                        break;
                }
            Gl.glPopMatrix();
        }

        internal bool InsideTargetArea(float x, float y)
        {
            float dx, dy;

            if (Width == Height)
            {
                dx = x - Width / 2;
                dy = y - Height / 2;
                return dx * dx + dy * dy <= Width * Width / 4;
            }
            else if (Width > Height)
            {
                if (x >= Height / 2 && x <= Width - Height / 2 && y >= 0 && y <= Height)
                    return true;

                dx = x - Height / 2;
                dy = y - Height / 2;
                if (dx * dx + dy * dy <= Height * Height / 4)
                    return true;

                dx = x - Width + Height / 2;
                return dx * dx + dy * dy <= Height * Height / 4;
            }
            else
            {
                if (x >= 0 && x <= Width && y >= Height / 2 && y <= Width - Height / 2)
                    return true;

                dx = x - Width / 2;
                dy = y - Width / 2;
                if (dx * dx + dy * dy <= Width * Width / 4)
                    return true;

                dy = y + Height - Width / 2;
                return dx * dx + dy * dy <= Height * Height / 4;
            }
        }
    }

    public class LCD
    {
        private int m_size;
        string m_text = "";
        bool m_overflow = false;
        float m_x0, m_y0;
        float m_dx = 3;
        public LCD(float x, float y, int nDigits)
        {
            m_x0 = 2;
            m_y0 = 3.5f;
            m_size = nDigits;
        }
        public bool IsFull {
            get
            {
                if (m_text.Contains(Calculator.DECIMAL_SEPARATOR))
                    return m_size <= m_text.Length - 1;
                else
                    return m_size <= m_text.Length;            
            }        
        }
        public bool UpdateText(string valueAsString)
        {
            m_text = valueAsString;
            return true;
        }
        public void Draw()
        {
            if (m_overflow)
                DisplayError();
            else
                DisplayDigits();
        }
        private void DisplayDigits()
        {
            Gl.glPushMatrix();
            Gl.glColor3f(1, 1, 1);
            float x = m_x0, y = m_y0;
            float stdOffset = m_dx * (m_size - m_text.Length + 1.5f) + 2;
            if (!(m_text.Contains(Calculator.DECIMAL_SEPARATOR)))
                stdOffset -= m_dx;
            if (stdOffset > 0)
                x += stdOffset;
            Gl.glTranslatef(x, y, 0);

            int digit;
            foreach (char c in m_text)
            {
                if (int.TryParse(c.ToString(), out digit))
                {
                    DrawDigit(digit);
                    Gl.glTranslatef(m_dx, 0, 0);
                }
                else
                {
                    switch (c)
                    {
                        case 'E':
                            DrawE();
                            Gl.glTranslatef(m_dx, 0, 0);
                            break;
                        case '+':
                            DrawPlus();
                            Gl.glTranslatef(m_dx / 4, 0, 0);
                            break;
                        case '-':
                            DrawMinus();
                            Gl.glTranslatef(m_dx / 4, 0, 0);
                            break;
                        case '.':
                        case ',':
                            DrawPoint();
                            Gl.glTranslatef(m_dx / 4, 0, 0);
                            break;
                        default:
                            DrawE();
                            break;
                    }
                }
            }

            Gl.glPopMatrix();
        }
        private void DrawMinus()
        {
            Gl.glBegin(Gl.GL_LINES);
            Gl.glVertex2f(-2f, 0);
            Gl.glVertex2f(-1f, 0);
            Gl.glEnd();
        }
        private void DrawPlus()
        {
            Gl.glBegin(Gl.GL_LINES);
            Gl.glVertex2f(-1.5f, 1);
            Gl.glVertex2f(-1.5f, -1);
            Gl.glEnd();
            Gl.glBegin(Gl.GL_LINES);
            Gl.glVertex2f(-2f, 0);
            Gl.glVertex2f(-1f, 0);
            Gl.glEnd();
        }
        private void DrawE()
        {
            Gl.glBegin(Gl.GL_LINE_STRIP);
            Gl.glVertex2f(1, -2);
            Gl.glVertex2f(-1, -2);
            Gl.glVertex2f(-1, 2);
            Gl.glVertex2f(1, 2);
            Gl.glEnd();
            Gl.glBegin(Gl.GL_LINES);
            Gl.glVertex2f(-1, 0);
            Gl.glVertex2f(0, 0);
            Gl.glEnd();
        }
        private void DrawPoint()
        {
            Gl.glBegin(Gl.GL_LINES);
            Gl.glVertex2f(-1, 2);
            Gl.glVertex2f(-1.3f, 2);
            Gl.glEnd();
        }
        public static void DisplayError()
        {
            Gl.glPushMatrix();
            Gl.glBegin(Gl.GL_LINES);
            Gl.glVertex2f(-1, 0);
            Gl.glVertex2f(10, 0);
            Gl.glEnd();
            Gl.glPopMatrix();
        }
        public static void DrawDigit(int i)
        {
            if (i != 1 && i != 4 && i != 6)
                DrawSegment(-1, -2, 1, -2);
            if (i != 1 && i != 2 && i != 3 && i != 7)
                DrawSegment(-1, 0, -1, -2);
            if (i != 5 && i != 6)
                DrawSegment(1, -0, 1, -2);
            if (i != 0 && i != 1 && i != 7)
                DrawSegment(-1, 0, 1, 0);
            if (i == 0 || i == 2 || i == 6 || i == 8)
                DrawSegment(-1, 0, -1, 2);
            if (i != 2)
                DrawSegment(1, 0, 1, 2);
            if (i != 1 && i != 4 && i != 7 && i != 9)
                DrawSegment(-1, 2, 1, 2);
        }
        public static void DrawSegment(float x1, float y1, float x2, float y2)
        {
            Gl.glBegin(Gl.GL_LINES);
            Gl.glVertex2f(x1, y1);
            Gl.glVertex2f(x2, y2);
            Gl.glEnd();
        }
    }
}
