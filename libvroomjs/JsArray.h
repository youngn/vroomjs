#pragma once

#include "vroomjs.h"
#include "JsObject.h"

class JsArray : public JsObject
{
public:
    JsArray(Local<Array> arr, JsContext* context)
        : JsObject(arr, context)
    {
    }

    Local<Array> ToLocal();
};

