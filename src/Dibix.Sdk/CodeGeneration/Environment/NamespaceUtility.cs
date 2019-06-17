﻿using System;
using System.Text;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class NamespaceUtility
    {
        public static string BuildNamespace(string @namespace, bool multipleAreas, string layerName)
        {
            if (String.IsNullOrEmpty(@namespace))
                return layerName;

            if (!String.IsNullOrEmpty(layerName))
            {
                StringBuilder sb = new StringBuilder();
                if (multipleAreas)
                {
                    string[] parts = @namespace.Split(new[] { '.' }, 2);

                    sb.Append(parts[0])
                      .Append('.')
                      .Append(layerName);

                    if (parts.Length > 1)
                        sb.Append('.')
                          .Append(parts[1]);
                }
                else
                {
                    sb.Append(layerName)
                      .Append('.')
                      .Append(@namespace);
                }

                @namespace = sb.ToString();
            }

            return @namespace;
        }
    }
}