namespace Watts.Azure.Tests.Objects
{
    using System;
    using Microsoft.WindowsAzure.Storage.Table;

    public class TestEntityMissingOneProperty : TableEntity
    {
        public TestEntityMissingOneProperty(string someId, int key, string someName, DateTime someDate)
        {
            this.PartitionKey = someId;
            this.RowKey = key.ToString();

            this.Id = someId;
            this.Key = key;
            this.Name = someName;
            this.Date = someDate;
        }

        public string Id { get; set; }

        public int Key { get; set; }

        public string Name { get; set; }

        public DateTime Date { get; set; }
    }
}