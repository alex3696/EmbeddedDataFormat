// -----------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------

using BenchmarkDotNet.Attributes;
using System;
using System.Security.Cryptography;

namespace Bench
{
    // For more information on the VS BenchmarkDotNet Diagnosers see https://learn.microsoft.com/visualstudio/profiling/profiling-with-benchmark-dotnet
    //[CPUUsageDiagnoser]
    //[MemoryDiagnoser(false)]
    //[SimpleJob(RuntimeMoniker.Net10_0, baseline: true)]
    public class Benchmarks
    {
        private SHA256 sha256 = SHA256.Create();
        private byte[] data;

        [GlobalSetup]
        public void Setup()
        {
            data = new byte[10000];
            new Random(42).NextBytes(data);
        }

        //[Benchmark]
        public byte[] Sha256()
        {
            return sha256.ComputeHash(data);
        }
    }
}
