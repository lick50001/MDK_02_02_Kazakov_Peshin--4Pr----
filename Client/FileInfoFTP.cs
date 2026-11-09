using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class FileInfoFTP
    {
        public byte[] File { get; set; }
        public string Name { get; set; }

        public FileInfoFTP(byte[] file, string name)
        {
            File = file;
            Name = name;
        }
    }
}