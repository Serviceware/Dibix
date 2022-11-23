using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Server;

namespace Dibix
{
    internal static class SqlDataRecordDiagnostics
    {
        public static string Dump(IEnumerable<SqlMetaData> metadata, IEnumerable<SqlDataRecord> records)
        {
            string[] columns = metadata.Select(DumpMetadata).ToArray();
            string[][] rows = records.Select(x =>
            {
                object[] values = new object[x.FieldCount];
                x.GetValues(values);
                return values.Select(y => y != DBNull.Value ? y.ToString() : "NULL").ToArray();
            }).ToArray();
            string dump = ToTable(columns, rows);
            return dump;
        }

        private static string DumpMetadata(SqlMetaData metadata)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(metadata.Name)
              .Append(" ")
              .Append(metadata.TypeName.ToUpperInvariant())
              .Append('(')
              .Append(metadata.MaxLength < 0 ? "MAX" : metadata.MaxLength)
              .Append(')');
            return sb.ToString();
        }

        private static string ToTable(IReadOnlyList<string> columns, string[][] rows, string separator = "  ")
        {
            StringBuilder sb = new StringBuilder();
            int[] sizes = new int[columns.Count];

            // Compute column sizes
            for (int i = 0; i < columns.Count; i++)
            {
                int columnHeaderSize = columns[i].Length;
                int maxCellSize = rows.Any() ? rows.Max(x => x[i].Length) : 0;
                sizes[i] = Math.Max(columnHeaderSize, maxCellSize);
            }

            // Write column header
            for (int i = 0; i < columns.Count; i++)
            {
                sb.Append(columns[i].PadRight(sizes[i], ' '));
                if (i + 1 < columns.Count)
                    sb.Append(separator);
            }
            sb.AppendLine();

            // Write Separator
            for (int i = 0; i < sizes.Length; i++)
            {
                sb.Append(new string('-', sizes[i]));
                if (i + 1 < sizes.Length)
                    sb.Append(separator);
            }

            // Write rows
            foreach (string[] row in rows)
            {
                sb.AppendLine();

                // Write cells
                for (int cellIndex = 0; cellIndex < row.Length; cellIndex++)
                {
                    sb.Append(row[cellIndex].PadRight(sizes[cellIndex]));
                    if (cellIndex + 1 < row.Length)
                        sb.Append(separator);
                }
            }

            return sb.ToString();
        }
    }
}