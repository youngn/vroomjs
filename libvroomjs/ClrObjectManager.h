#pragma once

#include "vroomjs.h"
#include <unordered_map>

class JsContext;
class ClrObjectRef;

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

        Entry(UniquePersistent<Object>&& objectHandle,
            std::unique_ptr<ClrObjectRef>&& clrObjectRef,
            std::unique_ptr<WeakCallbackArgs>&& weakCallbackArgs) :
            objectHandle(std::move(objectHandle)),
            clrObjectRef(std::move(clrObjectRef)),
            weakCallbackArgs(std::move(weakCallbackArgs))
        {
        }

        // no copying
        Entry(Entry const&) = delete;
        Entry& operator=(Entry const&) = delete;

        // only moving
        Entry(Entry&& that) noexcept
            : objectHandle(std::move(that.objectHandle)),
            clrObjectRef(std::move(that.clrObjectRef)),
            weakCallbackArgs(std::move(that.weakCallbackArgs))
        {
        }

        Entry& operator=(Entry&& that) noexcept
        {
            if (this != &that)
            {
                objectHandle = std::move(that.objectHandle);
                clrObjectRef = std::move(that.clrObjectRef);
                weakCallbackArgs = std::move(that.weakCallbackArgs);
            }
            return *this;
        }

        // Persistent handle to the proxy object. Note that UniquePersistent automatically
        // Reset()s upon destruction.
        UniquePersistent<Object> objectHandle;

        // The ClrObjectRef is only stored here so that it is deterministically deleted
        // regardless of whether the callback is ever invoked.
        std::unique_ptr<ClrObjectRef> clrObjectRef;

        // The WeakCallbackArgs is only stored here so that it is deterministically deleted
        // regardless of whether the callback is ever invoked.
        std::unique_ptr<WeakCallbackArgs> weakCallbackArgs;
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

