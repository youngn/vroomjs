// This file is part of the VroomJs library.
//
// Author:
//     Federico Di Gregorio <fog@initd.org>
//
// Copyright (c) 2013 
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

#ifndef LIBVROOMJS_H
#define LIBVROOMJS_H 

#include <v8.h>
#include <stdlib.h>
#include <stdint.h>
#include <iostream>
#include <cassert>

using namespace v8;

// jsvalue (JsValue on the CLR side) is a struct that can be easily marshaled
// by simply blitting its value (being only 16 bytes should be quite fast too).

#define JSVALUE_TYPE_UNKNOWN_ERROR  -1
#define JSVALUE_TYPE_EMPTY           0
#define JSVALUE_TYPE_NULL            1
#define JSVALUE_TYPE_BOOLEAN         2
#define JSVALUE_TYPE_INTEGER         3
#define JSVALUE_TYPE_NUMBER          4
#define JSVALUE_TYPE_STRING          5
#define JSVALUE_TYPE_DATE            6
#define JSVALUE_TYPE_INDEX           7
#define JSVALUE_TYPE_CLROBJECT      12
#define JSVALUE_TYPE_CLRERROR       13
#define JSVALUE_TYPE_JSOBJECT       14
#define JSVALUE_TYPE_JSERROR        16
#define JSVALUE_TYPE_JSFUNCTION     17
#define JSVALUE_TYPE_JSARRAY        18
#define JSVALUE_TYPE_JSSTRING       19

#ifdef _WIN32 
#define EXPORT __declspec(dllexport)
#else 
#define EXPORT
#endif

#ifdef _WIN32
#include "Windows.h"
#define CALLINGCONVENTION __stdcall
#define INCREMENT(x) InterlockedIncrement(&x)
#define DECREMENT(x) InterlockedDecrement(&x)
#else 
#define CALLINGCONVENTION
#define INCREMENT(x) __sync_fetch_and_add(&x, 1)
#define DECREMENT(x) __sync_fetch_and_add(&x, -1)
#endif

extern int32_t js_object_marshal_type;

extern long js_mem_debug_engine_count;
extern long js_mem_debug_context_count;
extern long js_mem_debug_clrobjectref_count;
extern long js_mem_debug_script_count;

extern "C" 
{
    struct jsvalue
    {
        // 8 bytes is the maximum CLR alignment; by putting the union first and a
        // int64_t inside it we make (almost) sure the offset of 'type' will always
        // be 8 and the total size 16. We add a check to JsContext_new anyway.
        
        union 
        {
            int32_t     i32;
            int64_t     i64;
            double      num;
            void*       ptr;
            uint16_t*   str;
            jsvalue*    arr;
        } value;
        
        int32_t         type;
        union
        {
            int32_t     length;     // length of str
            int32_t     templateId; // template ID for CLR object/error
        };
	};
	
	EXPORT void CALLINGCONVENTION jsvalue_dispose(jsvalue value);
}

//class JsValue;
//class JsValueDisposer;
//class JsErrorInfo;
//class JsEngine;
//class JsContext;


// The only way for the C++/V8 side to call into the CLR is to use the function
// pointers (CLR, delegates) defined below.

extern "C" 
{
    // We don't have a keepalive_add_f because that is managed on the CLR side.
    // Its definition would be "int (*keepalive_add_f) (ClrObjectRef obj)".
    
    typedef void (CALLINGCONVENTION* keepalive_remove_f) (int32_t context, int32_t id);
    typedef jsvalue (CALLINGCONVENTION* keepalive_get_property_value_f) (int32_t context, int32_t id, uint16_t* name);
    typedef jsvalue (CALLINGCONVENTION* keepalive_set_property_value_f) (int32_t context, int32_t id, uint16_t* name, jsvalue value);
    typedef jsvalue (CALLINGCONVENTION* keepalive_delete_property_f) (int32_t context, int32_t id, uint16_t* name);
    typedef jsvalue (CALLINGCONVENTION* keepalive_enumerate_properties_f) (int32_t context, int32_t id);
    typedef jsvalue (CALLINGCONVENTION* keepalive_invoke_f) (int32_t context, int32_t id, int32_t argCount, jsvalue* args);
    typedef jsvalue (CALLINGCONVENTION* keepalive_valueof_f) (int32_t context, int32_t id);
    typedef jsvalue (CALLINGCONVENTION* keepalive_tostring_f) (int32_t context, int32_t id);

	struct jscallbacks
	{
		keepalive_remove_f remove;
		keepalive_get_property_value_f get_property_value;
		keepalive_set_property_value_f set_property_value;
        keepalive_delete_property_f delete_property;
        keepalive_enumerate_properties_f enumerate_properties;
        keepalive_invoke_f invoke;
        keepalive_valueof_f valueof;
        keepalive_tostring_f tostring;
	};
}

#endif
