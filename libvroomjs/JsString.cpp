#include <cassert>

#include "vroomjs.h"
#include "JsString.h"


Persistent<String>* JsString::Create(Isolate* isolate, const uint16_t* value, int& len)
{
    assert(isolate != nullptr);
    assert(value != nullptr);

    Locker locker(isolate);
    HandleScope scope(isolate);

    Local<String> str;
    if (String::NewFromTwoByte(isolate, value).ToLocal(&str))
    {
        len = str->Length();
        return new Persistent<String>(isolate, str);
    }
    return nullptr;
}

int JsString::GetValue(Isolate* isolate, Persistent<String>* str, uint16_t* buffer)
{
    assert(isolate != nullptr);
    assert(str != nullptr);
    assert(buffer != nullptr);

    Locker locker(isolate);
    HandleScope scope(isolate);
    auto s = Local<String>::New(isolate, *str);
    return s->Write(isolate, buffer, 0, s->Length(), String::WriteOptions::NO_NULL_TERMINATION);
}
