namespace Watts.Azure.Common.Batch.Objects
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The output of a task (std out and err)
    /// </summary>
    public class TaskOutput 
    {
        public static TaskOutput Empty { get; } = new TaskOutput() { Name= string.Empty, StdOut = new List<string>(), StdErr = new List<string>() };

        public string Name { get; set; }

        public List<string> StdOut { get; set; }

        public List<string> StdErr { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(TaskOutput))
            {
                return false;
            }

            var otherTaskOutput = (TaskOutput) obj;

            // If the names do not match, they are not equal.
            if ( this.Name != otherTaskOutput.Name)
            {
                return false;
            }

            // If there is an unequal number of lines in either stdout or stderr, they are not equal.
            if (this.StdOut.Count != otherTaskOutput.StdOut.Count || this.StdErr.Count != otherTaskOutput.StdErr.Count)
            {
                return false;
            }

            // Compare all lines in stdout
            for (int i = 0; i < this.StdOut.Count; i++)
            {
                if (this.StdOut[i] != otherTaskOutput.StdOut[i])
                {
                    return false;
                }
            }

            // Compare all lines in stderr
            for (int i = 0; i < this.StdErr.Count; i++)
            {
                if (this.StdErr[i] != otherTaskOutput.StdErr[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}