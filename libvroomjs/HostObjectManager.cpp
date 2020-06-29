#include "HostObjectManager.h"
#include "HostObjectRef.h"
#include "HostObjectTemplate.h"
#include "JsContext.h"
#include "JsEngine.h"

#include <thread>

void HostObjectManager::WeakHandleCallback(const WeakCallbackInfo<HostObjectManager::WeakCallbackArgs>& info)
{
#ifdef DEBUG_TRACE_API
    std::cout << "WeakHandleCallback" << std::endl;
#endif
    auto args = info.GetParameter();

    std::cout << "WeakHandleCallback " << args->id << " " << std::this_thread::get_id() << std::endl;

    args->owner->RemoveEntry(args->id);
}

Local<Object> HostObjectManager::GetProxy(int id, int templateId)
{
    std::cout << "GetProxy " << id << " " << std::this_thread::get_id() << std::endl;

    auto isolate = context_->Isolate();

    auto search = proxyMap_.find(id);
    if (search != proxyMap_.end()) {
        return Local<Object>::New(isolate, search->second.objectHandle);
    }

    auto objectTemplate = context_->Engine()->Template(templateId);

    auto ref = new HostObjectRef(context_, id, objectTemplate->Callbacks());
    auto obj = objectTemplate->NewInstance(context_->Ctx(), ref);
     
    auto args = new WeakCallbackArgs { this, id };
    auto handle = UniquePersistent<Object>(isolate, obj);
    handle.SetWeak(args, HostObjectManager::WeakHandleCallback, v8::WeakCallbackType::kParameter);

    // The entry in proxyMap_ will own the UniquePersistent, HostObjectRef and the WeakCallbackArgs,
    // and will be responsible for releasing them.
    proxyMap_[id] = Entry(std::move(handle), std::unique_ptr<HostObjectRef>(ref), std::unique_ptr<WeakCallbackArgs>(args));

    return obj;
}

void HostObjectManager::RemoveEntry(int id)
{
    proxyMap_.erase(id);
}