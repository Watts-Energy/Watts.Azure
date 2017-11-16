namespace Watts.Azure.Common.Storage.Objects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;

    /// <summary>
    /// Represents a single collection in Azure DocumentDb and provides generic functions to interact with it.
    /// </summary>
    public class AzureDocumentDb
    {
        private readonly DocumentClient documentClient;

        private readonly string databaseName;
        private readonly string collectionName;
        private readonly List<string> partitionKeyPaths;

        public AzureDocumentDb(string databaseUri, string databaseAuthKey, string databaseName, string collectionName, List<string> partitionKeyPaths)
        {
            this.documentClient = new DocumentClient(new Uri(databaseUri), databaseAuthKey);

            this.databaseName = databaseName;
            this.collectionName = collectionName;
            this.partitionKeyPaths = partitionKeyPaths;

            this.CreateDbIfNotExists();
            this.CreateCollectionIfNotExists();
        }

        public string CollectionName => this.collectionName;

        public string DatabaseName => this.databaseName;

        /// <summary>
        /// Save the entity asynchronously
        /// </summary>
        /// <param name="entity">The entity to save. This must be serializable</param>
        public async Task SaveAsync(object entity)
        {
            ResourceResponse<Document> response = await this.documentClient.UpsertDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(this.databaseName, this.collectionName), entity);

            if (response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.OK)
            {
                throw new HttpRequestException($"Save returned {response.StatusCode}");
            }
        }

        public void Save(object entity)
        {
            var response = this.documentClient.UpsertDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(this.databaseName, this.collectionName), entity);

            response.Wait();

            if (response.IsFaulted)
            {
                throw new HttpRequestException($"Error when saving document");
            }
        }

        public async Task DeleteAsync<T>(Func<T, bool> predicate, PartitionKey partitionKey) where T : Resource
        {
            var docs = this.Query<T>(predicate);

            var options = new RequestOptions {PartitionKey = partitionKey};

            foreach (var doc in docs)
            {
                await this.documentClient.DeleteDocumentAsync(doc.SelfLink, options);
            }
        }

        public IEnumerable<T> Query<T>(Func<T, bool> predicate)
        {
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

            var retVal = this.documentClient.CreateDocumentQuery<T>(
                    UriFactory.CreateDocumentCollectionUri(this.databaseName, this.collectionName), queryOptions)
                    .Where(predicate);

            return retVal;
        }

        public int Count<T>(Func<T, bool> predicate)
        {
            FeedOptions options = new FeedOptions() { MaxItemCount = -1, EnableCrossPartitionQuery = true };

            var retVal = this.documentClient.CreateDocumentQuery<T>(
                    UriFactory.CreateDocumentCollectionUri(this.databaseName, this.collectionName), options)
                .Where(predicate).Count();

            return retVal;
        }

        public List<T> Query<T>(string query, FeedOptions options = null)
        {
            FeedOptions queryOptions = options ?? new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true };

            var retVal = this.documentClient.CreateDocumentQuery<T>(query, queryOptions).ToList();

            return retVal;
        }

        internal void CreateCollectionIfNotExists()
        {
            var myCollection = new DocumentCollection { Id = this.collectionName };

            this.partitionKeyPaths.ForEach(path =>
            {
                myCollection.PartitionKey.Paths.Add(path);
            });

            this.documentClient.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(this.databaseName), myCollection, new RequestOptions()).Wait();
        }

        internal void CreateDbIfNotExists()
        {
            this.documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = this.databaseName }).Wait();
        }
    }
}