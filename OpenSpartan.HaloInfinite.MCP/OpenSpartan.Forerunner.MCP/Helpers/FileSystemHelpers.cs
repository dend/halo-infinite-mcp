using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenSpartan.Forerunner.MCP.Helpers
{
    internal static class FileSystemHelpers
    {
        internal static void EnsureDirectoryExists(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            // If path appears to be a directory (no extension or ends with separator)
            if (string.IsNullOrEmpty(Path.GetExtension(path)) || 
                path.EndsWith(Path.DirectorySeparatorChar.ToString()) || 
                path.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                Directory.CreateDirectory(path);
            }
            else
            {
                // Assume it's a file path, create its directory
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
            }
        }

        internal static string NormalizePathForFileSystem(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // Replace characters that are invalid in file paths
            var invalidChars = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Distinct().ToArray();
            
            // Replace invalid characters with underscores
            foreach (var invalidChar in invalidChars)
            {
                path = path.Replace(invalidChar, '_');
            }

            // Ensure there are no consecutive directory separators
            while (path.Contains("//"))
                path = path.Replace("//", "/");
            
            return path.TrimStart('/');
        }
    }
}
