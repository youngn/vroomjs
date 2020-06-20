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
        assert(callbacks.delete_property != nullptr);
        assert(callbacks.enumerate_properties != nullptr);
        assert(callbacks.invoke != nullptr);
        assert(callbacks.valueof != nullptr);
        assert(callbacks.tostring != nullptr);
    }

    void Remove(int32_t context, int id) const {
        callbacks.remove(context, id);
    }
    JsValue GetPropertyValue(int32_t context, int32_t id, uint16_t* name) const {
        return callbacks.get_property_value(context, id, name);
    }
    JsValue SetPropertyValue(int32_t context, int32_t id, uint16_t* name, jsvalue value) const {
        return callbacks.set_property_value(context, id, name, value);
    }
    JsValue DeleteProperty(int32_t context, int32_t id, uint16_t* name) const {
        return callbacks.delete_property(context, id, name);
    }
    JsValue EnumerateProperties(int32_t context, int32_t id) const {
        return callbacks.enumerate_properties(context, id);
    }
    JsValue Invoke(int32_t context, int32_t id, int32_t argCount, jsvalue* args) const {
        return callbacks.invoke(context, id, argCount, args);
    }
    JsValue ValueOf(int32_t context, int32_t id) const {
        return callbacks.valueof(context, id);
    }
    JsValue ToString(int32_t context, int32_t id) const {
        return callbacks.tostring(context, id);
    }

private:
    jscallbacks callbacks;
};

