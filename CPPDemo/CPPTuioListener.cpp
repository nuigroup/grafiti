#include "CPPTuioListener.h"

CPPTuioListener::CPPTuioListener()
{

}

CPPTuioListener::~CPPTuioListener()
{

}

void CPPTuioListener::getTangibleLists(intptr_t &added, intptr_t &updated, intptr_t &removed)
{
	added = (intptr_t) &m_added;
	updated = (intptr_t) &m_updated;
	removed = (intptr_t) &m_removed;
}

void CPPTuioListener::addTuioObject(int sessionId, float x, float y, float angle)
{
	TangibleGestureListener *tangible = new TangibleGestureListener(sessionId, x, y, angle);
	m_tangibleGestureListeners[sessionId] = tangible;
	m_added.push_back(tangible);
}

void CPPTuioListener::updateTuioObject(int sessionId, float x, float y, float angle)
{
	TangibleGestureListener *tangible = m_tangibleGestureListeners[sessionId];
	tangible->update(x, y, angle);
	m_updated.push_back(tangible);
}

void CPPTuioListener::removeTuioObject(int sessionId, float x, float y, float angle)
{
	TangibleGestureListener *tangible = m_tangibleGestureListeners[sessionId];
	tangible->remove(x, y, angle);
	m_removed.push_back(tangible);
}

void CPPTuioListener::refresh(long timestamp)
{	
	m_added.clear();
	m_updated.clear();
	m_removed.clear();
}