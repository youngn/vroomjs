#pragma once

#include "vroomjs.h"

class JsContext;
class JsErrorInfo;

class JsValue
{
public:
    static JsValue ForValue(Local<Value> value, JsContext* context);
    static JsValue ForError(TryCatch& trycatch, JsContext* context);

    Local<Value> Extract(JsContext* context) {
        auto result = GetValue(context);
        Dispose();
        return result;
    }
    static JsValue ForEmpty() {
        return JsValue(JSVALUE_TYPE_EMPTY, 0, 0);
    }
    static JsValue ForNull() {
        return JsValue(JSVALUE_TYPE_NULL, 0, 0);
    }
    static JsValue ForBoolean(bool value) {
        return JsValue(JSVALUE_TYPE_BOOLEAN, 0, (int32_t)value);
    }
    static JsValue ForInt32(int32_t value) {
        return JsValue(JSVALUE_TYPE_INTEGER, 0, value);
    }
    static JsValue ForUInt32(uint32_t value) {
        return JsValue(JSVALUE_TYPE_INDEX, 0, (int64_t)value);
    }
    static JsValue ForNumber(double value) {
        return JsValue(JSVALUE_TYPE_NUMBER, 0, value);
    }
    static JsValue ForString(int32_t length, uint16_t* value) {
        assert(value != nullptr);
        return JsValue(JSVALUE_TYPE_STRING, length, value);
    }
    static JsValue ForDate(double value) {
        return JsValue(JSVALUE_TYPE_DATE, 0, value);
    }
    static JsValue ForJsString(Local<String> value, JsContext* context);
    static JsValue ForJsString(Persistent<String>* value, int32_t length) {
        assert(value != nullptr);
        return JsValue(JSVALUE_TYPE_JSSTRING, length, (void*)value);
    }
    static JsValue ForJsArray(Local<Array> value, JsContext* context);
    static JsValue ForJsFunction(Local<Function> value, JsContext* context);
    static JsValue ForJsObject(Local<Object> value, JsContext* context);
    static JsValue ForError(JsErrorInfo* value) {
        assert(value != nullptr);
        return JsValue(JSVALUE_TYPE_JSERROR, 0, (void*)value);
    }
    static JsValue ForHostObject(int32_t id) {
        return JsValue(JSVALUE_TYPE_HOSTOBJECT, 0, id);
    }

    int32_t ValueType() const {
        return v.type;
    }

    bool BooleanValue() const {
        assert(v.type == JSVALUE_TYPE_BOOLEAN);
        return v.value.i32 != 0;
    }
    int32_t Int32Value() const {
        assert(v.type == JSVALUE_TYPE_INTEGER);
        return v.value.i32;
    }
    uint32_t UInt32Value() const {
        assert(v.type == JSVALUE_TYPE_INDEX);
        return (uint32_t)v.value.i64;
    }
    double NumberValue() const {
        assert(v.type == JSVALUE_TYPE_NUMBER);
        return v.value.num;
    }
    uint16_t* StringValue() const {
        assert(v.type == JSVALUE_TYPE_STRING);
        return v.value.str;
    }
    double DateValue() const {
        assert(v.type == JSVALUE_TYPE_DATE);
        return v.value.num;
    }
    Persistent<Array>* JsArrayValue() const {
        assert(v.type == JSVALUE_TYPE_JSARRAY);
        return (Persistent<Array>*)v.value.ptr;
    }
    Persistent<Function>* JsFunctionValue() const {
        assert(v.type == JSVALUE_TYPE_JSFUNCTION);
        return (Persistent<Function>*)v.value.ptr;
    }
    Persistent<Object>* JsObjectValue() const {
        assert(v.type == JSVALUE_TYPE_JSOBJECT);
        return (Persistent<Object>*)v.value.ptr;
    }
    Persistent<Object>* HostErrorValue() const {
        assert(v.type == JSVALUE_TYPE_HOSTERROR);
        return (Persistent<Object>*)v.value.ptr;
    }
    Persistent<String>* JsStringValue() const {
        assert(v.type == JSVALUE_TYPE_JSSTRING);
        return (Persistent<String>*)v.value.ptr;
    }
    int32_t HostObjectIdValue() const {
        assert(v.type == JSVALUE_TYPE_HOSTOBJECT || v.type == JSVALUE_TYPE_HOSTERROR);
        return v.value.i32;
    }
    int32_t HostObjectTemplateIdValue() const {
        assert(v.type == JSVALUE_TYPE_HOSTOBJECT || v.type == JSVALUE_TYPE_HOSTERROR);
        return v.templateId;
    }

    JsValue(const jsvalue& value) {
        v = value;
    }

    operator jsvalue() const {
        return v;
    }

private:
    friend class JsErrorInfo;
    friend class JsValueDisposer;

    void Dispose();

    Local<Value> GetValue(JsContext* context);

    inline JsValue(int32_t type, int32_t length, int32_t i32) {
        v.type = type;
        v.length = length;
        v.value.i32 = i32;
    }
    inline JsValue(int32_t type, int32_t length, int64_t i64) {
        v.type = type;
        v.length = length;
        v.value.i64 = i64;
    }
    inline JsValue(int32_t type, int32_t length, double num) {
        v.type = type;
        v.length = length;
        v.value.num = num;
    }
    inline JsValue(int32_t type, int32_t length, void* ptr) {
        v.type = type;
        v.length = length;
        v.value.ptr = ptr;
    }
    inline JsValue(int32_t type, int32_t length, uint16_t* str) {
        v.type = type;
        v.length = length;
        v.value.str = str;
    }

    jsvalue v;
};

class JsValueDisposer {
public:
    static void DisposeValue(JsValue value) {
        value.Dispose();
    }
};

