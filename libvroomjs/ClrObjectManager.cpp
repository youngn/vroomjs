#include "ClrObjectManager.h"
#include "ManagedRef.h"
#include "JsContext.h"
#include "JsEngine.h"
#include <thread>

void ClrObjectManager::managed_destroy(const WeakCallbackInfo<ClrObjectManager::WeakCallbackArgs>& info)
{
#ifdef DEBUG_TRACE_API
    std::cout << "managed_destroy" << std::endl;
#endif
    auto args = info.GetParameter();

    std::cout << "managed_destroy " << args->id << " " << std::this_thread::get_id() << std::endl;

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

    auto ref = new ManagedRef(context_, id);
    auto t = context_->Engine()->Template();
    auto ctx = context_->Ctx();

    auto obj = t->InstanceTemplate()->NewInstance(ctx).ToLocalChecked();
    obj->SetInternalField(0, External::New(isolate, ref));
     
    auto args = new WeakCallbackArgs { this, id };
    auto handle = UniquePersistent<Object>(isolate, obj);
    handle.SetWeak(args, ClrObjectManager::managed_destroy, v8::WeakCallbackType::kParameter);

    // The entry in proxyMap_ will own the UniquePersistent, ManagedRef and the WeakCallbackArgs,
    // and will be responsible for releasing them.
    proxyMap_[id] = Entry(std::move(handle), std::unique_ptr<ManagedRef>(ref), std::unique_ptr<WeakCallbackArgs>(args));

    return obj;
}

void ClrObjectManager::RemoveEntry(int id)
{
    proxyMap_.erase(id);
}