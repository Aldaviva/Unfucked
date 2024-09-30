using OBSStudioClient.Enums;
using System.ComponentModel;

namespace Unfucked.OBS;

/// <summary>
/// Encapsulates connection and authentication logic for OBS WebSocket client instances.
/// </summary>
public interface IObsClientFactory {

    /// <summary>
    /// Create a new <see cref="IObsClient"/> instance, connect it to the server, and wait for it to successfully authenticate.
    /// </summary>
    /// <param name="obsHost">A URI containing the hostname and port of the OBS WebSocket server to connect to. Defaults to <c>ws://localhost:4455/</c>.</param>
    /// <param name="password">The WebSocket server password, if authentication is enabled, otherwise the empty string.</param>
    /// <param name="cancellationToken">If you want to stop connecting early.</param>
    /// <returns>A connected <see cref="IObsClient"/> instance if connection and authentication succeeded, otherwise <c>null</c> if either failed.</returns>
    Task<IObsClient?> Connect(Uri? obsHost = null, string password = "", CancellationToken cancellationToken = default);

}

/// <inheritdoc />
public class ObsClientFactory: IObsClientFactory {

    /// <inheritdoc />
    // ExceptionAdjustment: M:System.Uri.#ctor(System.String) -T:System.UriFormatException
    public async Task<IObsClient?> Connect(Uri? obsHost = null, string password = "", CancellationToken cancellationToken = default) {
        IObsClient obs = CreateClient();
        obsHost ??= new Uri("ws://localhost:4455/");

        TaskCompletionSource<bool> authenticated = new();
        obs.PropertyChanged += OnObsPropertyChanged;

        void OnObsPropertyChanged(object? _, PropertyChangedEventArgs eventArgs) {
            if (eventArgs.PropertyName == nameof(IObsClient.ConnectionState)) {
                switch (obs.ConnectionState) {
                    case ConnectionState.Connected:
                        authenticated.TrySetResult(true);
                        break;
                    case ConnectionState.Disconnected:
                        authenticated.TrySetResult(false);
                        break;
                    default:
                        break;
                }
            }
        }

        string hostname  = obsHost.Host;
        ushort port      = (ushort) (obsHost.IsDefaultPort ? 4455 : obsHost.Port);
        bool   connected = false;

        try {
            if (!await obs.ConnectAsync(false, password, hostname, port, EventSubscriptions.None).ConfigureAwait(false)) {
                return null;
            }

            connected = await authenticated.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

            return connected ? obs : null;
        } finally {
            obs.PropertyChanged -= OnObsPropertyChanged;
            if (!connected) {
                obs.Dispose();
            }
        }
    }

    /// <summary>
    /// Create a new <see cref="IObsClient"/> instance so <see cref="Connect"/> can connect to it. Useful for mocked testing.
    /// </summary>
    /// <returns>A new instance of <see cref="ObsClient"/></returns>
    protected virtual IObsClient CreateClient() => new ObsClient();

}