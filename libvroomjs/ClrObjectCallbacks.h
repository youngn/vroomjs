#pragma once

#include <cassert>
#include "vroomjs.h"
#include "JsValue.h"

class ClrObjectCallbacks
{
public:
    ClrObjectCallbacks(jscallbacks callbacks)
        :callbacks(callbacks)
    {
        assert(callbacks.remove != nullptr);
        assert(callbacks.get_property_value != nullptr);
        assert(callbacks.set_property_value != nullptr);
        assert(callbacks.valueof != nullptr);
        assert(callbacks.invoke != nullptr);
        assert(callbacks.delete_property != nullptr);
        assert(callbacks.enumerate_properties != nullptr);
    }

    JsValue GetPropertyValue(int32_t context, int32_t id, uint16_t* name) const {
        return callbacks.get_property_value(context, id, name);
    }
    JsValue SetPropertyValue(int32_t context, int32_t id, uint16_t* name, jsvalue value) const {
        return callbacks.set_property_value(context, id, name, value);
    }
    JsValue ValueOf(int32_t context, int32_t id) const {
        return callbacks.valueof(context, id);
    }
    JsValue Invoke(int32_t context, int32_t id, int32_t argCount, jsvalue* args) const {
        return callbacks.invoke(context, id, argCount, args);
    }
    JsValue DeleteProperty(int32_t context, int32_t id, uint16_t* name) const {
        return callbacks.delete_property(context, id, name);
    }
    JsValue EnumerateProperties(int32_t context, int32_t id) const {
        return callbacks.enumerate_properties(context, id);
    }
    void Remove(int32_t context, int id) const {
        callbacks.remove(context, id);
    }

private:
    jscallbacks callbacks;
};

