using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelLINQ
{
    class Program
    {
        public static void OrdinaryLinq()
        {
            const int count = 50;
            var items = Enumerable.Range(1, count).ToArray();
            var results = new int[count];

            items.AsParallel().ForAll(x => {
                int newValue = x * x * x;
                Console.Write($"{newValue}({Task.CurrentId})\t");
                results[x - 1] = newValue;
            });
            Console.WriteLine();
            Console.WriteLine();

            //foreach (var i in results)
            //{
            //    Console.WriteLine($"{i}\t");
            //}
            //Console.WriteLine();
            var cubes = items.AsParallel().AsOrdered().Select(x => x*x*x);

            var arr = cubes.ToArray();

            foreach (var i in cubes)
            {
                Console.Write($"{i}\t");
            }
            Console.WriteLine();
        }

        public static void Cancellations()
        {
            var cts = new CancellationTokenSource();
            var items = ParallelEnumerable.Range(1, 20);
            var results = items.WithCancellation(cts.Token).Select(i =>
            {
                double result = Math.Log10(i);
                //if (result > 1) throw new InvalidOperationException();
                Console.WriteLine($"i = {i}, tid = {Task.CurrentId}");
                return result;
            });

            try
            {
                foreach (var c in results)
                {
                    if (c > 1)
                        cts.Cancel();
                    Console.WriteLine($"result = {c}");
                }
            }
            catch (AggregateException ae)
            {

                ae.Handle(e => {
                    Console.WriteLine($"{e.GetType().Name}: {e.Message}");
                    return true;
                });
            }
            catch(OperationCanceledException e)
            {
                Console.WriteLine("Cancelled");
            }
        }

        public static void Exceptions()
        {

        }

        public static void Merge1()
        {
            var numbers = Enumerable.Range(1, 20).ToArray();

            var results = numbers.AsParallel()
                .WithMergeOptions(ParallelMergeOptions.NotBuffered)
                .Select(x => {
                    var result = Math.Log10(x);
                    Console.Write($"P {result}\t");
                    return result;
                });

            foreach (var result in results)
            {
                Console.Write($"C {result}\t");
            }
        }

        public static void CustomAggregation()
        {
            //var sum = Enumerable.Range(1, 1000).Sum();

            // var sum = Enumerable.Range(1, 1000).Aggregate(0, (i, acc) => i +acc);

            var sum = ParallelEnumerable.Range(1, 1000)
                .Aggregate(
                    0,
                    (partialSum, i) => partialSum += i,
                    (total, subtotal) => total += subtotal,
                    i => i);

            Console.WriteLine($"sum = {sum}");
        }

        static void Main(string[] args)
        {
            //OrdinaryLinq();
            // Cancellations();
            //Merge1();
            CustomAggregation();
        }
    }
}
