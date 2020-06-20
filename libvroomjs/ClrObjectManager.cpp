#include "ClrObjectManager.h"
#include "ClrObjectRef.h"
#include "JsContext.h"
#include "JsEngine.h"
#include <thread>

void ClrObjectManager::WeakHandleCallback(const WeakCallbackInfo<ClrObjectManager::WeakCallbackArgs>& info)
{
#ifdef DEBUG_TRACE_API
    std::cout << "WeakHandleCallback" << std::endl;
#endif
    auto args = info.GetParameter();

    std::cout << "WeakHandleCallback " << args->id << " " << std::this_thread::get_id() << std::endl;

    args->owner->RemoveEntry(args->id);
}

Local<Object> ClrObjectManager::GetProxy(int id)
{
    std::cout << "GetProxy " << id << " " << std::this_thread::get_id() << std::endl;

    auto isolate = context_->Isolate();

    auto search = proxyMap_.find(id);
    if (search != proxyMap_.end()) {
        return Local<Object>::New(isolate, search->second.objectHandle);
    }

    auto ref = new ClrObjectRef(context_, id);
    auto t = context_->Engine()->Template();
    auto ctx = context_->Ctx();

    auto obj = t->NewInstance(ctx).ToLocalChecked();
    obj->SetInternalField(0, External::New(isolate, ref));
     
    auto args = new WeakCallbackArgs { this, id };
    auto handle = UniquePersistent<Object>(isolate, obj);
    handle.SetWeak(args, ClrObjectManager::WeakHandleCallback, v8::WeakCallbackType::kParameter);

    // The entry in proxyMap_ will own the UniquePersistent, ClrObjectRef and the WeakCallbackArgs,
    // and will be responsible for releasing them.
    proxyMap_[id] = Entry(std::move(handle), std::unique_ptr<ClrObjectRef>(ref), std::unique_ptr<WeakCallbackArgs>(args));

    return obj;
}

void ClrObjectManager::RemoveEntry(int id)
{
    proxyMap_.erase(id);
}