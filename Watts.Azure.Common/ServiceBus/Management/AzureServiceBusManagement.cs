namespace Watts.Azure.Common.ServiceBus.Management
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using General;
    using Interfaces.Security;
    using Interfaces.ServiceBus;
    using Microsoft.Azure.Management.ServiceBus;
    using Microsoft.Azure.Management.ServiceBus.Models;
    using Microsoft.Rest;
    using Microsoft.Rest.Azure;
    using Objects;

    /// <summary>
    /// Provides CRUD-operations on azure service bus namespaces/topics, etc.
    /// The Service bus API is quite unstable and unreliable, so the strategy used is to retry failing operations up to a certain number of times
    /// before coming to the conclusion that there's an unresolvable problem with the communication.
    /// This is of course not optimal, but is necessary for the time being.
    ///
    /// In addition, some of the methods on the service bus management client do not have async implementations, and therefore async variants can not
    /// currently be provided in this implementation either.
    /// TODO implement async variants when the functionality is available in ServiceBusManagementClient.
    /// </summary>
    public class AzureServiceBusManagement : IAzureServiceBusManagement
    {
        private const int NumberOfRetriesOnFailure = 5;

        private const int MilliseondDelayBetweenAttemptsOnFailure = 500;

        /// <summary>
        /// Authenticator against Azure AD
        /// </summary>
        private readonly IAzureActiveDirectoryAuthentication auth;

        /// <summary>
        /// Management client for service bus.
        /// </summary>
        private readonly ServiceBusManagementClient managementClient;

        /// <summary>
        /// Create a new instance of AzureServiceBusManagement
        /// </summary>
        /// <param name="authenticator"></param>
        public AzureServiceBusManagement(IAzureActiveDirectoryAuthentication authenticator)
        {
            this.auth = authenticator;
            this.managementClient = new ServiceBusManagementClient(this.auth.GetServiceCredentials())
            {
                SubscriptionId = this.auth.SubscriptionId
            };
        }

        /// <summary>
        /// Get a list of all namespaces asynchronously.
        /// </summary>
        /// <returns></returns>
        public async Task<List<SBNamespace>> GetNamespacesAsync()
        {
            IPage<SBNamespace> namespaces = await this.managementClient.Namespaces.ListByResourceGroupAsync(this.auth.ResourceGroupName);

            return namespaces.ToList();
        }

        /// <summary>
        /// Get the namespace by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public SBNamespace GetNamespace(string name)
        {
            SBNamespace retVal = this.managementClient.Namespaces.Get(this.auth.ResourceGroupName, name);

            return retVal;
        }

        /// <summary>
        /// Create a namespace by name. If it already exists, it is updated with the default settings.
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        public SBNamespace CreateOrUpdateNamespace(string namespaceName, AzureLocation location)
        {
            var existingNamespaces = this.managementClient.Namespaces.List();

            if (existingNamespaces.Select(p => p.Name).Contains(namespaceName))
            {
                return existingNamespaces.SingleOrDefault(p => p.Name == namespaceName);
            }

            var namespaceParams = new SBNamespace
            {
                Location = location.ToString(),
                Sku = new SBSku()
                {
                    Tier = SkuTier.Standard,
                    Name = SkuName.Standard,
                }
            };

            return this.managementClient.Namespaces.CreateOrUpdate(this.auth.ResourceGroupName, namespaceName, namespaceParams);
        }

        /// <summary>
        /// Get a topic by namespace and name.
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="topicName"></param>
        /// <returns></returns>
        public SBTopic GetTopic(string namespaceName, string topicName)
        {
            if (string.IsNullOrEmpty(namespaceName))
            {
                throw new Exception("Namespace name is empty!");
            }

            return this.managementClient.Topics.Get(this.auth.ResourceGroupName, namespaceName, topicName);
        }

        /// <summary>
        /// Get all topics in the namespace that start with the given string.
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="startsWith"></param>
        /// <returns></returns>
        public List<SBTopic> GetTopics(string namespaceName, string startsWith = null)
        {
            if (string.IsNullOrEmpty(namespaceName))
            {
                throw new Exception("Namespace name is empty!");
            }

            List<SBTopic> retVal = new List<SBTopic>();

            IPage<SBTopic> pages = new Page<SBTopic>();

            // List topics by namespace. If exceptions are encountered, do not be disheartened, but try again, up to a maximum number of times with a delay between each
            // attempt
            Retry.Do(() =>
            {
                try
                {
                    pages = this.managementClient.Topics.ListByNamespace(this.auth.ResourceGroupName, namespaceName);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }).WithDelayInMs(MilliseondDelayBetweenAttemptsOnFailure).MaxTimes(NumberOfRetriesOnFailure).Go();

            // Add pages that match the 'startsWith' filter
            retVal.AddRange(pages.Where(p => string.IsNullOrEmpty(startsWith) || p.Name.StartsWith(startsWith)));

            // As long as the result points to a next page, keep getting the next page and add the results.
            while (!string.IsNullOrEmpty(pages.NextPageLink))
            {
                Retry.Do(() =>
                {
                    try
                    {
                        pages = this.managementClient.Topics.ListByNamespaceNext(pages.NextPageLink);
                        retVal.AddRange(pages.Where(p =>
                            string.IsNullOrEmpty(startsWith) || p.Name.StartsWith(startsWith)));
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                })
                    .WithDelayInMs(MilliseondDelayBetweenAttemptsOnFailure)
                    .MaxTimes(5)
                    .Go();
            }

            return retVal;
        }

        /// <summary>
        /// Get the primary connection string to a namespace by name.
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <returns></returns>
        public string GetNamespaceConnectionString(string namespaceName)
        {
            if (string.IsNullOrEmpty(namespaceName))
            {
                throw new Exception("Namespace name is empty!");
            }

            var token = this.auth.GetAuthorizationToken();
            var creds = new TokenCredentials(token);
            var sbClient = new ServiceBusManagementClient(creds)
            {
                SubscriptionId = this.auth.SubscriptionId,
            };

            var keys = sbClient.Namespaces.ListKeys(this.auth.ResourceGroupName, namespaceName, "RootManageSharedAccessKey");

            return keys.PrimaryConnectionString;
        }

        /// <summary>
        /// Create a topic in a namespace. If it already exists it is updated.
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="topicName"></param>
        /// <returns></returns>
        public AzureServiceBusTopicInfo CreateOrUpdateTopic(string namespaceName, string topicName)
        {
            try
            {
                if (string.IsNullOrEmpty(namespaceName))
                {
                    throw new ArgumentException(nameof(namespaceName));
                }

                if (string.IsNullOrEmpty(topicName))
                {
                    throw new ArgumentException(nameof(topicName));
                }

                var topicParams = new SBTopic
                {
                    EnablePartitioning = false,
                };

                this.managementClient.Topics.CreateOrUpdate(this.auth.ResourceGroupName, namespaceName, topicName, topicParams);

                return new AzureServiceBusTopicInfo()
                {
                    Name = topicName
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not create a topic...");
                Console.WriteLine(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Delete all topics in a namespace whose name match the given pattern.
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="topicNamePattern"></param>
        /// <param name="reportProgressDelegate"></param>
        public void DeleteTopics(string namespaceName, string topicNamePattern, Action<string> reportProgressDelegate = null)
        {
            var topics = this.GetTopics(namespaceName, topicNamePattern);

            int currentCount = 0;
            int totalCount = topics.Count;

            foreach (var t in topics)
            {
                this.DeleteTopic(namespaceName, t.Name);
                reportProgressDelegate?.Invoke($"Deleted {currentCount} of {totalCount} topics");
                currentCount++;
            }
        }

        /// <summary>
        /// Delete a topic in a namespace by its name.
        /// </summary>
        /// <param name="namespaceName"></param>
        /// <param name="topicName"></param>
        public void DeleteTopic(string namespaceName, string topicName)
        {
            if (string.IsNullOrEmpty(namespaceName))
            {
                throw new ArgumentException(nameof(namespaceName));
            }

            if (string.IsNullOrEmpty(topicName))
            {
                throw new ArgumentException(nameof(topicName));
            }

            Retry.Do(() =>
            {
                try
                {
                    this.managementClient.Topics.Delete(this.auth.ResourceGroupName, namespaceName, topicName);
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            })
                .WithDelayInMs(MilliseondDelayBetweenAttemptsOnFailure)
                .MaxTimes(NumberOfRetriesOnFailure)
                .Go();
        }
    }
}