using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace AutoEmbed.Internal
{
    public class EmbedItem
    {
        private readonly string resourceName;
        private readonly Assembly assembly;

        public EmbedItem(string resourceName, Assembly assembly)
        {
            this.resourceName = resourceName;
            this.assembly = assembly;
        }

        public string ReadAsString()
        {
            using var reader = new StreamReader(this.ReadAsStream());
            return reader.ReadToEnd();
        }

        public byte[] ReadAsBytes()
        {
            using var reader = this.ReadAsStream();
            byte[] data = new byte[reader.Length];
            reader.Read(data, 0, data.Length);
            return data;
        }

        public Stream ReadAsStream() => assembly.GetManifestResourceStream(this.resourceName);

        public static implicit operator string(EmbedItem item) => item.ReadAsString();

        public static implicit operator byte[](EmbedItem item) => item.ReadAsBytes();

        public static implicit operator Stream(EmbedItem item) => item.ReadAsStream();


    }
}
