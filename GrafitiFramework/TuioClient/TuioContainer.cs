/*
    TUIO C# Library - part of the reacTIVision project
    http://reactivision.sourceforge.net/
 
    Copyright (c) 2005-2008 Martin Kaltenbrunner <mkalten@iua.upf.edu>
    
    Modified by Alessandro De Nardi on 23rd June 2008 (see relative regions)
    <alessandro.denardi@gmail.com>

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/

using System;
using System.Collections.Generic;


namespace TUIO
{

public abstract class TuioContainer:TuioPoint {

	protected long session_id;
	protected float x_speed, y_speed;
	protected float motion_speed,motion_accel;
	protected List<TuioPoint> path;
	protected int state;
	protected long timestamp;
		
	public static readonly int UNDEFINED = -1;
	public static readonly int ADDED = 0;
	public static readonly int UPDATED = 1;
	public static readonly int REMOVED = 2;

    #region	Added by Alessandro De Nardi on 23rd June 2008
    public float XSpeed { get { return x_speed; } }
    public float YSpeed { get { return y_speed; } }
    public float MotionSpeed { get { return motion_speed; } }
    public float MotionAcceleration { get { return motion_accel; } }
    public int State { get { return state; } }
    public long SessionId { get { return session_id; } }
    public long TimeStamp { get { return timestamp; } }
	#endregion

		
	public TuioContainer (long s_id, float xpos, float ypos):base(xpos,ypos) {
		this.session_id = s_id;
		this.x_speed = 0.0f;
		this.y_speed = 0.0f;
		this.motion_speed = 0.0f;
		this.motion_accel = 0.0f;
		path = new List<TuioPoint>();
		path.Add(new TuioPoint(xpos,ypos));
		state = ADDED;
		timestamp = UNDEFINED;
	}
	
	public TuioContainer (TuioContainer c):base(c) {
		this.session_id = getSessionID();
		this.x_speed = 0.0f;
		this.y_speed = 0.0f;
		this.motion_speed = 0.0f;
		this.motion_accel = 0.0f;
		path = new List<TuioPoint>();
		path.Add(new TuioPoint(c.getX(),c.getY()));
		state = ADDED;
		timestamp = UNDEFINED;
	}
		

	public void update (float xpos, float ypos,float xspeed,float yspeed,float maccel) {
		base.update(xpos,ypos);

		this.x_speed = xspeed;
		this.y_speed = yspeed;
		this.motion_speed = (float)Math.Sqrt(xspeed*xspeed+yspeed*yspeed);
		this.motion_accel = maccel;
		path.Add(new TuioPoint(xpos,ypos));
		state = UPDATED;
		timestamp = UNDEFINED;
	}

	
	public void update (TuioContainer c) {
		base.update(c.getX(),c.getY());

		this.x_speed = c.getXSpeed();
		this.y_speed = c.getYSpeed();
		this.motion_speed = (float)Math.Sqrt(x_speed*x_speed+y_speed*y_speed);
		this.motion_accel = c.getMotionAccel();
		path.Add(new TuioPoint(xpos,ypos));
		state = UPDATED;
		timestamp = UNDEFINED;
	}
		
	public long getSessionID() {
		return session_id;
	}
			
	public float getXSpeed() {
		return x_speed;
	}
	
	public float getYSpeed() {
		return y_speed;
	}
			

	public List<TuioPoint> getPath() {
		return path;
	}
	
	public float getMotionSpeed() {
		return motion_speed;
	}
	
	public float getMotionAccel() {
		return motion_accel;
	}
	
	public int getState() {
			return state;
	}
		
	public void remove() {
			path.Clear();
			state = REMOVED;
	}
		
	public long getUpdateTime() {
			return timestamp;
	}

	
	public void setUpdateTime(long timestamp) {
			this.timestamp = timestamp;
	}
		


    }
}
