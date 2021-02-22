using System;
using System.IO;
using AutoEmbed;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            string fileA = Resources.fileA_txt;
            Console.WriteLine(fileA);

            string fileB = Resources.subdir.fileB_txt;
            byte[] fileC = Resources.subdir.fileC_bin;

            using (var fileCStream = new BinaryReader(Resources.subdir.fileC_bin))
            {
                Console.WriteLine(fileCStream.ReadInt32());
                Console.WriteLine(fileCStream.ReadBoolean());
                Console.WriteLine(fileCStream.ReadString());
            }
        }
    }
}
