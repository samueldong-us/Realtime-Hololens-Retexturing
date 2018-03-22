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
using Windows.UI.Input.Spatial;

namespace Realtime_Hololens_Retexturing.Common
{
    // Sample gesture handler.
    // Hooks up events to recognize a tap gesture, and keeps track of input using a boolean value.
    public class SpatialInputHandler
    {
        // API objects used to process gesture input, and generate gesture events.
        private SpatialInteractionManager interactionManager;

        // Used to indicate that a Pressed input event was received this frame.
        private SpatialInteractionSourceState sourceState;

        // Creates and initializes a GestureRecognizer that listens to a Person.
        public SpatialInputHandler()
        {
            // The interaction manager provides an event that informs the app when
            // spatial interactions are detected.
            interactionManager = SpatialInteractionManager.GetForCurrentView();

            // Bind a handler to the SourcePressed event.
            interactionManager.SourcePressed += OnSourcePressed;

            //
            // TODO: Expand this class to use other gesture-based input events as applicable to
            //       your app.
            //
        }

        // Checks if the user performed an input gesture since the last call to this method.
        // Allows the main update loop to check for asynchronous changes to the user
        // input state.
        public SpatialInteractionSourceState CheckForInput()
        {
            SpatialInteractionSourceState sourceState = this.sourceState;
            this.sourceState = null;
            return sourceState;
        }

        public void OnSourcePressed(SpatialInteractionManager sender, SpatialInteractionSourceEventArgs args)
        {
            sourceState = args.State;

            //
            // TODO: In your app or game engine, rewrite this method to queue
            //       input events in your input class or event handler.
            //
        }
    }
}