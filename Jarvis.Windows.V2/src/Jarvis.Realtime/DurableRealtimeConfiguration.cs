using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Jarvis.Realtime;

public sealed record RealtimeConfigurationStatus(
    bool EndpointPresent,
    bool DeploymentPresent,
    bool ApiKeyPresent,
    bool TranscriptionDeploymentPresent,
    string Source,
    IReadOnlyList<string> MissingConfiguration)
{
    public bool Configured => EndpointPresent && DeploymentPresent && ApiKeyPresent && TranscriptionDeploymentPresent;
}

public sealed record JarvisRealtimeConfiguration(AzureRealtimeOptions Options, RealtimeConfigurationStatus Status);

public interface IJarvisSecretStore
{
    string Identifier { get; }
    string? ReadSecret();
}

public interface IJarvisLocalConfigStore
{
    string Location { get; }
    RealtimeLocalConfiguration Read();
}

public sealed record RealtimeLocalConfiguration(
    string? Endpoint = null,
    string? Deployment = null,
    string? TranscriptionDeployment = null);

public sealed class JarvisRealtimeConfigurationProvider(
    IJarvisLocalConfigStore localConfig,
    IJarvisSecretStore secretStore)
{
    public const string CredentialIdentifier = "Jarvis.Windows.V2/AzureOpenAIRealtime";
    public static readonly string DefaultLocalConfigurationPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis.Windows.V2", "realtime.local.json");

    public static JarvisRealtimeConfiguration Load() =>
        new JarvisRealtimeConfigurationProvider(
            new JsonRealtimeLocalConfigStore(DefaultLocalConfigurationPath),
            new WindowsCredentialSecretStore(CredentialIdentifier)).Resolve();

    public JarvisRealtimeConfiguration Resolve()
    {
        var local = localConfig.Read();
        var endpoint = FirstNonEmpty(Environment.GetEnvironmentVariable("AZURE_OPENAI_REALTIME_ENDPOINT"), local.Endpoint);
        var deployment = FirstNonEmpty(Environment.GetEnvironmentVariable("AZURE_OPENAI_REALTIME_DEPLOYMENT"), local.Deployment);
        var apiKey = FirstNonEmpty(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY"), secretStore.ReadSecret());
        var transcriptionDeployment = FirstNonEmpty(
            Environment.GetEnvironmentVariable("AZURE_OPENAI_REALTIME_TRANSCRIPTION_DEPLOYMENT"),
            local.TranscriptionDeployment,
            "whisper-1");

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(endpoint)) missing.Add("AZURE_OPENAI_REALTIME_ENDPOINT");
        if (string.IsNullOrWhiteSpace(deployment)) missing.Add("AZURE_OPENAI_REALTIME_DEPLOYMENT");
        if (string.IsNullOrWhiteSpace(apiKey)) missing.Add("AZURE_OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(transcriptionDeployment)) missing.Add("AZURE_OPENAI_REALTIME_TRANSCRIPTION_DEPLOYMENT");

        var source = DescribeSource(
            Environment.GetEnvironmentVariable("AZURE_OPENAI_REALTIME_ENDPOINT"),
            Environment.GetEnvironmentVariable("AZURE_OPENAI_REALTIME_DEPLOYMENT"),
            Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY"),
            Environment.GetEnvironmentVariable("AZURE_OPENAI_REALTIME_TRANSCRIPTION_DEPLOYMENT"),
            local,
            apiKey);

        var status = new RealtimeConfigurationStatus(
            !string.IsNullOrWhiteSpace(endpoint),
            !string.IsNullOrWhiteSpace(deployment),
            !string.IsNullOrWhiteSpace(apiKey),
            !string.IsNullOrWhiteSpace(transcriptionDeployment),
            source,
            missing);

        return new(new AzureRealtimeOptions(
            endpoint,
            deployment,
            apiKey,
            PreferWebRtc: true,
            TranscriptionModel: transcriptionDeployment ?? "whisper-1"), status);
    }

    private static string DescribeSource(
        string? endpointEnv,
        string? deploymentEnv,
        string? apiKeyEnv,
        string? transcriptionEnv,
        RealtimeLocalConfiguration local,
        string? apiKey)
    {
        var sources = new List<string>();
        if (!string.IsNullOrWhiteSpace(endpointEnv) ||
            !string.IsNullOrWhiteSpace(deploymentEnv) ||
            !string.IsNullOrWhiteSpace(apiKeyEnv) ||
            !string.IsNullOrWhiteSpace(transcriptionEnv))
        {
            sources.Add("environment");
        }

        if (!string.IsNullOrWhiteSpace(local.Endpoint) ||
            !string.IsNullOrWhiteSpace(local.Deployment) ||
            !string.IsNullOrWhiteSpace(local.TranscriptionDeployment))
        {
            sources.Add("local_config");
        }

        if (string.IsNullOrWhiteSpace(apiKeyEnv) && !string.IsNullOrWhiteSpace(apiKey))
            sources.Add("windows_credential");

        return sources.Count == 0 ? "missing" : string.Join("+", sources);
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}

public sealed class JsonRealtimeLocalConfigStore(string path) : IJarvisLocalConfigStore
{
    public string Location => path;

    public RealtimeLocalConfiguration Read()
    {
        if (!File.Exists(path))
            return new();

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            return new(
                ReadString(root, "endpoint"),
                ReadString(root, "deployment"),
                ReadString(root, "transcriptionDeployment"));
        }
        catch (JsonException)
        {
            return new();
        }
        catch (IOException)
        {
            return new();
        }
        catch (UnauthorizedAccessException)
        {
            return new();
        }
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.ValueKind == JsonValueKind.Object &&
        root.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}

public sealed class WindowsCredentialSecretStore(string target) : IJarvisSecretStore
{
    private const int CredTypeGeneric = 1;
    private const int CredPersistLocalMachine = 2;

    public string Identifier => target;

    public string? ReadSecret()
    {
        if (!CredRead(target, CredTypeGeneric, 0, out var credentialPointer))
            return null;

        try
        {
            var credential = Marshal.PtrToStructure<NativeCredential>(credentialPointer);
            if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0)
                return null;

            var bytes = new byte[credential.CredentialBlobSize];
            Marshal.Copy(credential.CredentialBlob, bytes, 0, bytes.Length);
            return Encoding.Unicode.GetString(bytes).TrimEnd('\0');
        }
        finally
        {
            CredFree(credentialPointer);
        }
    }

    public static bool WriteSecret(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return false;

        var bytes = Encoding.Unicode.GetBytes(secret);
        var credential = new NativeCredential
        {
            Type = CredTypeGeneric,
            TargetName = Marshal.StringToCoTaskMemUni(JarvisRealtimeConfigurationProvider.CredentialIdentifier),
            CredentialBlob = Marshal.AllocCoTaskMem(bytes.Length),
            CredentialBlobSize = bytes.Length,
            Persist = CredPersistLocalMachine,
            UserName = Marshal.StringToCoTaskMemUni(Environment.UserName)
        };

        try
        {
            Marshal.Copy(bytes, 0, credential.CredentialBlob, bytes.Length);
            return CredWrite(ref credential, 0);
        }
        finally
        {
            Marshal.FreeCoTaskMem(credential.TargetName);
            Marshal.FreeCoTaskMem(credential.CredentialBlob);
            Marshal.FreeCoTaskMem(credential.UserName);
        }
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credential);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite(ref NativeCredential credential, int flags);

    [DllImport("advapi32.dll")]
    private static extern void CredFree(IntPtr buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public int Flags;
        public int Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public long LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }
}
