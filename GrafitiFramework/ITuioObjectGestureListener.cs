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

namespace Grafiti
{
    /// <summary>
    /// Provides functionality for Tuio objects (or related objects) for listening to gesture events.
    /// </summary>
    public interface ITuioObjectGestureListener : IGestureListener
    {
        // Returns the square distance to a conventional point (e.g. the center or the point
        // on the border closest to the given coordinates), coherently with the client application.
        float GetSquareDistance(float x, float y);
    }
}