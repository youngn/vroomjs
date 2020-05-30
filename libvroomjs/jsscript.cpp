#include <cassert>
#include "vroomjs.h"

long js_mem_debug_script_count;

JsScript *JsScript::New(JsEngine *engine) {
	assert(engine != nullptr);

	auto jsscript = new JsScript();
	jsscript->engine_ = engine;
	jsscript->script_ = nullptr;
	return jsscript;
}

jsvalue JsScript::Compile(const uint16_t* str, const uint16_t *resourceName = NULL) {
	assert(str != nullptr);

	jsvalue v;
	v.type = 0;
	script_ = engine_->CompileScript(str, resourceName, &v);
	return v;
}

void JsScript::Dispose() {

	if (script_ == nullptr)
		return;

	auto isolate = engine_->Isolate(); 
	if(isolate != nullptr) {
		// todo: do we really need the locker/isolate scope?
		Locker locker(isolate);
   	 	Isolate::Scope isolate_scope(isolate);
		script_->Reset();
	}
	delete script_;
	script_ = nullptr;
}
