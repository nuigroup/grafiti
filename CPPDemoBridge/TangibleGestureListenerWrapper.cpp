
#include "TangibleGestureListenerWrapper.h"


TangibleGestureListenerWrapper::TangibleGestureListenerWrapper(TangibleGestureListener* tangibleGestureListener)
{
	m_tangibleGestureListener = tangibleGestureListener;

	GestureEventManager::RegisterHandler(BasicMultiFingerGR::typeid, "Down",
		gcnew GestureEventHandler(this, &GrafitiW::TangibleGestureListenerWrapper::OnGestureEvent));
	GestureEventManager::RegisterHandler(BasicMultiFingerGR::typeid, "Up",
		gcnew GestureEventHandler(this, &GrafitiW::TangibleGestureListenerWrapper::OnGestureEvent));
	GestureEventManager::RegisterHandler(BasicMultiFingerGR::typeid, "Tap",
		gcnew GestureEventHandler(this, &GrafitiW::TangibleGestureListenerWrapper::OnGestureEvent));
	GestureEventManager::RegisterHandler(BasicMultiFingerGR::typeid, "DoubleTap",
		gcnew GestureEventHandler(this, &GrafitiW::TangibleGestureListenerWrapper::OnGestureEvent));
	GestureEventManager::RegisterHandler(BasicMultiFingerGR::typeid, "Enter",
		gcnew GestureEventHandler(this, &GrafitiW::TangibleGestureListenerWrapper::OnGestureEvent));
	GestureEventManager::RegisterHandler(BasicMultiFingerGR::typeid, "Leave",
		gcnew GestureEventHandler(this, &GrafitiW::TangibleGestureListenerWrapper::OnGestureEvent));
	GestureEventManager::RegisterHandler(BasicMultiFingerGR::typeid, "Hover",
		gcnew GestureEventHandler(this, &GrafitiW::TangibleGestureListenerWrapper::OnGestureEvent));
	GestureEventManager::RegisterHandler(BasicMultiFingerGR::typeid, "EndHover",
		gcnew GestureEventHandler(this, &GrafitiW::TangibleGestureListenerWrapper::OnGestureEvent));
}

float TangibleGestureListenerWrapper::GetSquareTargetAreaRadius()
{
	return m_tangibleGestureListener->getSquareTargetAreaRadius();
}

float TangibleGestureListenerWrapper::GetSquareDistance(float x, float y)
{
	//System::Console::WriteLine(m_tangibleGestureListener->getSessionId() + ": sqdist: " + m_tangibleGestureListener->getSquareDistance(x, y));
	return m_tangibleGestureListener->getSquareDistance(x, y);
}

void TangibleGestureListenerWrapper::OnGestureEvent(Object^ obj, GestureEventArgs^ args)
{
	System::Console::WriteLine(m_tangibleGestureListener->getSessionId() + ": " + args->EventId);
}