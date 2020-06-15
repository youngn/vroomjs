#pragma once

#include "vroomjs.h"
#include <unordered_map>

class JsContext;
class ManagedRef;

class ClrObjectManager
{
public:
    ClrObjectManager(JsContext* context)
        :context_(context)
    {
    }

    Local<Object> GetProxy(int id);

private:

    struct WeakCallbackArgs
    {
        ClrObjectManager* owner;
        int id;
    };

    struct Entry
    {
        Entry() {}

        Entry(UniquePersistent<Object>&& objectHandle, std::unique_ptr<ManagedRef>&& managedRef, std::unique_ptr<WeakCallbackArgs>&& callbackArgs)
            : objectHandle(std::move(objectHandle)),
            managedRef(std::move(managedRef)),
            callbackArgs(std::move(callbackArgs))
        {
        }

        // no copying
        Entry(Entry const&) = delete;
        Entry& operator=(Entry const&) = delete;

        // only moving
        Entry(Entry&& that) noexcept
            : objectHandle(std::move(that.objectHandle)),
            managedRef(std::move(that.managedRef)),
            callbackArgs(std::move(that.callbackArgs))
        {
        }

        Entry& operator=(Entry&& that) noexcept
        {
            if (this != &that)
            {
                objectHandle = std::move(that.objectHandle);
                managedRef = std::move(that.managedRef);
                callbackArgs = std::move(that.callbackArgs);
            }
            return *this;
        }

        // Persistent handle to the proxy object. Note that UniquePersistent automatically
        // Reset()s upon destruction.
        UniquePersistent<Object> objectHandle;

        // The ManagedRef is only stored here so that it is deterministically deleted
        // regardless of whether the callback is ever invoked.
        std::unique_ptr<ManagedRef> managedRef;

        // The WeakCallbackArgs is only stored here so that it is deterministically deleted
        // regardless of whether the callback is ever invoked.
        std::unique_ptr<WeakCallbackArgs> callbackArgs;
    };

    static void WeakHandleCallback(const WeakCallbackInfo<WeakCallbackArgs>& info);
    void RemoveEntry(int id);

    // Context that owns this object
    JsContext* context_;

    // Map of (object ID -> Entry)
    // Note that UniquePersistent automatically calls Reset() when destructed,
    // so clean-up of this entire thing is automatic: Destruction of ClrObjectManager
    // destructs proxyMap_, which in turn destructs each Entry, destructing the UniquePersistent handle,
    // thus removing the reference to the V8 Object.
    std::unordered_map<int, Entry> proxyMap_;
};

