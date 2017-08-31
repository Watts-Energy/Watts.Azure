namespace Watts.Azure.Common.Storage.Objects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
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

        /// <summary>
        /// Save the entity asynchronously
        /// </summary>
        /// <param name="entity">The entity to save. This must be serializable</param>
        public void Save(object entity)
        {
            var response = this.documentClient.UpsertDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(this.databaseName, this.collectionName), entity);

            response.Wait();

            if (response.IsFaulted)
            {
                throw new HttpRequestException();
            }
        }

        public List<T> Query<T>(Func<T, bool> predicate)
        {
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

            // Here we find the Andersen family via its LastName
            var retVal = this.documentClient.CreateDocumentQuery<T>(
                    UriFactory.CreateDocumentCollectionUri(this.databaseName, this.collectionName), queryOptions)
                    .Where(predicate).ToList();

            return retVal;
        }

        public List<T> Query<T>(string query)
        {
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

            // Here we find the Andersen family via its LastName
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