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
    /// <summary>
    /// An LGR recognizes gestures produced close by the tangible which class implements
    /// ITangibleGestureListener. The instance related to the tangible is also the only listener
    /// of the events that the LGR can raise. An instance of this class will be created for each
    /// group that is born close to such tangible, if there is at least one registered listener.
    /// </summary>
    public abstract class LocalGestureRecognizer : GestureRecognizer
    {
        #region Private or internal members
        private IGestureListener m_target;
        private float m_squareDistanceFromTarget;
        internal float SquareDistanceFromTarget { get { return m_squareDistanceFromTarget; } set { m_squareDistanceFromTarget = value; } }  
        #endregion

        #region Public properties
        public IGestureListener Target { get { return m_target; } internal set { m_target = value; } }
        #endregion        

        #region Constructor
        public LocalGestureRecognizer(GRConfiguration configuration) : base(configuration) { }
        #endregion

        #region Private or internal methods
        internal override sealed void AddHandler(string ev, GestureEventHandler handler)
        {
            GetEventInfo(ev).AddEventHandler(this, handler);
        }
        #endregion
    } 
}