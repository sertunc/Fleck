using System.Security.Cryptography.X509Certificates;

namespace Fleck.CertificateChecker.Checkers
{
    public class ContentMatchChecker : IChecker
    {
        public bool Check(X509Certificate2 certificate1, X509Certificate certificate2)
        {
            byte[] certificate1Bytes = certificate1.GetRawCertData();
            byte[] certificate2Bytes = certificate2.GetRawCertData();


            for (int i = 0; i < certificate1Bytes.Length; i++)
            {
                if (certificate1Bytes[i] != certificate2Bytes[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
