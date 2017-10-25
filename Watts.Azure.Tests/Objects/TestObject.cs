namespace Watts.Azure.Tests.Objects
{
    using System;
    using System.Runtime.Serialization;

    [DataContract, Serializable]
    public class TestObject
    {
        public TestObject()
        {
        }

        public TestObject(int a, string b)
        {
            this.A = a;
            this.B = b;
        }

        [DataMember]
        public int A { get; set; }

        [DataMember]
        public string B { get; set; }
    }
}