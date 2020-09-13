#pragma once

#include "vroomjs.h"
#include <unordered_map>

class JsContext;
class HostObjectRef;

class HostObjectManager
{
public:
    HostObjectManager(JsContext* context)
        :context_(context)
    {
    }

    Local<Object> GetProxy(int id, int templateId);

private:

    struct WeakCallbackArgs
    {
        HostObjectManager* owner;
        int id;
    };

    struct Entry
    {
        Entry() {}

        Entry(UniquePersistent<Object>&& objectHandle,
            std::unique_ptr<HostObjectRef>&& hostObjectRef,
            std::unique_ptr<WeakCallbackArgs>&& weakCallbackArgs) :
            objectHandle(std::move(objectHandle)),
            hostObjectRef(std::move(hostObjectRef)),
            weakCallbackArgs(std::move(weakCallbackArgs))
        {
        }

        // no copying
        Entry(Entry const&) = delete;
        Entry& operator=(Entry const&) = delete;

        // only moving
        Entry(Entry&& that) noexcept
            : objectHandle(std::move(that.objectHandle)),
            hostObjectRef(std::move(that.hostObjectRef)),
            weakCallbackArgs(std::move(that.weakCallbackArgs))
        {
        }

        Entry& operator=(Entry&& that) noexcept
        {
            if (this != &that)
            {
                objectHandle = std::move(that.objectHandle);
                hostObjectRef = std::move(that.hostObjectRef);
                weakCallbackArgs = std::move(that.weakCallbackArgs);
            }
            return *this;
        }

        // Persistent handle to the proxy object. Note that UniquePersistent automatically
        // Reset()s upon destruction.
        UniquePersistent<Object> objectHandle;

        // The HostObjectRef is only stored here so that it is deterministically deleted
        // regardless of whether the callback is ever invoked.
        std::unique_ptr<HostObjectRef> hostObjectRef;

        // The WeakCallbackArgs is only stored here so that it is deterministically deleted
        // regardless of whether the callback is ever invoked.
        std::unique_ptr<WeakCallbackArgs> weakCallbackArgs;
    };

    static void WeakHandleCallback(const WeakCallbackInfo<WeakCallbackArgs>& info);
    void ReleaseProxy(int id);

    // Context that owns this object
    JsContext* context_;

    // Map of (object ID -> Entry)
    // Note that UniquePersistent automatically calls Reset() when destructed,
    // so clean-up of this entire thing is automatic: Destruction of HostObjectManager
    // destructs proxyMap_, which in turn destructs each Entry, destructing the UniquePersistent handle,
    // thus removing the reference to the V8 Object.
    std::unordered_map<int, Entry> proxyMap_;
};

