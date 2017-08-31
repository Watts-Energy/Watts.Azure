namespace Watts.Azure.Utils.Exceptions
{
    using System;

    public class InvalidPredefinedEnvironmentException : Exception
    {
        public InvalidPredefinedEnvironmentException(string message) : base(message)
        {
        }
    }
}