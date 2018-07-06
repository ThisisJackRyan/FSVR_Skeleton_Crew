using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Dissonance.Editor.Windows.Welcome
{
    [Serializable]
    public class IntegrationMetadata
    {
        [SerializeField] private string _id;
        public string Id { get { return _id; } }

        [SerializeField] private int _version = -1;
        public int Version { get { return _version; } }

        public string Path { get; private set; }

        private IntegrationMetadata()
        {
        }

        private IntegrationMetadata(string id, int version, string path)
        {
            _id = id;
            _version = version;
            Path = path;
        }

        private static readonly string IntegrationsDirectoryPath = new[] {
            Application.dataPath,
            "Dissonance",
            "Integrations"
        }.Aggregate(System.IO.Path.Combine);

        public static IEnumerable<IntegrationMetadata> FindIntegrations()
        {
            if (!Directory.Exists(IntegrationsDirectoryPath))
                yield break;

            //Assume each directory in Dissonance/Integrations is an integration
            var directories = Directory.GetDirectories(IntegrationsDirectoryPath);

            foreach (var directory in directories)
            {
                var mdPath = System.IO.Path.Combine(directory, "dissonance-integration-metadata.json");
                
                //If the file doesn't exist, or it fails to parse, return a default value
                IntegrationMetadata metadata;
                if (!File.Exists(mdPath) || !TryParse(mdPath, out metadata))
                    metadata = new IntegrationMetadata(System.IO.Path.GetFileName(directory), 0, mdPath);

                yield return metadata;
            }
        }

        private static bool TryParse(string path, [CanBeNull] out IntegrationMetadata metadata)
        {
            try
            {
                metadata = JsonUtility.FromJson<IntegrationMetadata>(File.ReadAllText(path));
                metadata.Path = path;

                //If any of the values are the default this hasn't parsed correctly.
                if (metadata.Id == null || metadata.Version == -1)
                    throw new FormatException("Encountered default value after parsing");

                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning(string.Format("Failed to Parse Dissonance integration metadata at '{0}' - {1}", path, e));

                metadata = null;
                return false;
            }
        }
    }
}
