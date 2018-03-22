// Copyright (C) 2018 The Regents of the University of California (Regents).
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//
//     * Redistributions in binary form must reproduce the above
//       copyright notice, this list of conditions and the following
//       disclaimer in the documentation and/or other materials provided
//       with the distribution.
//
//     * Neither the name of The Regents or University of California nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDERS OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.
//
// Please contact the author of this library if you have any questions.
// Author: Samuel Dong (samuel_dong@umail.ucsb.edu)
using System;

namespace Realtime_Hololens_Retexturing.Common
{
    // A base class that tracks resources allocated by native code. This class is
    // used to release COM references to DirectX resources.
    public abstract class Disposer : IDisposable
    {
        #region Public methods

        // Releases resources allocated by native code (unmanaged resources).
        // All disposable objects that were added to the collection will be disposed of
        // when this method is called.
        public void Dispose()
        {
            if (!IsDisposed)
            {
                Dispose(true);
                IsDisposed = true;
            }
        }

        #endregion


        #region Protected methods and properties
        
        protected internal Disposer()
        {
        }
        
        // Indicates whether this instance is already disposed.
        protected internal bool IsDisposed { get; private set; }

        // Disposes all IDisposable object resources in the collection of disposable 
        // objects.
        // 
        // NOTE: Since this class exists to dispose of unmanaged resources, the 
        //       disposeManagedResources parameter is ignored.
        protected virtual void Dispose(bool disposeManagedResources)
        {
            // If the DisposeCollector exists, have it dispose of all COM objects.
            if (!IsDisposed && DisposeCollector != null)
            {
                DisposeCollector.Dispose();
            }

            // The DisposeCollector is done, and can be discarded.
            DisposeCollector = null;
        }
        
        // Adds an IDisposable object to the collection of disposable objects.
        protected internal T ToDispose<T>(T objectToDispose)
        {
            // If objectToDispose is not null, add it to the collection.
            if (!ReferenceEquals(objectToDispose, null))
            {
                // Create DisposeCollector if it doesn't already exist.
                if (DisposeCollector == null)
                {
                    DisposeCollector = new SharpDX.DisposeCollector();
                    IsDisposed = false;
                }

                return DisposeCollector.Collect(objectToDispose);
            }

            // Otherwise, return a default instance of type T.
            return default(T);
        }

        // Disposes of an IDisposable object immediately and also removes the object from the
        // collection of disposable objects.
        protected internal void RemoveAndDispose<T>(ref T objectToDispose)
        {
            // If objectToDispose is not null, and if the DisposeCollector is available, have 
            // the DisposeCollector get rid of objectToDispose.
            if (!ReferenceEquals(objectToDispose, null) && (DisposeCollector != null))
            {
                DisposeCollector.RemoveAndDispose(ref objectToDispose);
            }
        }
        
        // Removes an IDisposable object from the collection of disposable objects. Does not 
        // dispose of the object before removing it.
        protected internal void RemoveToDispose<T>(T objectToRemove)
        {
            // If objectToRemove is not null, have the DisposeCollector forget about it.
            if (!ReferenceEquals(objectToRemove, null) && (DisposeCollector != null))
            {
                DisposeCollector.Remove(objectToRemove);
            }
        }

        #endregion


        #region Private properties

        // The collection of disposable objects.
        private SharpDX.DisposeCollector DisposeCollector { get; set; }

        #endregion
    }
}
