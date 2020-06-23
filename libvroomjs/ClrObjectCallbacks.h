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
        // The 'remove' callback is not optional, because we need to inform the CLR side
        // when a V8 object is GC'd.
        assert(callbacks.remove != nullptr);
    }

    void Remove(int32_t context, int id) const {
        callbacks.remove(context, id);
    }
    JsValue GetPropertyValue(int32_t context, int32_t id, uint16_t* name) const {
        assert(callbacks.get_property_value != nullptr);
        return callbacks.get_property_value(context, id, name);
    }
    JsValue SetPropertyValue(int32_t context, int32_t id, uint16_t* name, jsvalue value) const {
        assert(callbacks.set_property_value != nullptr);
        return callbacks.set_property_value(context, id, name, value);
    }
    JsValue DeleteProperty(int32_t context, int32_t id, uint16_t* name) const {
        assert(callbacks.delete_property != nullptr);
        return callbacks.delete_property(context, id, name);
    }
    JsValue EnumerateProperties(int32_t context, int32_t id) const {
        assert(callbacks.enumerate_properties != nullptr);
        return callbacks.enumerate_properties(context, id);
    }
    JsValue Invoke(int32_t context, int32_t id, int32_t argCount, jsvalue* args) const {
        assert(callbacks.invoke != nullptr);
        return callbacks.invoke(context, id, argCount, args);
    }
    JsValue ValueOf(int32_t context, int32_t id) const {
        assert(callbacks.valueof != nullptr);
        return callbacks.valueof(context, id);
    }
    JsValue ToString(int32_t context, int32_t id) const {
        assert(callbacks.tostring != nullptr);
        return callbacks.tostring(context, id);
    }

private:
    jscallbacks callbacks;
};

