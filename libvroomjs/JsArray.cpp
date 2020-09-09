#include "JsArray.h"
#include "JsContext.h"

Local<Array> JsArray::ToLocal()
{
    return Local<Array>::Cast(Local<Object>::New(Context()->Isolate(), Obj()));
}
