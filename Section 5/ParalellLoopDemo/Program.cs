using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace ParallelLoopDemo
{
    class Program
    {
        static IEnumerable<int> Range(int start, int end, int step)
        {
            for (int i = start; i < end; i += step)
            {
                yield return i;
            }
        }

        static void ParallelLoopDemo()
        {
            var a = new Action(() => Console.WriteLine($"First {Task.CurrentId}"));
            var b = new Action(() => Console.WriteLine($"Second {Task.CurrentId}"));
            var c = new Action(() => Console.WriteLine($"Third {Task.CurrentId}"));

            Parallel.Invoke(a, b, c);

            Parallel.For(1, 11, i => {
                Console.WriteLine($"{i * i}\t");
            });

            string[] words = { "oh", "what", "a", "night" };
            Parallel.ForEach(words, word => {
                Console.WriteLine($"{word} has length {word.Length} (task {Task.CurrentId})");
            });
        }

        static void ParallelForEachDemo()
        {
            var a = new Action(() => Console.WriteLine($"First {Task.CurrentId}"));
            var b = new Action(() => Console.WriteLine($"Second {Task.CurrentId}"));
            var c = new Action(() => Console.WriteLine($"Third {Task.CurrentId}"));

            Parallel.Invoke(a, b, c);

            var po = new ParallelOptions();
            //po.MaxDegreeOfParallelism

            Parallel.For(1, 11, i => {
               // Console.WriteLine($"{i * i}\t");
            });

            Parallel.ForEach(Range(1, 20, 3), Console.WriteLine);
        }
        static ParallelLoopResult result;
        static void BreakCancelException()
        {
            var cts = new CancellationTokenSource();

            ParallelOptions po = new ParallelOptions();
            po.CancellationToken = cts.Token;

            result = Parallel.For(0, 20, (int x, ParallelLoopState state) => {
                Console.WriteLine($"{x}[{Task.CurrentId}]\t");
                if( x == 10)
                {
                    //throw new Exception();
                    //state.Stop();
                    // state.Break();
                    cts.Cancel();
                }
            });

            Console.WriteLine();
            Console.WriteLine($"Was loop completed? {result.IsCompleted}");
            if (result.LowestBreakIteration.HasValue)
                Console.WriteLine($"Lowest break iteration is {result.LowestBreakIteration}");
        }

        static void ThreadLocalStorageDemo()
        {
            int sum = 0;

            Parallel.For(1, 1001, 
                () => 0,
                (x, state, tls) => {
                tls += x;
                    Console.WriteLine($"Task {Task.CurrentId} has sum {tls}");
                    return tls;
            }, 
            partialSum =>
            {
                Console.WriteLine($"Partial value of task {Task.CurrentId} is {partialSum}");
                Interlocked.Add(ref sum, partialSum);
            });
            Console.WriteLine($"Sum of 1..100 = {sum}");
        }
        [Benchmark]
        static void SquareEachValue()
        {
            const int count = 100000;
            var values = Enumerable.Range(0, count);
            var results = new int[count];
            // very inneficient way of doing it.
            Parallel.ForEach(values, x => { results[x] = (int) Math.Pow(x, 2); });
        }

        static void Main(string[] args)
        {
            try
            {
                //ParallelLoopDemo();
                // ParallelForEachDemo();
                //BreakCancelException();
                //ThreadLocalStorageDemo();
                var summary = BenchmarkRunnerCore.Run<Program>();
                Console.WriteLine(summary);
            }
            catch (AggregateException ae)
            {
                ae.Handle(e => {
                    Console.WriteLine(e.Message);
                    return true;
                });
            }
            catch(OperationCanceledException)
            {

            }
        }
    }
}
