var PurrVoicePlugin = {
	PurrVoice_RequestPermission: function() {
		var goName = "PurrMicrophone";

		if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia ||
			!navigator.permissions || !navigator.permissions.query) {
			SendMessage(goName, "OnMicPermission", 0);
			return;
		}

		navigator.mediaDevices.getUserMedia({ audio: true }).then(function() {
			SendMessage(goName, "OnMicPermission", 1);
			navigator.permissions.query({ name: 'microphone' }).then(function(result) {
				// Listen for changes
				result.onchange = function() {
					var granted = result.state === 'granted' ? 1 : 0;
					SendMessage(goName, "OnMicPermission", granted);
				};
			}).catch(function(err) {
				// Handle error
			});
		}).catch(function(err) {
			SendMessage(goName, "OnMicPermission", 0);
			navigator.permissions.query({ name: 'microphone' }).then(function(result) {
				// Listen for changes
				result.onchange = function() {
					var granted = result.state === 'granted' ? 1 : 0;
					SendMessage(goName, "OnMicPermission", granted);
				};
			}).catch(function(err) {
				// Handle error
			});
		});
	},

	PurrVoice_EnumerateDevices: function() {
		var goName = "PurrMicrophone";

		if (!navigator.mediaDevices || !navigator.mediaDevices.enumerateDevices) {
			SendMessage(goName, "OnMicDevices", "{\"items\":[]}");
			return;
		}

		navigator.mediaDevices.enumerateDevices().then(function(devices) {
			var mics = devices.filter(function(d) {
				return d.kind === "audioinput";
			});

			var result = {
				items: mics.map(function (d) {
					return {
						id: d.deviceId,
						displayName: d.label || ""
					};
				})
			};

			SendMessage(goName, "OnMicDevices", JSON.stringify(result));
		}).catch(function(err) {
			// Handle error
		});
	},

	PurrVoice_StartRecording: function(deviceIdPtr, sampleRate, functionPtr) {
		var goName = "PurrMicrophone";
		var deviceId = UTF8ToString(deviceIdPtr);

		if (window._purrVoiceStreams === undefined)
			window._purrVoiceStreams = {};

		if (window._purrVoiceRecorders === undefined)
			window._purrVoiceRecorders = {};

		if (window._purrVoiceAudioCtxs === undefined)
			window._purrVoiceAudioCtxs = {};

		if (window._purrVoiceBuffers === undefined)
			window._purrVoiceBuffers = {};

		var getStream = function(callback) {
			if (window._purrVoiceStreams[deviceId]) {
				callback(window._purrVoiceStreams[deviceId]);
				return;
			}
			navigator.mediaDevices.getUserMedia({
				audio: { deviceId: { exact: deviceId } }
			}).then(function(stream) {
				window._purrVoiceStreams[deviceId] = stream;
				callback(stream);
			}).catch(function(err) {
				console.error("getUserMedia error for device", deviceId, err);
			});
		};

		getStream(function(stream) {
			// Create AudioContext with default sample rate (matches stream)
			var audioCtx = new (window.AudioContext || window.webkitAudioContext)();
			// Optionally, log the actual sample rate:
			// console.log("AudioContext sample rate:", audioCtx.sampleRate);

			var source = audioCtx.createMediaStreamSource(stream);

			var bufferSize = 2048;
			var recorder = audioCtx.createScriptProcessor(bufferSize, 1, 1);

			recorder.onaudioprocess = function(e) {
				var input = e.inputBuffer.getChannelData(0); // Float32Array

				var byteLength = input.length * 4; // 4 bytes per float
				var ptr = window._purrVoiceBuffers[deviceId];
				if (!ptr) {
					ptr = _malloc(byteLength);
					window._purrVoiceBuffers[deviceId] = ptr;
				} else if (window._purrVoiceBuffers[deviceId + "_size"] !== byteLength) {
					_free(ptr);
					ptr = _malloc(byteLength);
					window._purrVoiceBuffers[deviceId] = ptr;
				}
				window._purrVoiceBuffers[deviceId + "_size"] = byteLength;

				var heap = new Float32Array(HEAPF32.buffer, ptr, input.length);
				heap.set(input);
				var deviceIdPtr = _malloc(deviceId.length + 1);
				stringToUTF8(deviceId, deviceIdPtr, deviceId.length + 1);

				dynCall(
					"viiii",
					functionPtr,
					[ptr, input.length, audioCtx.sampleRate, deviceIdPtr]
				);

				_free(deviceIdPtr);
			};

			source.connect(recorder);
			recorder.connect(audioCtx.destination);

			if (audioCtx.state === "suspended") {
				audioCtx.resume();
			}

			window._purrVoiceRecorders[deviceId] = recorder;
			window._purrVoiceAudioCtxs[deviceId] = audioCtx;
		});
	},

	PurrVoice_StopRecording: function(deviceIdPtr) {
		if (window._purrVoiceStreams === undefined) {
			window._purrVoiceStreams = {};
		}
		if (window._purrVoiceRecorders === undefined) {
			window._purrVoiceRecorders = {};
		}
		if (window._purrVoiceAudioCtxs === undefined) {
			window._purrVoiceAudioCtxs = {};
		}

		var deviceId = UTF8ToString(deviceIdPtr);

		var recorder = window._purrVoiceRecorders[deviceId];
		if (recorder) {
			recorder.disconnect();
			delete window._purrVoiceRecorders[deviceId];
		}
		var audioCtx = window._purrVoiceAudioCtxs[deviceId];
		if (audioCtx) {
			audioCtx.close();
			delete window._purrVoiceAudioCtxs[deviceId];
		}
		var stream = window._purrVoiceStreams[deviceId];
		if (stream) {
			stream.getTracks().forEach(function(track) { track.stop(); });
			delete window._purrVoiceStreams[deviceId];
		}
	}
};

mergeInto(LibraryManager.library, PurrVoicePlugin);
