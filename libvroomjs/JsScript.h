#pragma once

#include "vroomjs.h"

class JsEngine;

class JsScript {
public:
	static JsScript* New(JsEngine* engine);

	jsvalue Compile(const uint16_t* str, const uint16_t* resourceName);
	void Dispose();
	Persistent<Script>* GetScript() { return script_; }

	inline virtual ~JsScript() {
		DECREMENT(js_mem_debug_script_count);
	}

private:
	inline JsScript() {
		INCREMENT(js_mem_debug_script_count);
	}
	JsEngine* engine_;
	Persistent<Script>* script_;
};

