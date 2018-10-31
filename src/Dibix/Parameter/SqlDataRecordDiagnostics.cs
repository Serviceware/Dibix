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
            string[][] cells = records.Select(x =>
            {
                object[] values = new object[x.FieldCount];
                x.GetValues(values);
                return values.Select(y => y != DBNull.Value ? y.ToString() : "NULL").ToArray();
            }).ToArray();
            string dump = ToTable(columns, cells);
            return dump;
        }

        private static string DumpMetadata(SqlMetaData metadata)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(metadata.Name)
              .Append(" ")
              .Append(metadata.TypeName.ToUpperInvariant())
              .Append('(')
              .Append(metadata.MaxLength < 0 ? "MAX" : (object)metadata.MaxLength)
              .Append(')');
            return sb.ToString();
        }

        private static string ToTable(string[] columns, string[][] cells, string separator = "  ")
        {
            StringBuilder sb = new StringBuilder();
            int[] sizes = new int[columns.Length];

            // Compute column sizes
            for (int i = 0; i < columns.Length; i++)
            {
                int columnHeaderSize = columns[i].Length;
                int maxCellSize = cells.Max(x => x[i].Length);
                sizes[i] = Math.Max(columnHeaderSize, maxCellSize);
            }

            // Write column header
            for (int i = 0; i < columns.Length; i++)
            {
                sb.Append(columns[i].PadRight(sizes[i], ' '));
                if (i + 1 < columns.Length)
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
            sb.AppendLine();

            // Write rows
            for (int rowIndex = 0; rowIndex < cells.Length; rowIndex++)
            {
                // Write cells
                for (int cellIndex = 0; cellIndex < cells[rowIndex].Length; cellIndex++)
                {
                    sb.Append(cells[rowIndex][cellIndex].PadRight(sizes[cellIndex]));
                    if (cellIndex + 1 < cells[rowIndex].Length)
                        sb.Append(separator);
                }

                if (rowIndex + 1 < cells[rowIndex].Length)
                    sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}