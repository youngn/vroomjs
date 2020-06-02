#include "vroomjs.h"
#include "JsErrorInfo.h"
#include "JsContext.h"
#include "JsValue.h"

JsErrorInfo* JsErrorInfo::Capture(TryCatch& trycatch, JsContext* context)
{
    assert(trycatch.HasCaught()); // an exception has been caught

    auto isolate = context->Isolate();
    auto ctx = context->Ctx();

    HandleScope scope(isolate);

    auto exception = trycatch.Exception();
    assert(!exception.IsEmpty()); // should only be empty if no exception was caught

    auto message = trycatch.Message();
    auto hasMessage = !message.IsEmpty();

    auto line = hasMessage ? message->GetLineNumber(ctx).FromMaybe(0) : 0;
    auto column = hasMessage ? message->GetStartColumn() : 0;

    auto resource = hasMessage 
        ? JsErrorInfo::CreateString(message->GetScriptResourceName()->ToString(ctx).FromMaybe(Local<String>()), context)
        : nullptr;

    auto text = hasMessage
        ? JsErrorInfo::CreateString(message->Get(), context)
        : nullptr;

    // todo: is ctor name really useful? JS Error has a .name property, and that's more important - see MDN
    auto type = exception->IsObject()
        ? JsErrorInfo::CreateString(Local<Object>::Cast(exception)->GetConstructorName(), context)
        : nullptr;

    auto error = JsValue::ForValue(exception, context);

    auto info = new JsErrorInfo(text, line, column, resource, type, error);
    return info;
}

uint16_t* JsErrorInfo::CreateString(Local<String> value, JsContext* context)
{
    if (value.IsEmpty())
        return nullptr;

    auto len = value->Length();

    auto str = new uint16_t[len + 1];
    value->Write(context->Isolate(), str);
    return str;
}
