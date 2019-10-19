using System;
using System.Threading;
using System.Threading.Tasks;

namespace DemoApp1
{
    class Program
    {
        static void Write(char c)
        {
            int i = 1000;
            while (i-- > 0)
            {
                Console.Write(c);
            }
        }

        static void Write(object o)
        {
            int i = 1000;
            while (i-- > 0)
            {
                Console.Write(o);
            }
        }

        static int TextLength(object o)
        {
            Console.WriteLine($"\nTask with id {Task.CurrentId} processing object {o}...");
            return o.ToString().Length;
        }

        #region Creating and starting tasks
        static void Task1()
        {
            Task.Factory.StartNew(() => Write('.'));

            var t = new Task(() => Write('?'));
            t.Start();

            Write('-');
        }

        static void CreateTask2()
        {


            Task t = new Task(Write, "hello");
            t.Start();
            Task.Factory.StartNew(Write, 123);

            string text1 = "testing", text2 = "this";
            var task1 = new Task<int>(TextLength, text1);
            task1.Start();
            Task<int> task2 = Task.Factory.StartNew<int>(TextLength, text2);

            Console.WriteLine($"Length of '{text1}' is {task1.Result}");
            Console.WriteLine($"Length of '{text2}' is {task2.Result}");
        }
        #endregion

        #region Cancelling a Task
        static void CancelTasks2()
        {
            /*-------------------------------------
            // Cancelling a task
            -------------------------------------*/
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            // subscribe to an event
            token.Register(() =>
            {
                Console.WriteLine("Cancellation has been requested.");
            });

            var t = new Task(() =>
            {
                int i = 0;
                while (true)
                {
                    // soft exit would break out
                    //if (token.IsCancellationRequested)
                    //break;
                    //if (token.IsCancellationRequested)
                    //{
                    //    throw new OperationCanceledException();
                    //}                        
                    //else
                    token.ThrowIfCancellationRequested();
                    Console.WriteLine($"{i++}\t");
                }
            }, token);
            t.Start();

            Task.Factory.StartNew(() =>
            {
                // Wait handle
                token.WaitHandle.WaitOne();
                Console.WriteLine("Wait handle release, cancellation was requested.");
            });

            Console.ReadKey();
            cts.Cancel();
        }

        static void CancelTasks3()
        {
            var planned = new CancellationTokenSource();
            var preventative = new CancellationTokenSource();
            var emergency = new CancellationTokenSource();

            var paranoid = CancellationTokenSource.CreateLinkedTokenSource(
                planned.Token, preventative.Token, emergency.Token);

            Task.Factory.StartNew(() =>
            {
                int i = 0;
                while (true)
                {
                    paranoid.Token.ThrowIfCancellationRequested();
                    Console.WriteLine($"{i++}\t");
                    Thread.Sleep(1000);
                }
            }, paranoid.Token);

            Console.ReadKey();
            emergency.Cancel();
        }
        #endregion

        #region Waiting for tasks

        static void WaitingForTask()
        {
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var t = new Task(() =>
            {
                // Thread.Sleep();
                //SpinWait.SpinUntil();
                Console.WriteLine("Press any key to disamr; you have 5 seconds");
                bool cancelled = token.WaitHandle.WaitOne(5000);
                Console.WriteLine(cancelled ? "Bomb disarmed" : "BOOM!");
            }, token);
            t.Start();
            Console.ReadKey();
            cts.Cancel();
        }

        static void WaitingForTask2()
        {
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            var t = new Task(() => {
                Console.WriteLine("I take 5 seconds");

                for(int i=0; i < 5; i++)
                {
                    token.ThrowIfCancellationRequested();
                    Thread.Sleep(1000);
                }
                Console.WriteLine("I'm done");
            }, token);
            t.Start();

            Task t2 = Task.Factory.StartNew(() => Thread.Sleep(3000), token);

            //Console.ReadKey();
            //cts.Cancel();

            Task.WaitAny(new[] { t, t2 }, 4000, token);

            Console.WriteLine($"Task t status is {t.Status}");
            Console.WriteLine($"Task t2 status is {t2.Status}");
            //Task.WaitAny(t, t2);
            //Task.WaitAll(t, t2);

            //t.Wait(token);
        }
        #endregion

        #region Exception Handling
        static void ExceptionHandler1()
        {
            var t = Task.Factory.StartNew(() =>
            {
                throw new InvalidOperationException("Can't do this!") {Source = "t" };
            });

            var t2 =Task.Factory.StartNew(() =>
            {
                throw new AccessViolationException("Can't do this!") { Source = "t2" };
            });
            try
            {
                Task.WaitAll(t, t2);
            }
            catch (AggregateException ae)
            {
                //foreach(var e in ae.InnerExceptions)
                //{
                //    Console.WriteLine($"Exception {e.GetType()} from {e.Source}");
                //}

                ae.Handle(e =>
               {
                   if (e is InvalidOperationException)
                   {
                       Console.WriteLine("Invalid op!");
                       return true;
                   }
                   else return false;

               });               
            }          
        }
        #endregion

        static void Main(string[] args)
        {
            // WaitingForTask();
            // WaitingForTask2();

            try
            {
                ExceptionHandler1();
            }
            catch (AggregateException ae)
            {
                foreach(var e in ae.InnerExceptions)
                {
                    Console.WriteLine($"Handled elsewhere: {e.GetType()}");
                }
                //throw;
            }

            Console.WriteLine("Main program done.");
            Console.ReadKey();
        }
    }
}
