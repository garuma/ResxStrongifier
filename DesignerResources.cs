using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Resources.Tools;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CSharp;

namespace NetFx
{
	public static class DesignerResources
	{
		public static string Execute (string language, ITaskItem resx, string rootNamespace, TaskLoggingHelper log)
		{
			var resxFile = resx.GetMetadata ("FullPath");
			// Same logic as ResXFileCodeGenerator.
			var resourcesTypeName = Path.GetFileNameWithoutExtension (resxFile);
			var targetNamespace = resx.GetMetadata ("CustomToolNamespace");
			var relativeDir = resx.GetMetadata ("CanonicalRelativeDir");
			var makePublic = false;
			bool.TryParse (resx.GetMetadata ("Public"), out makePublic);

			if (string.IsNullOrEmpty (targetNamespace)) {
				// Note that the custom tool namespace in newer versions of VS is saved
				// as item metadata. On older versions, it would have to be manually
				// set.
				targetNamespace = rootNamespace + "." + relativeDir
					.TrimEnd (Path.DirectorySeparatorChar)
					.Replace (Path.DirectorySeparatorChar, '.');

				log.LogMessage (MessageImportance.Low, "No CustomToolNamespace metadata found, determined TargetNamespace=" + targetNamespace);
			} else {
				log.LogMessage (MessageImportance.Low, "Using provided CustomToolNamespace={0} metadata as TargetNamespace for {1}", targetNamespace, resx.ItemSpec);
			}

			var targetClassName = Path.GetFileNameWithoutExtension (resxFile);
			var builder = new System.Text.StringBuilder();
			using (var writer = new StringWriter (builder)) {
				string[] errors = null;
				CSharpCodeProvider provider = new CSharpCodeProvider();
				CodeCompileUnit code = StronglyTypedResourceBuilder.Create (resxFile, targetClassName, targetNamespace, targetNamespace, provider, !makePublic, out errors);
				if (errors.Length > 0)
					foreach (var error in errors)
						log.LogError ("Error generating from '{0}'. {1}", resxFile, error);

				provider.GenerateCodeFromCompileUnit (code, writer, new CodeGeneratorOptions ());
			}

			return builder.ToString ();
		}
	}
}
