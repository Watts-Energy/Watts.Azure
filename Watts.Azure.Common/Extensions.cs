namespace Watts.Azure.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using General.Serialization;
    using Microsoft.ServiceBus.Messaging;
    using ServiceBus.Objects;

    public static class Extensions
    {
        /// <summary>
        /// Get a bool indicating whether the given file is a dll, but does not contain .vshost. in its name.
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        public static bool IsNonVisualStudioLibrary(this FileInfo fileInfo)
        {
            return fileInfo.Name.EndsWith(".dll") && !fileInfo.Name.Contains(".vshost.");
        }

        /// <summary>
        /// Get a bool indicating whether the file ends with .config.
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        public static bool IsConfigFile(this FileInfo fileInfo)
        {
            return fileInfo.Name.EndsWith(".config");
        }

        /// <summary>
        /// Taken from http://stackoverflow.com/questions/11463734/split-a-list-into-smaller-lists-of-n-size
        /// </summary>
        /// <typeparam name="T">The type of elements in the list to divide</typeparam>
        /// <param name="source"></param>
        /// <param name="chunkSize"></param>
        /// <returns>The elements of source, divided into chunks of chunksize</returns>
        public static List<List<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }

        /// <summary>
        /// Return a prettier format than the default tostring on TimeSpan.
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static string ToPrettyFormat(this TimeSpan span)
        {
            if (span == TimeSpan.Zero)
            {
                return "0 minutes";
            }

            var sb = new StringBuilder();
            if (span.Days > 0)
            {
                sb.AppendFormat("{0} day{1} ", span.Days, span.Days > 1 ? "s" : string.Empty);
            }

            if (span.Hours > 0)
            {
                sb.AppendFormat("{0} hour{1} ", span.Hours, span.Hours > 1 ? "s" : string.Empty);
            }

            if (span.Minutes > 0)
            {
                sb.AppendFormat("{0} minute{1} ", span.Minutes, span.Minutes > 1 ? "s" : string.Empty);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get a bool indicating whether the given string contains any of the given patterns.
        /// </summary>
        /// <param name="word"></param>
        /// <param name="patterns"></param>
        /// <returns></returns>
        public static bool ContainsAny(this string word, string[] patterns)
        {
            return patterns.Any(word.Contains);
        }

        /// <summary>
        /// Convert the datetime to a format that's usable when querying table storage (ISO8601).
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string ToIso8601(this DateTime dateTime)
        {
            return dateTime.ToString("s");
        }

        /// <summary>
        /// Convert the datetimeoffset to the ISO8601 format.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string ToIso8601(this DateTimeOffset dateTime)
        {
            return dateTime.ToString("s");
        }

        public static BrokeredMessage ToBrokeredMessage(this object obj, MessageFormat messageFormat = MessageFormat.Json)
        {
            object messageContent = messageFormat == MessageFormat.Json ? Json.ToJson(obj) : obj;

            BrokeredMessage message = new BrokeredMessage(messageContent);

            if (messageFormat == MessageFormat.Json)
            {
                message.ContentType = "application/json";
            }

            foreach (PropertyInfo prop in obj.GetType().GetProperties())
            {
                string name = prop.Name;
                string value = prop.GetValue(obj).ToString();

                message.Properties.Add(name, value);
            }

            return message;
        }

        public static T GetContainedObject<T>(this BrokeredMessage message)
        {
            if (message.ContentType == "application/json")
            {
                return Json.FromJson<T>(message.GetBody<string>());
            }
            else
            {
                return message.GetBody<T>();
            }
        }

        public static AzureServiceBusTopic ToTopicInstance(this AzureServiceBusTopicInfo topicInfo)
        {
            return new AzureServiceBusTopic(topicInfo.Name, "not-to-be-used", topicInfo.PrimaryConnectionString);
        }

        public static AzureServiceBusTopic ToTopicInstance(
            this AzureServiceBusTopicSubscriptionInfo topicSubscriptionInfo)
        {
            return new AzureServiceBusTopic(topicSubscriptionInfo.Name, topicSubscriptionInfo.SubscriptionName, topicSubscriptionInfo.PrimaryConnectionString);
        }
    }
}