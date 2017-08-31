namespace Watts.Azure.Utils.Helpers.DataFactory
{
    using Common.Interfaces.Storage;

    public class DataCopyBuilderWithSource : DataCopyBuilderWithAuthentication
    {
        public DataCopyBuilderWithSource(
                DataCopyBuilderWithAuthentication parent,
                IAzureTableStorage sourceTable,
                string sourceQuery = "") : base(parent, parent.Authenticator)
        {
            this.SourceTable = sourceTable;
            this.SourceQuery = sourceQuery;
        }

        public IAzureTableStorage SourceTable { get; set; }

        public string SourceQuery { get; set; } = string.Empty;

        public DataCopyBuilderWithSource WithSourceQuery(string query)
        {
            this.SourceQuery = query;
            return this;
        }

        public DataCopyBuilderWithSourceAndTarget ToTable(IAzureTableStorage targetTable)
        {
            return new DataCopyBuilderWithSourceAndTarget(this, targetTable);
        }
    }
}