mergeInto(LibraryManager.library, {

    JS_StartCanvasRecorder: function (fps) {
        try {
            var canvas = document.querySelector("#unity-canvas") || document.querySelector("canvas");
            if (!canvas) {
                console.error("WebGL Canvas element not found.");
                return;
            }

            window.canvasRecorderChunks = [];
            var stream = canvas.captureStream(fps);
            
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

            window.mediaRecorder.start(100);
        } catch (e) {
            console.error("Failed to start MediaRecorder: ", e);
        }
    },

    JS_StopCanvasRecorder: function (csharpCallbackPtr) {
        if (!window.mediaRecorder) {
            console.warn("No active MediaRecorder found.");
            return;
        }

        window.mediaRecorder.onstop = function () {
            var blob = new Blob(window.canvasRecorderChunks, { type: 'video/webm' });
            window.canvasRecorderChunks = [];

            var reader = new FileReader();
            reader.onloadend = function () {
                var arrayBuffer = reader.result;
                var uint8Array = new Uint8Array(arrayBuffer);

                var bufferSize = uint8Array.length;
                var bufferPtr = _malloc(bufferSize);
                HEAPU8.set(uint8Array, bufferPtr);

                // ✅ FIX: Use Module.dynCall with signature 'vii' (void, int, int) 
                // instead of raw dynCall which throws 'Cannot read properties of undefined'
                if (typeof Module.dynCall === 'function') {
                    Module.dynCall('vii', csharpCallbackPtr, [bufferPtr, bufferSize]);
                } else if (typeof dynCall === 'function') {
                    dynCall('vii', csharpCallbackPtr, [bufferPtr, bufferSize]);
                } else {
                    console.error("dynCall is unavailable in this Unity WebGL build version.");
                }

                _free(bufferPtr);
            };
            reader.readAsArrayBuffer(blob);
        };

        window.mediaRecorder.stop();
    }
});