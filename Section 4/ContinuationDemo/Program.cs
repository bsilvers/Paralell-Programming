using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ContinuationDemo
{
    class Program
    {
        static void Continuation1()
        {
            var task = Task.Factory.StartNew(() => {
                Console.WriteLine("Boiling water");
            });

            var task2 = task.ContinueWith(t =>
            {
                Console.WriteLine($"Completed task {t.Id}, pour water into cup.");
            });

            task2.Wait();
        }

        static void Continuation2()
        {
            var task = Task.Factory.StartNew(() => "Task 1");
            var task2 = Task.Factory.StartNew(() => "Task 2");

            var task3 = Task.Factory.ContinueWhenAll(new[] { task, task2 }, 
                tasks =>
            {
                Console.WriteLine("Tasks completed.");
                foreach (var t in tasks)
                {
                    Console.WriteLine($"-{t.Result}");
                }

                Console.WriteLine("All tasks done.");
            });

            task3.Wait();
        }

        static void ChildTasks()
        {
            var parent = new Task(() => {
                // detached
                var child = new Task(() =>
                {
                    Console.WriteLine("Child task starting.");
                    Thread.Sleep(3000);
                    Console.WriteLine("Child task finishing.");
                   // throw new Exception();
                }, TaskCreationOptions.AttachedToParent);

                var completionHandler = child.ContinueWith(t => {
                    Console.WriteLine($"Hooray, task {t.Id}'s state is {t.Status}");
                }, TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.OnlyOnRanToCompletion);

                var failHandler = child.ContinueWith(t => {
                    Console.WriteLine($"Oops, task {t.Id}'s state is {t.Status}");
                }, TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.OnlyOnFaulted);

                child.Start();
            });
            parent.Start();

            try
            {
                parent.Wait();
            }
            catch (AggregateException ae)
            {
                ae.Handle( e=> true);
            }
            
        }
        static Barrier barrier = new Barrier(2, b =>
        {
            Console.WriteLine($"Phase {b.CurrentPhaseNumber} is finished");
            // b.ParticipantCount
        });
        static void BarrierDemo()
        {
            var water = Task.Factory.StartNew(Water);
            var cup = Task.Factory.StartNew(Cup);

            var tea = Task.Factory.ContinueWhenAll(new[] { water, cup }, tasks =>
             {
                 Console.WriteLine("Enjoy your cup of tea.");
             });
            tea.Wait();
        }

        static void Water()
        {
            Console.WriteLine("Putting the kettle on (takes a bit longer)");
            Thread.Sleep(2000);
            barrier.SignalAndWait();  // signal wait  2
            Console.WriteLine("Pouring water into cup"); // 0
            barrier.SignalAndWait(); // 1
            Console.WriteLine("Putting the kettle away");
        }

        static void Cup()
        {
            Console.WriteLine("Finding the nicest cup of tea(fast)");
            barrier.SignalAndWait(); // 1
            Console.WriteLine("Adding tea.");
            barrier.SignalAndWait(); // 2
            Console.WriteLine("Adding sugar");
        }
        static int taskCount = 5;
        static CountdownEvent cte = new CountdownEvent(taskCount);
        static Random random = new Random();
        static void CountdownEvents()
        {
            for (int i = 0; i < taskCount; i++)
            {
                Console.WriteLine($"Entering task {Task.CurrentId}");
                Thread.Sleep(random.Next(3000));
                cte.Signal();
                Console.WriteLine($"Exiting task {Task.CurrentId}");
            }

            var finalTask = Task.Factory.StartNew(() => {
                Console.WriteLine($"Waiting for other tasks to complete in {Task.CurrentId}");
                cte.Wait();
                Console.WriteLine("All tasks completed");
            });
            finalTask.Wait();
        }

        static void ManualResetEvents()
        {
            var evt = new ManualResetEventSlim();
            Task.Factory.StartNew(() => {
                Console.WriteLine("Boiling water");
                evt.Set();
            });

            var makeTea = Task.Factory.StartNew(() => {
                Console.WriteLine("Waiting for water...");
                evt.Wait();
                Console.WriteLine("Here is your tea.");
            });

            makeTea.Wait();
        }

        static void AutoResetEvents()
        {
            var evt = new AutoResetEvent(false);
            Task.Factory.StartNew(() => {
                Console.WriteLine("Boiling water");
                evt.Set(); // true
            });

            var makeTea = Task.Factory.StartNew(() => {
                Console.WriteLine("Waiting for water...");
                evt.WaitOne(); // false false
                Console.WriteLine("Here is your tea.");
                var ok = evt.WaitOne(1000); // false false
                if (ok)
                {
                    Console.WriteLine("Enjoy your tea");
                } else
                {
                    Console.WriteLine("No tea for you");
                }
            });

            makeTea.Wait();
        }
        
        static void SemaphoreDemo()
        {
            var semaphore = new SemaphoreSlim(2,10);

            for (int i = 0; i < 20; i++)
            {
                Task.Factory.StartNew(() => {
                    Console.WriteLine($"Entering task {Task.CurrentId}");
                    semaphore.Wait();
                    Console.WriteLine($"Processing task {Task.CurrentId}");
                });
            }

            while (semaphore.CurrentCount <= 2)
            {
                Console.WriteLine($"Semaphore count: {semaphore.CurrentCount}");
                Console.ReadKey();
                semaphore.Release(2); // Relase  count of 2
            }
        }

        static void Main(string[] args)
        {
            //Continuation1();
            //Continuation2();
            // ChildTasks();
            // BarrierDemo();
            // CountdownEvents();
            // ManualResetEvents();
            // AutoResetEvents();
            SemaphoreDemo();
        }
    }
}
