namespace Watts.Azure.Common.Exceptions
{
    using System;

    /// <summary>
    /// An exception thrown when creation of a table in azure table storage fails.
    /// </summary>
    public class CouldNotCreateTableException : Exception
    {
        /// <summary>
        /// Creates a new instance of CouldNotCreateTableException
        /// </summary>
        /// <param name="tableName"></param>
        public CouldNotCreateTableException(string tableName) : base($"Creation of table {tableName} was unsuccessful")
        {
        }
    }
}