using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Core {
	public class GpuInfo {
		public int deviceId;
		public string description = "";

		override public string ToString() {
			return $"[{deviceId}] {description}";
		}
	}

	public enum OnnxRunnerChoice {
		Default,
		CPU,
		CPUForCoreML,
	}

	public class Onnx {

		private static bool cudaAvailable = DetectCuda();

		private static readonly Dictionary<int, OrtEpDevice> devices = initializeDevices();

		private static bool DetectCuda() {
			try {
				return OS.IsLinux() && CudaGpuDetector.IsCudaAvailable() && CudaGpuDetector.IsCuDnnAvailable();
			} catch (Exception e) {
				Log.Warning(e, "CUDA detection failed. Falling back to CPU.");
				return false;
			}
		}

		// Native onnxruntime may be unavailable (missing VC++ redist on Windows, missing
		// native deps elsewhere). This runs in a static initializer, so an escaping
		// exception would poison the whole Onnx class with TypeInitializationException
		// and take down every caller, not just GPU enumeration.
		private static Dictionary<int, OrtEpDevice> initializeDevices() {
			try {
				var env = OrtEnv.Instance();
				var ortDevices = env.GetEpDevices();

				return ortDevices
					.Where(device => device.EpName.ToLower().Contains("dml"))
					.Select((device, index) => new { index, device })
					.ToDictionary(x => x.index, x => x.device);
			} catch (Exception e) {
				Log.Warning(e, "Failed to enumerate ONNX execution devices. GPU acceleration will be unavailable.");
				return new Dictionary<int, OrtEpDevice>();
			}
		}

		public static List<string> getRunnerOptions() {
			if (OS.IsWindows()) {
				return new List<string> {
				"CPU",
				"DirectML"
				};
			} else if (OS.IsMacOS()) {
				return new List<string> {
				"CPU",
				"CoreML"
				};
			} else if (cudaAvailable) {
				return new List<string> {
				"CPU",
				"CUDA"
				};
			} else if (OS.IsAndroid()) {
				return new List<string> {
				"CPU",
				"NNAPI"
				};
			}
			return new List<string> {
				"CPU"
			};
		}

		public static List<GpuInfo> getGpuInfo() {
			List<GpuInfo> gpuList = new List<GpuInfo>();
			try {
				if (cudaAvailable) {
					return CudaGpuDetector.GetCudaDevices();
				}

				if (OS.IsAndroid()) {
					return new List<GpuInfo>{new GpuInfo {
						deviceId = 0, // eliminate exception of taking OnnxGpuOptions[0]
                    }};
				}

				var env = OrtEnv.Instance();
				var ortDevices = env.GetEpDevices();

				var i = 0;
				foreach (var device in ortDevices.Where(device => device.EpName.ToLower().Contains("dml"))) {
					var description = "";
					foreach (var item in device.HardwareDevice.Metadata.Entries) {
						if (item.Key.ToLower() == "description") {
							description = $"{item.Value} ({device.HardwareDevice.Type})";
							break;
						}
					}
					if (string.IsNullOrEmpty(description)) { // fallback
						description = $"{device.EpName} {device.HardwareDevice.Vendor} ({device.HardwareDevice.Type})";
					}
					devices[i] = device;
					gpuList.Add(new GpuInfo {
						deviceId = i++,
						description = description
					});
				}
			} catch (Exception e) {
				// GPU enumeration is optional metadata for a preferences dropdown. Never let
				// a native-library failure here prevent Preferences from opening.
				Log.Warning(e, "Failed to query GPU info. GPU acceleration will be unavailable.");
			}
			return gpuList;
		}

		private static SessionOptions getOnnxSessionOptions(bool coremlEnableOnSubgraphs = false) {
			SessionOptions options = new SessionOptions();
			List<string> runnerOptions = getRunnerOptions();
			string runner = Preferences.Default.OnnxRunner;
			if (String.IsNullOrEmpty(runner)) {
				runner = runnerOptions[0];
			}
			if (!runnerOptions.Contains(runner)) {
				runner = "CPU";
			}
			switch (runner) {
				case "DirectML":
					if (devices.TryGetValue(Preferences.Default.OnnxGpu, out var d)) {
						options.AppendExecutionProvider(
							OrtEnv.Instance(),
							new List<OrtEpDevice> { d },
							new Dictionary<string, string> { }
						 );
					} else {
						Log.Warning($"DirectML device {Preferences.Default.OnnxGpu} unavailable. Falling back to CPU.");
					}
					break;
				case "CoreML":
					// Note: MLProgram format has stricter validation and may fail with complex DiffSinger models
					// that have topological sorting issues (e.g., variance_predictor with diffusion embeddings)
					// so we always use NeuralNetwork format (default) as MLProgram fails with complex models.
					options.AppendExecutionProvider("CoreML", new Dictionary<string, string> {
						{ "MLComputeUnits", "ALL" },
						{ "RequireStaticInputShapes", "1"},
						{ "ModelFormat", "NeuralNetwork"},
						{ "EnableOnSubgraphs", coremlEnableOnSubgraphs ? "1" : "0" }  // Disable subgraph processing to avoid complex control flow issues
                    });
					break;
				case "CUDA":
					options.AppendExecutionProvider_CUDA(Preferences.Default.OnnxGpu);
					break;
				case "NNAPI":
					options.AppendExecutionProvider_Nnapi();
					break;
			}
			return options;
		}

		public static InferenceSession getInferenceSession(byte[] model, OnnxRunnerChoice runnerChoice = OnnxRunnerChoice.Default) {
			if (runnerChoice == OnnxRunnerChoice.CPU ||
				(runnerChoice == OnnxRunnerChoice.CPUForCoreML && Preferences.Default.OnnxRunner == "CoreML")) {
				return new InferenceSession(model);
			} else {
				// Try with CoreML subgraphs enabled first, fallback to default if it fails
				if (OS.IsMacOS() && Preferences.Default.OnnxRunner == "CoreML") {
					try {
						return new InferenceSession(model, getOnnxSessionOptions(coremlEnableOnSubgraphs: true));
					} catch (Exception e) {
						Log.Warning(e, "Failed to create session with CoreML subgraphs enabled, falling back to default settings");
					}
				}
				return new InferenceSession(model, getOnnxSessionOptions());
			}
		}

		public static InferenceSession getInferenceSession(string modelPath, OnnxRunnerChoice runnerChoice = OnnxRunnerChoice.Default) {
			if (runnerChoice == OnnxRunnerChoice.CPU ||
				(runnerChoice == OnnxRunnerChoice.CPUForCoreML && Preferences.Default.OnnxRunner == "CoreML")) {
				return new InferenceSession(modelPath);
			} else {
				// Try with CoreML subgraphs enabled first, fallback to default if it fails
				if (OS.IsMacOS() && Preferences.Default.OnnxRunner == "CoreML") {
					try {
						return new InferenceSession(modelPath, getOnnxSessionOptions(coremlEnableOnSubgraphs: true));
					} catch (Exception e) {
						Log.Warning(e, "Failed to create session with CoreML subgraphs enabled, falling back to default settings");
					}
				}
				return new InferenceSession(modelPath, getOnnxSessionOptions());
			}
		}

		public static void VerifyInputNames(InferenceSession session, IEnumerable<NamedOnnxValue> inputs) {
			var sessionInputNames = session.InputNames.ToHashSet();
			var givenInputNames = inputs.Select(v => v.Name).ToHashSet();
			var missing = sessionInputNames
				.Except(givenInputNames)
				.OrderBy(s => s, StringComparer.InvariantCulture)
				.ToArray();
			if (missing.Length > 0) {
				throw new ArgumentException("Missing input(s) for the inference session: " + string.Join(", ", missing));
			}
			var unexpected = givenInputNames
				.Except(sessionInputNames)
				.OrderBy(s => s, StringComparer.InvariantCulture)
				.ToArray();
			if (unexpected.Length > 0) {
				throw new ArgumentException("Unexpected input(s) for the inference session: " + string.Join(", ", unexpected));
			}
		}
	}
}
