namespace Watts.Azure.Tests
{
    using System.IO;

    public class Constants
    {
        public static string CredentialsFilePath => Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName, "testEnvironment.testenv");
    }
}