using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

using System.Threading.Tasks;
namespace ConcurrentCollectionDemo
{
    class Program
    {
        private static ConcurrentDictionary<string, string> capitals = new ConcurrentDictionary<string, string>();

        public static void AddParis()
        {
            bool success = capitals.TryAdd("France", "Paris");
            // who called this function
            string who = Task.CurrentId.HasValue ? ("Task " + Task.CurrentId) : "Main thread";
            Console.WriteLine($"{who} {(success ? "added" : "did not add")} the element");
        }

        static void Concurrent1()
        {
            Task.Factory.StartNew(AddParis).Wait();
            AddParis();

            //capitals["Russia"] = "Leningrad";
            //capitals["Russia"] = "Moscow";

            // Console.WriteLine(capitals["Russia"]);

            //capitals["Russia"] = "Leningrad";
            //capitals.AddOrUpdate("Russia", "Moscow",
            //    (k, old) => old + " --> Moscow");
            //Console.WriteLine($"The capital of Russia is {capitals["Russia"]}");

            //capitals["Sweden"] = "Uppsala";
            var capOfSweden = capitals.GetOrAdd("Sweden", "Stockholm");
            Console.WriteLine($"The captial of Sweden is {capOfSweden}");


            const string toRemove = "Russia";
            string removed;
            var didRemove = capitals.TryRemove(toRemove, out removed);
            if (didRemove)
            {
                Console.WriteLine($"We just removed {removed}");
            } else
            {
                Console.WriteLine($"Failed to remove the capital of {toRemove}");
            }
            // count is an expensive operation
            foreach (var kv in capitals)
            {
                Console.WriteLine($" - {kv.Value} is the capital of {kv.Key} ");
            }

        }
        // FIFO
        static void ConcurrentQueue1()
        {
            var q = new ConcurrentQueue<int>();
            q.Enqueue(1);
            q.Enqueue(2);

            // 2 1 <- front
            int result;
            if(q.TryDequeue(out result))
            {
                Console.WriteLine($"Removed element {result}");
            }
            if(q.TryPeek(out result))
            {
                Console.WriteLine($"Front element is {result}");
            }


        }
        // LIFO
        static void CurrentStack()
        {
            var stack = new ConcurrentStack<int>();
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);
            stack.Push(4);
            int result;
            if( stack.TryPeek(out result))
            {
                Console.WriteLine($"{result} is on top");
            }
            if (stack.TryPop(out result))
                Console.WriteLine($"Popped {result}");

            var items = new int[5];
            if(stack.TryPopRange(items, 0 ,5) > 0)
            {
                var text = string.Join(", ", items.Select(i => i.ToString()));
                Console.WriteLine($"Popped these items: {text}");
            }

        }
        // no ordering
        static void ConcurrentBag1()
        {
            var bag = new ConcurrentBag<int>();
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                var i1 = i;
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    bag.Add(i1);
                    Console.WriteLine($"{Task.CurrentId} has added {i1}");
                    int result;
                    if(bag.TryPeek(out result)){
                        Console.WriteLine($"{Task.CurrentId} has peeded the value {result}");
                    }
                }));
            }
            Task.WaitAll(tasks.ToArray());
            int last;
            if(bag.TryTake(out last))
            {
                Console.WriteLine($"I got {last}");
            }
        }
        static BlockingCollection<int> messages = new BlockingCollection<int>(new ConcurrentBag<int>(), 10);
        static CancellationTokenSource cts = new CancellationTokenSource();
        static Random random = new Random();
        // producer-consumer pattern
        static void ProducerConsumer()
        {
            var producer = Task.Factory.StartNew(RunProducer);
            var consumer = Task.Factory.StartNew(RunConsumer);

            try
            {
                Task.WaitAll(new[] { producer, consumer }, cts.Token);
            }
            catch (AggregateException ae)
            {
                ae.Handle(e => true);
            }
        }

        private static void RunProducer()
        {
            while (true)
            {
                cts.Token.ThrowIfCancellationRequested();
                int i = random.Next(100);
                messages.Add(i);
                Console.WriteLine($"+{i}\t");
                Thread.Sleep(random.Next(100));
            }
        }

        private static void RunConsumer()
        {
            foreach (var item in messages.GetConsumingEnumerable())
            {
                cts.Token.ThrowIfCancellationRequested();
                Console.WriteLine($"-{item}\t");
                Thread.Sleep(random.Next(1000));
            }
        }

        static void Main(string[] args)
        {
            //  Concurrent1();
            //ConcurrentQueue1();
            //CurrentStack();
            //   ConcurrentBag1();
            //ProducerConsumer(); 
            Task.Factory.StartNew(ProducerConsumer, cts.Token);
            Console.ReadKey();
            cts.Cancel();
        }
    }
}
;