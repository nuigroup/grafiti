#pragma once
#ifndef TANGIBLEGESTURELISTENER_H
#define TANGIBLEGESTURELISTENER_H

class TangibleGestureListener
{
private:
	int m_sessionId;
	int state;
	float m_x, m_y, m_angle;

	float m_targetAreaRadius;

	static const int added = 0;
	static const int updated = 1;
	static const int removed = 2;

public:
	TangibleGestureListener(int sessionId, float x, float y, float angle);

	int getSessionId();

	void update(float x, float y, float angle);
	void remove(float x, float y, float angle);

	float getSquareTargetAreaRadius();

	float getSquareDistance(float x, float y);
};

#endif