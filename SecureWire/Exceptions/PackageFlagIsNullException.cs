using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureWire.Exceptions
{
    [Serializable]
    public class PackageFlagIsNullException : Exception
    {
        public PackageFlagIsNullException()
        { }

        public PackageFlagIsNullException(string message)
            : base(message)
        { }

        public PackageFlagIsNullException(Exception innerException, string message = "The package-Flag was null")
            : base(message, innerException)
        { }
    }
}
