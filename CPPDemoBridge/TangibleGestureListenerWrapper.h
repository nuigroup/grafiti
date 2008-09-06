#pragma once
#ifndef TANGIBLEGESTURELISTENERWRAPPER_H
#define TANGIBLEGESTURELISTENERWRAPPER_H

#using <mscorlib.dll>
#include <iostream>
#include "TangibleGestureListener.h"

using namespace Grafiti;
using namespace Grafiti::GestureRecognizers;
using namespace std;

namespace GrafitiW
{
	public ref class TangibleGestureListenerWrapper : public ITangibleGestureListener
	{
	private:
		TangibleGestureListener* m_tangibleGestureListener;

	public:
		TangibleGestureListenerWrapper(TangibleGestureListener* tangibleGestureListener);

		float GetSquareTargetAreaRadius();
		virtual float GetSquareDistance(float x, float y);

		void OnGestureEvent(Object^ obj, GestureEventArgs^ args);
	};
}

using namespace GrafitiW;

#endif