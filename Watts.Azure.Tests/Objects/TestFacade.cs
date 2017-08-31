namespace Watts.Azure.Tests.Objects
{
    using System;
    using System.Collections.Generic;

    public class TestFacade
    {
        public static List<TestEntity> RandomEntities(int count, string partitionKey = "")
        {
            List<TestEntity> retVal = new List<TestEntity>();

            Random rand = new Random(DateTime.Now.Millisecond);

            var defaultPartitionKey = Guid.NewGuid().ToString();

            for (int i = 0; i < count; i++)
            {
                string id = string.IsNullOrEmpty(partitionKey) ? defaultPartitionKey : partitionKey;
                string name = string.IsNullOrEmpty(partitionKey) ? $"Name{count}" : partitionKey;
                DateTime date = DateTime.Now;
                double value = rand.NextDouble();

                TestEntity entity = new TestEntity(id, i, name, date, value);

                retVal.Add(entity);
            }

            return retVal;
        }
    }
}