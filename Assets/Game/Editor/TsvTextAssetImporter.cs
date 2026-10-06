using System.IO;
using System.Text;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace ValorChronicle.Editor.Localization
{
    [ScriptedImporter(1, "tsv")]
    public sealed class TsvTextAssetImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext context)
        {
            string text = File.ReadAllText(
                context.assetPath,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false,
                    throwOnInvalidBytes: true));
            var asset = new TextAsset(text)
            {
                name = Path.GetFileNameWithoutExtension(context.assetPath)
            };
            context.AddObjectToAsset("text", asset);
            context.SetMainObject(asset);
        }
    }
}
