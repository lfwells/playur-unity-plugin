mergeInto(LibraryManager.library, {

    JS_StartCanvasRecorder: function (fps) {
        try {
            // 1. Clean up any stuck or active MediaRecorder instance and its tracks before starting a new session
            if (window.mediaRecorder) {
                if (window.mediaRecorder.state !== "inactive") {
                    console.warn("[CanvasRecorder] Stopping active MediaRecorder before starting a new session.");
                    window.mediaRecorder.stop();
                }
                if (window.mediaRecorder.stream) {
                    window.mediaRecorder.stream.getTracks().forEach(function (track) {
                        track.stop();
                    });
                }
            }

            var canvas = document.querySelector("#unity-canvas") || document.querySelector("canvas");
            if (!canvas) {
                console.error("[CanvasRecorder] WebGL Canvas element not found.");
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
            console.log("[CanvasRecorder] MediaRecorder started successfully.");
        } catch (e) {
            console.error("[CanvasRecorder] Failed to start MediaRecorder: ", e);
        }
    },

    JS_StopCanvasRecorder: function (csharpCallbackPtr) {
        if (!window.mediaRecorder || window.mediaRecorder.state === "inactive") {
            console.warn("[CanvasRecorder] No active MediaRecorder found to stop.");
            return;
        }

        window.mediaRecorder.onstop = function () {
            // 2. Explicitly stop all stream tracks so Safari and Chrome release canvas capture completely
            if (window.mediaRecorder.stream) {
                window.mediaRecorder.stream.getTracks().forEach(function (track) {
                    track.stop();
                });
            }

            var blob = new Blob(window.canvasRecorderChunks, { type: 'video/webm' });
            window.canvasRecorderChunks = [];

            var reader = new FileReader();
            reader.onloadend = function () {
                var arrayBuffer = reader.result;
                var uint8Array = new Uint8Array(arrayBuffer);

                var bufferSize = uint8Array.length;
                var bufferPtr = _malloc(bufferSize);
                HEAPU8.set(uint8Array, bufferPtr);

                // 3. Emscripten dynCall wrapper to prevent 'Cannot read properties of undefined (reading apply)'
                if (typeof Module !== 'undefined' && typeof Module.dynCall === 'function') {
                    Module.dynCall('vii', csharpCallbackPtr, [bufferPtr, bufferSize]);
                } else if (typeof dynCall === 'function') {
                    dynCall('vii', csharpCallbackPtr, [bufferPtr, bufferSize]);
                } else {
                    console.error("[CanvasRecorder] dynCall is unavailable in this Unity WebGL build version.");
                }

                _free(bufferPtr);
            };
            reader.readAsArrayBuffer(blob);
        };

        window.mediaRecorder.stop();
    }
});