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

        Entry(UniquePersistent<Object>&& objectHandle, std::unique_ptr<WeakCallbackArgs>&& callbackArgs)
            : objectHandle(std::move(objectHandle)),
            callbackArgs(std::move(callbackArgs))
        {
        }

        // no copying
        Entry(Entry const&) = delete;
        Entry& operator=(Entry const&) = delete;

        // only moving
        Entry(Entry&& that) noexcept
            : objectHandle(std::move(that.objectHandle)),
            callbackArgs(std::move(that.callbackArgs))
        {
        }

        Entry& operator=(Entry&& that) noexcept
        {
            if (this != &that)
            {
                objectHandle = std::move(that.objectHandle);
                callbackArgs = std::move(that.callbackArgs);
            }
            return *this;
        }

        UniquePersistent<Object> objectHandle;
        std::unique_ptr<WeakCallbackArgs> callbackArgs;
    };

    static void managed_destroy(const WeakCallbackInfo<WeakCallbackArgs>& info);
    void RemoveEntry(int id);

    // Context that owns this object
    JsContext* context_;

    // Map of (object ID -> Persistent Object handle)
    // Note that UniquePersistent automatically calls Reset() when destructed,
    // so clean-up of this entire thing is automatic: Destruction of ClrObjectManager
    // destructs proxyMap_, which in turn destructs each UniquePersistent handle,
    // thus removing the reference to the V8 Object.
    std::unordered_map<int, Entry> proxyMap_;
};

