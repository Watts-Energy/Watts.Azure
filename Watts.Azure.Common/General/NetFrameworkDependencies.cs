namespace Watts.Azure.Common.General
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Interfaces.General;

    /// <summary>
    /// General-purpose dependency resolver for a .net executable. It finds all libraries and configs located in the same directory as an executable.
    /// </summary>
    public class NetFrameworkDependencies : IDependencyResolver
    {
        /// <summary>
        /// The path to the .NET assembly that will be executed in Azure Batch.
        /// </summary>
        private readonly string executablePath;

        /// <summary>
        /// A list of patterns that the dependency resolver will exclude.
        /// </summary>
        private readonly string[] excludePatterns = new string[0];

        /// <summary>
        /// Create a new instance of a dependency resolver for a .NET executable.
        /// Will automatically find all .dlls in the same folder as the assembly and also include the config file for the
        /// main executable (at executablePath) if present.
        /// </summary>
        /// <param name="executablePath"></param>
        /// <param name="excludePatterns"></param>
        public NetFrameworkDependencies(string executablePath, List<string> excludePatterns = null)
        {
            this.executablePath = executablePath;

            if (excludePatterns != null)
            {
                this.excludePatterns = excludePatterns.ToArray();
            }
        }

        /// <summary>
        /// Find all libraries (.dll) and configuration files (.config) in the same folder as the executable at the specified path.
        /// It ignores files that contain .vshost. in their name.
        /// </summary>
        /// <returns>A list of full paths to all dependencies.</returns>
        public IEnumerable<string> Resolve()
        {
            var retVal = new List<string>();

            if (!File.Exists(this.executablePath))
            {
                throw new FileNotFoundException(this.executablePath);
            }

            var executableInfo = new FileInfo(this.executablePath);

            retVal.Add(this.executablePath);
            retVal.AddRange(
                executableInfo.Directory?.GetFiles().Where(p => !p.Name.ContainsAny(this.excludePatterns) && (p.IsNonVisualStudioLibrary() || p.IsConfigFile())).Select(fi => fi.FullName).ToList());

            return retVal;
        }
    }
}