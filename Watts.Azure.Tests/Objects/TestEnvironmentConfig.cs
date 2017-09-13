namespace Watts.Azure.Tests.Objects
{
    using System.Text.RegularExpressions;
    using Microsoft.WindowsAzure.Storage;
    using Newtonsoft.Json;
    using Watts.Azure.Utils.Objects;

    public class TestEnvironmentConfig
    {
        public PredefinedBatchEnvironment BatchEnvironment { get; set; }

        public PredefinedDataCopyEnvironment DataCopyEnvironment { get; set; }

        public PredefinedDataLakeStoreEnvironment DataLakeEnvironment { get; set; }

        public string FileshareConnnectionString { get; set; }

        [JsonIgnore]
        public string FileshareAccountKey
        {
            get
            {
                Regex regex = new Regex("AccountKey=([^;]*);");
                var matches = regex.Match(this.FileshareConnnectionString);

                // Take the match but remove the AccountKey= string.
                var value = matches.Value.Replace("AccountKey=", string.Empty);

                // Remove the trailing semicolon
                return value.Substring(0, value.Length - 1);
            }
        }

        [JsonIgnore]
        internal CloudStorageAccount TestFileShareAccount
        {
            get
            {
                CloudStorageAccount account = CloudStorageAccount.Parse(this.FileshareConnnectionString);
                return account;
            }
        }

        [JsonIgnore]
        internal CloudStorageAccount StorageAccount => CloudStorageAccount.Parse($"DefaultEndpointsProtocol=https;AccountName={this.BatchEnvironment.BatchStorageAccountSettings.StorageAccountName};AccountKey={this.BatchEnvironment.BatchStorageAccountSettings.StorageAccountKey};EndpointSuffix=core.windows.net");
    }
}