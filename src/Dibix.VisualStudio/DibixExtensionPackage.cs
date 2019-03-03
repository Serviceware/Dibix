using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Dibix.Sdk;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;

namespace Dibix.VisualStudio
{
    /// <remarks>
    /// We know that synchronous auto loads will be deprecated in future versions,
    /// but right now we don't know another way to ensure that the SQL code analysis extension
    /// will be loaded BEFORE a database project is actually loaded and not in parallel.
    /// </remarks>
    [Guid("9B336B30-57D9-4F88-8C3A-6976CAE9C29A")]
    [InstalledProductRegistration("#110", "#112", "1.0")]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string)]
    public sealed class DibixExtensionPackage : Package
    {
        #region Overrides
        protected override void Initialize()
        {
            SolutionEvents.OnBeforeOpenProject += OnBeforeOpenProject;
        }
        #endregion

        #region Private Methods
        private static void OnBeforeOpenProject(object sender, BeforeOpenProjectEventArgs e)
        {
            try
            {
                string projectDirectory = Path.GetDirectoryName(e.Filename);
                Assembly sdkAssembly = SdkAssemblyLoader.Load(projectDirectory);
                Type initializerType = sdkAssembly.GetType("Dibix.Sdk.SdkInitializer", true);
                initializerType.InvokeMember("Initialize", BindingFlags.InvokeMethod, null, null, null);
            }
            finally
            {
                SolutionEvents.OnBeforeOpenProject -= OnBeforeOpenProject;
            }
        }
        #endregion
    }
}
