#pragma once
#ifndef CPPTUIOLISTENERWRAPPER_H
#define CPPTUIOLISTENERWRAPPER_H

#using <mscorlib.dll>
#include <iostream>
#include <vector>
#include "CPPTuioListener.h"
#include "TangibleGestureListenerWrapper.h"

using namespace std;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;
using namespace TUIO;
using namespace Grafiti;


ref class CPPTuioListenerWrapper : public TuioListener, public IGrafitiClientGUIManager
{
private:
	CPPTuioListener* m_cppTuioListener;

	vector<TangibleGestureListener*> *m_added, *m_updated, *m_removed;
	Dictionary<int, TangibleGestureListenerWrapper^>^ m_tangibleGestureListenerWrappers;

	List<ITangibleGestureListener^>^ m_hitTangibles;

public:
	CPPTuioListenerWrapper(CPPTuioListener* cppTuioListener);

	virtual void addTuioObject(TuioObject^ tuioObject);
	virtual void updateTuioObject(TuioObject^ tuioObject);
	virtual void removeTuioObject(TuioObject^ tuioObject);
	virtual void addTuioCursor(TuioCursor^ tuioCursor);
	virtual void updateTuioCursor(TuioCursor^ tuioCursor);
	virtual void removeTuioCursor(TuioCursor^ tuioCursor);
	virtual void refresh(__int64 timeStamp);

	
	virtual List<ITangibleGestureListener^>^ HitTestTangibles(float x, float y);
	virtual IGestureListener^ HitTest(float x, float y);
	virtual void PointToClient(IGestureListener^ target, float x, float y, [Out] float% cx, [Out] float% cy);
};

#endif