using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenSpartan.HaloInfinite.MCP.Helpers
{
    internal static class FileSystemHelpers
    {
        internal static void EnsureDirectoryExists(string path)
        {
            var file = new FileInfo(path);
            file.Directory.Create();
        }
    }
}
