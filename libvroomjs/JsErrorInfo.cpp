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

    auto error = JsValue::ForValue(exception, context);
    auto text = JsErrorInfo::CreateString(exception->ToString(ctx).FromMaybe(Local<String>()), context);

    auto message = trycatch.Message();
    auto hasMessage = !message.IsEmpty();

    auto line = hasMessage ? message->GetLineNumber(ctx).FromMaybe(0) : 0;
    auto column = hasMessage ? message->GetStartColumn() : 0;

    auto resource = hasMessage 
        ? JsErrorInfo::CreateString(message->GetScriptResourceName()->ToString(ctx).FromMaybe(Local<String>()), context)
        : nullptr;

    auto description = hasMessage
        ? JsErrorInfo::CreateString(message->Get(), context)
        : nullptr;

    // todo: is ctor name really useful? JS Error has a .name property, and that's more important - see MDN
    auto type = exception->IsObject()
        ? JsErrorInfo::CreateString(Local<Object>::Cast(exception)->GetConstructorName(), context)
        : nullptr;

    Local<Value> stackStrValue;
    auto stackstr = trycatch.StackTrace(ctx).ToLocal(&stackStrValue)
        ? JsErrorInfo::CreateString(stackStrValue->ToString(ctx).FromMaybe(Local<String>()), context)
        : nullptr;

    auto stackFrames = CaptureStackFrames(message->GetStackTrace(), context);

    return new JsErrorInfo(description, line, column, resource, type, text, error, stackstr, stackFrames);
}

JsErrorInfo::jsstackframe* JsErrorInfo::CaptureStackFrames(Local<StackTrace> stackTrace, JsContext* context)
{
    // The handle will be empty unless isolate.SetCaptureStackTraceForUncaughtExceptions(true)
    // has been called
    if (stackTrace.IsEmpty())
        return nullptr;

    auto count = stackTrace->GetFrameCount();
    if (count == 0)
        return nullptr;

    auto isolate = context->Isolate();

    jsstackframe* head = nullptr;
    jsstackframe* current = nullptr;
    jsstackframe* prev = nullptr;

    for (int i = 0; i < count; i++) {

        auto frame = stackTrace->GetFrame(isolate, i);

        auto line = frame->GetLineNumber();
        auto column = frame->GetColumn();
        auto resource = JsErrorInfo::CreateString(frame->GetScriptName(), context);
        auto function = JsErrorInfo::CreateString(frame->GetFunctionName(), context);

        current = new jsstackframe(line, column, resource, function);
        if (head == nullptr)
            head = current;
        if (prev != nullptr)
            prev->next = current;
        prev = current;
    }

    return head;
}

char16_t* JsErrorInfo::CreateString(Local<String> value, JsContext* context)
{
    if (value.IsEmpty())
        return nullptr;

    auto len = value->Length();

    auto str = new uint16_t[len + 1];
    value->Write(context->Isolate(), str);
    return (char16_t*)str;
}
