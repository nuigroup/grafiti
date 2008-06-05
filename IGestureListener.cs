/*
	grafiti library

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



namespace grafiti

{

    public interface IGestureListener

    {

        bool Contains(float x, float y);

        void GetPosition(out float x, out float y);



        // this will be removed (listeners have to register event handlers

        // through Surface, so this won't be needed anymore 

        List<GestureRecognizer> GetLocalGRs(); 

    }

}