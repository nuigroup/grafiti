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
    /// [0,1] for height, [0,Settings.SCREEN_RATIO] for width.
    /// </summary>
    public interface IGrafitiClientGUIManager
    {
        /// <summary>
        /// Determines the visual elements which target area includes a specific point.
        /// The function distinguishes (by delivering different output values): Z-ordered 
        /// control gesture listeners, Z-ordered controls that are no gesture listeners and
        /// tangible gesture listeners. Only one of these values should refer to an existing
        /// visual element (or a non-empty set of elements in case of the tangibles).
        /// </summary>
        /// <param name="x">X coordinate of the point.</param>
        /// <param name="y">Y coordinate of the point.</param>
        /// <param name="zGestureListener">The Z-ordered control listener at the specified point.</param>
        /// <param name="zControl">The Z-ordered control that is not a listener at the specified point.</param>
        /// <param name="tangibleListeners">The listeners associated to the tangibles at the specified point.</param>
        void GetVisualsAt(float x, float y,
            out IGestureListener zListener,
            out object zControl,
            out List<ITangibleGestureListener> tangibleListeners);


        /// <summary>
        /// Takes a GUI control and a point specified in Grafiti-coordinate-system and 
        /// returns the point relative to the GUI component in client's coordinates.
        /// </summary>
        /// <param name="target">The GUI component.</param>
        /// <param name="x">X coordinate of the point to convert.</param>
        /// <param name="y">Y coordinate of the point to convert.</param>
        /// <param name="cx">X coordinate of the converted point.</param>
        /// <param name="cy">Y coordinate of the converted point.</param>
        void PointToClient(IGestureListener target, float x, float y, out float cx, out float cy);
    }
}