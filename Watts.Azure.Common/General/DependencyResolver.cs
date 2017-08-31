namespace Watts.Azure.Common.General
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces.General;

    /// <summary>
    /// General dependency resolver class for specifying the dependencies of an executable to run in Azure Batch.
    /// </summary>
    public class DependencyResolver : IManualDependencyResolver
    {
        /// <summary>
        /// The function to use when resolving dependencies.
        /// </summary>
        private readonly Func<IEnumerable<string>> resolveFunction;

        /// <summary>
        /// List of all manually added dependencies, which are in addition to the automatically resolved dependencies.
        /// </summary>
        private readonly List<string> addedDependencies = new List<string>();

        private DependencyResolver(Func<IEnumerable<string>> resolveFunc)
        {
            this.resolveFunction = resolveFunc;
        }

        /// <summary>
        /// Create a new instance of DependencyResolver that will do so by applying the given function.
        /// The Func should return a list of all libraries and other files that the execution of whatever it to be executed in batch,
        /// depends on.
        /// </summary>
        /// <param name="returnDependencyFilePathsFunction"></param>
        /// <returns></returns>
        public static DependencyResolver UsingFunction(Func<IEnumerable<string>> returnDependencyFilePathsFunction)
        {
            return new DependencyResolver(returnDependencyFilePathsFunction);
        }

        /// <summary>
        /// Manually specify a dependency on the file at the given path.
        /// </summary>
        /// <param name="filepath"></param>
        public void AddFileDependency(string filepath)
        {
            this.addedDependencies.Add(filepath);
        }

        /// <summary>
        /// Generate the list of filepaths to required files.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> Resolve()
        {
            List<string> retVal = this.resolveFunction().ToList();

            retVal.AddRange(this.addedDependencies);

            return retVal;
        }
    }
}