namespace Watts.Azure.Tests
{
    using System.IO;
    using System.Reflection;

    public class Constants
    {
        public static string CredentialsFilePath
        {
            get
            {
                var testAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                // Go back from Debug, to bin, then the root and then one outside that, so that the testEnvironment file is placed outside the repos.
                return Path.Combine(Directory.GetParent(testAssemblyDirectory).Parent.Parent.Parent.FullName, "testEnvironment.testenv");
            }
        } 
    }
}