namespace Watts.Azure.Common.Storage.Objects
{
    /// <summary>
    /// Column in a table, with name and type.
    /// </summary>
    public class TableColumn
    {
        public string Name { get; set; }

        public string Type { get; set; }
    }
}