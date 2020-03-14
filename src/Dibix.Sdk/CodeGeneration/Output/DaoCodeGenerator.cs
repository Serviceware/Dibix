using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Dibix.Sdk.CodeGeneration.CSharp;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class DaoCodeGenerator : CodeGenerator
    {
        #region Fields
        private static readonly string GeneratorName = typeof(DaoCodeGenerator).Assembly.GetName().Name;
        private static readonly string Version = FileVersionInfo.GetVersionInfo(typeof(DaoCodeGenerator).Assembly.Location).FileVersion;
        private readonly IList<DaoWriter> _writers;
        private readonly ISchemaRegistry _schemaRegistry;
        #endregion

        #region Constructor
        public DaoCodeGenerator(IErrorReporter errorReporter, ISchemaRegistry schemaRegistry) : base(errorReporter)
        {
            this._schemaRegistry = schemaRegistry;
            this._writers = new Collection<DaoWriter>();
            this._writers.AddRange(SelectWriters());
        }
        #endregion

        #region Overrides
        protected override void Write(StringWriter writer, CodeGenerationModel model)
        {
            IList<DaoWriter> writers = this._writers.Where(x => x.HasContent(model)).ToArray();
            //if (!writers.Any())
            //    return;

            string generatedCodeAnnotation = $"{typeof(GeneratedCodeAttribute).Name}(\"{GeneratorName}\", \"{Version}\")";

            // Prepare writer
            bool isArtifactAssembly = model.CompatibilityLevel == CodeGeneratorCompatibilityLevel.Full;
            IEnumerable<string> globalAnnotations = isArtifactAssembly ? Enumerable.Repeat("ArtifactAssembly", 1) : Enumerable.Empty<string>();
            globalAnnotations = globalAnnotations.Concat(writers.SelectMany(x => x.GetGlobalAnnotations(model)).Distinct().OrderBy(x => x.Length));
            CSharpWriter output = new CSharpWriter(writer, model.RootNamespace, globalAnnotations);

            DaoCodeGenerationContext context = new DaoCodeGenerationContext(output.Root, generatedCodeAnnotation, model, this._schemaRegistry);

            IList<IGrouping<string, DaoWriter>> childWriterGroups = writers.GroupBy(x => x.LayerName).ToArray();
            for (int i = 0; i < childWriterGroups.Count; i++)
            {
                IGrouping<string, DaoWriter> nestedWriterGroup = childWriterGroups[i];
                
                // Don't enter layer name if project has multiple areas
                if (context.Model.AreaName != null)
                    context.Output = output.Root.BeginScope(nestedWriterGroup.Key);

                IList<DaoWriter> nestedWriters = nestedWriterGroup.ToArray();
                for (int j = 0; j < nestedWriters.Count; j++)
                {
                    DaoWriter nestedWriter = nestedWriters[j];
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
        private static IEnumerable<DaoWriter> SelectWriters()
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