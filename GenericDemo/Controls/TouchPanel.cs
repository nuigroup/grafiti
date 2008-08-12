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
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using Grafiti;

namespace GenericDemo.TouchControls
{
    public class TouchPanel : Panel, IGestureListener
    {
        public TouchPanel() : this(-10) { } // TODO: define a list of default priority numbers

        public TouchPanel(int priorityNumber)
            : base()
        {
            GestureEventManager.Instance.SetPriorityNumber(typeof(BasicMultiFingerGR), priorityNumber);
            GestureEventManager.Instance.RegisterHandler(typeof(BasicMultiFingerGR), "Down", OnEvent);
            GestureEventManager.Instance.RegisterHandler(typeof(BasicMultiFingerGR), "Up", OnEvent);
            GestureEventManager.Instance.RegisterHandler(typeof(BasicMultiFingerGR), "Tap", OnEvent);
            GestureEventManager.Instance.RegisterHandler(typeof(BasicMultiFingerGR), "DoubleTap", OnEvent);
            GestureEventManager.Instance.RegisterHandler(typeof(BasicMultiFingerGR), "TripleTap", OnEvent);
            GestureEventManager.Instance.RegisterHandler(typeof(BasicMultiFingerGR), "Enter", OnEvent);
            GestureEventManager.Instance.RegisterHandler(typeof(BasicMultiFingerGR), "Leave", OnEvent);
            GestureEventManager.Instance.RegisterHandler(typeof(BasicMultiFingerGR), "Hover", OnEvent);
            GestureEventManager.Instance.RegisterHandler(typeof(BasicMultiFingerGR), "EndHover", OnEvent);
            GestureEventManager.Instance.RegisterHandler(typeof(BasicMultiFingerGR), "Move", OnEvent);
        }

        public event BasicMultiFingerEventHandler FingerDown;
        public event BasicMultiFingerEventHandler FingerUp;
        public event BasicMultiFingerEventHandler FingerTap;
        public event BasicMultiFingerEventHandler FingerDoubleTap;
        public event BasicMultiFingerEventHandler FingerTripleTap;
        public event BasicMultiFingerEventHandler FingerEnter;
        public event BasicMultiFingerEventHandler FingerLeave;
        public event BasicMultiFingerEventHandler FingerHover;
        public event BasicMultiFingerEventHandler FingerEndHover;
        public event BasicMultiFingerEventHandler FingerMove;



        public void OnEvent(object obj, GestureEventArgs args)
        {
            //Console.WriteLine("{0} on touch button {1}", args.EventId, this.Name);
            BasicMultiFingerEventArgs cArgs = (BasicMultiFingerEventArgs)args;

            float xf, yf;
            Surface.Instance.PointToClient(this, cArgs.X, cArgs.Y, out xf, out yf);
            int x = (int)xf;
            int y = (int)yf;

            if (GetChildAtPoint(new Point(x,y)) != null)
                return;

            BasicMultiFingerEventArgs cArgsRelative = new BasicMultiFingerEventArgs(
                cArgs.EventId, cArgs.GroupId, x, y, cArgs.NFingers, cArgs.DragStartingListener);

            MouseButtons button = MouseButtons.Left;

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
