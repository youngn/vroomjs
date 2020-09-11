#pragma once

#include <forward_list>
#include <cassert>

class Disposable
{
public:
    Disposable()
        :disposed_(false), owner_(nullptr)
    {
    }

    void Dispose()
    {
        Dispose(true);
    };

    void RegisterOwnedDisposable(Disposable* disposable) {
        assert(disposable->owner_ == nullptr);
        disposable->owner_ = this;
        ownedDisposables_.push_front(disposable);
    }

    virtual ~Disposable() { }

protected:
    virtual void DisposeCore() = 0;


private:
    void Dispose(bool notifyOwner) {

        if (disposed_)
            return;

        // Dispose owned objects
        for (auto it = ownedDisposables_.begin(); it != ownedDisposables_.end(); ++it) {
            (*it)->Dispose(false);
        }
        ownedDisposables_.clear();

        // Dispose core
        DisposeCore();

        // Possibly notify owner
        if (owner_ && notifyOwner) {
            owner_->OwnedObjectDisposed(this);
        }

        disposed_ = true;
    }

    void OwnedObjectDisposed(Disposable* disposable) {
        ownedDisposables_.remove(disposable);
    }

    Disposable* owner_;
    std::forward_list<Disposable*> ownedDisposables_;
    bool disposed_;
};
