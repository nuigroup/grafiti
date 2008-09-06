
#include "CPPTuioListenerWrapper.h"

CPPTuioListenerWrapper::CPPTuioListenerWrapper(CPPTuioListener *cppTuioListener)
{
	m_cppTuioListener = cppTuioListener;

	intptr_t added, updated, removed;
	m_cppTuioListener->getTangibleLists(added, updated, removed);
	m_added = (vector<TangibleGestureListener*>*)added;
	m_updated = (vector<TangibleGestureListener*>*)updated;
	m_removed = (vector<TangibleGestureListener*>*)removed;

	m_tangibleGestureListenerWrappers = gcnew Dictionary<int, TangibleGestureListenerWrapper^>;

	m_hitTangibles = gcnew List<ITangibleGestureListener^>();
}

void CPPTuioListenerWrapper::addTuioObject(TuioObject^ tuioObject)
{
	m_cppTuioListener->addTuioObject(
		tuioObject->getFiducialID(),
		tuioObject->getX() * Surface::SCREEN_RATIO,
		tuioObject->getY(),
		tuioObject->getAngle());

	TangibleGestureListenerWrapper^ tangibleGestureListenerWrapper;
	for (vector<TangibleGestureListener*>::iterator it = m_added->begin(); it != m_added->end(); ++ it)
	{
		tangibleGestureListenerWrapper = gcnew TangibleGestureListenerWrapper(*it);
		m_tangibleGestureListenerWrappers[(*it)->getSessionId()] = tangibleGestureListenerWrapper;
	}
}
void CPPTuioListenerWrapper::updateTuioObject(TuioObject^ tuioObject)
{
	m_cppTuioListener->updateTuioObject(
		tuioObject->getFiducialID(),
		tuioObject->getX() * Surface::SCREEN_RATIO,
		tuioObject->getY(),
		tuioObject->getAngle());
	
	TangibleGestureListenerWrapper^ tangibleGestureListenerWrapper;
	for (vector<TangibleGestureListener*>::iterator it = m_updated->begin(); it != m_updated->end(); ++ it)
	{
		tangibleGestureListenerWrapper = m_tangibleGestureListenerWrappers[(*it)->getSessionId()];
		//
	}
}
void CPPTuioListenerWrapper::removeTuioObject(TuioObject^ tuioObject)
{
	m_cppTuioListener->removeTuioObject(
		tuioObject->getFiducialID(),
		tuioObject->getX() * Surface::SCREEN_RATIO,
		tuioObject->getY(),
		tuioObject->getAngle());

	TangibleGestureListenerWrapper^ tangibleGestureListenerWrapper;
	for (vector<TangibleGestureListener*>::iterator it = m_removed->begin(); it != m_removed->end(); ++ it)
	{
		tangibleGestureListenerWrapper = m_tangibleGestureListenerWrappers[(*it)->getSessionId()];
		m_tangibleGestureListenerWrappers->Remove((*it)->getSessionId());
	}
}
void CPPTuioListenerWrapper::addTuioCursor(TuioCursor^ tuioCursor)
{

}
void CPPTuioListenerWrapper::updateTuioCursor(TuioCursor^ tuioCursor)
{

}
void CPPTuioListenerWrapper::removeTuioCursor(TuioCursor^ tuioCursor)
{

}
void CPPTuioListenerWrapper::refresh(__int64 timeStamp)
{
	m_cppTuioListener->refresh((long)timeStamp);
}



List<ITangibleGestureListener^>^ CPPTuioListenerWrapper::HitTestTangibles(float x, float y)
{
	m_hitTangibles->Clear();

	Dictionary<int, TangibleGestureListenerWrapper^>::ValueCollection::Enumerator e = 
		m_tangibleGestureListenerWrappers->Values->GetEnumerator();
	while(e.MoveNext())
	{
		if(e.Current->GetSquareDistance(x, y) < e.Current->GetSquareTargetAreaRadius())
			m_hitTangibles->Add(e.Current);
	}
	return m_hitTangibles;
}

IGestureListener ^CPPTuioListenerWrapper::HitTest(float x, float y)
{
	return nullptr;
}

void CPPTuioListenerWrapper::PointToClient(Grafiti::IGestureListener^ target, float x, float y, [Out] float% cx, [Out] float% cy)
{
	cx = x;
	cy = y;
}