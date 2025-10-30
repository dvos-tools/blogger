using System;

namespace com.DvosTools.blogger
{
    /// <summary>
    /// Exception that can be thrown when something goes wrong within the UnityLogCatcher
    /// </summary>
    public class UnitLogCatcherException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the UnitLogCatcherException class
        /// </summary>
        public UnitLogCatcherException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the UnitLogCatcherException class with a specified error message
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public UnitLogCatcherException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the UnitLogCatcherException class with a specified error message and a reference to the inner exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public UnitLogCatcherException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}