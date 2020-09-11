#include <cstring>
#include <iostream>
#include <cassert>

#include "vroomjs.h"
#include "JsEngine.h"
#include "JsContext.h"
#include "HostObjectTemplate.h"


long js_mem_debug_engine_count;

static const int Mega = 1024 * 1024;


JsEngine::JsEngine(int32_t max_young_space, int32_t max_old_space)
{
    allocator_ = v8::ArrayBuffer::Allocator::NewDefaultAllocator();

    Isolate::CreateParams create_params;
    create_params.array_buffer_allocator = allocator_;

    isolate_ = Isolate::New(create_params);

    // todo: is there a perf penalty for this? maybe it should be an option on the engine
    isolate_->SetCaptureStackTraceForUncaughtExceptions(true);

    // todo: SetResourceConstraints doesn't seem to exist anymore
    //isolate_->Enter();
    //if (max_young_space > 0 && max_old_space > 0) {
    //	v8::ResourceConstraints constraints;
    //	constraints.set_max_young_space_size(max_young_space * Mega);
    //	constraints.set_max_old_space_size(max_old_space * Mega);
    //
    //	v8::SetResourceConstraints(&constraints);
    //}
    //isolate_->Exit();

    // Do this last, in case anything above fails
    INCREMENT(js_mem_debug_engine_count);
}

void JsEngine::TerminateExecution()
{
    isolate_->TerminateExecution();
}

void JsEngine::DumpHeapStats()
{
    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);

    // gc first.
    // TODO: IdleNotification no longer exists?
    //while(!V8::IdleNotification()) {};

    HeapStatistics stats;
    isolate_->GetHeapStatistics(&stats);
    std::wcout << "Heap size limit " << (stats.heap_size_limit() / Mega) << std::endl;
    std::wcout << "Total heap size " << (stats.total_heap_size() / Mega) << std::endl;
    std::wcout << "Heap size executable " << (stats.total_heap_size_executable() / Mega) << std::endl;
    std::wcout << "Total physical size " << (stats.total_physical_size() / Mega) << std::endl;
    std::wcout << "Used heap size " << (stats.used_heap_size() / Mega) << std::endl;
}

JsContext* JsEngine::NewContext(int32_t id)
{
    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    HandleScope scope(isolate_);

    auto context = new JsContext(id, this);
    RegisterOwnedDisposable(context);
    return context;
}

int JsEngine::AddTemplate(hostobjectcallbacks callbacks)
{
    templates_.push_back(new HostObjectTemplate(isolate_, callbacks));
    return templates_.size() - 1; // template id
}

void JsEngine::DisposeCore()
{
    // Templates must be deleted before the isolate is disposed.
    for (int i = 0; i < templates_.size(); i++)
        delete templates_[i];

    isolate_->Dispose();
    // Isolates can only be Dispose()'d, not deleted
    isolate_ = nullptr;

    delete allocator_;
    allocator_ = nullptr;
}

// todo: remove unused method
void JsEngine::DisposeObject(Persistent<Object>* obj)
{
    assert(obj != nullptr);

    // todo: not sure we actually need this stuff just to Reset a Persistent handle
    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);

    obj->Reset();
}




