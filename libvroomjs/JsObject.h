#pragma once

#include "vroomjs.h"

class JsValue;
class JsContext;

class JsObject
{
public:
    JsObject(Persistent<Object>* obj, JsContext* context)
        :obj_(obj), context_(context)
    {
        assert(obj != nullptr);
        assert(context != nullptr);
    };

    JsValue GetPropertyNames();
    JsValue GetPropertyValue(const uint16_t* name);
    JsValue GetPropertyValue(const uint32_t index);
    JsValue SetPropertyValue(const uint16_t* name, JsValue value);
    JsValue SetPropertyValue(const uint32_t index, JsValue value);

protected:
    JsContext* Context() { return context_; }

private:
    Persistent<Object>* obj_;
    JsContext* context_;
};

