using System;

namespace Fleck
{
    public interface IWebSocketServer : IDisposable
    {
        void Start(Action<IWebSocketConnection> config);
        event EventHandler<ClientCertificateValidationEventArgs> OnClientCertificateValidation;
    }
}
