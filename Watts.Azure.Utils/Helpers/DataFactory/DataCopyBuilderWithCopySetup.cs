namespace Watts.Azure.Utils.Helpers.DataFactory
{
    using Common.DataFactory.Copy;
    using Common.Interfaces.Security;

    public class DataCopyBuilderWithCopySetup : DataCopyBuilderWithDataFactorySetup
    {
        public DataCopyBuilderWithCopySetup(DataCopyBuilderWithDataFactorySetup parent, CopyTableSetup copySetup) : base(parent, parent.DataFactorySetup)
        {
            this.CopySetup = copySetup;
        }

        public CopyTableSetup CopySetup { get; set; }

        public DataCopyBuilderWithCopySetup WithTimeoutInMinutes(int numberOfMinutes)
        {
            this.CopySetup.TimeoutInMinutes = numberOfMinutes;
            return this;
        }

        public DataCopyBuilderWithCopySetup CreateTargetTableIfNotExists()
        {
            this.CopySetup.CreateTargetTableIfNotExists = true;
            return this;
        }

        public DataCopyBuilderWithAuthentication AuthenticateUsing(IAzureActiveDirectoryAuthentication authenticator)
        {
            return new DataCopyBuilderWithAuthentication(this, authenticator);
        }
    }
}