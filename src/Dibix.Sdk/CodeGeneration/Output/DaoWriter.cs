using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class DaoWriter : OutputWriter, IWriter
    {
        #region Fields
        private static readonly string GeneratorName = typeof(DaoWriter).Assembly.GetName().Name;
        private static readonly string Version = FileVersionInfo.GetVersionInfo(typeof(DaoWriter).Assembly.Location).FileVersion;
        private readonly IList<IDaoWriter> _writers;
        #endregion

        #region Constructor
        public DaoWriter()
        {
            this._writers = new Collection<IDaoWriter>();
            this._writers.AddRange(SelectWriters());
        }
        #endregion

        #region Overrides
        protected override void Write(StringWriter writer, OutputConfiguration configuration, SourceArtifacts artifacts)
        {
            IList<IDaoWriter> writers = this._writers.Where(x => x.HasContent(configuration, artifacts)).ToArray();
            //if (!writers.Any())
            //    return;

            string generatedCodeAnnotation = $"{typeof(GeneratedCodeAttribute).Name}(\"{GeneratorName}\", \"{Version}\")";

            // Prepare writer
            CSharpWriter output = new CSharpWriter(writer, configuration.Namespace);

            DaoWriterContext context = new DaoWriterContext(output.Root, generatedCodeAnnotation, configuration, artifacts, Format);

            for (int i = 0; i < writers.Count; i++)
            {
                IDaoWriter nestedWriter = writers[i];

                using (output.Root.CreateRegion(nestedWriter.RegionName))
                {
                    nestedWriter.Write(context);
                }

                if (i + 1 < writers.Count)
                    output.Root.AddSeparator();
            }

            output.Generate();
        }
        #endregion

        #region Private Methods
        private static IEnumerable<IDaoWriter> SelectWriters()
        {
            yield return new DaoExecutorWriter();
            yield return new DaoGridResultClassWriter();
            yield return new DaoContractClassWriter();
            yield return new DaoStructuredTypeWriter();
            yield return new DaoApiConfigurationWriter();
        }
        #endregion
    }
}