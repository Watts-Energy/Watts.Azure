namespace Watts.Azure.Common.Exceptions
{
    using System;

    public class CannotSetFilterOnInitializedServiceBusException : Exception
    {
        public CannotSetFilterOnInitializedServiceBusException() : base("You may not invoke SetFilter after the topic has been initialized (by a call to Initialize) as the filter must be set during the initialization")
        {
        }
    }
}