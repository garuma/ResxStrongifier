﻿using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NetFx
{
	/// <summary>
	/// Generates a typed class for the given input .resx files.
	/// </summary>
	public static class StringResources
	{
		/// <summary>
		/// Default class name to use if no TargetClassName metadata is provided
		/// for the input resx files.
		/// </summary>
		public const string DefaultClassName = "Strings";

		/// <summary>
		/// Generates the strong typed resources for the given resx input files.
		/// </summary>
		/// <remarks>a remark</remarks>
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

			var targetClassName = resx.GetMetadata ("TargetClassName");
			if (string.IsNullOrEmpty (targetClassName))
				targetClassName = DefaultClassName;

			var rootArea = ResourceFile.Build (resxFile, targetClassName);
			var generator = Generator.Create (language, targetNamespace, resourcesTypeName, targetClassName, makePublic, rootArea);
			return generator.TransformText ();
		}
	}
}
