namespace Watts.Azure.Common.Exceptions
{
    using System;

    public class MustRedirectOutputException : Exception
    {
        public MustRedirectOutputException(string message) :
            base(message)
        {
        }
    }
}