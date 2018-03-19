namespace Watts.Azure.Common.Exceptions
{
    using System;

    public class TableDeleteFailedException : Exception
    {
        public TableDeleteFailedException(string tableName) : base($"Deleting the table {tableName} was unsuccessful")
        {
        }
    }
}