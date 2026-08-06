mergeInto(LibraryManager.library, {

    // Starts recording the HTML5 canvas using MediaRecorder API
    JS_StartCanvasRecorder: function (fps) {
        try {
            // Locate the WebGL canvas element
            var canvas = document.querySelector("#unity-canvas") || document.querySelector("canvas");
            if (!canvas) {
                console.error("[CanvasRecorder] WebGL Canvas element not found.");
                return;
            }

            window.canvasRecorderChunks = [];
            
            // Capture stream at designated FPS
            var stream = canvas.captureStream(fps);
            
            // Prefer webm format
            var options = { mimeType: 'video/webm;codecs=vp8' };
            if (!MediaRecorder.isTypeSupported(options.mimeType)) {
                options = { mimeType: 'video/webm' };
            }

            window.mediaRecorder = new MediaRecorder(stream, options);

            window.mediaRecorder.ondataavailable = function (event) {
                if (event.data && event.data.size > 0) {
                    window.canvasRecorderChunks.push(event.data);
                }
            };

            window.mediaRecorder.start(100); // Grab slice every 100ms
            console.log("[CanvasRecorder] WebGL MediaRecorder started.");
        } catch (e) {
            console.error("[CanvasRecorder] Failed to start MediaRecorder: ", e);
        }
    },

    // Stops recording and sends the raw WebM byte array back to C#
    JS_StopCanvasRecorder: function (csharpCallbackPtr) {
        if (!window.mediaRecorder) {
            console.warn("[CanvasRecorder] No active MediaRecorder found.");
            // Pass null pointer and 0 length on failure
            dynCall('vpi', csharpCallbackPtr, [0, 0]);
            return;
        }

        window.mediaRecorder.onstop = function () {
            var blob = new Blob(window.canvasRecorderChunks, { type: 'video/webm' });
            window.canvasRecorderChunks = [];

            var reader = new FileReader();
            reader.onloadend = function () {
                var arrayBuffer = reader.result;
                var uint8Array = new Uint8Array(arrayBuffer);

                // Allocate memory in Emscripten heap for C#
                var bufferSize = uint8Array.length;
                var bufferPtr = _malloc(bufferSize);
                HEAPU8.set(uint8Array, bufferPtr);

                // Invoke C# delegate pointer with (bufferPtr, bufferSize)
                dynCall('vpi', csharpCallbackPtr, [bufferPtr, bufferSize]);

                // Free allocated memory
                _free(bufferPtr);
            };
            reader.readAsArrayBuffer(blob);
        };

        window.mediaRecorder.stop();
        console.log("[CanvasRecorder] WebGL MediaRecorder stopping...");
    }
});