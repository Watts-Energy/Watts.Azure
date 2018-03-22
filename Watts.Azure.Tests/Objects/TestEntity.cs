namespace Watts.Azure.Tests.Objects
{
    using System;

    public class TestEntity : TestEntityMissingOneProperty
    {
        public TestEntity(string someId, int key, string someName, DateTime someDate, double someValue)
            : base(someId, key, someName, someDate)
        {
            this.Value = someValue;
        }

        public double Value { get; set; }
    }
}