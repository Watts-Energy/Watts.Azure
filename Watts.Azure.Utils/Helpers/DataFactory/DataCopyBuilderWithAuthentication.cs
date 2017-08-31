namespace Watts.Azure.Utils.Helpers.DataFactory
{
    using Common.Interfaces.Security;
    using Common.Interfaces.Storage;

    public class DataCopyBuilderWithAuthentication : DataCopyBuilderWithCopySetup
    {
        public DataCopyBuilderWithAuthentication(DataCopyBuilderWithCopySetup parent, IAzureActiveDirectoryAuthentication authenticator)
            : base(parent, parent.CopySetup)
        {
            this.Authenticator = authenticator;
        }

        public IAzureActiveDirectoryAuthentication Authenticator { get; set; }

        public DataCopyBuilderWithSource CopyFromTable(IAzureTableStorage storage)
        {
            return new DataCopyBuilderWithSource(this, storage);
        }
    }
}