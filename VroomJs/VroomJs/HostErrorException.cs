using System;

namespace VroomJs
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// Allows users to customize conversion of .NET exceptions to script errors,
    /// or to create a script error that is not based on a .NET  exception.
    /// </remarks>
    public class HostErrorException : Exception
    {
        public HostErrorException(Exception exception, string errorName = null, string message = null)
            : this(new HostErrorInfo(exception, errorName, message))
        {
        }

        public HostErrorException(string message, string errorName = null)
            : this(new HostErrorInfo(null, errorName, message))
        {
        }

        private HostErrorException(HostErrorInfo errorInfo)
            : base(errorInfo.Message ?? errorInfo.Exception?.Message, errorInfo.Exception)
        {
            ErrorInfo = errorInfo;
        }

        public HostErrorInfo ErrorInfo { get; }
    }
}
