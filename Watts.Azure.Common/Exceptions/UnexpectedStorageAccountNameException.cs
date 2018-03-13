namespace Watts.Azure.Common.Exceptions
{
    using System;

    public class UnexpectedStorageAccountNameException : Exception
    {
        public UnexpectedStorageAccountNameException(string message) : base(message)
        {
        }
    }
}