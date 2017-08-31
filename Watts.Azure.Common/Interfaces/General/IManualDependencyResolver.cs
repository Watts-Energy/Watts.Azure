namespace Watts.Azure.Common.Interfaces.General
{
    /// <summary>
    /// Dependency resolver for adding all dependencies to be uploaded to batch, manually.
    /// </summary>
    public interface IManualDependencyResolver : IDependencyResolver
    {
        void AddFileDependency(string filepath);
    }
}