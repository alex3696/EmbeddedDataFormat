// -----------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------

using BenchmarkDotNet.Running;
using TestPerfomance;

namespace Bench;

internal class Program
{
    static void Main(string[] args)
    {
        // var _ = BenchmarkRunner.Run(typeof(Program).Assembly);
        //BenchmarkRunner.Run<SortedListVsDictionary>();
        //BenchmarkRunner.Run<PerfCrc16>();
        BenchmarkRunner.Run<Schema>();
    }
}
