namespace Watts.Azure.Common.Storage.Objects
{
    using System;
    using System.Collections.Generic;
    using System.Data.Services.Client;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Exceptions;
    using Interfaces.Storage;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;

    public class AzureTableStorage : IAzureTableStorage
    {
        private const int MaxEntitiesPerBatch = 100;

        /// <summary>
        /// Creates a new instance of AzureTableStorage, with specified name and connection string.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="connectionString"></param>
        public AzureTableStorage(string tableName, string connectionString)
        {
            this.StorageAccount = CloudStorageAccount.Parse(connectionString);
            this.Name = tableName;
            this.ConnectionString = connectionString;
            this.TableClient = this.StorageAccount.CreateCloudTableClient();
        }

        public string ConnectionString { get; }

        public string Name { get; set; }

        protected CloudStorageAccount StorageAccount { get; set; }

        protected CloudTableClient TableClient { get; set; }

        public static AzureTableStorage Connect(string connectionString, string tableName)
        {
            return new AzureTableStorage(tableName, connectionString);
        }

        public CloudTable GetTableReference()
        {
            return this.TableClient.GetTableReference(this.Name);
        }

        /// <summary>
        /// Create a table if it doesn't exist.
        /// This throws a CouldNotCreateTableException if the operation is unsuccessful
        /// </summary>
        /// <returns>The cloud table</returns>
        public CloudTable CreateTableIfNotExists()
        {
            // Retrieve a reference to the table.
            CloudTable table = this.TableClient.GetTableReference(this.Name);

            if (!table.Exists())
            {
                try
                {
                    // Create the table if it doesn't exist.
                    var result = table.CreateIfNotExists();

                    if (!result)
                    {
                        throw new CouldNotCreateTableException(this.Name);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception when creating the table {this.Name}");
                    throw;
                }
            }

            return table;
        }

        public async Task<CloudTable> CreateTableIfNotExistsAsync()
        {
            // Retrieve a reference to the table.
            CloudTable table = this.TableClient.GetTableReference(this.Name);

            if (!table.Exists())
            {
                try
                {
                    // Create the table if it doesn't exist.
                    var result = await table.CreateIfNotExistsAsync();

                    if (!result)
                    {
                        throw new CouldNotCreateTableException(this.Name);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception when creating the table {this.Name}");
                    throw;
                }
            }

            return table;
        }

        /// <summary>
        /// Create a table from a template entity.
        /// </summary>
        /// <param name="templateEntity"></param>
        /// <returns></returns>
        public CloudTable CreateTableFromTemplateEntity(DynamicTableEntity templateEntity)
        {
            CloudTable table = this.TableClient.GetTableReference(this.Name);

            this.Insert(templateEntity);
            this.Delete(templateEntity);

            return table;
        }

        /// <summary>
        /// Delete this table if it exists.
        /// </summary>
        /// <returns></returns>
        public bool DeleteIfExists()
        {
            CloudTable table = this.TableClient.GetTableReference(this.Name);

            var result = table.DeleteIfExists();

            return result;
        }

        /// <summary>
        /// Get all entities with the given partitionkey
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="partitionKey"></param>
        /// <returns></returns>
        public List<T> Get<T>(string partitionKey)
            where T : ITableEntity, new()
        {
            var table = this.CreateTableIfNotExists();

            // Create the table query.
            var rangeQuery = new TableQuery<T>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));

            return table.ExecuteQuery(rangeQuery).ToList();
        }

        /// <summary>
        /// Get the first entity with the specified partitionKey.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="partitionKey"></param>
        /// <returns></returns>
        public T GetFirst<T>(string partitionKey)
            where T : ITableEntity, new()
        {
            var table = this.CreateTableIfNotExists();

            // Create the table query.
            var rangeQuery = new TableQuery<T>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey)).Take(1);

            return table.ExecuteQuery(rangeQuery).FirstOrDefault();
        }

        /// <summary>
        /// Get the results of the specified query as DynamicTableEntitys
        /// </summary>
        /// <param name="tableQueryString"></param>
        /// <returns></returns>
        public List<DynamicTableEntity> Get(string tableQueryString)
        {
            var table = this.CreateTableIfNotExists();

            var query = new TableQuery().Where(tableQueryString);

            var result = table.ExecuteQuery(query);

            return result.ToList();
        }

        /// <summary>
        /// Get the first 'count' entities of the table.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<DynamicTableEntity> GetTop(int count)
        {
            var table = this.GetTableReference();

            var query = new TableQuery().Take(count);

            var result = table.ExecuteQuery(query);

            return result.ToList();
        }

        /// <summary>
        /// Selects a small number of entities and finds the union of the columns in those entities.
        /// The argument for selecting more than one is that the first entity may not contain all columns, whereas it's assumed to be less likely that
        /// all 10 top entities do not. The number may have to be increased at some point, or made into a parameter.
        /// If partitionKeyType and/or rowKeyType is not manually supplied, the type "String" is assumed.
        /// </summary>
        /// <param name="partitionKeyType"></param>
        /// <param name="rowKeyType"></param>
        /// <returns></returns>
        public DataStructure GetStructure(string partitionKeyType = null, string rowKeyType = null)
        {
            var retVal = new DataStructure();

            retVal.AddDefaultKeyStructure(partitionKeyType, rowKeyType);

            var table = this.GetTableReference();
            var query = new TableQuery().Take(1000);

            var entities = table.ExecuteQuery(query).ToList();

            if (entities.Count == 0)
            {
                return DataStructure.Empty;
            }

            foreach (var entity in entities)
            {
                foreach (var prop in entity.Properties)
                {
                    retVal.AddColumn(prop.Key, prop.Value);
                }
            }

            retVal.ReplaceNullTypesWithString();

            return retVal;
        }

        /// <summary>
        /// Query the table storage.
        /// </summary>
        /// <typeparam name="T">Type of entity expected to be returned.</typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<T> Query<T>(TableQuery<T> query)
            where T : ITableEntity, new()
        {
            var table = this.CreateTableIfNotExists();

            return table.ExecuteQuery(query).ToList();
        }

        public async Task<List<T>> QueryAsync<T>(TableQuery<T> query, CancellationToken cancellationToken = default(CancellationToken), Action<IList<T>> onProgress = null)
            where T : ITableEntity, new()
        {
            var table = await this.CreateTableIfNotExistsAsync();

            var items = new List<T>();
            TableContinuationToken token = null;

            do
            {
                TableQuerySegment<T> seg = await table.ExecuteQuerySegmentedAsync<T>(query, token, cancellationToken);
                token = seg.ContinuationToken;
                items.AddRange(seg);
                onProgress?.Invoke(items);
            } while (token != null && !cancellationToken.IsCancellationRequested);

            return items;
        }

        public List<T> Query<T>(Func<T, bool> predicate)
            where T : ITableEntity, new()
        {
            var table = this.CreateTableIfNotExists();

            var results = table.CreateQuery<T>().Where(predicate).ToList();

            return results;
        }

        /// <summary>
        /// Attempt to insert an entity into the given table.
        /// Note that this throws a CouldNotInsertEntityException if unsuccessful.
        /// </summary>
        /// <typeparam name="T">The type of entity to insert</typeparam>
        /// <param name="entity">The entity to insert</param>
        public void Insert<T>(T entity) where T : ITableEntity
        {
            var table = this.CreateTableIfNotExists();

            TableOperation insertOperation = TableOperation.Insert(entity);
            var result = table.Execute(insertOperation);

            Console.WriteLine("Insert returned HttpStatusCode {0}", result.HttpStatusCode);

            // If the operation does not return either 201 (Created) or 204 (No Content), throw an exception
            // According to https://docs.microsoft.com/en-us/rest/api/storageservices/fileservices/insert-entity:
            // "The status code depends on the value of the Prefer header. If the Prefer header is set to return-no-content,
            // then a successful operation returns status code 204 (No Content). If the Prefer header is not specified
            // or if it is set to return-content, then a successful operation returns status code 201 (Created)."
            if (result.HttpStatusCode != (int)HttpStatusCode.Created && result.HttpStatusCode != (int)HttpStatusCode.NoContent)
            {
                throw new CouldNotInsertEntityException(entity.GetType().Name, this.Name);
            }
        }

        /// <summary>
        /// Attempt to insert an entity into the given table.
        /// Note that this throws a CouldNotInsertEntityException if unsuccessful.
        /// </summary>
        /// <typeparam name="T">The type of entity to insert</typeparam>
        /// <param name="entity">The entity to insert</param>
        public async Task InsertAsync<T>(T entity) where T : ITableEntity
        {
            var table = this.CreateTableIfNotExists();

            TableOperation insertOperation = TableOperation.Insert(entity);
            var result = await table.ExecuteAsync(insertOperation);

            Console.WriteLine("Insert returned HttpStatusCode {0}", result.HttpStatusCode);

            // If the operation does not return either 201 (Created) or 204 (No Content), throw an exception
            // According to https://docs.microsoft.com/en-us/rest/api/storageservices/fileservices/insert-entity:
            // "The status code depends on the value of the Prefer header. If the Prefer header is set to return-no-content,
            // then a successful operation returns status code 204 (No Content). If the Prefer header is not specified
            // or if it is set to return-content, then a successful operation returns status code 201 (Created)."
            if (result.HttpStatusCode != (int)HttpStatusCode.Created && result.HttpStatusCode != (int)HttpStatusCode.NoContent)
            {
                throw new CouldNotInsertEntityException(entity.GetType().Name, this.Name);
            }
        }

        /// <summary>
        /// Batch-insert the list of entities into the table.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="entities"></param>
        public void Insert<T>(List<T> entities, Action<string> reportProgressAction = null) where T : ITableEntity
        {
            var table = this.CreateTableIfNotExists();

            // Divide the entities into batches and insert each batch.
            var batches = entities.ChunkBy(MaxEntitiesPerBatch);

            int numberOfBatches = batches.Count;
            int currentBatch = 0;

            batches.ForEach(p =>
            {
                // Create the batch operation.
                var batchOperation = new TableBatchOperation();

                // Add all entities to the batch insert operation.
                p.ForEach(q => batchOperation.Insert(q));

                // Execute the batch operation.
                table.ExecuteBatch(batchOperation);

                reportProgressAction?.Invoke($"Processed {currentBatch} of {numberOfBatches} ({currentBatch * MaxEntitiesPerBatch} entities)");
                currentBatch++;
            });
        }

        /// <summary>
        /// If the entity already exists, update it. Otherwise insert it.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="entity"></param>
        public void Upsert<T>(T entity) where T : ITableEntity
        {
            var table = this.CreateTableIfNotExists();

            TableOperation upsertOperation = TableOperation.InsertOrReplace(entity);
            var result = table.Execute(upsertOperation);

            Console.WriteLine("Upsert returned status code {0}", result.HttpStatusCode);

            // https://docs.microsoft.com/en-us/rest/api/storageservices/fileservices/insert-or-replace-entity:
            //  "A successful operation returns status code 204 (No Content)."
            if (result.HttpStatusCode != (int)HttpStatusCode.NoContent)
            {
                throw new CouldNotUpsertEntityException(entity.GetType().Name, this.Name);
            }
        }

        /// <summary>
        /// If the entity already exists, update it.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="entity"></param>
        public void Update<T>(T entity) where T : ITableEntity
        {
            var table = this.CreateTableIfNotExists();

            TableOperation upsertOperation = TableOperation.Merge(entity);
            var result = table.Execute(upsertOperation);

            // https://docs.microsoft.com/en-us/rest/api/storageservices/fileservices/insert-or-replace-entity:
            //  "A successful operation returns status code 204 (No Content)."
            if (result.HttpStatusCode != (int)HttpStatusCode.NoContent)
            {
                throw new CouldNotUpsertEntityException(entity.GetType().Name, this.Name);
            }
        }

        /// <summary>
        /// Batch-insert the list of entities into the table.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="entities"></param>
        public void Upsert<T>(List<T> entities) where T : ITableEntity
        {
            var table = this.CreateTableIfNotExists();

            // Divide the entities into batches and insert each batch.
            var batches = entities.ChunkBy(MaxEntitiesPerBatch);

            batches.ForEach(p =>
            {
                // Create the batch operation.
                var batchOperation = new TableBatchOperation();

                // Add both customer entities to the batch insert operation.
                p.ForEach(q => batchOperation.InsertOrReplace(q));

                // Execute the batch operation.
                var results = table.ExecuteBatch(batchOperation);

                // If any of the operations returned something different from NoContent, return the 
                if (results.Any(r => r.HttpStatusCode != (int)HttpStatusCode.NoContent))
                {
                    throw new CouldNotUpsertBatchException(p as List<ITableEntity>);
                }
            });
        }

        /// <summary>
        /// Batch-insert the list of entities into the table.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="entities"></param>
        public async Task UpsertAsync<T>(List<T> entities) where T : ITableEntity
        {
            var table = await this.CreateTableIfNotExistsAsync();

            // Divide the entities into batches and insert each batch.
            var batches = entities.ChunkBy(MaxEntitiesPerBatch);

            batches.ForEach(async p =>
            {
                // Create the batch operation.
                var batchOperation = new TableBatchOperation();

                Console.WriteLine($"Going to upsert {p.Count} entities");

                // Add both customer entities to the batch insert operation.
                p.ForEach(q => batchOperation.InsertOrReplace(q));

                // Execute the batch operation.
                await table.ExecuteBatchAsync(batchOperation);
            });
        }

        /// <summary>
        /// Batch-update the list of entities into the table.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="entities"></param>
        public void Update<T>(List<T> entities) where T : ITableEntity
        {
            var table = this.CreateTableIfNotExists();

            // Divide the entities into batches and insert each batch.
            var batches = entities.ChunkBy(MaxEntitiesPerBatch);

            batches.ForEach(p =>
            {
                // Create the batch operation.
                var batchOperation = new TableBatchOperation();

                Console.WriteLine($"Going to update {p.Count} entities");

                // Add both entities to the batch insert operation.
                p.ForEach(q => batchOperation.Merge(q));

                // Execute the batch operation.
                var result = table.ExecuteBatch(batchOperation);
            });
        }

        /// <summary>
        /// Batch-update the list of entities into the table.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="entities"></param>
        public async Task UpdateAsync<T>(List<T> entities) where T : ITableEntity
        {
            var table = await this.CreateTableIfNotExistsAsync();

            // Divide the entities into batches and insert each batch.
            var batches = entities.ChunkBy(MaxEntitiesPerBatch);

            batches.ForEach(async p =>
            {
                // Create the batch operation.
                var batchOperation = new TableBatchOperation();

                Console.WriteLine($"Going to update {p.Count} entities");

                // Add both entities to the batch insert operation.
                p.ForEach(q => batchOperation.Merge(q));

                // Execute the batch operation.
                await table.ExecuteBatchAsync(batchOperation);
            });
        }

        public bool Delete<T>(T entity) where T : ITableEntity
        {
            var table = this.CreateTableIfNotExists();

            var deleteOperation = TableOperation.Delete(entity);

            var result = table.Execute(deleteOperation);

            return result.HttpStatusCode == (int)HttpStatusCode.NoContent;
        }

        public async Task<bool> DeleteAsync<T>(T entity) where T : ITableEntity
        {
            var table = await this.CreateTableIfNotExistsAsync();

            var deleteOperation = TableOperation.Delete(entity);

            var result = await table.ExecuteAsync(deleteOperation);

            return result.HttpStatusCode == (int)HttpStatusCode.NoContent;
        }
    }
}