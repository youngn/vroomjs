#include <cassert>

#include "vroomjs.h"
#include "JsScript.h"
#include "JsContext.h"
#include "JsEngine.h"
#include "JsValue.h"

long js_mem_debug_script_count;

JsScript::JsScript(Local<Script> script, JsContext* context)
    : context_(context), script_(context->Isolate(), script)
{
    INCREMENT(js_mem_debug_script_count);
}

void JsScript::DisposeCore()
{
    script_.Reset();
}

JsValue JsScript::Execute()
{
    auto isolate = context_->Isolate();

    Locker locker(isolate);
    Isolate::Scope isolate_scope(isolate);
    HandleScope scope(isolate);

    auto ctx = context_->Ctx();
    Context::Scope contextScope(ctx);

    TryCatch trycatch(isolate);

    auto script = Local<Script>::New(isolate, script_);

    Local<Value> result;
    if (script->Run(ctx).ToLocal(&result)) {
        return JsValue::ForValue(result, context_);
    }
    else {
        return JsValue::ForError(trycatch, context_);
    }
}
