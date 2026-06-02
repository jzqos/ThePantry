
let html5QrCode;
let lastScannedCode = "";
let lastScannedTime = 0;
const COOLDOWN_MS = 1000;
let _dotNetHelper = null;
let _elementId = null;
let _paused = false;

function getQrBox() {
    return { width: 250, height: 150 };
}

window.barcodeScanner = {
    start: async function (dotNetHelper, elementId) {
        _dotNetHelper = dotNetHelper;
        _elementId = elementId;
        _paused = false;

        if (html5QrCode) {
            await this.stop();
        }

        // Build format list at call-time so the global is guaranteed to exist.
        // Falls back to no restriction (scans everything) if the enum isn't available.
        let scannerConfig = {};
        try {
            if (typeof Html5QrcodeSupportedFormats !== 'undefined') {
                scannerConfig.formatsToSupport = [
                    Html5QrcodeSupportedFormats.QR_CODE,
                    Html5QrcodeSupportedFormats.EAN_13,
                    Html5QrcodeSupportedFormats.EAN_8,
                    Html5QrcodeSupportedFormats.UPC_A,
                    Html5QrcodeSupportedFormats.UPC_E,
                    Html5QrcodeSupportedFormats.CODE_128,
                    Html5QrcodeSupportedFormats.CODE_39,
                    Html5QrcodeSupportedFormats.ITF,
                    Html5QrcodeSupportedFormats.DATA_MATRIX,
                    Html5QrcodeSupportedFormats.AZTEC,
                ];
            }
        } catch(e) { console.warn("Could not set scan formats:", e); }

        html5QrCode = new Html5Qrcode(elementId, scannerConfig);
        const config = { fps: 10, qrbox: getQrBox() };

        try {
            await html5QrCode.start(
                { facingMode: "environment" },
                config,
                (decodedText, decodedResult) => {
                    if (_paused) return;
                    const now = Date.now();
                    if (decodedText !== lastScannedCode || (now - lastScannedTime) > COOLDOWN_MS) {
                        lastScannedCode = decodedText;
                        lastScannedTime = now;

                        // Audio feedback
                        const playBeep = () => {
                            try {
                                const audioCtx = new (window.AudioContext || window.webkitAudioContext)();
                                const oscillator = audioCtx.createOscillator();
                                const gainNode = audioCtx.createGain();
                                oscillator.connect(gainNode);
                                gainNode.connect(audioCtx.destination);
                                oscillator.type = 'sine';
                                oscillator.frequency.setValueAtTime(880, audioCtx.currentTime);
                                gainNode.gain.setValueAtTime(0.1, audioCtx.currentTime);
                                gainNode.gain.exponentialRampToValueAtTime(0.01, audioCtx.currentTime + 0.1);
                                oscillator.start();
                                oscillator.stop(audioCtx.currentTime + 0.1);
                            } catch (e) { console.error("Web Audio fallback failed", e); }
                        };

                        try {
                            const audio = new Audio('/beep.mp3');
                            audio.play().catch(err => {
                                console.warn("MP3 play failed, using fallback", err);
                                playBeep();
                            });
                        } catch (e) {
                            playBeep();
                        }

                        // Capture image from video stream
                        let imageData = null;
                        try {
                            const video = document.querySelector(`#${elementId} video`);
                            if (video) {
                                const canvas = document.createElement('canvas');
                                canvas.width = video.videoWidth;
                                canvas.height = video.videoHeight;
                                const ctx = canvas.getContext('2d');
                                ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
                                imageData = canvas.toDataURL('image/jpeg', 0.3);
                            }
                        } catch (e) {
                            console.error("Failed to capture image", e);
                        }

                        // Pause immediately so no duplicate fires while .NET is processing
                        _paused = true;

                        console.log("Barcode detected:", decodedText);
                        dotNetHelper.invokeMethodAsync('HandleBarcodeDetected', decodedText, imageData)
                            .then(() => {
                                const element = document.getElementById(elementId);
                                if (element) {
                                    element.style.border = "5px solid #28a745";
                                    setTimeout(() => element.style.border = "none", 500);
                                }
                            })
                            .catch(err => {
                                console.error("Error invoking HandleBarcodeDetected:", err);
                                const element = document.getElementById(elementId);
                                if (element) {
                                    element.style.border = "5px solid #dc3545";
                                    setTimeout(() => element.style.border = "none", 500);
                                }
                            });
                    }
                },
                (errorMessage) => {
                    // parse error, ignore it.
                }
            );
        } catch (err) {
            console.error("Unable to start scanning", err);
            throw err;
        }

        // Restart scanner on orientation change so the camera feed resets cleanly
        window.addEventListener('orientationchange', barcodeScanner._handleOrientationChange);
    },

    _handleOrientationChange: async function () {
        if (!_dotNetHelper || !_elementId) return;
        // Small delay to let the browser finish rotating
        setTimeout(async () => {
            await barcodeScanner.stop();
            await barcodeScanner.start(_dotNetHelper, _elementId);
        }, 400);
    },

    pauseScanning: function () {
        _paused = true;
    },

    resetCooldown: function () {
        _paused = false;
        // Keep lastScannedCode so the same barcode still has a 1-second cooldown.
        // Without this, dismissing the unknown-item card while the barcode is still
        // in view immediately re-processes the same item.
        lastScannedTime = Date.now();
    },

    stop: async function () {
        window.removeEventListener('orientationchange', barcodeScanner._handleOrientationChange);
        if (html5QrCode && html5QrCode.isScanning) {
            await html5QrCode.stop();
            html5QrCode = null;
        }
    },

    // Capture a still frame from the running scanner's video feed
    captureFrame: function (elementId) {
        try {
            const video = document.querySelector(`#${elementId} video`);
            if (!video) return null;
            const canvas = document.createElement('canvas');
            canvas.width = video.videoWidth;
            canvas.height = video.videoHeight;
            canvas.getContext('2d').drawImage(video, 0, 0);
            return canvas.toDataURL('image/jpeg', 0.8);
        } catch (e) {
            console.error("captureFrame failed", e);
            return null;
        }
    }
};
