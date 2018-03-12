namespace Watts.Azure.Common.Batch.Objects
{
    using Interfaces.Wrappers;
    using Microsoft.Azure.Batch;
    using Constants = Common.Constants;

    public class AzureMachineConfig
    {
        public string Size { get; set; }

        public int NumberOfNodes { get; set; }

        public VirtualMachineConfiguration VirtualMachineConfiguration { get; set; }

        public CloudServiceConfiguration CloudServiceConfiguration { get; set; }

        public OperatingSystemFamily OperatingSystemFamily => this.IsLinux() ? OperatingSystemFamily.Linux : OperatingSystemFamily.Windows;

        /// <summary>
        /// Single-core, 2 GB memory, 10 GB Local SSD, Max 2 data disks, 2x500 max data disk throughput, max nics / Network bandwidth: 1 / moderate
        /// at the time of writing (2017)
        /// </summary>
        /// <returns>A machine config corresponding to a small machine</returns>
        public static AzureMachineConfig Small()
        {
            return Constants.SmallOneCore.WindowsServer2012R2();
        }

        /// <summary>
        /// Dual-core, 4 GB memory, 20 GB Local SSD, Max 4 data disks, 4x500 max data disk throughput, max nics / Network bandwidth: 2 / moderate
        /// </summary>
        /// <returns>A machine config corresponding to a 'medium' machine</returns>
        public static AzureMachineConfig Medium()
        {
            return Constants.MediumTwoCores.WindowsServer2012R2();
        }

        /// <summary>
        /// Quad-core, 7 GB memory, 285 GB Local SSD, Max 8 data disks, 8x500 max data disk throughput, max nics / Network bandwidth: 2 / high
        /// </summary>
        /// <returns>A machine config corresponding to a 'large' machine</returns>
        public static AzureMachineConfig Large()
        {
            return Constants.LargeFourCores.WindowsServer2012R2();
        }

        public static AzureMachineConfig StandardD1()
        {
            return Constants.Standard_D1.WindowsServer2012R2();
        }

        public static AzureMachineConfig StandardD1_V2()
        {
            return Constants.Standard_D1_v2.WindowsServer2012R2();
        }

        public static AzureMachineConfig StandardA1_V2()
        {
            return Constants.Standard_A1_v2.WindowsServer2012R2();
        }

        public AzureMachineConfig WindowsServer2012R2()
        {
            this.VirtualMachineConfiguration = null;
            this.CloudServiceConfiguration = new CloudServiceConfiguration(osFamily: "4");
            return this;
        }

        /// <summary>
        /// Use Ubuntu
        /// </summary>
        /// <param name="client"></param>
        /// <returns>An azure machine config for UbuntServer</returns>
        public AzureMachineConfig Ubuntu(IAzureBatchClient client)
        {
            this.VirtualMachineConfiguration = PredefinedMachines.GetUbuntu14_04VmConfiguration(client);
            this.CloudServiceConfiguration = null;
            this.Size = this.WindowsSizeToLinuxSize();
            return this;
        }

        public AzureMachineConfig Debian(IAzureBatchClient client)
        {
            this.VirtualMachineConfiguration = PredefinedMachines.GetDebian8VmConfiguration(client);
            this.CloudServiceConfiguration = null;
            this.Size = this.WindowsSizeToLinuxSize();
            return this;
        }

        /// <summary>
        /// Specify the number of nodes to use in the pool.
        /// </summary>
        /// <param name="numberOfInstances">The number of machines to use</param>
        /// <returns>A machine config specifying the given number of instances</returns>
        public AzureMachineConfig Instances(int numberOfInstances)
        {
            this.NumberOfNodes = numberOfInstances;
            return this;
        }

        public bool IsLinux()
        {
            return this.VirtualMachineConfiguration != null;
        }

        internal string WindowsSizeToLinuxSize()
        {
            switch (this.Size.ToLower())
            {
                case "small":
                    return "STANDARD_A1";

                case "medium":
                    return "STANDARD_A2";

                case "large":
                    return "STANDARD_A3";

                case "extralarge":
                    return "STANDARD_A4";

                default:
                    return this.Size;
            }
        }
    }
}