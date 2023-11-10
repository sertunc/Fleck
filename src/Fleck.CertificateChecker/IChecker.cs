using System.Security.Cryptography.X509Certificates;

namespace Fleck.CertificateChecker
{
    public interface IChecker
    {
        bool Check(X509Certificate2 certificate1, X509Certificate certificate2);
    }
}
