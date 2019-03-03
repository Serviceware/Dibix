using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VSLangProj;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Dibix.VisualStudio
{
    /// <summary>
    /// Base code generator with site implementation
    /// </summary>
    public abstract class BaseCodeGeneratorWithSite : BaseCodeGenerator, IObjectWithSite
    {
        #region Fields
        private object _site;
        private CodeDomProvider _codeDomProvider;
        private ServiceProvider _siteServiceProvider;
        private ServiceProvider _globalServiceProvider;
        #endregion

        #region Properties
        protected System.IServiceProvider ServiceProvider => this.GlobalServiceProvider;

        /// <summary>
        /// Demand-creates a ServiceProvider
        /// </summary>
        private ServiceProvider SiteServiceProvider
        {
            get
            {
                if (this._siteServiceProvider != null)
                    return this._siteServiceProvider;

                this._siteServiceProvider = new ServiceProvider(this._site as IServiceProvider);
                Debug.Assert(this._siteServiceProvider != null, "Unable to get ServiceProvider from site object.");
                return this._siteServiceProvider;
            }
        }

        private ServiceProvider GlobalServiceProvider
        {
            get
            {
                if (this._globalServiceProvider == null)
                {
                    ServiceProvider siteServiceProvider = this.SiteServiceProvider;
                    if (siteServiceProvider != null)
                    {
                        if (siteServiceProvider.GetService(typeof(IVsHierarchy)) is IVsHierarchy service)
                        {
                            ErrorHandler.ThrowOnFailure(service.GetSite(out IServiceProvider ppSP));
                            if (ppSP != null)
                                this._globalServiceProvider = new ServiceProvider(ppSP);
                        }
                    }
                }
                return this._globalServiceProvider;
            }
        }
        #endregion

        #region IObjectWithSite Members
        /// <summary>
        /// GetSite method of IOleObjectWithSite
        /// </summary>
        /// <param name="riid">interface to get</param>
        /// <param name="ppvSite">IntPtr in which to stuff return value</param>
        void IObjectWithSite.GetSite(ref Guid riid, out IntPtr ppvSite)
        {
            if (this._site == null)
                throw new COMException("object is not sited", VSConstants.E_FAIL);

            IntPtr pUnknownPointer = Marshal.GetIUnknownForObject(this._site);
            Marshal.QueryInterface(pUnknownPointer, ref riid, out IntPtr intPointer);

            if (intPointer == IntPtr.Zero)
                throw new COMException("site does not support requested interface", VSConstants.E_NOINTERFACE);

            ppvSite = intPointer;
        }

        /// <summary>
        /// SetSite method of IOleObjectWithSite
        /// </summary>
        /// <param name="pUnkSite">site for this object to use</param>
        void IObjectWithSite.SetSite(object pUnkSite)
        {
            this._site = pUnkSite;
            this._codeDomProvider = null;
            this._siteServiceProvider = null;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Method to get a service by its GUID
        /// </summary>
        /// <param name="serviceGuid">GUID of service to retrieve</param>
        /// <returns>An object that implements the requested service</returns>
        protected object GetService(Guid serviceGuid) => this.SiteServiceProvider.GetService(serviceGuid);

        /// <summary>
        /// Method to get a service by its Type
        /// </summary>
        /// <param name="serviceType">Type of service to retrieve</param>
        /// <returns>An object that implements the requested service</returns>
        protected object GetService(Type serviceType) => this.SiteServiceProvider.GetService(serviceType);

        /// <summary>
        /// Returns a CodeDomProvider object for the language of the project containing
        /// the project item the generator was called on
        /// </summary>
        /// <returns>A CodeDomProvider object</returns>
        protected virtual CodeDomProvider GetCodeProvider()
        {
            if (this._codeDomProvider == null)
            {
                //Query for IVSMDCodeDomProvider/SVSMDCodeDomProvider for this project type
                if (this.GetService(typeof(SVSMDCodeDomProvider)) is IVSMDCodeDomProvider provider)
                {
                    this._codeDomProvider = provider.CodeDomProvider as CodeDomProvider;
                }
                else
                {
                    //In the case where no language specific CodeDom is available, fall back to C#
                    this._codeDomProvider = CodeDomProvider.CreateProvider("C#");
                }
            }
            return this._codeDomProvider;
        }

        /// <summary>
        /// Returns the EnvDTE.ProjectItem object that corresponds to the project item the code 
        /// generator was called on
        /// </summary>
        /// <returns>The EnvDTE.ProjectItem of the project item the code generator was called on</returns>
        protected ProjectItem GetProjectItem()
        {
            object p = this.GetService(typeof(ProjectItem));
            Debug.Assert(p != null, "Unable to get Project Item.");
            return (ProjectItem)p;
        }

        /// <summary>
        /// Returns the EnvDTE.Project object of the project containing the project item the code 
        /// generator was called on
        /// </summary>
        /// <returns>
        /// The EnvDTE.Project object of the project containing the project item the code generator was called on
        /// </returns>
        protected Project GetProject()
        {
            return this.GetProjectItem().ContainingProject;
        }

        /// <summary>
        /// Returns the VSLangProj.VSProjectItem object that corresponds to the project item the code 
        /// generator was called on
        /// </summary>
        /// <returns>The VSLangProj.VSProjectItem of the project item the code generator was called on</returns>
        protected VSProjectItem GetVSProjectItem()
        {
            return (VSProjectItem)this.GetProjectItem().Object;
        }

        /// <summary>
        /// Returns the VSLangProj.VSProject object of the project containing the project item the code 
        /// generator was called on
        /// </summary>
        /// <returns>
        /// The VSLangProj.VSProject object of the project containing the project item 
        /// the code generator was called on
        /// </returns>
        protected VSProject GetVSProject()
        {
            return (VSProject)this.GetProject().Object;
        }
        #endregion

        #region Overrides
        /// <summary>
        /// Gets the default extension of the output file from the CodeDomProvider
        /// </summary>
        /// <returns></returns>
        protected override string GetDefaultExtension()
        {
            CodeDomProvider codeDom = this.GetCodeProvider();
            Debug.Assert(codeDom != null, "CodeDomProvider is NULL.");
            string extension = codeDom.FileExtension;
            if (extension.Length > 0)
                extension = "." + extension.TrimStart(".".ToCharArray());

            return extension;
        }
        #endregion
    }
}