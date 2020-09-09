#include "JsFunction.h"
#include "JsValue.h"
#include "JsContext.h"


Local<Function> JsFunction::ToLocal()
{
    return Local<Function>::Cast(Local<Object>::New(Context()->Isolate(), Obj()));
}

JsValue JsFunction::Invoke(JsValue receiver, int argCount, JsValue* args)
{
    assert(argCount >= 0);
    assert(argCount == 0 || args != NULL);

    auto context = Context();
    auto isolate = context->Isolate();

    Locker locker(isolate);
    Isolate::Scope isolate_scope(isolate);
    HandleScope scope(isolate);

    auto ctx = context->Ctx();
    Context::Scope contextScope(ctx);

    auto funcLocal = Local<Function>::Cast(Local<Object>::New(isolate, Obj()));
    auto recv = receiver.Extract(context);

    TryCatch trycatch(isolate);

    std::vector<Local<Value>> argv(argCount);
    if (args != nullptr) {
        for (int i = 0; i < argCount; i++) {
            argv[i] = args[i].Extract(context);
        }
    }

    Local<Value> retVal;
    if (funcLocal->Call(ctx, recv, argCount, &argv[0]).ToLocal(&retVal)) {
        return JsValue::ForValue(retVal, context);
    }
    else {
        return JsValue::ForError(trycatch, context);
    }
}
