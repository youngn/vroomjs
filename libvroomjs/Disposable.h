#pragma once

class Disposable
{
public:
    virtual void Dispose() = 0;
    virtual bool IsDisposed() = 0;
    virtual ~Disposable() { }
};
