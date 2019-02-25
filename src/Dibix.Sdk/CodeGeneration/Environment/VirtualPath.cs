using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    [DebuggerDisplay("{Path} (Recursive: {IsRecursive})")]
    public class VirtualPath
    {
        public ICollection<string> Segments { get; }
        public string Path => String.Join(System.IO.Path.DirectorySeparatorChar.ToString(), this.Segments);
        public bool IsRecursive { get; private set; }
        public bool IsCurrent => !this.Segments.Any();

        public VirtualPath()
        {
            this.Segments = new Collection<string>();
        }

        public static implicit operator string(VirtualPath virtualPath) => virtualPath.Path;

        public static implicit operator VirtualPath(string virtualPath)
        {
            char separator = System.IO.Path.DirectorySeparatorChar;
            string separatorStr = separator.ToString();
            virtualPath = virtualPath.Replace("/", separatorStr).Trim(separator);
            string[] parts = virtualPath.Split(separator);

            VirtualPath result = new VirtualPath();
            for (int i = 0; i < parts.Length; i++)
            {
                bool isStart = i == 0;
                bool isEnd = i + 1 == parts.Length;
                string part = parts[i];

                result.IsRecursive = isEnd && part == "**";
                bool skipSegment = result.IsRecursive || (isStart || isEnd) && (part == "*" || part == ".");
                if (!skipSegment)
                    result.Segments.Add(part);
            }

            return result;
        }
    }
}