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

using System.Collections.Generic;
using Grafiti;

namespace Grafiti
{
    /// <summary>
    /// Provides functionality to manage the comunication between Grafiti and the client
    /// about the targets (GUI controls and tangible objects).
    /// All functions use the Grafiti's coordinate system, that is
    /// [0,1] for height, [0,Grafiti.Surface.SCREEN_RATIO] for width.
    /// </summary>
    public interface IGrafitiClientGUIManager
    {
        /// <summary>
        /// Returns an enumerable object containing the gesture listeners associated to
        /// tangible objects which interaction area includes the given Grafiti point.
        /// </summary>
        /// <param name="x">X coordinate of the Grafiti point.</param>
        /// <param name="y">Y coordinate of the Grafiti point.</param>
        /// <returns>The tangibles listeners.</returns>
        IEnumerable<ITuioObjectGestureListener> HitTestTangibles(float x, float y);

        /// <summary>
        /// Returns the GUI component at the specified Grafiti point.
        /// </summary>
        /// <param name="x">X coordinate of the Grafiti point.</param>
        /// <param name="y">Y coordinate of the Grafiti point.</param>
        /// <returns>The GUI control.</returns>
        IGestureListener HitTest(float x, float y);

        /// <summary>
        /// Converts a point from Grafiti's coordinates into client's coordinates, relative
        /// to the given GUI component.
        /// </summary>
        /// <param name="target">The GUI component.</param>
        /// <param name="x">X coordinate of the Grafiti point.</param>
        /// <param name="y">X coordinate of the Grafiti point.</param>
        /// <returns>The point in client's coordinate relative to the component.</returns>
        System.Drawing.Point PointToClient(IGestureListener target, float x, float y);
    }
}