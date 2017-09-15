namespace Watts.Azure.Utils.Helpers.DataFactory
{
    using Common.Interfaces.Security;
    using Watts.Azure.Common.Interfaces.DataFactory;

    public class DataCopyBuilderWithAuthentication : DataCopyBuilderWithCopySetup
    {
        public DataCopyBuilderWithAuthentication(DataCopyBuilderWithCopySetup parent, IAzureActiveDirectoryAuthentication authenticator)
            : base(parent, parent.CopySetup)
        {
            this.Authenticator = authenticator;
        }

        public IAzureActiveDirectoryAuthentication Authenticator { get; set; }

        public DataCopyBuilderWithSource CopyFrom(IAzureLinkedService source)
        {
            return new DataCopyBuilderWithSource(this, source);
        }
    }
}