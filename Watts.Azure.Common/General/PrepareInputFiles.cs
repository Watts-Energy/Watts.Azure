namespace Watts.Azure.Common.General
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Interfaces.General;

    /// <summary>
    /// Prepares files for batch execution. Each file will serve as argument for one task to be executed in batch.
    /// </summary>
    public class PrepareInputFiles : IPrepareInputFiles
    {
        /// <summary>
        /// A delegate that prepares files to serve as batch task inputs. It should return a list of file paths to be uploaded to a blob containiner
        /// and from there distributed to the appropriate nodes.
        /// </summary>
        private readonly Func<List<string>> preparationDelegate;

        /// <summary>
        /// Creates an instance of PrepareInputFiles that will do so by applying the given function.
        /// </summary>
        /// <param name="func"></param>
        private PrepareInputFiles(Func<List<string>> func)
        {
            this.preparationDelegate = func;
        }

        /// <summary>
        /// Create an instance of PrepareInputFiles using the given delegate (which will return a list of file paths to upload and distribute to tasks).
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public static PrepareInputFiles UsingFunction(Func<List<string>> func)
        {
            return new PrepareInputFiles(func);
        }

        /// <summary>
        /// Execute the actual file preparation delegate.
        /// </summary>
        /// <returns></returns>
        public Task<List<string>> PrepareFiles()
        {
            return Task.FromResult(this.preparationDelegate());
        }
    }
}