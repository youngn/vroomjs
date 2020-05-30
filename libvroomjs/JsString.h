#pragma once

#include "vroomjs.h"

class JsString
{
public:
	static Persistent<String>* Create(Isolate* isolate, const uint16_t* value, int& len);
	static int GetValue(Isolate* isolate, Persistent<String>* str, uint16_t* buffer);
};
