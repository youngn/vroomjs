// This file is part of the VroomJs library.
//
// Author:
//     Federico Di Gregorio <fog@initd.org>
//
// Copyright © 2013 Federico Di Gregorio <fog@initd.org>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#include <iostream>
#include <libplatform/libplatform.h>
#include <cassert>

#include "vroomjs.h"
#include "JsEngine.h"
#include "JsContext.h"
#include "JsScript.h"
#include "JsValue.h"
#include "JsString.h"
#include "JsObject.h"
#include "JsFunction.h"

using namespace v8;

// Store Platform so that we can delete it later.
v8::Platform* v8platform;

extern "C" 
{
    EXPORT void CALLINGCONVENTION js_initialize(const char* directory_path)
    {
#ifdef DEBUG_TRACE_API
        std::wcout << "js_initialize " << type << std::endl;
#endif
        assert(directory_path != nullptr);

        // todo: protect this from multiple calls?

        //v8::V8::InitializeICUDefaultLocation(directory_path);
        v8::V8::InitializeExternalStartupData(directory_path);

        v8platform = v8::platform::NewDefaultPlatform().release();
        V8::InitializePlatform(v8platform);
        V8::Initialize();
    }

    EXPORT void CALLINGCONVENTION js_shutdown()
    {
#ifdef DEBUG_TRACE_API
        std::wcout << "js_shutdown " << type << std::endl;
#endif

        v8::V8::Dispose();
        v8::V8::ShutdownPlatform();

        delete v8platform;
        v8platform = nullptr;
    }

    EXPORT void CALLINGCONVENTION js_dispose(Disposable* disposable)
    {
#ifdef DEBUG_TRACE_API
        std::wcout << "js_dispose" << std::endl;
#endif
        assert(disposable != nullptr);
        disposable->Dispose();
        delete disposable;
    }

	EXPORT JsEngine* CALLINGCONVENTION jsengine_new(
		int32_t max_young_space,
        int32_t max_old_space) 
	{
#ifdef DEBUG_TRACE_API
		std::wcout << "jsengine_new" << std::endl;
#endif
        return new JsEngine(max_young_space, max_old_space);
	}

	EXPORT void CALLINGCONVENTION jsengine_terminate_execution(JsEngine* engine) {
#ifdef DEBUG_TRACE_API
                std::wcout << "jsengine_terminate_execution" << std::endl;
#endif
        assert(engine != nullptr);
		engine->TerminateExecution();
	}

    EXPORT void CALLINGCONVENTION jsengine_dump_heap_stats(JsEngine* engine) {
#ifdef DEBUG_TRACE_API
                std::wcout << "jsengine_dump_heap_stats" << std::endl;
#endif
        assert(engine != nullptr);
        engine->DumpHeapStats();
	}

	EXPORT void CALLINGCONVENTION js_dump_allocated_items() {
#ifdef DEBUG_TRACE_API
                std::wcout << "js_dump_allocated_items" << std::endl;
#endif
		std::wcout << "Total allocated Js engines " << js_mem_debug_engine_count << std::endl;
		std::wcout << "Total allocated Js contexts " << js_mem_debug_context_count << std::endl;
		std::wcout << "Total allocated Js scripts " << js_mem_debug_script_count << std::endl;
		std::wcout << "Total allocated Host Object Refs " << js_mem_debug_hostobjectref_count << std::endl;
	}

    EXPORT int CALLINGCONVENTION jsengine_add_template(JsEngine* engine, hostobjectcallbacks callbacks)
    {
#ifdef DEBUG_TRACE_API
        std::wcout << "jsengine_add_template" << std::endl;
#endif
        assert(engine != nullptr);
        return engine->AddTemplate(callbacks);
    }

    EXPORT JsContext* CALLINGCONVENTION jscontext_new(int32_t id, JsEngine *engine)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jscontext_new" << std::endl;
#endif
        assert(engine != nullptr);
        return engine->NewContext(id);
    }

	EXPORT void CALLINGCONVENTION jscontext_force_gc()
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jscontext_force_gc" << std::endl;
#endif
        // TODO: this method is no longer part of the API - investigate
        //while(!V8::IdleNotification()) {};
    }
    
    EXPORT jsvalue CALLINGCONVENTION jscontext_execute(JsContext* context, const uint16_t* code, const uint16_t *resourceName)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jscontext_execute" << std::endl;
#endif
        assert(context != nullptr);
        assert(code != nullptr);

        return context->Execute(code, resourceName);
    }

	EXPORT jsvalue CALLINGCONVENTION jscontext_get_global(JsContext* context)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jscontext_get_global" << std::endl;
#endif
        assert(context != nullptr);
        return context->GetGlobal();
    }
	
    EXPORT jsvalue CALLINGCONVENTION jscontext_set_variable(JsContext* context, const uint16_t* name, jsvalue value)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jscontext_set_variable" << std::endl;
#endif
        assert(context != nullptr);
        assert(name != nullptr);

        return context->SetVariable(name, value);
    }

    EXPORT jsvalue CALLINGCONVENTION jscontext_get_variable(JsContext* context, const uint16_t* name)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jscontext_get_variable" << std::endl;
#endif
        assert(context != nullptr);
        assert(name != nullptr);

        return context->GetVariable(name);
    }

    EXPORT jsvalue CALLINGCONVENTION jscontext_new_object(JsContext* context)
    {
#ifdef DEBUG_TRACE_API
        std::wcout << "jscontext_new_object" << std::endl;
#endif
        assert(context != nullptr);

        return context->CreateObject();
    }

    EXPORT jsvalue CALLINGCONVENTION jscontext_new_array(JsContext* context, int len, const jsvalue* elements)
    {
#ifdef DEBUG_TRACE_API
        std::wcout << "jscontext_new_array" << std::endl;
#endif
        assert(context != nullptr);
        assert(len >= 0);

        return context->CreateArray(len, (const JsValue*)elements);
    }

    EXPORT jsvalue CALLINGCONVENTION jscontext_get_proxy(JsContext* context, jsvalue hostObject)
    {
#ifdef DEBUG_TRACE_API
        std::wcout << "jscontext_get_proxy" << std::endl;
#endif
        assert(context != nullptr);

        return context->GetHostObjectProxy(hostObject);
    }

    EXPORT jsvalue CALLINGCONVENTION jsobject_get_named_property_value(JsContext* context, JsObject* obj, const uint16_t* name)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jsobject_get_named_property_value" << std::endl;
#endif
        assert(context != nullptr);
        assert(obj != nullptr);
        assert(name != nullptr);

        return obj->GetPropertyValue(name);
    }
    
    EXPORT jsvalue CALLINGCONVENTION jsobject_set_named_property_value(JsContext* context, JsObject* obj, const uint16_t* name, jsvalue value)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jsobject_set_named_property_value" << std::endl;
#endif
        assert(context != nullptr);
        assert(obj != nullptr);
        assert(name != nullptr);

        return obj->SetPropertyValue(name, value);
    }    

    EXPORT jsvalue CALLINGCONVENTION jsobject_get_indexed_property_value(JsContext* context, JsObject* obj, const uint32_t index)
    {
#ifdef DEBUG_TRACE_API
        std::wcout << "jsobject_get_indexed_property_value" << std::endl;
#endif
        assert(context != nullptr);
        assert(obj != nullptr);

        return obj->GetPropertyValue(index);
    }

    EXPORT jsvalue CALLINGCONVENTION jsobject_set_indexed_property_value(JsContext* context, JsObject* obj, const uint32_t index, jsvalue value)
    {
#ifdef DEBUG_TRACE_API
        std::wcout << "jsobject_set_indexed_property_value" << std::endl;
#endif
        assert(context != nullptr);
        assert(obj != nullptr);

        return obj->SetPropertyValue(index, value);
    }

	EXPORT jsvalue CALLINGCONVENTION jsobject_get_property_names(JsContext* context, JsObject* obj)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jsobject_get_property_names" << std::endl;
#endif
        assert(context != nullptr);
        assert(obj != nullptr);

        return obj->GetPropertyNames();
    }    
	    
	EXPORT jsvalue CALLINGCONVENTION jsfunction_invoke(JsContext* context, JsFunction* obj, jsvalue receiver, int argCount, jsvalue* args)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jsfunction_invoke" << std::endl;
#endif
        assert(context != nullptr);
        assert(obj != nullptr);
        assert(argCount >= 0);
        assert(argCount == 0 || args != nullptr);

        return obj->Invoke(receiver, argCount, (JsValue*)args);
    }        

	EXPORT JsScript* CALLINGCONVENTION jsscript_new(JsContext *context)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jsscript_new" << std::endl;
#endif
        assert(context != nullptr);
        return context->NewScript();
    }

	EXPORT jsvalue CALLINGCONVENTION jsscript_compile(JsScript* script, const uint16_t* code, const uint16_t *resourceName)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jsscript_compile" << std::endl;
#endif
        assert(script != nullptr);
        assert(code != nullptr);

        return script->Compile(code, resourceName);
    }

    EXPORT jsvalue CALLINGCONVENTION jsscript_execute(JsScript* script)
    {
#ifdef DEBUG_TRACE_API
        std::wcout << "jsscript_execute" << std::endl;
#endif
        assert(script != nullptr);

        return script->Execute();
    }

    EXPORT jsvalue CALLINGCONVENTION jsstring_new(JsEngine* engine, const uint16_t* value)
    {
#ifdef DEBUG_TRACE_API
        std::wcout << "jsstring_new" << std::endl;
#endif
        assert(engine != nullptr);
        assert(value != nullptr);

        int len;
        auto str = JsString::Create(engine->Isolate(), value, len);

        if (str != nullptr)
            return JsValue::ForJsString(str, len);

        return JsValue::ForEmpty();
    }

    EXPORT int CALLINGCONVENTION jsstring_get_value(JsEngine* engine, Persistent<String>* str, uint16_t* buffer)
    {
#ifdef DEBUG_TRACE_API
        std::wcout << "jsstring_get_value" << std::endl;
#endif
        assert(engine != nullptr);
        assert(str != nullptr);
        assert(buffer != nullptr);

        return JsString::GetValue(engine->Isolate(), str, buffer);
    }
                
    EXPORT void CALLINGCONVENTION jsvalue_dispose(jsvalue value)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jsvalue_dispose" << std::endl;
#endif
        JsValueDisposer::DisposeValue(value);
    }       
}
