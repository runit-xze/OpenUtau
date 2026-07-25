using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.App {
	public class Program {
		// Initialization code. Don't use any Avalonia, third-party APIs or any
		// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
		// yet and stuff might break.
		[STAThread]
		public static void Main(string[] args) {
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			if (args.Length > 0) {
				string arg = args[0].ToLowerInvariant();
				if (arg == "--help" || arg == "-h") {
					Console.Error.WriteLine("OpenUtau — UTAU community editor");
					Console.Error.WriteLine();
					Console.Error.WriteLine("Usage: OpenUtau [--help | --version]");
					Console.Error.WriteLine();
					Console.Error.WriteLine("Launches the GUI editor. No CLI rendering mode yet.");
					return;
				}
				if (arg == "--version" || arg == "-v") {
					var ver = Assembly.GetEntryAssembly()?.GetName().Version;
					Console.Error.WriteLine(ver?.ToString() ?? "unknown");
					return;
				}
			}
			InitLogging();
			RegisterSignalHandlers();
			string processName = Process.GetCurrentProcess().ProcessName;
			if (processName != "dotnet") {
				var exists = Process.GetProcessesByName(processName).Count() > 1;
				if (exists) {
					Log.Information($"Process {processName} already open. Exiting.");
					return;
				}
			}
			Log.Information($"{Environment.OSVersion}");
			Log.Information($"{RuntimeInformation.OSDescription} " +
				$"{RuntimeInformation.OSArchitecture} " +
				$"{RuntimeInformation.ProcessArchitecture}");
			Log.Information($"OpenUtau v{Assembly.GetEntryAssembly()?.GetName().Version} " +
				$"{RuntimeInformation.RuntimeIdentifier}");
			Log.Information($"Data path = {PathManager.Inst.DataPath}");
			Log.Information($"Cache path = {PathManager.Inst.CachePath}");
			foreach (var error in Preferences.LoadingErrors) {
				Log.Error(error.Message);
			}
			var cusomDataPath = string.IsNullOrEmpty(PathManager.Inst.CustomDataPath) ? "none" : PathManager.Inst.CustomDataPath;
			Log.Information($"Custom Data path = {cusomDataPath}");
			Log.Information($"System encoding = {Encoding.GetEncoding(0)?.WebName ?? "null"}");
			try {
				Run(args);
				Log.Information($"Exiting.");
			} finally {
				if (!OS.IsMacOS()) {
					NetMQ.NetMQConfig.Cleanup(/*block=*/false);
					// Cleanup() hangs on macOS https://github.com/zeromq/netmq/issues/1018
				}
			}
			Log.Information($"Exited.");
		}

		static void RegisterSignalHandlers() {
			if (OperatingSystem.IsLinux()) {
				PosixSignalRegistration.Create(PosixSignal.SIGTERM, ctx => {
					Log.Information("Received SIGTERM; shutting down.");
					ctx.Cancel = true;
					Environment.Exit(0);
				});
				PosixSignalRegistration.Create(PosixSignal.SIGINT, ctx => {
					Log.Information("Received SIGINT; shutting down.");
					ctx.Cancel = true;
					Environment.Exit(0);
				});
			}
		}

		// Avalonia configuration, don't remove; also used by visual designer.
		public static AppBuilder BuildAvaloniaApp() {
			FontManagerOptions fontOptions = new();
			if (OS.IsLinux()) {
				using Process process = Process.Start(new ProcessStartInfo("fc-match") {
					ArgumentList = { "-f", "%{family}" },
					RedirectStandardOutput = true
				})!;
				process.WaitForExit();

				string fontFamily = process.StandardOutput.ReadToEnd();
				if (!string.IsNullOrEmpty(fontFamily)) {
					string[] fontFamilies = fontFamily.Split(',');
					fontOptions.DefaultFamilyName = fontFamilies[0];
				}
			} else if (OS.IsMacOS()) {
				//To avoid text display corruption, specify Hiragino Sans font first.
				//Due to the specification of AvaloniaUI, this only affects when the language is set to Japanese.
				fontOptions.DefaultFamilyName = "Hiragino Sans, Segoe UI, San Francisco, Helvetica Neue";
			}
			return AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.LogToTrace()
				.UseReactiveUI()
				.With(fontOptions)
				.With(new X11PlatformOptions { EnableIme = true });
		}

		public static void Run(string[] args)
			=> BuildAvaloniaApp()
				.StartWithClassicDesktopLifetime(
					args, ShutdownMode.OnMainWindowClose);

		public static void InitLogging() {
			var logConfig = new LoggerConfiguration()
				.MinimumLevel.Verbose()
				.WriteTo.Debug()
				.WriteTo.Logger(lc => lc
					.MinimumLevel.Information()
					.WriteTo.File(PathManager.Inst.LogFilePath, rollingInterval: RollingInterval.Day, encoding: Encoding.UTF8))
				.WriteTo.Logger(lc => lc
					.MinimumLevel.ControlledBy(DebugViewModel.Sink.Inst.LevelSwitch)
					.WriteTo.Sink(DebugViewModel.Sink.Inst));
			if (OperatingSystem.IsLinux()) {
				// stderr is captured by journald under systemd, and shows in
				// the terminal when launched from one. Structured output goes
				// to stderr so journald indexes the fields.
				logConfig.WriteTo.Console(
					standardErrorFromLevel: Serilog.Events.LogEventLevel.Verbose);
			}
			Log.Logger = logConfig.CreateLogger();
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((sender, args) => {
				Log.Error((Exception)args.ExceptionObject, "Unhandled exception");
			});
			Log.Information("Logging initialized.");
		}
	}
}
