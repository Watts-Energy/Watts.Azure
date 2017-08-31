namespace Watts.Azure.Common.Batch.Objects
{
    using System.Collections.Generic;

    /// <summary>
    /// The output of a task (std out and err)
    /// </summary>
    public class TaskOutput
    {
        public string Name { get; set; }

        public List<string> Output { get; set; }
    }
}