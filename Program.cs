using System;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using PdfRpt.Core.Helper;
using PdfRpt.Core.Contracts;

namespace dotnetcore_travis_test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Reproduce ReopenForReading Bug");
            Console.WriteLine("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-");

            FileAccess[] fileAccessModes = { FileAccess.Read, FileAccess.ReadWrite };
            foreach (var fileAccessMode in fileAccessModes)
            {
                Console.WriteLine($"Testing with FileAccess.{fileAccessMode.ToString()}");
                // Generate test pdfs
                string[] generatedFiles = new string[3];
                for (var i = 0; i < generatedFiles.Length; i++)
                {
                    var filename = $"{fileAccessMode.ToString()}_temp_{i}.pdf";
                    using (Stream outputFileStream = new FileStream(filename, FileMode.Create))
                    {
                        var doc = new Document(iTextSharp.text.PageSize.A4);
                        var writer = PdfWriter.GetInstance(doc, outputFileStream);
                        doc.Open();
                        doc.Add(new Paragraph { $"Reproduce ReopenForReading Bug : Loop {i}" });
                        doc.Close();
                    }
                    generatedFiles[i] = filename;
                    Console.WriteLine($"Created {filename}");
                }

                var inputStreams = generatedFiles
                    .Select(n => (Stream)new FileStream(n, FileMode.Open, fileAccessMode)).ToList();
                var mergedFilename = $"{fileAccessMode.ToString()}_merged.pdf";
                var mergePdfDocuments = new MergePdfDocuments
                {
                    DocumentMetadata = new DocumentMetadata { Title = "Test Merge" },
                    InputFileStreams = inputStreams,
                    OutputFileStream = new FileStream(mergedFilename, FileMode.Create)
                };
                mergePdfDocuments.PerformMerge();
                Console.WriteLine($"Created merged pdf: {mergedFilename}");

                // clean up streams we opened
                foreach (var stream in inputStreams)
                {
                    stream?.Close();
                    stream?.Dispose();
                }
                // also clean merged pdf streams just in case
                foreach (var stream in mergePdfDocuments.InputFileStreams)
                {
                    stream?.Close();
                    stream?.Dispose();
                }
                mergePdfDocuments.OutputFileStream.Close();
                mergePdfDocuments.OutputFileStream.Dispose();

                // try delete temp files
                foreach (var tempFile in generatedFiles)
                {
                    try
                    {
                        File.Delete(tempFile);
                        Console.WriteLine($"Deleted {tempFile}");
                    }
                    catch (System.Exception e)
                    {
                        Console.WriteLine($"Unable to delete temp file {tempFile}: {e.Message}");
                    }
                }
                File.Delete(mergedFilename);
                Console.WriteLine($"Deleted {mergedFilename}");
                Console.WriteLine("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-");
            }
        }
    }
}
