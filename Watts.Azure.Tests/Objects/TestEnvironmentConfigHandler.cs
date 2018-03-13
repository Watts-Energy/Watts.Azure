namespace Watts.Azure.Tests.Objects
{
    using System.IO;
    using Common;
    using Common.Security;
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
        private readonly string filePath;

        public TestEnvironmentConfigHandler(string filepath)
        {
            this.filePath = filepath;
        }

        public static TestEnvironmentConfig DefaultEnvironment => new TestEnvironmentConfig()
        {
            BatchEnvironment = new BatchEnvironment()
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

            FileshareConnectionString = "fileshare connection string",

            DataCopyEnvironment = new DataCopyEnvironment()
            {
                SubscriptionId = "Subscription id",
                Credentials = new AppActiveDirectoryAuthenticationCredentials()
                {
                    ClientId = "Application client id",
                    ClientSecret = "Application client secret",
                    TenantId = "Ad tenant id"
                },
                StorageAccountSettings = new StorageAccountSettings()
                {
                    StorageAccountName = "storage account name",
                    StorageAccountKey = "storage account key"
                }
            },

            DataLakeEnvironment = new DataLakeStoreEnvironment()
            {
                SubscriptionId = "Subscription id",
                ResourceGroupName = "Resource group name",
                Credentials = new AppActiveDirectoryAuthenticationCredentials()
                {
                    ClientId = "Application client id",
                    ClientSecret = "Application client secret",
                    TenantId = "Ad tenant id"
                },
                DataLakeStoreName = "Data lake store name"
            },

            ServiceBusEnvironment = new AzureServiceBusEnvironment()
            {
                NamespaceName = "Service bus namespace",
                ResourceGroupName = "Resource group name",
                Location = AzureLocation.NorthEurope,
                Credentials = new AppActiveDirectoryAuthenticationCredentials()
                {
                    ClientId = "Application client id",
                    ClientSecret = "Application client secret",
                    TenantId = "Ad tenant id"
                },
                ServiceBusConnectionString = "Namespace connection string"
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