using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureWire.Exceptions
{
    [Serializable]
    public class PackageException : Exception
    {
        public PackageException()
        { }

        public PackageException(string message)
            : base(message)
        { }

        public PackageException(Exception innerException, string message)
            : base(message, innerException)
        { }
    }
}
