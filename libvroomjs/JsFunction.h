#pragma once

#include "vroomjs.h"
#include "JsObject.h"

class JsFunction : public JsObject
{
public:
    JsFunction(Local<Function> func, JsContext* context)
        : JsObject(func, context)
    {
    }

    Local<Function> ToLocal();

    JsValue Invoke(JsValue receiver, int argCount, JsValue* args);
};

