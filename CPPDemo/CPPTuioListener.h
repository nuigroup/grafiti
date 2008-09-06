#pragma once
#ifndef CPPTUIOLISTENER_H
#define CPPTUIOLISTENER_H


#include <iostream>
#include <vector>
#include <hash_map>
#include "TangibleGestureListener.h"
#include "GestureListener.h"

using namespace std;
using namespace stdext;


class CPPTuioListener// : public TuioListener // The interface has been simplified
{
private:
	hash_map<int, TangibleGestureListener*> m_tangibleGestureListeners;
	vector<TangibleGestureListener*> m_added, m_updated, m_removed;

public:
	CPPTuioListener();
	~CPPTuioListener();
	
	void getTangibleLists(intptr_t &added, intptr_t &updated, intptr_t &removed);
	
	void addTuioObject(int sessionId, float x, float y, float angle);
	void updateTuioObject(int sessionId, float x, float y, float angle);
	void removeTuioObject(int sessionId, float x, float y, float angle);
	//void addTuioCursor(int sessionId, float x, float y);
	//void updateTuioCursor(int sessionId, float x, float y);
	//void removeTuioCursor(int sessionId, float x, float y);
	void refresh(long timestamp);
};

#endif
