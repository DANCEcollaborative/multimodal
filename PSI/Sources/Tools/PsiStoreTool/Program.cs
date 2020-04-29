﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace PsiStoreTool
{
    using System;
    using System.Collections.Generic;
    using CommandLine;

    /// <summary>
    /// Psi store command-line tool.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Display command-line parser errors.
        /// </summary>
        /// <param name="errors">Errors reported.</param>
        /// <returns>Success flag.</returns>
        private static int DisplayParseErrors(IEnumerable<Error> errors)
        {
            Console.WriteLine("Errors:");
            var ret = 0;
            foreach (var error in errors)
            {
                Console.WriteLine($"{error}");
                if (error.StopsProcessing)
                {
                    ret = 1;
                }
            }

            return ret;
        }

        private static int Main(string[] args)
        {
            Console.WriteLine($"Platform for Situated Intelligence Store Tool");
            try
            {
                return Parser.Default.ParseArguments<Verbs.List, Verbs.Info, Verbs.Messages, Verbs.Save, Verbs.Send>(args)
                    .MapResult(
                        (Verbs.List opts) => Utility.ListStreams(opts.Store, opts.Path),
                        (Verbs.Info opts) => Utility.DisplayStreamInfo(opts.Stream, opts.Store, opts.Path),
                        (Verbs.Messages opts) => Utility.DisplayStreamMessages(opts.Stream, opts.Store, opts.Path, opts.Number),
                        (Verbs.Save opts) => Utility.SaveStreamMessages(opts.Stream, opts.Store, opts.Path, opts.File, opts.Format),
                        (Verbs.Send opts) => Utility.SendStreamMessages(opts.Stream, opts.Store, opts.Path, opts.Topic, opts.Address, opts.Format),
                        DisplayParseErrors);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }
    }
}
