using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FourE.Cards.Editor
{
    /// <summary>
    /// Configura le immagini delle carte come Sprite e le collega ai dati carta con nome equivalente.
    /// </summary>
    [InitializeOnLoad]
    public sealed class CardArtworkAutoAssigner : AssetPostprocessor
    {
        private const string ARTWORK_FOLDER = "Assets/cards";
        private const string CARD_DATA_FILTER = "t:CardDataSO";
        private const string ARTWORK_PROPERTY = "_artwork";

        private static readonly IReadOnlyDictionary<string, string> ArtworkAliases =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "compito", "compitoacasa" },
                { "minacciare", "minaccia" },
                { "studiare", "studio" }
            };

        private static bool _isAssigning;

        static CardArtworkAutoAssigner()
        {
            EditorApplication.delayCall += AssignAllArtwork;
        }

        /// <summary>
        /// Aggiorna gli abbinamenti quando cambia un'immagine nella cartella artwork.
        /// </summary>
        /// <param name="importedAssets">Asset appena importati.</param>
        /// <param name="deletedAssets">Asset appena eliminati.</param>
        /// <param name="movedAssets">Asset appena spostati.</param>
        /// <param name="movedFromAssetPaths">Percorsi di origine degli asset spostati.</param>
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (_isAssigning ||
                (!ContainsArtworkPath(importedAssets) &&
                 !ContainsArtworkPath(deletedAssets) &&
                 !ContainsArtworkPath(movedAssets) &&
                 !ContainsArtworkPath(movedFromAssetPaths)))
            {
                return;
            }

            EditorApplication.delayCall += AssignAllArtwork;
        }

        /// <summary>
        /// Importa tutti gli artwork come Sprite e li assegna alle carte corrispondenti.
        /// </summary>
        [MenuItem("4E/Cards/Assign Artwork")]
        private static void AssignAllArtwork()
        {
            if (_isAssigning || !AssetDatabase.IsValidFolder(ARTWORK_FOLDER))
            {
                return;
            }

            _isAssigning = true;
            try
            {
                Dictionary<string, Sprite> artworkByName = LoadArtwork();
                string[] cardGuids = AssetDatabase.FindAssets(CARD_DATA_FILTER);
                bool hasChanges = false;

                foreach (string cardGuid in cardGuids)
                {
                    string cardPath = AssetDatabase.GUIDToAssetPath(cardGuid);
                    CardDataSO card = AssetDatabase.LoadAssetAtPath<CardDataSO>(cardPath);
                    if (card == null)
                    {
                        continue;
                    }

                    string normalizedCardName = Normalize(card.CardName);
                    artworkByName.TryGetValue(normalizedCardName, out Sprite artwork);

                    SerializedObject serializedCard = new(card);
                    SerializedProperty artworkProperty = serializedCard.FindProperty(ARTWORK_PROPERTY);
                    if (artworkProperty.objectReferenceValue == artwork)
                    {
                        continue;
                    }

                    artworkProperty.objectReferenceValue = artwork;
                    serializedCard.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(card);
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    AssetDatabase.SaveAssets();
                }
            }
            finally
            {
                _isAssigning = false;
            }
        }

        /// <summary>
        /// Carica gli artwork disponibili dopo averne verificato le impostazioni di importazione.
        /// </summary>
        /// <returns>Mappa degli Sprite indicizzata per nome normalizzato.</returns>
        private static Dictionary<string, Sprite> LoadArtwork()
        {
            Dictionary<string, Sprite> artworkByName = new(StringComparer.Ordinal);
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { ARTWORK_FOLDER });

            foreach (string textureGuid in textureGuids)
            {
                string texturePath = AssetDatabase.GUIDToAssetPath(textureGuid);
                EnsureSpriteImport(texturePath);

                Sprite artwork = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
                if (artwork == null)
                {
                    continue;
                }

                string fileName = Path.GetFileNameWithoutExtension(texturePath);
                string normalizedName = Normalize(fileName);
                if (ArtworkAliases.TryGetValue(normalizedName, out string alias))
                {
                    normalizedName = alias;
                }

                artworkByName[normalizedName] = artwork;
            }

            return artworkByName;
        }

        /// <summary>
        /// Imposta una texture come Sprite singolo adatto alla UI.
        /// </summary>
        /// <param name="assetPath">Percorso Unity della texture.</param>
        private static void EnsureSpriteImport(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer ||
                (importer.textureType == TextureImporterType.Sprite &&
                 importer.spriteImportMode == SpriteImportMode.Single))
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }

        /// <summary>
        /// Verifica se almeno un percorso appartiene alla cartella degli artwork.
        /// </summary>
        /// <param name="paths">Percorsi da controllare.</param>
        /// <returns>True se e presente un percorso artwork.</returns>
        private static bool ContainsArtworkPath(IEnumerable<string> paths)
        {
            foreach (string path in paths)
            {
                if (path.StartsWith(ARTWORK_FOLDER, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Normalizza un nome rimuovendo spazi e punteggiatura senza distinzione tra maiuscole.
        /// </summary>
        /// <param name="value">Nome da normalizzare.</param>
        /// <returns>Nome normalizzato.</returns>
        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            char[] normalized = new char[value.Length];
            int index = 0;
            foreach (char character in value)
            {
                if (char.IsLetterOrDigit(character))
                {
                    normalized[index++] = char.ToLowerInvariant(character);
                }
            }

            return new string(normalized, 0, index);
        }
    }
}
