namespace Watts.Azure.Common.Exceptions
{
    using System;

    /// <summary>
    /// Exception thrown when inserting an entity into Azure table storage fails.
    /// </summary>
    public class CouldNotInsertEntityException : Exception
    {
        public CouldNotInsertEntityException(string entityType, string tableName)
            : base($"Unable to insert entity of type {entityType} into table {tableName}")
        {
        }
    }
}