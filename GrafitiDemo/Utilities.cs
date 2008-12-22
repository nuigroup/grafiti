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
//using System.Collections.Generic;
//using System.Text;
using Tao.FreeGlut;
using Tao.OpenGl;
//using Grafiti;

namespace GrafitiDemo
{
    public class MyColor
    {
        public double R, G, B;
        public MyColor(double r, double g, double b)
        {
            R = r;
            G = g;
            B = b;
        }

    }

    public static class Utilities
    {
        public static void DrawPlainSquare(float size)
        {
            Gl.glBegin(Gl.GL_QUADS);
            MakeRectangle(size, size);
            Gl.glEnd();
        }

        public static void DrawPlainRectangle(float width, float height)
        {
            Gl.glBegin(Gl.GL_QUADS);
            MakeRectangle(width, height);
            Gl.glEnd();
        }

        private static void MakeRectangle(float w, float h)
        {
            float w2 = w / 2;
            float h2 = h / 2;
            Gl.glVertex2f(-w2, h2);
            Gl.glVertex2f(w2, h2);
            Gl.glVertex2f(w2, -h2);
            Gl.glVertex2f(-w2, -h2);
        }

        public static void DrawEmptyCircle(float radius)
        {
            Gl.glBegin(Gl.GL_LINES);
            MakeCircle(radius);
            Gl.glEnd();
        }

        public static void DrawPlainCircle(float radius)
        {
            Gl.glBegin(Gl.GL_POLYGON);
            MakeCircle(radius);
            Gl.glEnd();
        }

        private static void MakeCircle(float radius)
        {
            float x, y;
            x = (float)radius * (float)Math.Cos(359 * Math.PI / 180.0f);
            y = (float)radius * (float)Math.Sin(359 * Math.PI / 180.0f);
            for (int j = 0; j < 360; j++)
            {
                Gl.glVertex2f(x, y);
                x = (float)radius * (float)Math.Cos(j * Math.PI / 180.0f);
                y = (float)radius * (float)Math.Sin(j * Math.PI / 180.0f);
                Gl.glVertex2f(x, y);
            }
        }
    }
}
