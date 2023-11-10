using System.Security.Cryptography.X509Certificates;

namespace Fleck.CertificateChecker.Checkers
{
    public class LengthChecker : IChecker
    {
        public bool Check(X509Certificate2 certificate1, X509Certificate certificate2)
        {
            byte[] certificate1Bytes = certificate1.GetRawCertData();
            byte[] certificate2Bytes = certificate2.GetRawCertData();

            if (certificate1Bytes.Length != certificate2Bytes.Length)
            {
                return false;
            }

            return true;
        }
    }
}
