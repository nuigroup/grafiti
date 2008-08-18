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
using Grafiti.GestureRecognizers;

namespace GenericDemo.TouchControls
{
    public class TouchRadioButton : RadioButton, IGestureListener
    {
        public TouchRadioButton() : this(-10) { } // TODO: define a list of default priority numbers

        public TouchRadioButton(int priorityNumber)
            : base()
        {
            AutoCheck = true;
            GestureEventManager.SetPriorityNumber(typeof(BasicMultiFingerGR), priorityNumber);
            GestureEventManager.RegisterHandler(typeof(BasicMultiFingerGR), "Enter", OnEvent);
        }

        public void OnEvent(object obj, GestureEventArgs args)
        {
            BasicMultiFingerEventArgs cArgs = (BasicMultiFingerEventArgs)args;
            IGestureListener dragFromControl = cArgs.DragStartingListener;
            // have to start dragging the finger from a TouchRadioButton of the same group
            if (dragFromControl is TouchRadioButton &&
                ((TouchRadioButton)dragFromControl).GetContainerControl() == GetContainerControl())
            {
                if (args.EventId == "Enter")
                {
                    Checked = true;
                }
            }
        }
    }
}
