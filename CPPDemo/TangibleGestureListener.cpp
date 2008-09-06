
#include "TangibleGestureListener.h"

TangibleGestureListener::TangibleGestureListener(int sessionId, float x, float y, float angle)
{
	m_sessionId = sessionId;
	state = added;

	m_x = x;
	m_y = y;
	m_angle = angle;

	m_targetAreaRadius = 0.5f; // default
}
int TangibleGestureListener::getSessionId()
{
	return m_sessionId;
}
void TangibleGestureListener::update(float x, float y, float angle)
{
	state = updated;

	m_x = x;
	m_y = y;
	m_angle = angle;
}
void TangibleGestureListener::remove(float x, float y, float angle)
{
	state = removed;

	m_x = x;
	m_y = y;
	m_angle = angle;
}
float TangibleGestureListener::getSquareTargetAreaRadius()
{
	return m_targetAreaRadius * m_targetAreaRadius;
}
float TangibleGestureListener::getSquareDistance(float x, float y)
{
	float dx = m_x - x;
	float dy = m_y - y;
	return dx * dx + dy * dy;
}
