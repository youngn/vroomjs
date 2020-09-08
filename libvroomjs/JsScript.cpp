#include <cassert>

#include "vroomjs.h"
#include "JsScript.h"
#include "JsContext.h"
#include "JsEngine.h"
#include "JsValue.h"

long js_mem_debug_script_count;

JsScript::JsScript(JsContext* context)
    : context_(context), script_(nullptr)
{
    // Do this last, in case anything above fails
    INCREMENT(js_mem_debug_script_count);
}

JsValue JsScript::Compile(const uint16_t * code, const uint16_t* resourceName)
{
    assert(script_ == nullptr); // This method can only be called (successfully) once
    assert(code != nullptr);

    auto isolate = context_->Isolate();

    Locker locker(isolate);
    Isolate::Scope isolate_scope(isolate);
    HandleScope scope(isolate);

    auto ctx = context_->Ctx();
    Context::Scope contextScope(ctx);

    TryCatch trycatch(isolate);

    auto source = String::NewFromTwoByte(isolate, code).ToLocalChecked();

    auto res_name = resourceName != NULL
        ? String::NewFromTwoByte(isolate, resourceName).ToLocalChecked()
        : String::Empty(isolate);

    ScriptOrigin scriptOrigin(res_name);

    Local<Script> script;
    if (!Script::Compile(ctx, source, &scriptOrigin).ToLocal(&script))
    {
        // Compilation failed e.g. syntax error
        return JsValue::ForError(trycatch, context_);
    }

    script_ = new Persistent<Script>(isolate, script);

    return JsValue::ForEmpty();
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

    auto script = Local<Script>::New(isolate, *script_);

    Local<Value> result;
    if (script->Run(ctx).ToLocal(&result)) {
        return JsValue::ForValue(result, context_);
    }
    else {
        return JsValue::ForError(trycatch, context_);
    }
}

void JsScript::Dispose()
{
    if (IsDisposed())
        return;

    // Is the context/engine still alive (i.e. the Isolate is still alive)?
    if (!context_->IsDisposed() && !context_->Engine()->IsDisposed()) {

        script_->Reset();
    }

    delete script_;
    script_ = nullptr;
}
