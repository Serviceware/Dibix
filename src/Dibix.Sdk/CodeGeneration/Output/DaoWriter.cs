using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class DaoWriter : OutputWriter, IWriter
    {
        #region Fields
        private static readonly string GeneratorName = typeof(DaoWriter).Assembly.GetName().Name;
        private static readonly string Version = FileVersionInfo.GetVersionInfo(typeof(DaoWriter).Assembly.Location).FileVersion;
        private readonly IList<IDaoChildWriter> _writers;
        #endregion

        #region Constructor
        public DaoWriter()
        {
            this._writers = new Collection<IDaoChildWriter>();
            this._writers.AddRange(SelectWriters());
        }
        #endregion

        #region Overrides
        protected override void Write(StringWriter writer, OutputConfiguration configuration, SourceArtifacts artifacts)
        {
            IList<IDaoChildWriter> writers = this._writers.Where(x => x.HasContent(artifacts)).ToArray();
            //if (!writers.Any())
            //    return;

            string generatedCodeAnnotation = $"{typeof(GeneratedCodeAttribute).Name}(\"{GeneratorName}\", \"{Version}\")";

            // Prepare writer
            IEnumerable<string> globalAnnotations = writers.SelectMany(x => x.GetGlobalAnnotations(configuration)).Distinct().OrderBy(x => x.Length);
            CSharpWriter output = new CSharpWriter(writer, configuration.RootNamespace, globalAnnotations);

            WriterContext context = new WriterContext(output.Root, generatedCodeAnnotation, configuration, artifacts, Format);

            IList<IGrouping<string, IDaoChildWriter>> childWriterGroups = writers.GroupBy(x => x.LayerName).ToArray();
            for (int i = 0; i < childWriterGroups.Count; i++)
            {
                IGrouping<string, IDaoChildWriter> nestedWriterGroup = childWriterGroups[i];
                
                // Don't enter layer name if project has multiple areas
                if (context.Configuration.AreaName != null)
                    context.Output = output.Root.BeginScope(nestedWriterGroup.Key);

                IList<IDaoChildWriter> nestedWriters = nestedWriterGroup.ToArray();
                for (int j = 0; j < nestedWriters.Count; j++)
                {
                    IDaoChildWriter nestedWriter = nestedWriters[j];
                    using (context.Output.CreateRegion(nestedWriter.RegionName))
                    {
                        nestedWriter.Write(context);
                    }

                    if (j + 1 < nestedWriters.Count)
                        context.Output.AddSeparator();
                }

                if (i + 1 < childWriterGroups.Count)
                    context.Output.AddSeparator();
            }

            output.Generate();
        }
        #endregion

        #region Private Methods
        private static IEnumerable<IDaoChildWriter> SelectWriters()
        {
            yield return new DaoExecutorWriter();
            yield return new DaoExecutorInputClassWriter();
            yield return new DaoGridResultClassWriter();
            yield return new DaoContractClassWriter();
            yield return new DaoStructuredTypeWriter();
            yield return new DaoApiConfigurationWriter();
        }
        #endregion
    }
}