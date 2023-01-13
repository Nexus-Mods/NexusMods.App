// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using NexusMods.DataModel.Benchmarks;
using NexusMods.DataModel.Sorting;

BenchmarkRunner.Run<Sorting>();

//new Sorting().Sort();