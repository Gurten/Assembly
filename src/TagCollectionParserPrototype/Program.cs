using System;
using System.IO;
using Blamite.Injection;
using Blamite.IO;

namespace TagCollectionParserPrototype
{
    class Program
    {
        static void Main(string[] args)
        {

            string tagFilePath = @"C:\Users\gurten\Documents\tags\reach\ff_plat_1x1.tagc";
            Console.WriteLine("Hello World!");
            TagContainer container;
            using (var reader = new EndianReader(File.OpenRead(tagFilePath), Blamite.IO.Endian.LittleEndian))
                container = TagContainerReader.ReadTagContainer(reader);

            Console.WriteLine(container.ToString());
        }
    }
}
