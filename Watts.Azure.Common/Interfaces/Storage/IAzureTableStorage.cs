namespace Watts.Azure.Common.Interfaces.Storage
{
    using System;
    using System.Collections.Generic;
    using DataFactory;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// Interface for an Azure TableStorage
    /// </summary>
    public interface IAzureTableStorage : IAzureLinkedService
    {
        CloudTable GetTableReference();

        /// <summary>
        /// Create a table if it doesn't exist.
        /// </summary>
        /// <returns>The cloud table</returns>
        CloudTable CreateTableIfNotExists();

        CloudTable CreateTableFromTemplateEntity(DynamicTableEntity templateEntity);

        bool DeleteIfExists();

        /// <summary>
        /// Insert an entity into the given table. Note that the entity must implement ITableEntity.
        /// </summary>
        /// <typeparam name="T">The type of the entity to insert</typeparam>
        /// <param name="entity"></param>
        void Insert<T>(T entity) where T : ITableEntity;

        /// <summary>
        /// Batch-insert the list of entities into the table.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="entity"></param>
        /// <param name="reportProgressAction">An action to report progress on (optional)</param>
        void Insert<T>(List<T> entity, Action<string> reportProgressAction = null) where T : ITableEntity;

        /// <summary>
        /// If the entity already exists, update it. Otherwise insert it.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="entity"></param>
        void Upsert<T>(T entity) where T : ITableEntity;

        /// <summary>
        /// Update the entity if it exists.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="entity"></param>
        void Update<T>(T entity) where T : ITableEntity;

        /// <summary>
        /// Batch-insert the list of entities into the table.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="entity"></param>
        void Upsert<T>(List<T> entity) where T : ITableEntity;

        /// <summary>
        /// Batch update the entitities
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="entities"></param>
        void Update<T>(List<T> entities) where T : ITableEntity;

        /// <summary>
        /// Get a single entity by a partitionkey. Note that this will throw an exception if
        /// there is more than one with the same partition key.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="partitionKey"></param>
        /// <returns></returns>
        List<T> Get<T>(string partitionKey) where T : ITableEntity, new();

        List<DynamicTableEntity> GetTop(int count);

        List<DynamicTableEntity> Get(string tableQueryString);

        bool Delete<T>(T entity) where T : ITableEntity;
    }
}