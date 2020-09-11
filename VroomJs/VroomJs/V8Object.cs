using System;
using System.Runtime.InteropServices;

namespace VroomJs
{
    public abstract class V8Object
    {
        private readonly V8Object _owner;
        private bool _disposed;

        protected V8Object(V8Object owner = null)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            DisposeCore();

            _owner?.OwnedObjectDisposed(this);
        }

        protected V8Object Owner => _owner;

        protected virtual void DisposeCore()
        {

        }

        protected internal void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            _owner?.CheckDisposed();
        }

        protected virtual void OwnedObjectDisposed(V8Object ownedObject)
        {

        }
    }

    public abstract class V8Object<THandle> : V8Object, IDisposable
        where THandle : SafeHandle
    {
        protected V8Object(THandle handle, V8Object owner = null)
            :base(owner)
        {
            Handle = handle ?? throw new ArgumentNullException(nameof(handle));
        }

        internal THandle Handle { get; }

        protected override void DisposeCore()
        {
            Handle.Dispose();

            base.DisposeCore();
        }
    }
}
