#pragma once

#include "vroomjs.h"
#include "JsObject.h"

class JsFunction : JsObject
{
public:
    JsFunction(Persistent<Function>* func, JsContext* context)
        : JsObject((Persistent<Object>*)func, context), func_(func)
    {
    }

    JsValue Invoke(JsValue receiver, int argCount, JsValue* args);

private:
    Persistent<Function>* func_;
};

