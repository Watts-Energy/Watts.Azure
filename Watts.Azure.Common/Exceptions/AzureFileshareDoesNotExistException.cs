namespace Watts.Azure.Common.Exceptions
{
    using System;

    public class AzureFileshareDoesNotExistException : Exception
    {
        public AzureFileshareDoesNotExistException(string shareName) :
            base($"The share {shareName} could not be found")
        {
        }
    }
}