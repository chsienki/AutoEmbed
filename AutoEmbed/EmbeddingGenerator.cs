using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AutoEmbed
{
    [Generator]
    public class EmbeddingGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            using (var res = Assembly.GetExecutingAssembly().GetManifestResourceStream("AutoEmbed.EmbedItem.cs"))
            {
                context.AddSource("embedItem", SourceText.From(res, Encoding.UTF8, canBeEmbedded: true));
            }

            DirectoryHolder holder = new DirectoryHolder();
            foreach (var file in context.AdditionalFiles)
            {
                var options = context.AnalyzerConfigOptions.GetOptions(file);
                if (options.TryReadMetadataFlag("AdditionalFiles", "IsAutoEmbed")
                    && options.TryReadMetadata("EmbeddedResource", "ManifestResourceName", out string resourceName)
                    && options.TryReadMetadata("EmbeddedResource", "OriginalItemSpec", out string originalItemSpec)
                    && !string.IsNullOrWhiteSpace(resourceName)
                    && !string.IsNullOrWhiteSpace(originalItemSpec))
                {
                    holder.AddEntry(originalItemSpec, resourceName);
                }
            }

            if (holder.HasItems)
            {
                var dh2 = holder.Collapse();
                var resourceText = dh2.BuildSource();
                context.AddSource("resources", resourceText);
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            //TODO: update to latest and use post init
        }

    }

    public class DirectoryHolder
    {
        Dictionary<string, DirectoryHolder> _subDirs = new Dictionary<string, DirectoryHolder>();

        List<(string path, string resourceName)> _leafItems = new List<(string, string)>();

        public bool HasItems { get => _leafItems.Count > 0 || _subDirs.Count > 0; }

        public void AddEntry(string path, string resourceName)
        {
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory))
            {
                _leafItems.Add((path, resourceName));
            }
            else
            {
                var root = directory.Split(Path.DirectorySeparatorChar)[0];
                if (!_subDirs.ContainsKey(root))
                {
                    _subDirs.Add(root, new DirectoryHolder());
                }
                _subDirs[root].AddEntry(path.Substring(root.Length + 1), resourceName);
            }
        }

        public DirectoryHolder Collapse()
        {
            if (_leafItems.Count > 0 || _subDirs.Count > 1)
            {
                return new DirectoryHolder()
                {
                    _subDirs = _subDirs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Collapse()),
                    _leafItems = _leafItems
                };
            }
            else
            {
                return _subDirs.Values.Single().Collapse();
            }
        }

        public string BuildSource()
        {
            if (!HasItems)
                return string.Empty;

            StringBuilder sb = new StringBuilder(@"
namespace AutoEmbed
{
    internal static class Resources
    {
");
            BuildSourceInner(sb, "Resources");
            sb.Append(@"
    }
}");
            return sb.ToString();
        }

        private void BuildSourceInner(StringBuilder sb, string parent)
        {
            foreach (var kvp in _subDirs)
            {
                string className = escape(kvp.Key);
                if (!SyntaxFacts.IsValidIdentifier(className))
                {
                    // TODO: issue a diagnostic that we couldn't generate this
                    continue;
                }
                if (className.Equals(parent, StringComparison.OrdinalIgnoreCase))
                {
                    className += "_";
                }

                if (SyntaxFacts.GetKeywordKind(className) != SyntaxKind.None)
                {
                    className = "@" + className;
                }

                sb.Append($@"
        internal class {className}
        {{
");
                kvp.Value.BuildSourceInner(sb, className);

                sb.Append($@"
        }}
");
            }

            foreach (var (path, resourceName) in _leafItems)
            {
                sb.Append($@"
        internal static global::AutoEmbed.Internal.EmbedItem {escape(Path.GetFileName(path))} {{ get; }} = new global::AutoEmbed.Internal.EmbedItem(""{resourceName}"", System.Reflection.Assembly.GetExecutingAssembly());
");
            }

            string escape(string input)
            {
                input = input.Replace('.', '_').Replace(' ', '_');
                if (!SyntaxFacts.IsValidIdentifier(input))
                {
                    input = "_" + input;
                }
                return input;
            }
        }
    }

    public static class Extensions
    {
        public static bool TryReadMetadata(this AnalyzerConfigOptions options, string itemType, string metadataName, out string value)
        {
            return options.TryGetValue($"build_metadata.{itemType}.{metadataName}", out value);
        }
        public static bool TryReadMetadataFlag(this AnalyzerConfigOptions options, string itemType, string metadataName)
        {
            return TryReadMetadata(options, itemType, metadataName, out var result)
                && bool.TryParse(result, out var resultBool)
                && resultBool;
        }

    }
}
