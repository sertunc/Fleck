using Fleck.CertificateChecker.Checkers;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Fleck.CertificateChecker
{
    public class CertificateCheckerWrapper : IChecker
    {
        private readonly List<IChecker> checkers;

        public CertificateCheckerWrapper()
        {
            checkers = new List<IChecker>()
            {
                new ContentMatchChecker(),
                new EqualsChecker(),
                new LengthChecker(),
            };
        }

        public bool Check(X509Certificate2 certificate1, X509Certificate certificate2)
        {
            var result = true;

            foreach (var item in checkers)
            {
                result = item.Check(certificate1, certificate2);

                if (result == false)
                    break;
            }

            return result;
        }
    }
}
