// This file is part of the VroomJs library.
//
// Author:
//     Federico Di Gregorio <fog@initd.org>
//
// Copyright (c) 2013 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

namespace VroomJs
{
    public class KeepAliveDictionaryStore : IKeepAliveStore
    {
        private Dictionary<int, object> _forwardLookup = new Dictionary<int,object>();
        private Dictionary<object, int> _reverseLookup = new Dictionary<object, int>();

        private int _idCounter = 0;

        public int MaxSlots => int.MaxValue;

        public int AllocatedSlots => _forwardLookup.Count;

        public int UsedSlots => _forwardLookup.Count;

        public int Insert(object obj)
        {
            int id;
            if (_reverseLookup.TryGetValue(obj, out id))
                return id;

            id = _idCounter++;

            _forwardLookup.Add(id, obj);
            _reverseLookup.Add(obj, id);

            return id;
        }

        public object Get(int id)
        {
            if (_forwardLookup.TryGetValue(id, out object obj))
                return obj;

            throw new InvalidOperationException("Object ID not found.");
        }

        public void Remove(int id)
        {
            if (!_forwardLookup.TryGetValue(id, out object obj))
                return;

            _forwardLookup.Remove(id);
            _reverseLookup.Remove(obj);

            // todo: not sure if we want this to be responsible for disposal?
            //var disposable = obj as IDisposable;
            //if (disposable != null)
            //    disposable.Dispose();
        }

        public void Clear()
        {
            _forwardLookup.Clear();
            _reverseLookup.Clear();
        }
    }
}
