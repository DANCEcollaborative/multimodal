﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MicrosoftSpeech
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Microsoft.Psi.Language;
    using Microsoft.Speech.Recognition;

    /// <summary>
    /// Static helper methods.
    /// </summary>
    [Obsolete("The MicrosoftSpeechRecognizer component has been deprecated. Consider using the SystemSpeechRecognizer component available in Microsoft.Psi.Speech.Windows instead.", false)]
    public static class MicrosoftSpeech
    {
        /// <summary>
        /// Method to construct the IntentData (intents and entities) from
        /// a SemanticValue.
        /// </summary>
        /// <param name="semanticValue">The SemanticValue object.</param>
        /// <returns>An IntentData object containing the intents and entities.</returns>
        public static IntentData BuildIntentData(SemanticValue semanticValue)
        {
            List<Intent> intentList = new List<Intent>();

            // Consider top-level semantics to be intents.
            foreach (var entry in semanticValue)
            {
                intentList.Add(new Intent()
                {
                    Value = entry.Key,
                    Score = entry.Value.Confidence,
                });
            }

            List<Entity> entityList = ExtractEntities(semanticValue);

            return new IntentData()
            {
                Intents = intentList.ToArray(),
                Entities = entityList.ToArray(),
            };
        }

        /// <summary>
        /// Creates a new speech recognition engine.
        /// </summary>
        /// <param name="language">The language for the recognition engine.</param>
        /// <param name="grammars">The grammars to load.</param>
        /// <returns>A new speech recognition engine object.</returns>
        internal static SpeechRecognitionEngine CreateSpeechRecognitionEngine(string language, Microsoft.Psi.Speech.GrammarInfo[] grammars)
        {
            var recognizer = new SpeechRecognitionEngine(new CultureInfo(language));
            foreach (var grammarInfo in grammars)
            {
                Grammar grammar = new Grammar(grammarInfo.FileName)
                {
                    Name = grammarInfo.Name,
                };
                recognizer.LoadGrammar(grammar);
            }

            return recognizer;
        }

        /// <summary>
        /// Method to extract all entities contained within a SemanticValue.
        /// </summary>
        /// <param name="semanticValue">The SemanticValue object.</param>
        /// <returns>The list of extracted entities.</returns>
        private static List<Entity> ExtractEntities(SemanticValue semanticValue)
        {
            List<Entity> entityList = new List<Entity>();
            foreach (var entry in semanticValue)
            {
                // We currently only consider leaf nodes (whose underlying
                // value is of type string) as entities.
                if (entry.Value.Value is string)
                {
                    // Extract the entity's type (key), value and confidence score.
                    entityList.Add(new Entity()
                    {
                        Type = entry.Key,
                        Value = (string)entry.Value.Value,
                        Score = entry.Value.Confidence,
                    });
                }
                else
                {
                    // Keep looking for leaf nodes.
                    entityList.AddRange(ExtractEntities(entry.Value));
                }
            }

            return entityList;
        }
    }
}
