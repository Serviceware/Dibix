﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Dibix.Sdk.Tests {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resource {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resource() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Dibix.Sdk.Tests.Resource", typeof(Resource).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /*------------------------------------------------------------------------------
        ///// &lt;auto-generated&gt;
        /////     This code was generated by a tool.
        /////     Runtime Version:4.0.30319.42000
        /////
        /////     Changes to this file may cause incorrect behavior and will be lost if
        /////     the code is regenerated.
        ///// &lt;/auto-generated&gt;
        /////----------------------------------------------------------------------------*/
        ///using System.Collections.Generic;
        ///using System.Collections.ObjectModel;
        ///using System.Reflection;
        ///using D [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string ParserTest {
            get {
                return ResourceManager.GetString("ParserTest", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /*------------------------------------------------------------------------------
        ///// &lt;auto-generated&gt;
        /////     This code was generated by a tool.
        /////     Runtime Version:4.0.30319.42000
        /////
        /////     Changes to this file may cause incorrect behavior and will be lost if
        /////     the code is regenerated.
        ///// &lt;/auto-generated&gt;
        /////----------------------------------------------------------------------------*/
        ///using System.Reflection;
        ///using Dibix;
        ///
        ///namespace This.Is.A.Custom.Namespace
        ///{
        ///    internal static clas [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string SourcesTest {
            get {
                return ResourceManager.GetString("SourcesTest", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Dibix.Sdk.Tests.Database\Lint\dbx_lint_error_001.sql(13,2) : error SQLLINT#001: [SQLLINT#001] Invalid casing for &apos;SeLeCT&apos; [Select]
        ///Dibix.Sdk.Tests.Database\Lint\dbx_lint_error_001.sql(3,13) : error SQLLINT#001: [SQLLINT#001] Invalid casing for &apos;nvarchar&apos; [Identifier]
        ///Dibix.Sdk.Tests.Database\Lint\dbx_lint_error_001.sql(3,22) : error SQLLINT#001: [SQLLINT#001] Invalid casing for &apos;max&apos; [Identifier]
        ///Dibix.Sdk.Tests.Database\Lint\dbx_lint_error_001.sql(4,13) : error SQLLINT#001: [SQLLINT#001] Invalid casing  [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string SqlCasingLintRule {
            get {
                return ResourceManager.GetString("SqlCasingLintRule", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Dibix.Sdk.Tests.Database\Lint\dbx_lint_error_003.sql(3,2) : error SQLLINT#003: [SQLLINT#003] The use of RETURN expressions is not allowed.
        /// </summary>
        internal static string SqlNoReturnLintRule {
            get {
                return ResourceManager.GetString("SqlNoReturnLintRule", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Dibix.Sdk.Tests.Database\Lint\dbx_lint_error_002.sql(9,7) : error SQLLINT#002: [SQLLINT#002] Missing schema specification
        ///Dibix.Sdk.Tests.Database\Lint\dbx_lint_error_002.sql(17,7) : error SQLLINT#002: [SQLLINT#002] Missing schema specification
        ///Dibix.Sdk.Tests.Database\Lint\dbx_lint_error_002.sql(22,14) : error SQLLINT#002: [SQLLINT#002] Missing schema specification
        ///Dibix.Sdk.Tests.Database\Lint\dbx_lint_error_002.sql(24,14) : error SQLLINT#002: [SQLLINT#002] Missing schema specification
        ///Dibix.Sdk.Tests [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string SqlSchemaLintRule {
            get {
                return ResourceManager.GetString("SqlSchemaLintRule", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Dibix.Sdk.Tests.Database\Lint\dbx_lint_error_004.sql(3,28) : error SQLLINT#004: [SQLLINT#004] Invalid ascii string literal. Please specify unicode (N&apos;&apos;).
        /// </summary>
        internal static string SqlUnicodeLintRule {
            get {
                return ResourceManager.GetString("SqlUnicodeLintRule", resourceCulture);
            }
        }
    }
}
