#include "HostObjectTemplate.h"
#include "HostObjectRef.h"

HostObjectTemplate::HostObjectTemplate(Isolate* isolate, jscallbacks callbacks)
    :callbacks_(callbacks), isolate_(isolate)
{
    Locker locker(isolate);
    HandleScope scope(isolate);

    // Setup the template we'll use for all host object references.
    auto t = ObjectTemplate::New(isolate);
    t->SetInternalFieldCount(1); // stores ptr to HostObjectRef
    t->SetHandler(
        NamedPropertyHandlerConfiguration(
            callbacks.get_property_value != nullptr ? GetPropertyValueCallback : nullptr,
            callbacks.set_property_value != nullptr ? SetPropertyValueCallback : nullptr,
            nullptr,
            callbacks.delete_property != nullptr ? DeletePropertyCallback : nullptr,
            callbacks.enumerate_properties != nullptr ? EnumeratePropertiesCallback : nullptr
        )
    );

    if (callbacks.invoke != nullptr) {
        t->SetCallAsFunctionHandler(InvokeCallback);
    }

    // TODO: the "valueOf" callback was originally set on the prototype i.e. :
    // fo->PrototypeTemplate()->Set(isolate_, "valueOf", FunctionTemplate::New(isolate_, HostObjectRef::ValueOfCallback));
    // Is there some advantage to doing that? AFAICT, this achieves the same result.
    // Note that interceptors get priority over accessors, so these methods can be provided by the GetPropertyValue callback
    // if desired. The reason for having dedicated callbacks is to guarantee a sane implementation of these
    // methods exists on all objects, in case the GetPropertyValue callback chooses to ignore these properties.
    if (callbacks.valueof != nullptr) {
        t->Set(isolate, "valueOf", FunctionTemplate::New(isolate, ValueOfCallback));
    }
    if (callbacks.tostring != nullptr) {
        t->Set(isolate, "toString", FunctionTemplate::New(isolate, ToStringCallback));
    }

    template_ = UniquePersistent<ObjectTemplate>(isolate, t);
}

Local<Object> HostObjectTemplate::NewInstance(Local<Context> ctx, HostObjectRef* ref) const
{
    auto t = Local<ObjectTemplate>::New(isolate_, template_);
    auto obj = t->NewInstance(ctx).ToLocalChecked();
    obj->SetInternalField(0, External::New(isolate_, ref));
    return obj;
}

HostObjectRef* HostObjectTemplate::GetHostObjectRef(Local<Object> obj)
{
    auto ext = Local<External>::Cast(obj->GetInternalField(0));
    return (HostObjectRef*)ext->Value();
}

void HostObjectTemplate::GetPropertyValueCallback(Local<Name> name, const PropertyCallbackInfo<Value>& info)
{
#ifdef DEBUG_TRACE_API
    std::cout << "GetPropertyValueCallback" << std::endl;
#endif

    GetHostObjectRef(info.Holder())->GetPropertyValue(name, info);
}

void HostObjectTemplate::SetPropertyValueCallback(Local<Name> name, Local<Value> value, const PropertyCallbackInfo<Value>& info)
{
#ifdef DEBUG_TRACE_API
    std::cout << "SetPropertyValueCallback" << std::endl;
#endif

    GetHostObjectRef(info.Holder())->SetPropertyValue(name, value, info);
}

void HostObjectTemplate::DeletePropertyCallback(Local<Name> name, const PropertyCallbackInfo<Boolean>& info)
{
#ifdef DEBUG_TRACE_API
    std::cout << "DeletePropertyCallback" << std::endl;
#endif

    GetHostObjectRef(info.Holder())->DeleteProperty(name, info);
}

void HostObjectTemplate::EnumeratePropertiesCallback(const PropertyCallbackInfo<Array>& info)
{
#ifdef DEBUG_TRACE_API
    std::cout << "EnumeratePropertiesCallback" << std::endl;
#endif

    GetHostObjectRef(info.Holder())->EnumerateProperties(info);
}

void HostObjectTemplate::InvokeCallback(const FunctionCallbackInfo<Value>& info)
{
#ifdef DEBUG_TRACE_API
    std::cout << "InvokeCallback" << std::endl;
#endif

    GetHostObjectRef(info.Holder())->Invoke(info);
}

void HostObjectTemplate::ValueOfCallback(const FunctionCallbackInfo<Value>& info)
{
#ifdef DEBUG_TRACE_API
    std::cout << "ValueOfCallback" << std::endl;
#endif

    GetHostObjectRef(info.Holder())->ValueOf(info);
}

void HostObjectTemplate::ToStringCallback(const FunctionCallbackInfo<Value>& info)
{
#ifdef DEBUG_TRACE_API
    std::cout << "ToStringCallback" << std::endl;
#endif

    GetHostObjectRef(info.Holder())->ToString(info);
}


