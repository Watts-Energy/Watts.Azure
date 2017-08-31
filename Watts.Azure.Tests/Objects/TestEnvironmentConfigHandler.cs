namespace Watts.Azure.Tests.Objects
{
    using System.IO;
    using Newtonsoft.Json;
    using Watts.Azure.Common.Batch.Objects;
    using Watts.Azure.Common.Storage.Objects;
    using Watts.Azure.Utils.Objects;

    /// <summary>
    /// Reads the batch credentials from the test environment config file.
    /// NOTE: Always ensure that you do not commit your test environment config file (testEnvironment.testenv)
    /// </summary>
    public class TestEnvironmentConfigHandler
    {
        private string filePath;

        public TestEnvironmentConfigHandler(string filepath)
        {
            this.filePath = filepath;
        }

        public static TestEnvironmentConfig DefaultEnvironment => new TestEnvironmentConfig()
        {
            BatchEnvironment = new PredefinedBatchEnvironment()
            {
                BatchAccountSettings = new BatchAccountSettings()
                {
                    BatchAccountName = "batch account name",
                    BatchAccountKey = "batch account key",
                    BatchAccountUrl = "batch account url"
                },
                BatchStorageAccountSettings = new StorageAccountSettings()
                {
                    StorageAccountName = "storage account name",
                    StorageAccountKey = "storage account key"
                }
            },

            FileshareConnnectionString = "fileshare connection string",

            DataCopyEnvironment = new PredefinedDataCopyEnvironment()
            {
                SubscriptionId = "Subscription id",
                ActiveDirectoryTenantId = "Ad tenant id",
                AdfClientId = "Application client id",
                ClientSecret = "Application client secret",
                StorageAccountSettings = new StorageAccountSettings()
                {
                    StorageAccountName = "storage account name",
                    StorageAccountKey = "storage account key"
                }
            }
        };

        public TestEnvironmentConfig GetTestEnvironment()
        {
            if (File.Exists(this.filePath))
            {
                string contents = File.ReadAllText(this.filePath);

                return JsonConvert.DeserializeObject<TestEnvironmentConfig>(contents);
            }
            else
            {
                this.GenerateFile();
                return DefaultEnvironment;
            }
        }

        internal void GenerateFile()
        {
            File.WriteAllText(this.filePath, JsonConvert.SerializeObject(DefaultEnvironment, Formatting.Indented));
        }
    }
}