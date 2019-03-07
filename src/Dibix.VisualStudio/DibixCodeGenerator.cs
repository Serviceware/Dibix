using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.Shell;
using VSLangProj80;

namespace Dibix.VisualStudio
{
    [ComVisible(true)]
    [ProvideObject(typeof(Dibix))]
    [Guid("C80E7D6B-4FA9-4E52-9C4E-0BABA5C05FF4")]
    [CodeGeneratorRegistration(typeof(Dibix), "Dibix SQL accessor generator", vsContextGuids.vsContextGuidVCSProject, GeneratesDesignTimeSource = true)]
    public sealed class Dibix : BaseCodeGeneratorWithSite
    {
        protected override byte[] GenerateCode(string inputFilePath, string inputFileContent, string defaultNamespace)
        {
            Assembly sdkAssembly = AppDomain.CurrentDomain
                                            .GetAssemblies()
                                            .Single(x => x.GetName().Name == Constants.SdkAssemblyName);
            Type initializerType = sdkAssembly.GetType(Constants.SdkAdapterTypeName);
            object[] args = 
            {
                inputFilePath,
                inputFileContent,
                defaultNamespace,
                base.ServiceProvider
            };
            string generated = (string)initializerType.InvokeMember("InvokeGenerator", BindingFlags.InvokeMethod, null, null, args);
            return Encoding.UTF8.GetBytes(generated);
        }
    }
}