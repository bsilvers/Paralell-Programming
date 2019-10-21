``` ini

BenchmarkDotNet=v0.11.5, OS=Windows 10.0.17763.720 (1809/October2018Update/Redstone5)
Intel Core i7-7820HQ CPU 2.90GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.0.100
  [Host]     : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), 64bit RyuJIT
  DefaultJob : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), 64bit RyuJIT


```
|                 Method |       Mean |     Error |    StdDev |
|----------------------- |-----------:|----------:|----------:|
|        SquareEachValue | 2,431.2 us | 47.560 us | 66.672 us |
| SquareEachValueChunked |   370.8 us |  7.331 us |  6.499 us |
