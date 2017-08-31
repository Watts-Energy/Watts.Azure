namespace Watts.Azure.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Batch.Objects;
    using Interfaces.Wrappers;
    using Microsoft.Azure.Batch;

    public class PredefinedMachines
    {
        public static AzureMachineConfig SmallOneCore => new AzureMachineConfig()
        {
            Size = "small"
        };

        public static AzureMachineConfig MediumTwoCores => new AzureMachineConfig()
        {
            Size = "medium"
        };

        public static AzureMachineConfig LargeFourCores => new AzureMachineConfig()
        {
            Size = "large"
        };

        public static AzureMachineConfig ExtraLargeEightCores => new AzureMachineConfig()
        {
            Size = "extralarge"
        };

        /// <summary>
        /// Get an UbuntuServer 14.04 VM configuration by looking up and choosing the correct one among available SKUs.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static VirtualMachineConfiguration GetUbuntu14_04VmConfiguration(IAzureBatchClient client)
        {
            // Obtain a collection of all available node agent SKUs.
            // This allows us to select from a list of supported
            // VM image/node agent combinations.
            List<NodeAgentSku> nodeAgentSkus =
                client.PoolOperations.ListNodeAgentSkus().ToList();

            // Define a delegate specifying properties of the VM image
            // that we wish to use.
            Func<ImageReference, bool> isUbuntu1404 = imageRef =>
                imageRef.Publisher == "Canonical" &&
                imageRef.Offer == "UbuntuServer" &&
                imageRef.Sku.Contains("14.04");

            // Obtain the first node agent SKU in the collection that matches
            // Ubuntu Server 14.04. Note that there are one or more image
            // references associated with this node agent SKU.
            NodeAgentSku ubuntuAgentSku = nodeAgentSkus.First(sku =>
                sku.VerifiedImageReferences.Any(isUbuntu1404));

            // Select an ImageReference from those available for node agent.
            ImageReference imageReference =
                ubuntuAgentSku.VerifiedImageReferences.First(isUbuntu1404);

            // Create the VirtualMachineConfiguration for use when actually
            // creating the pool
            VirtualMachineConfiguration virtualMachineConfiguration =
                new VirtualMachineConfiguration(
                    imageReference: imageReference,
                    nodeAgentSkuId: ubuntuAgentSku.Id,
                    windowsConfiguration: null);

            return virtualMachineConfiguration;
        }

        public static VirtualMachineConfiguration GetDebian8VmConfiguration(IAzureBatchClient client)
        {
            // Obtain a collection of all available node agent SKUs.
            // This allows us to select from a list of supported
            // VM image/node agent combinations.
            List<NodeAgentSku> nodeAgentSkus =
                client.PoolOperations.ListNodeAgentSkus().ToList();

            // Define a delegate specifying properties of the VM image
            // that we wish to use.
            Func<ImageReference, bool> isDebian8 = imageRef =>
                imageRef.Publisher == "Credativ" &&
                imageRef.Offer == "Debian" &&
                imageRef.Sku.Equals("8");

            // Obtain the first node agent SKU in the collection that matches
            // Ubuntu Server 14.04. Note that there are one or more image
            // references associated with this node agent SKU.
            NodeAgentSku ubuntuAgentSku = nodeAgentSkus.First(sku =>
                sku.VerifiedImageReferences.Any(isDebian8));

            // Select an ImageReference from those available for node agent.
            ImageReference imageReference =
                ubuntuAgentSku.VerifiedImageReferences.First(isDebian8);

            // Create the VirtualMachineConfiguration for use when actually
            // creating the pool
            VirtualMachineConfiguration virtualMachineConfiguration =
                new VirtualMachineConfiguration(
                    imageReference: imageReference,
                    nodeAgentSkuId: ubuntuAgentSku.Id,
                    windowsConfiguration: null);

            return virtualMachineConfiguration;
        }

        public static VirtualMachineConfiguration GetLinuxConfigurationById(IAzureBatchClient client, string id)
        {
            NodeAgentSku nodeAgentSku =
                client.PoolOperations.ListNodeAgentSkus().SingleOrDefault(p => p.Id.Equals(id));

            if (nodeAgentSku == null)
            {
                return null;
            }

            // Select an ImageReference from those available for node agent.
            ImageReference imageReference =
                nodeAgentSku.VerifiedImageReferences.First();

            // Create the VirtualMachineConfiguration for use when actually
            // creating the pool
            VirtualMachineConfiguration virtualMachineConfiguration =
                new VirtualMachineConfiguration(
                    imageReference: imageReference,
                    nodeAgentSkuId: nodeAgentSku.Id,
                    windowsConfiguration: null);

            return virtualMachineConfiguration;
        }
    }
}