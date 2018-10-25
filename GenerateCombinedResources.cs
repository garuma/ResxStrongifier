using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace NetFx
{
	/// <summary>
	/// Generates a typed class for the given input .resx files.
	/// </summary>
	public class GenerateCombinedResources : Task
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
		public ITaskItem[] ResxFiles { get; set; }

		/// <summary>
		/// Root namespace for the containing project.
		/// </summary>
		[Required]
		public string RootNamespace { get; set; }

		/// <summary>
		/// Generated stronly typed code files.
		/// </summary>
		[Output]
		public ITaskItem[] GeneratedFiles { get; set; }

		/// <summary>
		/// Generates the strong typed resources for the given resx input files.
		/// </summary>
		/// <remarks>a remark</remarks>
		public override bool Execute()
		{
			if (Language != "C#")
			{
				Log.LogError("Language {0} is not supported yet.", Language);
				return !Log.HasLoggedErrors;
			}

			var generatedFiles = new List<ITaskItem>(ResxFiles.Length);

			foreach (var resx in ResxFiles)
			{
				bool generateStringResources;
				if (!bool.TryParse (resx.GetMetadata("GenerateStringResources"), out generateStringResources))
					generateStringResources = true;

				var stringResources = generateStringResources ? StringResources.Execute(Language, resx, RootNamespace, Log) : "";
				var designerResources = DesignerResources.Execute(Language, resx, RootNamespace, Log);

				var targetFile = resx.GetMetadata ("GeneratedOutput");
				Directory.CreateDirectory (Path.GetDirectoryName (targetFile));
				File.WriteAllText(targetFile, stringResources + System.Environment.NewLine + System.Environment.NewLine + designerResources);

				generatedFiles.Add(new TaskItem(resx) {
					ItemSpec = targetFile
				});
			}

			GeneratedFiles = generatedFiles.ToArray();

			return !Log.HasLoggedErrors;
		}
	}
}
