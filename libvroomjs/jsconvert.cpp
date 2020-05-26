#include <iostream>
#include "vroomjs.h"
#include <cassert>

using namespace v8;

JsConvert::JsConvert(Isolate* isolate) {
    assert(isolate != nullptr);
    this->isolate_ = isolate;
}

JsValue JsConvert::AnyFromV8(Local<Value> value, Local<Object> thisArg) const
{
    if (value->IsNull() || value->IsUndefined()) {
       return JsValue::ForNull();
    }

    if (value->IsBoolean()) {
        return JsValue::ForBoolean(value->BooleanValue(isolate_));
    }

    if (value->IsInt32()) {
        return JsValue::ForInt32(value->Int32Value(isolate_->GetCurrentContext()).FromMaybe(0));
    }

    if (value->IsUint32()) {
        return JsValue::ForUInt32(value->Uint32Value(isolate_->GetCurrentContext()).FromMaybe(0));
    }

    if (value->IsNumber()) {
        return JsValue::ForNumber(value->NumberValue(isolate_->GetCurrentContext()).FromMaybe(0.0));
    }

    if (value->IsString()) {
        //v = StringFromV8(Local<String>::Cast(value));
        auto s = Local<String>::Cast(value);
        auto len = s->Length();

        // todo: is this the best way to convert?
        uint16_t* str = new uint16_t[len + 1];
        s->Write(isolate_, str);
        return JsValue::ForString(len, str);
    }

    if (value->IsDate()) {
        return JsValue::ForDate(value->NumberValue(isolate_->GetCurrentContext()).FromMaybe(0.0));
    }

    if (value->IsArray()) {
        auto arr = Local<Array>::Cast(value);
        return JsValue::ForJsArray(new Persistent<Array>(isolate_, arr));
    }

    if (value->IsFunction()) {
        auto function = Local<Function>::Cast(value);
        return JsValue::ForJsFunction(new Persistent<Function>(isolate_, function));
    }

    if (value->IsObject()) {
        auto obj = Local<Object>::Cast(value);
        if (obj->InternalFieldCount() == 1) {
            //v = ManagedFromV8(obj);
        }
        else {
            return JsValue::ForJsObject(new Persistent<Object>(isolate_, obj));
        }
    }

    // Default to a generic error.
    return JsValue::ForUnknownError();
}

Local<Value> JsConvert::AnyToV8(JsValue value, int32_t contextId) const
{
    if (value.ValueType() == JSVALUE_TYPE_EMPTY) {
        return Local<Value>();
    }
    if (value.ValueType() == JSVALUE_TYPE_NULL) {
        return Null(isolate_);
    }
    if (value.ValueType() == JSVALUE_TYPE_BOOLEAN) {
        return Boolean::New(isolate_, value.BooleanValue());
    }
    if (value.ValueType() == JSVALUE_TYPE_INTEGER) {
        return Int32::New(isolate_, value.Int32Value());
    }
    if (value.ValueType() == JSVALUE_TYPE_NUMBER) {
        return Number::New(isolate_, value.NumberValue());
    }
    if (value.ValueType() == JSVALUE_TYPE_STRING) {
        return String::NewFromTwoByte(isolate_, value.StringValue()).ToLocalChecked();
    }
    if (value.ValueType() == JSVALUE_TYPE_DATE) {
        return Date::New(isolate_->GetCurrentContext(), value.DateValue()).ToLocalChecked();
    }
    if (value.ValueType() == JSVALUE_TYPE_JSOBJECT) {
        auto pObj = value.JsObjectValue();
        return Local<Object>::New(isolate_, *pObj);
    }
    if (value.ValueType() == JSVALUE_TYPE_JSARRAY) {
        auto pObj = value.JsArrayValue();
        return Local<Array>::New(isolate_, *pObj);
    }
    if (value.ValueType() == JSVALUE_TYPE_FUNCTION) {
        auto pObj = value.JsFunctionValue();
        return Local<Function>::New(isolate_, *pObj);
    }

    // Arrays are converted to JS native arrays.
    if (value.ValueType() == JSVALUE_TYPE_ARRAY) {
        //auto arr = Array::New(isolate_, v.length);
        //for(int i = 0; i < v.length; i++) {
        //    arr->Set(isolate_->GetCurrentContext(), i, AnyToV8(v.value.arr[i], contextId));
        //}
        //return arr;        
    }

    // This is an ID to a managed object that lives inside the JsContext keep-alive
    // cache. We just wrap it and the pointer to the engine inside an External. A
    // managed error is still a CLR object so it is wrapped exactly as a normal
    // managed object.
    if (value.ValueType() == JSVALUE_TYPE_MANAGED || value.ValueType() == JSVALUE_TYPE_MANAGED_ERROR) {

        //auto ref = new ManagedRef(this, contextId, v.length);
        //auto t = Local<FunctionTemplate>::New(isolate_, *managed_template_);

        //auto obj = t->InstanceTemplate()->NewInstance(isolate_->GetCurrentContext()).ToLocalChecked();
        //obj->SetInternalField(0, External::New(isolate_, ref));

        // todo: not sure if any of this is needed, revisit
        //Persistent<Object> persistent = Persistent<Object>::New(object);
        //persistent->SetInternalField(0, External::New(ref));
        //persistent.MakeWeak(NULL, managed_destroy);
        //return persistent;
        //return obj;
    }

    // todo: throw?
    return Null(isolate_);
}

JsValue JsConvert::ErrorFromV8(TryCatch& trycatch) const
{
    assert(trycatch.HasCaught()); // an exception has been caught

    HandleScope scope(isolate_);

    auto ctx = isolate_->GetCurrentContext();

    auto exception = trycatch.Exception();
    assert(!exception.IsEmpty());

    // Is this a managed exception?
    Local<Object> obj;
    if (exception->ToObject(ctx).ToLocal(&obj) && obj->InternalFieldCount() == 1) {
        auto wrap = Local<External>::Cast(obj->GetInternalField(0));
        auto ref = (ManagedRef*)wrap->Value();
        return JsValue::ForManagedError(ref->Id());
    }

    auto message = trycatch.Message();
    auto hasMessage = !message.IsEmpty();

    auto line = hasMessage ? message->GetLineNumber(isolate_->GetCurrentContext()).FromMaybe(0) : 0;
    auto column = hasMessage ? message->GetStartColumn() : 0;
    auto resource = hasMessage ? AnyFromV8(message->GetScriptResourceName()) : JsValue::ForEmpty();
    auto text = hasMessage ? AnyFromV8(message->Get()) : JsValue::ForEmpty();
    auto error = AnyFromV8(exception);

    // todo: is ctor name really useful? I think JS Error has a .name property, and that's more important - see MDN
    auto type = exception->IsObject() ? AnyFromV8(Local<Object>::Cast(exception)->GetConstructorName()) : JsValue::ForEmpty();

    auto errorInfo = new JsErrorInfo(text, line, column, resource, type, error);
    return JsValue::ForError(errorInfo);
}


