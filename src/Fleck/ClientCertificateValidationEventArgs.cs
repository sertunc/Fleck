using System;
using System.Security.Cryptography.X509Certificates;

namespace Fleck
{
    public class ClientCertificateValidationEventArgs : EventArgs
    {
        public X509Certificate ClientCertificate { get; private set; }

        public IWebSocketConnection SocketConnection { get; private set; }

        public ClientCertificateValidationEventArgs(X509Certificate clientCertificate, IWebSocketConnection socketConnection = null)
        {
            ClientCertificate = clientCertificate ?? throw new ArgumentNullException(nameof(clientCertificate));
            SocketConnection = socketConnection;
        }
    }
}
