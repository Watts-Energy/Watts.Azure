namespace Watts.Azure.Common.OutputHelper
{
    using System;
    using System.IO;
    using Storage.Objects;

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("OutputHelper invoked with arguments: {0}", string.Join(", ", args));

            string blobConnectionString = args[0];

            // The name that the uploaded blob should be given.
            string blobName = args[1];

            var blobStorage = AzureBlobStorage.Connect(blobConnectionString, "batchoutput");

            for (int i = 2; i < args.Length; i++)
            {
                if (!File.Exists(args[i]))
                {
                    Console.Error.WriteLine("The file {0} does not exist", args[i]);
                    continue;
                }

                string contents;

                Console.WriteLine("Reading output...");

                using (FileStream fileStream = new FileStream(
                                                              args[i],
                                                              FileMode.Open,
                                                              FileAccess.Read,
                                                              FileShare.Read))
                {
                    using (StreamReader streamReader = new StreamReader(fileStream))
                    {
                        contents = streamReader.ReadToEnd();

                        Console.WriteLine("contents is {0}", contents);
                    }
                }

                FileInfo fi = new FileInfo(args[i]);

                var fileLines = contents.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                File.WriteAllLines($"file{i}.txt", fileLines);

                Console.WriteLine("Wrote all lines to {0}", $"file{i}.txt");

                Console.WriteLine($"Uploading file to blob {blobName}_{fi.Name}...");
                blobStorage.UploadFromFile($"file{i}.txt", $"{fi.Name}");
            }
        }
    }
}