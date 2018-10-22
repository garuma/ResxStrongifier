using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NetFx
{
	/// <summary>
	/// Generates a typed class for the given input .resx files.
	/// </summary>
	public class StringResources : Task
	{
		/// <summary>
		/// Default class name to use if no TargetClassName metadata is provided
		/// for the input resx files.
		/// </summary>
		public const string DefaultClassName = "Strings";

		/// <summary>
		/// Language of the containing project.
		/// </summary>
		[Required]
		public string Language { get; set; }

		/// <summary>
		/// The resource files to process.
		/// </summary>
		[Required]
		public Microsoft.Build.Framework.ITaskItem[] ResxFiles { get; set; }

		/// <summary>
		/// Root namespace for the containing project.
		/// </summary>
		[Required]
		public string RootNamespace { get; set; }

		/// <summary>
		/// Generated stronly typed code files.
		/// </summary>
		[Output]
		public Microsoft.Build.Framework.ITaskItem[] GeneratedFiles { get; set; }

		/// <summary>
		/// Generates the strong typed resources for the given resx input files.
		/// </summary>
		/// <remarks>a remark</remarks>
		public override bool Execute ()
		{
			var generatedFiles = new List<ITaskItem> (ResxFiles.Length);

			foreach (var resx in ResxFiles) {
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
					targetNamespace = RootNamespace + "." + relativeDir
						.TrimEnd (Path.DirectorySeparatorChar)
						.Replace (Path.DirectorySeparatorChar, '.');

					Log.LogMessage (MessageImportance.Low, "No CustomToolNamespace metadata found, determined TargetNamespace=" + targetNamespace);
				} else {
					Log.LogMessage (MessageImportance.Low, "Using provided CustomToolNamespace={0} metadata as TargetNamespace for {1}", targetNamespace, resx.ItemSpec);
				}

				var targetClassName = resx.GetMetadata ("TargetClassName");
				if (string.IsNullOrEmpty (targetClassName))
					targetClassName = DefaultClassName;

				var rootArea = ResourceFile.Build (resxFile, targetClassName);
				var generator = Generator.Create (Language, targetNamespace, resourcesTypeName, targetClassName, makePublic, rootArea);

				var output = generator.TransformText ();
				var targetFile = resx.GetMetadata("GeneratedOutput");

				if (!Directory.Exists (Path.GetDirectoryName (targetFile)))
					Directory.CreateDirectory (Path.GetDirectoryName (targetFile));

				File.WriteAllText (targetFile, output);
				generatedFiles.Add (new TaskItem (resx) {
					ItemSpec = targetFile
				});
			}

			GeneratedFiles = generatedFiles.ToArray ();

			return true;
		}
	}
}
