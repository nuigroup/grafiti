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
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Grafiti;
//using System.Runtime.InteropServices;

namespace Grafiti.TouchControls
{

    public class TouchButton : Button, IGestureListener
    {
        //[DllImport("user32.dll")]
        //private extern static IntPtr SetFocus(IntPtr hwnd);

        public TouchButton() : this(-10) { } // TODO: define a list of default priority numbers

        public TouchButton(int priorityNumber)
            : base()
        {
            GestureEventManager.Instance.SetPriorityNumber(typeof(BasicMultiFingerGR), priorityNumber);
            GestureEventManager.Instance.RegisterHandler(typeof(BasicMultiFingerGR), "Down", OnEvent);
            GestureEventManager.Instance.RegisterHandler(typeof(BasicMultiFingerGR), "Up", OnEvent);
            GestureEventManager.Instance.RegisterHandler(typeof(BasicMultiFingerGR), "Tap", OnEvent);
            GestureEventManager.Instance.RegisterHandler(typeof(BasicMultiFingerGR), "Enter", OnEvent);
            GestureEventManager.Instance.RegisterHandler(typeof(BasicMultiFingerGR), "Leave", OnEvent);
            GestureEventManager.Instance.RegisterHandler(typeof(BasicMultiFingerGR), "Hover", OnEvent);
            GestureEventManager.Instance.RegisterHandler(typeof(BasicMultiFingerGR), "EndHover", OnEvent);
            GestureEventManager.Instance.RegisterHandler(typeof(BasicMultiFingerGR), "Move", OnEvent);
        }

        public event GestureEventHandler FingerDown;
        public event GestureEventHandler FingerUp;
        public event GestureEventHandler FingerTap;
        public event GestureEventHandler FingerEnter;
        public event GestureEventHandler FingerLeave;
        public event GestureEventHandler FingerHover;
        public event GestureEventHandler FingerEndHover;
        public event GestureEventHandler FingerMove;


        public void OnEvent(object obj, GestureEventArgs args)
        {
            //Console.WriteLine("{0} on touch button {1}", args.EventId, this.Name);
            BasicMultiFingerEventArgs cArgs = (BasicMultiFingerEventArgs)args;

            MouseButtons button = MouseButtons.Left;

            Point p = Surface.Instance.PointToClient(this, cArgs.X, cArgs.Y);
            int x = p.X;
            int y = p.Y;

            BasicMultiFingerEventArgs cArgsRelative = new BasicMultiFingerEventArgs(
                cArgs.EventId, cArgs.GroupId, x, y, cArgs.NFingers, cArgs.DragStartingListener);

            switch (cArgsRelative.EventId)
            {
                case "Down":
                    if (cArgsRelative.NFingers == 1) // only the first finger down is considered
                    {
                        this.OnMouseDown(new MouseEventArgs(button, 0, x, y, 0));
                        if (FingerDown != null)
                            FingerDown(this, cArgsRelative);
                    }
                    break;
                case "Up":
                    if (cArgsRelative.NFingers == 0) // only the last finger up is considered
                    {
                        this.OnMouseUp(new MouseEventArgs(button, 0, x, y, 0));
                        if (FingerUp != null)
                            FingerUp(this, cArgsRelative);

                        if (cArgsRelative.DragStartingListener == this)
                        {
                            //this.OnMouseClick(new MouseEventArgs(button, 1, x, y, 0));
                            PerformClick();
                            if (FingerTap != null)
                                FingerTap(this, cArgsRelative);
                        }
                    }
                    break;
                case "Move":
                    this.OnDragOver(new DragEventArgs(null, 0, x, y, DragDropEffects.None, DragDropEffects.None));
                    if (FingerMove != null)
                        FingerMove(this, cArgsRelative);
                    break;
                case "Hover":
                    this.OnMouseHover(new EventArgs());
                    if (FingerHover != null)
                        FingerHover(this, cArgsRelative);
                    break;
                case "EndHover":
                    if (FingerEndHover != null)
                        FingerEndHover(this, cArgsRelative);
                    break;
                case "Enter":
                    this.OnMouseEnter(new EventArgs());
                    this.OnDragEnter(new DragEventArgs(null, 0, x, y, DragDropEffects.None, DragDropEffects.None));
                    if (FingerEnter != null)
                        FingerEnter(this, cArgsRelative);
                    if (cArgsRelative.DragStartingListener == this)
                    {
                        Focus(); // why it doesn't work?
                        //SetFocus(Handle); 
                        OnGotFocus(new EventArgs());
                    }
                    break;
                case "Leave":
                    this.OnDragLeave(new EventArgs());
                    this.OnMouseLeave(new EventArgs());
                    if (FingerLeave != null)
                        FingerLeave(this, cArgsRelative);
                    if (cArgsRelative.DragStartingListener == this)
                        OnLostFocus(new EventArgs());
                    break;
            }
        }
    }
}
