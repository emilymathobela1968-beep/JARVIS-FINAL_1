using System.Text.Json;
using Jarvis.Infrastructure;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using Windows.System;

namespace Jarvis.App;

public sealed partial class BarehandsWindow : Window
{
    private static readonly string DiagnosticsLog = Path.Combine(AppContext.BaseDirectory, "logs", "barehands-webview-diagnostics.log");
    private bool stageStarting;
    private readonly BarehandsServerHost serverHost = new();

    public BarehandsWindow()
    {
        InitializeComponent();
        Title = "JARVIS // BAREHANDS";
        Activated += BarehandsWindow_Activated;
        Content.KeyDown += BarehandsWindow_KeyDown;
        Closed += async (_, _) => await serverHost.DisposeAsync();
    }

    private async void BarehandsWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (!stageStarting)
        {
            stageStarting = true;
            MaximizeWorkspace();
            await InitializeStageAsync();
        }

        StageWebView.Focus(FocusState.Programmatic);
    }

    private void MaximizeWorkspace()
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }
    }

    private async Task InitializeStageAsync()
    {
        try
        {
            var server = await serverHost.StartAsync();
            if (!server.Success)
            {
                CameraStatusText.Text = "Stage unavailable";
                TrackingStatusText.Text = "Server not running";
                Report("server_start_failed", server.Message);
                return;
            }
            await StageWebView.EnsureCoreWebView2Async();
            StageWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            StageWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            StageWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            StageWebView.CoreWebView2.PermissionRequested += StageWebView_PermissionRequested;
            StageWebView.CoreWebView2.WebMessageReceived += (_, args) => HandleWebMessage(args);
            StageWebView.CoreWebView2.NavigationCompleted += (_, args) => Report("navigation_completed", $"success={args.IsSuccess}; error={args.WebErrorStatus}");
            await StageWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(DiagnosticScript);
            StageWebView.Source = BarehandsServerHost.StageUri;
        }
        catch (Exception ex)
        {
            CameraStatusText.Text = "Workspace error";
            TrackingStatusText.Text = "Not started";
            Report("workspace_error", ex.Message);
        }
    }

    private void StageWebView_PermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
    {
        if (e.PermissionKind == CoreWebView2PermissionKind.Camera &&
            Uri.TryCreate(e.Uri, UriKind.Absolute, out var origin) &&
            string.Equals(origin.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(origin.Host, "127.0.0.1", StringComparison.Ordinal) &&
            origin.Port == BarehandsServerHost.LoopbackPort)
        {
            e.State = CoreWebView2PermissionState.Allow;
            CameraStatusText.Text = "Permission allowed";
            Report("camera_permission", "allowed for fixed loopback origin");
            return;
        }

        Report("permission_rejected", $"kind={e.PermissionKind}");
    }

    private void HandleWebMessage(CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            using var document = JsonDocument.Parse(args.TryGetWebMessageAsString());
            var root = document.RootElement;
            if (!root.TryGetProperty("scope", out var scope) || scope.GetString() != "barehands") return;
            var eventName = root.TryGetProperty("event", out var eventValue) ? eventValue.GetString() ?? "unknown" : "unknown";
            var detail = root.TryGetProperty("detail", out var detailValue) ? detailValue.GetRawText() : "{}";
            UpdateStatus(eventName, detail);
            Report(eventName, detail);
            if (eventName == "return_requested")
            {
                Close();
            }
        }
        catch (JsonException)
        {
            Report("diagnostic_parse_error", "invalid WebView diagnostic payload");
        }
    }

    private void UpdateStatus(string eventName, string detail)
    {
        switch (eventName)
        {
            case "get_user_media_success":
                CameraStatusText.Text = "Live stream";
                break;
            case "get_user_media_error":
            case "enumerate_devices_error":
                CameraStatusText.Text = "Camera error";
                break;
            case "video_playing":
                CameraStatusText.Text = "Live video";
                break;
            case "hand_tracker_ready":
                TrackingStatusText.Text = "Ready";
                break;
            case "hand_tracker_error":
                TrackingStatusText.Text = "Initialization error";
                break;
        }
    }

    private void Report(string eventName, string detail)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DiagnosticsLog)!);
        File.AppendAllText(DiagnosticsLog, $"{DateTimeOffset.Now:O} event={eventName} detail={detail}{Environment.NewLine}");
    }

    private async void CycleCamera_Click(object sender, RoutedEventArgs e) => await SendStageKeyAsync("c", "KeyC");
    private async void ReloadModels_Click(object sender, RoutedEventArgs e) => await SendStageKeyAsync("r", "KeyR");
    private void ReturnToJarvis_Click(object sender, RoutedEventArgs e) => Close();

    private void BarehandsWindow_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            e.Handled = true;
            Close();
        }
    }

    private async Task SendStageKeyAsync(string key, string code)
    {
        if (StageWebView.CoreWebView2 is null) return;
        StageWebView.Focus(FocusState.Programmatic);
        await StageWebView.CoreWebView2.ExecuteScriptAsync(
            $"window.dispatchEvent(new KeyboardEvent('keydown', {{ key: '{key}', code: '{code}', bubbles: true }}));");
    }

    private const string DiagnosticScript = """
        (() => {
          const report = (event, detail = {}) => {
            if (window.chrome && window.chrome.webview) {
              window.chrome.webview.postMessage(JSON.stringify({ scope: "barehands", event, detail }));
            }
          };
          const err = error => ({ name: String(error?.name || "Error"), message: String(error?.message || error || "unknown") });
          window.addEventListener("error", event => report("window_error", err(event.error || event.message)));
          window.addEventListener("unhandledrejection", event => report("unhandled_rejection", err(event.reason)));
          window.addEventListener("keydown", event => {
            if (event.key === "Escape") report("return_requested");
          });

          const media = navigator.mediaDevices;
          if (!media) { report("media_devices_missing"); return; }
          const enumerateDevices = media.enumerateDevices.bind(media);
          media.enumerateDevices = async () => {
            report("enumerate_devices_requested");
            try {
              const devices = await enumerateDevices();
              const counts = devices.reduce((result, device) => {
                result[device.kind] = (result[device.kind] || 0) + 1;
                return result;
              }, {});
              report("enumerate_devices_success", counts);
              return devices;
            } catch (error) { report("enumerate_devices_error", err(error)); throw error; }
          };
          const getUserMedia = media.getUserMedia.bind(media);
          media.getUserMedia = async constraints => {
            report("get_user_media_requested", { video: Boolean(constraints?.video), audio: Boolean(constraints?.audio) });
            try {
              const stream = await getUserMedia(constraints);
              report("get_user_media_success", { tracks: stream.getTracks().map(track => ({ kind: track.kind, readyState: track.readyState, enabled: track.enabled, muted: track.muted })) });
              return stream;
            } catch (error) { report("get_user_media_error", err(error)); throw error; }
          };
          let trackerReported = false;
          const inspect = () => {
            const video = document.getElementById("cam");
            if (video && !video.dataset.jarvisDiagnosticsAttached) {
              video.dataset.jarvisDiagnosticsAttached = "1";
              ["loadedmetadata", "canplay", "playing", "error"].forEach(eventName => video.addEventListener(eventName, () =>
                report(`video_${eventName}`, { readyState: video.readyState, width: video.videoWidth, height: video.videoHeight, hasStream: Boolean(video.srcObject) })));
            }
            const boot = document.getElementById("boot");
            if (!boot && !trackerReported) { trackerReported = true; report("hand_tracker_ready"); }
            else if (boot && boot.textContent.includes("STAGE FAILED TO BOOT") && !trackerReported) {
              trackerReported = true;
              report("hand_tracker_error", { message: boot.textContent.trim() });
            }
          };
          const beginInspection = () => {
            const root = document.documentElement;
            if (!root) { setTimeout(beginInspection, 0); return; }
            new MutationObserver(inspect).observe(root, { childList: true, subtree: true, characterData: true });
            inspect();
          };
          beginInspection();
          document.addEventListener("DOMContentLoaded", inspect);
          report("diagnostics_ready", { secureContext: window.isSecureContext, origin: location.origin });
        })();
        """;
}
