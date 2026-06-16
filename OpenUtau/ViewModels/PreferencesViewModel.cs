using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using OpenUtau.Audio;
using OpenUtau.Classic;
using OpenUtau.Core;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using OpenUtau.Core.Render;
using Serilog;

namespace OpenUtau.App.ViewModels {
    public class LyricsHelperOption {
        public readonly Type klass;
        public LyricsHelperOption(Type klass) {
            this.klass = klass;
        }
        public override string ToString() {
            return klass.Name;
        }
    }

    public class PreferencesViewModel : ViewModelBase {
        // General
        private CultureInfo? language;
        private CultureInfo? sortingOrder;

        public List<CultureInfo>? Languages { get; }
        public CultureInfo? Language {
            get => language;
            set => this.RaiseAndSetIfChanged(ref language, value);
        }
        public List<CultureInfo>? SortingOrders { get; }
        public CultureInfo? SortingOrder {
            get => sortingOrder;
            set => this.RaiseAndSetIfChanged(ref sortingOrder, value);
        }
        [Reactive] public bool Beta { get; set; }

        // Playback
        private List<AudioOutputDevice>? audioOutputDevices;
        private AudioOutputDevice? audioOutputDevice;

        public List<AudioOutputDevice>? AudioOutputDevices {
            get => audioOutputDevices;
            set => this.RaiseAndSetIfChanged(ref audioOutputDevices, value);
        }
        public AudioOutputDevice? AudioOutputDevice {
            get => audioOutputDevice;
            set => this.RaiseAndSetIfChanged(ref audioOutputDevice, value);
        }
        [Reactive] public bool UseSystemDefaultDevice { get; set; }
        [Reactive] public int PreferPortAudio { get; set; }
        [Reactive] public int LockStartTime { get; set; }
        [Reactive] public int PlaybackAutoScroll { get; set; }
        [Reactive] public double PlayPosMarkerMargin { get; set; }

        // Paths
        public string SingerPath => PathManager.Inst.SingersPath;
        public string AdditionalSingersPath => !string.IsNullOrWhiteSpace(PathManager.Inst.AdditionalSingersPath) ? PathManager.Inst.AdditionalSingersPath : "(None)";
        [Reactive] public bool InstallToAdditionalSingersPath { get; set; }
        [Reactive] public bool LoadDeepFolders { get; set; }

        // Editing
        public List<LyricsHelperOption> LyricsHelpers { get; } =
            ActiveLyricsHelper.Inst.Available
                .Select(klass => new LyricsHelperOption(klass))
                .ToList();
        [Reactive] public LyricsHelperOption? LyricsHelper { get; set; }
        [Reactive] public bool LyricsHelperBrackets { get; set; }
        [Reactive] public bool PenPlusDefault { get; set; }

        // Render
        [Reactive] public bool PreRender { get; set; }
        [Reactive] public int NumRenderThreads { get; set; }
        public int LogicalCoreCount {
            get => Environment.ProcessorCount;
        }
        [Reactive] public bool HighThreads { get; set; }
        public int SafeMaxThreadCount {
            get => Math.Min(8, LogicalCoreCount / 2);
        }
        [Reactive] public bool SkipRenderingMutedTracks { get; set; }
        [Reactive] public bool ClearCacheOnQuit { get; set; }
        public List<string> OnnxRunnerOptions { get; set; }
        [Reactive] public string OnnxRunner { get; set; }
        public List<GpuInfo> OnnxGpuOptions { get; set; }
        [Reactive] public GpuInfo OnnxGpu { get; set; }
        [Reactive] public bool ShowOnnxGpu { get; set; }

        // Appearance
        [Reactive] public string ThemeName { get; set; }
        [Reactive] public int DegreeStyle { get; set; }
        [Reactive] public bool UseTrackColor { get; set; }
        [Reactive] public bool ShowPortrait { get; set; }
        [Reactive] public bool ShowIcon { get; set; }
        [Reactive] public bool ShowGhostNotes { get; set; }
        [Reactive] public bool ThemeEditable { get; set; }
        public List<string> ThemeItems => ThemeManager.GetAvailableThemes();
        public bool IsThemeEditorOpen => Views.ThemeEditorWindow.IsOpen;

        // UTAU
        public List<string> DefaultRendererOptions { get; set; }
        [Reactive] public string DefaultRenderer { get; set; }
        [Reactive] public int OtoEditor { get; set; }
        public string VLabelerPath => Preferences.Default.VLabelerPath;
        public string SetParamPath => Preferences.Default.SetParamPath;

        // Diffsinger
        public List<int> DiffSingerStepsOptions { get; } = new List<int> { 2, 5, 10, 20, 50, 100, 200, 500, 1000 };
        public List<int> DiffSingerStepsVarianceOptions { get; } = new List<int> { 2, 5, 10, 20, 50, 100, 200, 500, 1000 };
        public List<int> DiffSingerStepsPitchOptions { get; } = new List<int> { 2, 5, 10, 20, 50, 100, 200, 500, 1000 };
        [Reactive] public int DiffSingerSteps { get; set; }
        [Reactive] public int DiffSingerStepsVariance { get; set; }
        [Reactive] public int DiffSingerStepsPitch { get; set; }
        [Reactive] public double DiffSingerDepth { get; set; }
        [Reactive] public bool DiffSingerTensorCache { get; set; }
        [Reactive] public bool DiffSingerLangCodeHide { get; set; }

        // Advanced
        [Reactive] public bool RememberMid { get; set; }
        [Reactive] public bool RememberUst { get; set; }
        [Reactive] public bool RememberVsqx { get; set; }
        public string WinePath => Preferences.Default.WinePath;

        public PreferencesViewModel() {
            var audioOutput = PlaybackManager.Inst.AudioOutput;
            if (audioOutput != null) {
                AudioOutputDevices = audioOutput.GetOutputDevices();
                int deviceNumber = audioOutput.DeviceNumber;
                var device = AudioOutputDevices.FirstOrDefault(d => d.deviceNumber == deviceNumber);
                if (device != null) {
                    AudioOutputDevice = device;
                }
            }
            UseSystemDefaultDevice = Preferences.Default.UseSystemDefaultAudioDevice;
            PreferPortAudio = Preferences.Default.PreferPortAudio ? 1 : 0;
            PlaybackAutoScroll = Preferences.Default.PlaybackAutoScroll;
            PlayPosMarkerMargin = Preferences.Default.PlayPosMarkerMargin;
            LockStartTime = Preferences.Default.LockStartTime;
            InstallToAdditionalSingersPath = Preferences.Default.InstallToAdditionalSingersPath;
            LoadDeepFolders = Preferences.Default.LoadDeepFolderSinger;
            ToolsManager.Inst.Initialize();
            var pattern = new Regex(@"Strings\.([\w-]+)\.axaml");
            Languages = App.GetLanguages().Keys
                .Select(lang => CultureInfo.GetCultureInfo(lang))
                .ToList();
            Language = string.IsNullOrEmpty(Preferences.Default.Language)
                ? null
                : CultureInfo.GetCultureInfo(Preferences.Default.Language);
            SortingOrders = Languages.ToList();
            SortingOrders.Insert(0, CultureInfo.InvariantCulture);
            SortingOrder = Preferences.Default.SortingOrder == null ? Language
                : string.IsNullOrEmpty(Preferences.Default.SortingOrder) ? CultureInfo.InvariantCulture
                : CultureInfo.GetCultureInfo(Preferences.Default.SortingOrder);
            PreRender = Preferences.Default.PreRender;
            DefaultRendererOptions = Renderers.getRendererOptions();
            DefaultRenderer = String.IsNullOrEmpty(Preferences.Default.DefaultRenderer) ?
               DefaultRendererOptions[0] : Preferences.Default.DefaultRenderer;
            NumRenderThreads = Preferences.Default.NumRenderThreads;
            OnnxRunnerOptions = Onnx.getRunnerOptions();
            OnnxRunner = String.IsNullOrEmpty(Preferences.Default.OnnxRunner) ?
               OnnxRunnerOptions[0] : Preferences.Default.OnnxRunner;
            OnnxGpuOptions = Onnx.getGpuInfo();
            OnnxGpu = OnnxGpuOptions.FirstOrDefault(x => x.deviceId == Preferences.Default.OnnxGpu, OnnxGpuOptions[0]);
            ShowOnnxGpu = OnnxRunner == "DirectML";
            DiffSingerDepth = Preferences.Default.DiffSingerDepth * 100;
            DiffSingerSteps = Preferences.Default.DiffSingerSteps;
            DiffSingerStepsVariance = Preferences.Default.DiffSingerStepsVariance;
            DiffSingerStepsPitch = Preferences.Default.DiffSingerStepsPitch;
            DiffSingerTensorCache = Preferences.Default.DiffSingerTensorCache;
            DiffSingerLangCodeHide = Preferences.Default.DiffSingerLangCodeHide;
            SkipRenderingMutedTracks = Preferences.Default.SkipRenderingMutedTracks;
            ThemeName = Preferences.Default.ThemeName;
            DegreeStyle = Preferences.Default.DegreeStyle;
            UseTrackColor = Preferences.Default.UseTrackColor;
            ShowPortrait = Preferences.Default.ShowPortrait;
            ShowIcon = Preferences.Default.ShowIcon;
            ShowGhostNotes = Preferences.Default.ShowGhostNotes;
            Beta = Preferences.Default.Beta;
            LyricsHelper = LyricsHelpers.FirstOrDefault(option => option.klass.Equals(ActiveLyricsHelper.Inst.GetPreferred()));
            LyricsHelperBrackets = Preferences.Default.LyricsHelperBrackets;
            OtoEditor = Preferences.Default.OtoEditor;
            RememberMid = Preferences.Default.RememberMid;
            RememberUst = Preferences.Default.RememberUst;
            RememberVsqx = Preferences.Default.RememberVsqx;
            ClearCacheOnQuit = Preferences.Default.ClearCacheOnQuit;

            MessageBus.Current.Listen<ThemeEditorStateChangedEvent>()
                .Subscribe(_ => this.RaisePropertyChanged(nameof(IsThemeEditorOpen)));
            
            this.WhenAnyValue(vm => vm.UseSystemDefaultDevice)
                .Skip(1)
                .Subscribe(useDefault => {
                    Preferences.Default.UseSystemDefaultAudioDevice = useDefault;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.AudioOutputDevice)
                .Skip(1)
                .WhereNotNull()
                .SubscribeOn(RxApp.MainThreadScheduler)
                .Subscribe(device => {
                    if (UseSystemDefaultDevice) {
                        return;
                    }
                    if (PlaybackManager.Inst.AudioOutput != null) {
                        try {
                            PlaybackManager.Inst.AudioOutput.SelectDevice(device.guid, device.deviceNumber);
                        } catch (Exception e) {
                            DocManager.Inst.ExecuteCmd(new ErrorMessageNotification($"Failed to select device {device.name}", e));
                        }
                    }
                });
            this.WhenAnyValue(vm => vm.PreferPortAudio)
                .Skip(1)
                .Subscribe(index => {
                    Preferences.Default.PreferPortAudio = index > 0;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.PlaybackAutoScroll)
                .Skip(1)
                .Subscribe(autoScroll => {
                    Preferences.Default.PlaybackAutoScroll = autoScroll;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.PlayPosMarkerMargin)
                .Skip(1)
                .Subscribe(playPosMarkerMargin => {
                    Preferences.Default.PlayPosMarkerMargin = playPosMarkerMargin;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.LockStartTime)
                .Skip(1)
                .Subscribe(lockStartTime => {
                    Preferences.Default.LockStartTime = lockStartTime;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.InstallToAdditionalSingersPath)
                .Skip(1)
                .Subscribe(additionalSingersPath => {
                    Preferences.Default.InstallToAdditionalSingersPath = additionalSingersPath;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.LoadDeepFolders)
                .Skip(1)
                .Subscribe(loadDeepFolders => {
                    Preferences.Default.LoadDeepFolderSinger = loadDeepFolders;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.PreRender)
                .Skip(1)
                .Subscribe(preRender => {
                    Preferences.Default.PreRender = preRender;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.Language)
                .Skip(1)
                .Subscribe(lang => {
                    Preferences.Default.Language = lang?.Name ?? string.Empty;
                    Preferences.Save();
                    App.SetLanguage(Preferences.Default.Language);
                });
            this.WhenAnyValue(vm => vm.SortingOrder)
                .Skip(1)
                .Subscribe(so => {
                    Preferences.Default.SortingOrder = so?.Name ?? null;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.ThemeName)
                .Skip(1)
                .Subscribe(themeName => {
                    ThemeEditable = themeName != "Light" && themeName != "Dark";
                    if (!IsThemeEditorOpen) {
                        Preferences.Default.ThemeName = themeName;
                        Preferences.Save();
                        App.SetTheme();
                    }
                });
            this.WhenAnyValue(vm => vm.DegreeStyle)
                .Skip(1)
                .Subscribe(degreeStyle => {
                    Preferences.Default.DegreeStyle = degreeStyle;
                    Preferences.Save();
                    MessageBus.Current.SendMessage(new PianorollRefreshEvent("Part"));
                });
            this.WhenAnyValue(vm => vm.UseTrackColor)
                .Skip(1)
                .Subscribe(trackColor => {
                    Preferences.Default.UseTrackColor = trackColor;
                    Preferences.Save();
                    MessageBus.Current.SendMessage(new PianorollRefreshEvent("TrackColor"));
                });
            this.WhenAnyValue(vm => vm.ShowPortrait)
                .Skip(1)
                .Subscribe(showPortrait => {
                    Preferences.Default.ShowPortrait = showPortrait;
                    Preferences.Save();
                    MessageBus.Current.SendMessage(new PianorollRefreshEvent("Portrait"));
                });
            this.WhenAnyValue(vm => vm.ShowIcon)
                .Skip(1)
                .Subscribe(showIcon => {
                    Preferences.Default.ShowIcon = showIcon;
                    Preferences.Save();
                    MessageBus.Current.SendMessage(new PianorollRefreshEvent("Portrait"));
                });
            this.WhenAnyValue(vm => vm.ShowGhostNotes)
                .Skip(1)
                .Subscribe(showGhostNotes => {
                    Preferences.Default.ShowGhostNotes = showGhostNotes;
                    Preferences.Save();
                    MessageBus.Current.SendMessage(new PianorollRefreshEvent("Part"));
                });
            this.WhenAnyValue(vm => vm.Beta)
                .Skip(1)
                .Subscribe(beta => {
                    Preferences.Default.Beta = beta;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.LyricsHelper)
                .Skip(1)
                .Subscribe(option => {
                    ActiveLyricsHelper.Inst.Set(option?.klass);
                    Preferences.Default.LyricHelper = option?.klass?.Name ?? string.Empty;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.LyricsHelperBrackets)
                .Skip(1)
                .Subscribe(brackets => {
                    Preferences.Default.LyricsHelperBrackets = brackets;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.OtoEditor)
                .Skip(1)
                .Subscribe(index => {
                    Preferences.Default.OtoEditor = index;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.NumRenderThreads)
                .Skip(1)
                .Subscribe(index => {
                    Preferences.Default.NumRenderThreads = index;
                    HighThreads = index > SafeMaxThreadCount ? true : false;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.DefaultRenderer)
                .Skip(1)
                .Subscribe(index => {
                    Preferences.Default.DefaultRenderer = index;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.OnnxRunner)
                .Skip(1)
                .Subscribe(index => {
                    Preferences.Default.OnnxRunner = index;
                    Preferences.Save();
                    ToggleOnnxGpuDisplay(index == "DirectML");
                });
            this.WhenAnyValue(vm => vm.OnnxGpu)
                .Skip(1)
                .Subscribe(index => {
                    Preferences.Default.OnnxGpu = index.deviceId;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.RememberMid)
                .Skip(1)
                .Subscribe(index => {
                    Preferences.Default.RememberMid = index;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.RememberUst)
                .Skip(1)
                .Subscribe(index => {
                    Preferences.Default.RememberUst = index;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.RememberVsqx)
                .Skip(1)
                .Subscribe(index => {
                    Preferences.Default.RememberVsqx = index;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.ClearCacheOnQuit)
                .Skip(1)
                .Subscribe(index => {
                    Preferences.Default.ClearCacheOnQuit = index;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.DiffSingerSteps)
                .Skip(1)
                .Subscribe(index => {
                    Preferences.Default.DiffSingerSteps = index;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.DiffSingerStepsVariance)
                .Skip(1)
                .Subscribe(index => {
                    Preferences.Default.DiffSingerStepsVariance = index;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.DiffSingerStepsPitch)
                .Skip(1)
                .Subscribe(index => {
                    Preferences.Default.DiffSingerStepsPitch = index;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.DiffSingerDepth)
                .Skip(1)
                .Subscribe(index => {
                    Preferences.Default.DiffSingerDepth = index / 100;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.DiffSingerTensorCache)
                .Skip(1)
                .Subscribe(useCache => {
                    Preferences.Default.DiffSingerTensorCache = useCache;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.DiffSingerLangCodeHide)
                .Skip(1)
                .Subscribe(useCache => {
                    Preferences.Default.DiffSingerLangCodeHide = useCache;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.SkipRenderingMutedTracks)
                .Skip(1)
                .Subscribe(skipRenderingMutedTracks => {
                    Preferences.Default.SkipRenderingMutedTracks = skipRenderingMutedTracks;
                    Preferences.Save();
                });
        }

        public void TestAudioOutputDevice() {
            try {
                PlaybackManager.Inst.PlayTestSound();
            } catch (Exception e) {
                Log.Error(e, "Failed to play test sound.");
                DocManager.Inst.ExecuteCmd(new ErrorMessageNotification("Failed to play test sound.", e));
            }
        }

        public void OpenResamplerLocation() {
            try {
                string path = PathManager.Inst.ResamplersPath;
                Directory.CreateDirectory(path);
                OS.OpenFolder(path);
            } catch (Exception e) {
                DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(e));
            }
        }

        public void SetAddlSingersPath(string path) {
            Preferences.Default.AdditionalSingerPath = path;
            Preferences.Save();
            this.RaisePropertyChanged(nameof(AdditionalSingersPath));
        }

        public void SetVLabelerPath(string path) {
            Preferences.Default.VLabelerPath = path;
            Preferences.Save();
            this.RaisePropertyChanged(nameof(VLabelerPath));
        }

        public void SetSetParamPath(string path) {
            Preferences.Default.SetParamPath = path;
            Preferences.Save();
            this.RaisePropertyChanged(nameof(SetParamPath));
        }

        public void SetWinePath(string path) {
            Preferences.Default.WinePath = path;
            Preferences.Save();
            ToolsManager.Inst.Initialize();
            this.RaisePropertyChanged(nameof(WinePath));
        }

        public void RefreshThemes() {
            this.RaisePropertyChanged(nameof(ThemeItems));
        }

        public void ToggleOnnxGpuDisplay(bool show) {
            ShowOnnxGpu = show;
        }
    }
}
