using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Threading;
using Fleck.Helpers;

namespace Fleck
{
    public class SocketWrapper : ISocket
    {
        public const UInt32 KeepAliveInterval = 60000;
        public const UInt32 RetryInterval = 10000;

        private readonly Socket _socket;
        private Stream _stream;
        private CancellationTokenSource _tokenSource;
        private TaskFactory _taskFactory;

        public string RemoteIpAddress
        {
            get
            {
                var endpoint = _socket.RemoteEndPoint as IPEndPoint;
                return endpoint != null ? endpoint.Address.ToString() : null;
            }
        }

        public int RemotePort
        {
            get
            {
                var endpoint = _socket.RemoteEndPoint as IPEndPoint;
                return endpoint != null ? endpoint.Port : -1;
            }
        }

        public X509Certificate2 Certificate { get; set; }

        public event EventHandler<ClientCertificateValidationEventArgs> OnClientCertificateValidation;

        public void SetKeepAlive(Socket socket, UInt32 keepAliveInterval, UInt32 retryInterval)
        {
            int size = sizeof(UInt32);
            UInt32 on = 1;

            byte[] inArray = new byte[size * 3];
            Array.Copy(BitConverter.GetBytes(on), 0, inArray, 0, size);
            Array.Copy(BitConverter.GetBytes(keepAliveInterval), 0, inArray, size, size);
            Array.Copy(BitConverter.GetBytes(retryInterval), 0, inArray, size * 2, size);
            socket.IOControl(IOControlCode.KeepAliveValues, inArray, null);
        }

        public SocketWrapper(Socket socket)
        {
            _tokenSource = new CancellationTokenSource();
            _taskFactory = new TaskFactory(_tokenSource.Token);
            _socket = socket;
            if (_socket.Connected)
                _stream = new NetworkStream(_socket);

            // The tcp keepalive default values on most systems
            // are huge (~7200s). Set them to something more reasonable.
            if (FleckRuntime.IsRunningOnWindows())
            {
                SetKeepAlive(socket, KeepAliveInterval, RetryInterval);
            }
        }

        private bool CertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            OnClientCertificateValidation?.Invoke(sender, new ClientCertificateValidationEventArgs(certificate));
            return true;
        }

        public Task Authenticate(SslProtocols enabledSslProtocols, Action callback, Action<Exception> error)
        {
            var ssl = new SslStream(_stream, true, new RemoteCertificateValidationCallback(CertificateValidationCallback));
            _stream = new QueuedStream(ssl);

            Func<AsyncCallback, object, IAsyncResult> begin = (cb, s) => ssl.BeginAuthenticateAsServer(Certificate, true, enabledSslProtocols, false, cb, s);

            Task task = Task.Factory.FromAsync(begin, ssl.EndAuthenticateAsServer, null);
            task.ContinueWith(t => callback(), TaskContinuationOptions.NotOnFaulted)
                .ContinueWith(t => error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
            task.ContinueWith(t => error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);

            return task;
        }

        public void Listen(int backlog)
        {
            _socket.Listen(backlog);
        }

        public void Bind(EndPoint endPoint)
        {
            _socket.Bind(endPoint);
        }

        public bool Connected
        {
            get { return _socket.Connected; }
        }

        public Stream Stream
        {
            get { return _stream; }
        }

        public bool NoDelay
        {
            get { return _socket.NoDelay; }
            set { _socket.NoDelay = value; }
        }

        public EndPoint LocalEndPoint
        {
            get { return _socket.LocalEndPoint; }
        }

        public Task<int> Receive(byte[] buffer, Action<int> callback, Action<Exception> error, int offset)
        {
            try
            {
                Func<AsyncCallback, object, IAsyncResult> begin =
               (cb, s) => _stream.BeginRead(buffer, offset, buffer.Length, cb, s);

                Task<int> task = Task.Factory.FromAsync<int>(begin, _stream.EndRead, null);
                task.ContinueWith(t => callback(t.Result), TaskContinuationOptions.NotOnFaulted)
                    .ContinueWith(t => error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
                task.ContinueWith(t => error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
                return task;
            }
            catch (Exception e)
            {
                error(e);
                return null;
            }
        }

        public Task<ISocket> Accept(Action<ISocket> callback, Action<Exception> error)
        {
            Func<IAsyncResult, ISocket> end = r => _tokenSource.Token.IsCancellationRequested ? null : new SocketWrapper(_socket.EndAccept(r));
            var task = _taskFactory.FromAsync(_socket.BeginAccept, end, null);
            task.ContinueWith(t => callback(t.Result), TaskContinuationOptions.OnlyOnRanToCompletion)
                .ContinueWith(t => error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
            task.ContinueWith(t => error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
            return task;
        }

        public void Dispose()
        {
            _tokenSource.Cancel();
            if (_stream != null) _stream.Dispose();
            if (_socket != null) _socket.Dispose();
        }

        public void Close()
        {
            _tokenSource.Cancel();
            if (_stream != null) _stream.Close();
            if (_socket != null) _socket.Close();
        }

        public int EndSend(IAsyncResult asyncResult)
        {
            _stream.EndWrite(asyncResult);
            return 0;
        }

        public Task Send(byte[] buffer, Action callback, Action<Exception> error)
        {
            if (_tokenSource.IsCancellationRequested)
                return null;

            try
            {
                Func<AsyncCallback, object, IAsyncResult> begin =
                    (cb, s) => _stream.BeginWrite(buffer, 0, buffer.Length, cb, s);

                Task task = Task.Factory.FromAsync(begin, _stream.EndWrite, null);
                task.ContinueWith(t => callback(), TaskContinuationOptions.NotOnFaulted)
                    .ContinueWith(t => error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
                task.ContinueWith(t => error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);

                return task;
            }
            catch (Exception e)
            {
                error(e);
                return null;
            }
        }
    }
}
