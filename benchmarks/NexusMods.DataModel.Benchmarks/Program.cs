// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using NexusMods.DataModel.Benchmarks;
using NexusMods.DataModel.Sorting;

BenchmarkRunner.Run<Sorting>();
/*
var sorting = new Sorting();
sorting.NumItems = 1000;
sorting.Setup();
sorting.Sort();
var a = 5;
*/