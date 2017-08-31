namespace Watts.Azure.Common.Interfaces.General
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for a class that prepares input files to be uploaded to Azure batch as input to the processing.
    /// </summary>
    public interface IPrepareInputFiles
    {
        Task<List<string>> PrepareFiles();
    }
}