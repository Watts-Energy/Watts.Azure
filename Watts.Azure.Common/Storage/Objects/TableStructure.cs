namespace Watts.Azure.Common.Storage.Objects
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// Represents the structure of a table in Azure Table Storage.
    /// </summary>
    public class TableStructure
    {
        public TableStructure()
        {
            this.Columns = new List<TableColumn>();
        }

        public static TableStructure Empty => new TableStructure();

        public List<TableColumn> Columns { get; set; }

        /// <summary>
        /// Add a column to the table structure. If it already exists of the type of the column is "null", nothing happens.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="property"></param>
        public void AddColumn(string name, EntityProperty property)
        {
            if (this.Columns.SingleOrDefault(p => p.Name.Equals(name) && !p.Type.Equals("null")) == null)
            {
                TableColumn column = new TableColumn()
                {
                    Type = property.PropertyType.ToString(),
                    Name = name
                };

                this.Columns.Add(column);
            }
        }

        /// <summary>
        /// Add a column to the table structure. If it already exists of the type of the column is "null", nothing happens.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        public void AddColumn(string name, string type)
        {
            if (this.Columns.SingleOrDefault(p => p.Name.Equals(name) && !p.Type.Equals("null")) == null)
            {
                TableColumn column = new TableColumn()
                {
                    Type = type,
                    Name = name
                };

                this.Columns.Add(column);
            }
        }

        /// <summary>
        /// Add the keys of this table to the TableStructure (partitionKey and rowKey).
        /// Can be overridden in case there, at some point, is a possibility of creating composite keys.
        /// </summary>
        /// <param name="partitionKeyType"></param>
        /// <param name="rowKeyType"></param>
        public virtual void AddDefaultKeyStructure(string partitionKeyType, string rowKeyType)
        {
            this.AddKey("PartitionKey", partitionKeyType);
            this.AddKey("RowKey", rowKeyType);
        }

        /// <summary>
        /// Add a key to the structure
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="keyType"></param>
        public void AddKey(string keyName, string keyType)
        {
            if (keyType != null)
            {
                this.AddColumn(keyName, keyType);
            }
            else
            {
                this.AddColumn(keyName, EntityProperty.GeneratePropertyForString(string.Empty));
            }
        }

        /// <summary>
        /// Replace all columns that have type null in the current structure, with "String" type columns.
        /// </summary>
        public void ReplaceNullTypesWithString()
        {
            foreach (var column in this.Columns)
            {
                if (column.Type.Equals("null"))
                {
                    column.Type = "String";
                }
            }
        }
    }
}