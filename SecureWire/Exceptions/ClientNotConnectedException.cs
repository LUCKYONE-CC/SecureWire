using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureWire.Exceptions
{
    [Serializable]
    public class ClientNotConnectedException : Exception
    {
        public ClientNotConnectedException()
        { }

        public ClientNotConnectedException(string message)
            : base(message)
        { }

        public ClientNotConnectedException(Exception innerException, string message = "The client is not connected")
            : base(message, innerException)
        { }
    }
}
