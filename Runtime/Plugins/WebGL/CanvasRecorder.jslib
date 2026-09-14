mergeInto(LibraryManager.library, {

    JS_StartCanvasRecorder: function (fps) {
        console.log("[CanvasRecorder JS] JS_StartCanvasRecorder called with FPS target:", fps);
        try {
            // 1. Clean up any stuck or active MediaRecorder instance and its tracks before starting a new session
            if (window.mediaRecorder) {
                console.log("[CanvasRecorder JS] Existing window.mediaRecorder instance found. State:", window.mediaRecorder.state);
                if (window.mediaRecorder.state !== "inactive") {
                    console.warn("[CanvasRecorder JS] Stopping active MediaRecorder before starting a new session.");
                    window.mediaRecorder.stop();
                }
                if (window.mediaRecorder.stream) {
                    console.log("[CanvasRecorder JS] Cleaning up existing stream tracks...");
                    window.mediaRecorder.stream.getTracks().forEach(function (track) {
                        console.log("[CanvasRecorder JS] Stopping active track:", track.id, "| Kind:", track.kind, "| ReadyState:", track.readyState);
                        track.stop();
                    });
                }
            } else {
                console.log("[CanvasRecorder JS] No previous window.mediaRecorder instance detected.");
            }

            var canvas = document.querySelector("#unity-canvas") || document.querySelector("canvas");
            if (!canvas) {
                console.error("[CanvasRecorder JS] CRITICAL: WebGL Canvas element (#unity-canvas or <canvas>) not found in DOM!");
                return;
            }
            console.log("[CanvasRecorder JS] Canvas element located:", canvas);

            window.canvasRecorderChunks = [];
            console.log("[CanvasRecorder JS] Capturing stream from canvas at", fps, "FPS...");
            var stream = canvas.captureStream(fps);

            var tracks = stream.getTracks();
            console.log("[CanvasRecorder JS] Stream created with", tracks.length, "track(s):");
            tracks.forEach(function (track, idx) {
                console.log("  Track [" + idx + "]: id=" + track.id + ", kind=" + track.kind + ", state=" + track.readyState + ", enabled=" + track.enabled);
            });
            
            var options = { mimeType: 'video/webm;codecs=vp8' };
            if (!MediaRecorder.isTypeSupported(options.mimeType)) {
                console.warn("[CanvasRecorder JS] 'video/webm;codecs=vp8' is not supported on this browser. Falling back to default 'video/webm'.");
                options = { mimeType: 'video/webm' };
            } else {
                console.log("[CanvasRecorder JS] Codec selected: video/webm;codecs=vp8");
            }

            window.mediaRecorder = new MediaRecorder(stream, options);
            console.log("[CanvasRecorder JS] MediaRecorder instantiated. Initial state:", window.mediaRecorder.state);

            window.mediaRecorder.ondataavailable = function (event) {
                if (event.data && event.data.size > 0) {
                    console.log("[CanvasRecorder JS] Chunk received. Size:", event.data.size, "bytes.");
                    window.canvasRecorderChunks.push(event.data);
                } else {
                    console.warn("[CanvasRecorder JS] Data available event fired with empty/zero-size chunk.");
                }
            };

            window.mediaRecorder.onerror = function (event) {
                console.error("[CanvasRecorder JS] MediaRecorder Error encountered:", event.error || event);
            };

            window.mediaRecorder.start(100);
            console.log("[CanvasRecorder JS] MediaRecorder.start(100) invoked. New state:", window.mediaRecorder.state);
        } catch (e) {
            console.error("[CanvasRecorder JS] Exception caught during JS_StartCanvasRecorder:", e);
        }
    },

    JS_StopCanvasRecorder: function (csharpCallbackPtr) {
        console.log("[CanvasRecorder JS] JS_StopCanvasRecorder called. Callback Pointer:", csharpCallbackPtr);

        if (!window.mediaRecorder) {
            console.warn("[CanvasRecorder JS] JS_StopCanvasRecorder called, but window.mediaRecorder is undefined.");
            return;
        }

        console.log("[CanvasRecorder JS] Current MediaRecorder state before stop:", window.mediaRecorder.state);
        if (window.mediaRecorder.state === "inactive") {
            console.warn("[CanvasRecorder JS] MediaRecorder is already inactive.");
            return;
        }

        window.mediaRecorder.onstop = function () {
            console.log("[CanvasRecorder JS] MediaRecorder.onstop event fired.");
            console.log("[CanvasRecorder JS] Total chunks collected:", window.canvasRecorderChunks.length);

            // Explicitly stop all stream tracks so Safari/Chrome release canvas capture completely
            if (window.mediaRecorder.stream) {
                var tracks = window.mediaRecorder.stream.getTracks();
                console.log("[CanvasRecorder JS] Teardown: Stopping", tracks.length, "stream track(s)...");
                tracks.forEach(function (track) {
                    console.log("  Stopping track:", track.id, "| Previous state:", track.readyState);
                    track.stop();
                    console.log("  Track state after stop():", track.readyState);
                });
            } else {
                console.warn("[CanvasRecorder JS] No active stream object found on mediaRecorder to stop tracks.");
            }

            console.log("[CanvasRecorder JS] Assembling WebM Blob from chunks...");
            var blob = new Blob(window.canvasRecorderChunks, { type: 'video/webm' });
            console.log("[CanvasRecorder JS] Blob generated. Total binary size:", blob.size, "bytes.");
            window.canvasRecorderChunks = [];

            var reader = new FileReader();
            reader.onloadstart = function () {
                console.log("[CanvasRecorder JS] FileReader loading started...");
            };

            reader.onloadend = function () {
                console.log("[CanvasRecorder JS] FileReader finished loading ArrayBuffer.");
                var arrayBuffer = reader.result;
                var uint8Array = new Uint8Array(arrayBuffer);

                var bufferSize = uint8Array.length;
                console.log("[CanvasRecorder JS] Allocating WASM memory via _malloc for", bufferSize, "bytes...");
                var bufferPtr = _malloc(bufferSize);
                console.log("[CanvasRecorder JS] WASM Memory Pointer allocated at address:", bufferPtr);

                HEAPU8.set(uint8Array, bufferPtr);
                console.log("[CanvasRecorder JS] Byte array copied into WASM HEAPU8 memory.");

                // Emscripten dynCall wrapper with 'vii' signature (void return, two int parameters: ptr and size)
                if (typeof Module !== 'undefined' && typeof Module.dynCall === 'function') {
                    console.log("[CanvasRecorder JS] Invoking C# callback via Module.dynCall('vii',", csharpCallbackPtr, ", [", bufferPtr, ",", bufferSize, "])");
                    Module.dynCall('vii', csharpCallbackPtr, [bufferPtr, bufferSize]);
                } else if (typeof dynCall === 'function') {
                    console.log("[CanvasRecorder JS] Invoking C# callback via fallback dynCall('vii', ...)");
                    dynCall('vii', csharpCallbackPtr, [bufferPtr, bufferSize]);
                } else {
                    console.error("[CanvasRecorder JS] CRITICAL: Neither Module.dynCall nor dynCall is available in this Unity WebGL build!");
                }

                console.log("[CanvasRecorder JS] Freeing WASM memory buffer at address:", bufferPtr);
                _free(bufferPtr);
                console.log("[CanvasRecorder JS] WASM memory buffer freed cleanly.");
            };

            reader.onerror = function (err) {
                console.error("[CanvasRecorder JS] FileReader encountered an error:", err);
            };

            reader.readAsArrayBuffer(blob);
        };

        console.log("[CanvasRecorder JS] Invoking mediaRecorder.stop()...");
        window.mediaRecorder.stop();
    }
});