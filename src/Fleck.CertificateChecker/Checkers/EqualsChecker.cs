using System.Security.Cryptography.X509Certificates;

namespace Fleck.CertificateChecker.Checkers
{
    public class EqualsChecker : IChecker
    {
        public bool Check(X509Certificate2 certificate1, X509Certificate certificate2)
        {
            return certificate1.Equals(certificate2);
        }
    }
}
