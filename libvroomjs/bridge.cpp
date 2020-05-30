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
        assert(directory_path != NULL);

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

	EXPORT JsEngine* CALLINGCONVENTION jsengine_new(
        jscallbacks callbacks,
		int32_t max_young_space,
        int32_t max_old_space) 
	{
#ifdef DEBUG_TRACE_API
		std::wcout << "jsengine_new" << std::endl;
#endif
        assert(callbacks.remove != NULL);
        assert(callbacks.get_property_value != NULL);
        assert(callbacks.set_property_value != NULL);
        assert(callbacks.valueof != NULL);
        assert(callbacks.invoke != NULL);
        assert(callbacks.delete_property != NULL);
        assert(callbacks.enumerate_properties != NULL);

        return new JsEngine(max_young_space, max_old_space, callbacks);
	}

	EXPORT void CALLINGCONVENTION jsengine_terminate_execution(JsEngine* engine) {
#ifdef DEBUG_TRACE_API
                std::wcout << "jsengine_terminate_execution" << std::endl;
#endif
        assert(engine != NULL);
		engine->TerminateExecution();
	}

    EXPORT void CALLINGCONVENTION jsengine_dump_heap_stats(JsEngine* engine) {
#ifdef DEBUG_TRACE_API
                std::wcout << "jsengine_dump_heap_stats" << std::endl;
#endif
        assert(engine != NULL);
        engine->DumpHeapStats();
	}

	EXPORT void CALLINGCONVENTION js_dump_allocated_items() {
#ifdef DEBUG_TRACE_API
                std::wcout << "js_dump_allocated_items" << std::endl;
#endif
		std::wcout << "Total allocated Js engines " << js_mem_debug_engine_count << std::endl;
		std::wcout << "Total allocated Js contexts " << js_mem_debug_context_count << std::endl;
		std::wcout << "Total allocated Js scripts " << js_mem_debug_script_count << std::endl;
		std::wcout << "Total allocated Managed Refs " << js_mem_debug_managedref_count << std::endl;
	}

	EXPORT void CALLINGCONVENTION jsengine_dispose(JsEngine* engine)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jsengine_dispose" << std::endl;
#endif
        assert(engine != NULL);
        engine->Dispose();
        delete engine;
    }

    EXPORT JsContext* CALLINGCONVENTION jscontext_new(int32_t id, JsEngine *engine)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jscontext_new" << std::endl;
#endif
        assert(engine != NULL);
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

    EXPORT void jscontext_dispose(JsContext* context)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jscontext_dispose" << std::endl;
#endif
        assert(context != NULL);
        context->Dispose();
        delete context;
    }
    
    EXPORT void CALLINGCONVENTION jsengine_dispose_object(JsEngine* engine, Persistent<Object>* obj)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jscontext_dispose_object" << std::endl;
#endif
        if (engine != NULL) {
            // Allow V8 GC to reclaim the JS Object
            engine->DisposeObject(obj);
		}

        // Delete the Persistent handle (not the Object, which is owned by V8)
		delete obj;
    }     
    
    EXPORT jsvalue CALLINGCONVENTION jscontext_execute(JsContext* context, const uint16_t* str, const uint16_t *resourceName)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jscontext_execute" << std::endl;
#endif
        assert(context != NULL);
        assert(str != NULL);

        return context->Execute(str, resourceName);
    }

	EXPORT jsvalue CALLINGCONVENTION jscontext_execute_script(JsContext* context, JsScript *script)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jscontext_execute_script" << std::endl;
#endif
        assert(context != NULL);
        assert(script != NULL);

        return context->Execute(script);
    }

	EXPORT jsvalue CALLINGCONVENTION jscontext_get_global(JsContext* context)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jscontext_get_global" << std::endl;
#endif
        assert(context != NULL);
        return context->GetGlobal();
    }
	
    EXPORT jsvalue CALLINGCONVENTION jscontext_set_variable(JsContext* context, const uint16_t* name, jsvalue value)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jscontext_set_variable" << std::endl;
#endif
        assert(context != NULL);
        assert(name != NULL);

        return context->SetVariable(name, value);
    }

    EXPORT jsvalue CALLINGCONVENTION jscontext_get_variable(JsContext* context, const uint16_t* name)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jscontext_get_variable" << std::endl;
#endif
        assert(context != NULL);
        assert(name != NULL);

        return context->GetVariable(name);
    }

    EXPORT jsvalue CALLINGCONVENTION jsobject_get_named_property_value(JsContext* context, Persistent<Object>* obj, const uint16_t* name)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jsobject_get_named_property_value" << std::endl;
#endif
        assert(context != NULL);
        assert(obj != NULL);
        assert(name != NULL);

        return context->GetPropertyValue(obj, name);
    }
    
    EXPORT jsvalue CALLINGCONVENTION jsobject_set_named_property_value(JsContext* context, Persistent<Object>* obj, const uint16_t* name, jsvalue value)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jsobject_set_named_property_value" << std::endl;
#endif
        assert(context != NULL);
        assert(obj != NULL);
        assert(name != NULL);

        return context->SetPropertyValue(obj, name, value);
    }    

    EXPORT jsvalue CALLINGCONVENTION jsobject_get_indexed_property_value(JsContext* context, Persistent<Object>* obj, const uint32_t index)
    {
#ifdef DEBUG_TRACE_API
        std::wcout << "jsobject_get_indexed_property_value" << std::endl;
#endif
        assert(context != NULL);
        assert(obj != NULL);

        return context->GetPropertyValue(obj, index);
    }

    EXPORT jsvalue CALLINGCONVENTION jsobject_set_indexed_property_value(JsContext* context, Persistent<Object>* obj, const uint32_t index, jsvalue value)
    {
#ifdef DEBUG_TRACE_API
        std::wcout << "jsobject_set_indexed_property_value" << std::endl;
#endif
        assert(context != NULL);
        assert(obj != NULL);

        return context->SetPropertyValue(obj, index, value);
    }

	EXPORT jsvalue CALLINGCONVENTION jsobject_get_property_names(JsContext* context, Persistent<Object>* obj)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jsobject_get_property_names" << std::endl;
#endif
        assert(context != NULL);
        assert(obj != NULL);

        return context->GetPropertyNames(obj);
    }    
	    
	EXPORT jsvalue CALLINGCONVENTION jsfunction_invoke(JsContext* context, Persistent<Function>* obj, jsvalue receiver, int argCount, jsvalue* args)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jsfunction_invoke" << std::endl;
#endif
        assert(context != NULL);
        assert(obj != NULL);
        assert(argCount >= 0);
        assert(argCount == 0 || args != NULL);

        return context->InvokeFunction(obj, receiver, argCount, (JsValue*)args);
    }        

	EXPORT JsScript* CALLINGCONVENTION jsscript_new(JsEngine *engine)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jsscript_new" << std::endl;
#endif
        assert(engine != NULL);

        return JsScript::New(engine);
    }

	EXPORT void CALLINGCONVENTION jsscript_dispose(JsScript *script)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jsscript_dispose" << std::endl;
#endif
        assert(script != NULL);

        script->Dispose();
		delete script;
    }

	EXPORT jsvalue CALLINGCONVENTION jsscript_compile(JsScript* script, const uint16_t* str, const uint16_t *resourceName)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jsscript_compile" << std::endl;
#endif
        assert(script != NULL);
        assert(str != NULL);

        return script->Compile(str, resourceName);
    }

    EXPORT jsvalue CALLINGCONVENTION jsvalue_alloc_string(const uint16_t* str)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jsvalue_alloc_string" << std::endl;
#endif
        assert(str != NULL);

        jsvalue v;
    
        // todo: use strlen?
        int length = 0;
        while (str[length] != '\0')
            length++;
          
        v.length = length;
        v.value.str = new uint16_t[length+1];
        if (v.value.str != NULL) {
            for (int i=0 ; i < length ; i++)
                 v.value.str[i] = str[i];
            v.value.str[length] = '\0';
            v.type = JSVALUE_TYPE_STRING;
        }

        return v;
    }

    EXPORT jsvalue CALLINGCONVENTION jsstring_new(JsEngine* engine, const uint16_t* value)
    {
#ifdef DEBUG_TRACE_API
        std::wcout << "jsengine_new_string" << std::endl;
#endif
        assert(engine != NULL);
        assert(value != NULL);

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
        assert(engine != NULL);
        assert(str != NULL);
        assert(buffer != NULL);

        return JsString::GetValue(engine->Isolate(), str, buffer);
    }

    EXPORT jsvalue CALLINGCONVENTION jsvalue_alloc_array(const int32_t length)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jsvalue_alloc_array" << std::endl;
#endif
        jsvalue v;
          
        v.value.arr = new jsvalue[length];
        if (v.value.arr != NULL) {
            v.length = length;
            v.type = JSVALUE_TYPE_ARRAY;
        }

        return v;
    }        
                
    EXPORT void CALLINGCONVENTION jsvalue_dispose(jsvalue value)
    {
#ifdef DEBUG_TRACE_API
		std::wcout << "jsvalue_dispose" << std::endl;
#endif
        JsValueDisposer::DisposeValue(value);
    }       
}
