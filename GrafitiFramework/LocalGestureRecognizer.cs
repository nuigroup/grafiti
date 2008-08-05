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
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using TUIO;

namespace Grafiti
{
    public abstract class LocalGestureRecognizer : GestureRecognizer
    {
        private ITuioObjectGestureListener m_target;
        private float m_squareDistanceFromTarget;

        public ITuioObjectGestureListener Target { get { return m_target; } internal set { m_target = value; } }
        internal float SquareDistanceFromTarget { get { return m_squareDistanceFromTarget; } set { m_squareDistanceFromTarget = value; } }

        public LocalGestureRecognizer(GRConfigurator configurator) : base(configurator) { }

        internal override sealed void AddHandler(string ev, GestureEventHandler handler)
        {
            GetEventInfo(ev).AddEventHandler(this, handler);
        }

        internal void OnTargetRemoved1() 
        {
            OnTargetRemoved();
        }

        /// <summary>
        /// Called if the group removes (goes out from) the lgr's target, so that Process() 
        /// won't be called anymore This can happen if LGR_TARGET_LIST is set to 
        /// INTERSECTION_TARGET_LIST in the global settings.
        /// Override this to handle the finalization of the lgr (e.g. send events like 'HoverEnded').
        /// </summary>
        protected void OnTargetRemoved() { }
    } 
}