namespace Watts.Azure.Common.Exceptions
{
    using System;

    /// <summary>
    /// An exception indicating that an upsert failed...
    /// </summary>
    public class CouldNotUpsertEntityException : Exception
    {
        public CouldNotUpsertEntityException(string entityType, string tableName)
            : base($"Unable to upsert entity of type {entityType} into table {tableName}")
        {
        }
    }
}