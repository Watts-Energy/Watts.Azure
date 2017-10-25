namespace Watts.Azure.Tests.Objects
{
    using System;
    using System.Runtime.Serialization;
    using Microsoft.WindowsAzure.Storage.Table;

    public class TestEntity : TableEntity
    {
        public TestEntity(string someId, int key, string someName, DateTime someDate, double someValue)
        {
            this.PartitionKey = someId.ToString();
            this.RowKey = key.ToString();

            this.Id = someId;
            this.Key = key;
            this.Name = someName;
            this.Date = someDate;
            this.Value = someValue;
        }

        public string Id { get; set; }

        public int Key { get; set; }

        public string Name { get; set; }

        public DateTime Date { get; set; }

        public double Value { get; set; }
    }
}