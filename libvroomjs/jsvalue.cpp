#include "vroomjs.h"
#include <cassert>

void JsValue::Dispose()
{
    switch (v.type) {

    case JSVALUE_TYPE_STRING:
    case JSVALUE_TYPE_STRING_ERROR:
        delete[] v.value.str;
        break;

    case JSVALUE_TYPE_ARRAY:
        for (int i = 0; i < v.length; i++) {
            ((JsValue*)&v.value.arr[i])->Dispose();
        }
        delete[] v.value.arr;
        break;

    case JSVALUE_TYPE_ERROR:
        auto info = (jserrorinfo*)v.value.ptr;
        ((JsErrorInfo*)info)->Dispose();
        delete info;
        break;
    }

    // Set it to empty in case Dispose() gets called again for some reason.
    v = JsValue(JSVALUE_TYPE_EMPTY, 0, 0);
}