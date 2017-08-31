namespace Watts.Azure.Common.Batch.Objects
{
    using System.Collections.Generic;

    public class BatchConsoleCommand
    {
        public string BaseCommand { get; set; }

        public List<string> Arguments { get; set; } = new List<string>();
    }
}