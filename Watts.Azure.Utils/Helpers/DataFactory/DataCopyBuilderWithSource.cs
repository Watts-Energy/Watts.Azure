namespace Watts.Azure.Utils.Helpers.DataFactory
{
    using Watts.Azure.Common.Interfaces.DataFactory;

    public class DataCopyBuilderWithSource : DataCopyBuilderWithAuthentication
    {
        public DataCopyBuilderWithSource(
                DataCopyBuilderWithAuthentication parent,
                IAzureLinkedService sourceTable,
                string sourceQuery = "") : base(parent, parent.Authenticator)
        {
            this.Source = sourceTable;
            this.SourceQuery = sourceQuery;
        }

        public IAzureLinkedService Source { get; set; }

        public string SourceQuery { get; set; } = string.Empty;

        public DataCopyBuilderWithSource WithSourceQuery(string query)
        {
            this.SourceQuery = query;
            return this;
        }

        public DataCopyBuilderWithSourceAndTarget To(IAzureLinkedService target)
        {
            return new DataCopyBuilderWithSourceAndTarget(this, target);
        }
    }
}