using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace DataSharingApp
{
    class BankAccount
    {

        #region Properties
        private int balance;

        //public object padLock = new object();
        public int Balance { get => balance; set => balance = value; }

        public void Deposit(int amount)
        {
            Balance += amount;

            //  lock (padLock)
            //  {
            //  Balance += amount;
            //  }
            // Interlocked.Add(ref balance, amount);

            //Thread.MemoryBarrier();
        }

        public void Withdraw(int amount)
        {
            Balance -= amount;
            // lock (padLock)
            // {
            // Balance -= amount;
            //  }
            //Interlocked.Add(ref balance, -amount);
        } 

        public void Transfer(BankAccount where, int amount)
        {
            Balance -= amount;
            where.Balance += amount;
        }
        #endregion
    }


    class Program
    {
        static SpinLock sl = new SpinLock(true);
        #region Non Atomic
        static void Critical1()
        {
            var tasks = new List<Task>();
            var ba = new BankAccount();

            // This operation(s) are not atomic
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    for (int j = 0; j < 1000; j++)
                    {
                        ba.Deposit(100);
                    }
                }));

                tasks.Add(Task.Factory.StartNew(() =>
                {

                    for (int j = 0; j < 1000; j++)
                    {
                        ba.Withdraw(100);
                    }
                }));
            }
            Task.WaitAll(tasks.ToArray());

            Console.WriteLine($"Final balance is {ba.Balance}.");
        } 
        #endregion

        #region Interlocked Operations
        static void CriticalSpinLock()
        {
            var tasks = new List<Task>();
            var ba = new BankAccount();

            SpinLock sl = new SpinLock();

            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Factory.StartNew(() => {
                    for (int j = 0; j < 1000; j++)
                    {
                        var lockTaken = false;

                        try
                        {
                            sl.Enter(ref lockTaken);
                            ba.Deposit(100);
                        }
                        finally
                        {
                            if (lockTaken) sl.Exit();
                        }
                    }
                }));

                tasks.Add(Task.Factory.StartNew(() => {

                    for (int j = 0; j < 1000; j++)
                    {
                        var lockTaken = false;
                        try
                        {
                            sl.Enter(ref lockTaken);
                            ba.Withdraw(100);
                        }
                        finally
                        {
                            if (lockTaken) sl.Exit();
                        }
                    }
                }));
            }
            Task.WaitAll(tasks.ToArray());

            Console.WriteLine($"Final balance is {ba.Balance}.");
        }

        static void LockRecursion(int x)
        {
            bool lockTaken = false;

            try
            {
                sl.Enter(ref lockTaken);
            }
            catch (LockRecursionException e)
            {
                Console.WriteLine("Exception:" + e);
                throw;
            }
            finally
            {
                if (lockTaken)
                {
                    Console.WriteLine($"Took a lock, x= {x}");
                    LockRecursion(x - 1);
                    sl.Exit();
                }
                else
                {
                    Console.WriteLine($"Failed to take a lock, x ={x}");
                }
            }
        }
        #endregion

        #region Mutex
        static void MutexExample()
        {
            var tasks = new List<Task>();
            var ba = new BankAccount();
            Mutex mutex = new Mutex();

            // This operation(s) are not atomic
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Factory.StartNew(() => {
                    for (int j = 0; j < 1000; j++)
                    {
                        bool haveLock = mutex.WaitOne();
                        try
                        {
                            ba.Deposit(100);
                        }
                        finally
                        {
                            if (haveLock) mutex.ReleaseMutex();
                        }
                    }
                }));

                tasks.Add(Task.Factory.StartNew(() => {

                    for (int j = 0; j < 1000; j++)
                    {
                        bool haveLock = mutex.WaitOne();
                        try
                        {
                            ba.Withdraw(100);
                        }
                        finally
                        {
                            if (haveLock) mutex.ReleaseMutex();
                        }
                    }
                }));
            }
            Task.WaitAll(tasks.ToArray());

            Console.WriteLine($"Final balance is {ba.Balance}.");
        }

        static void MutexExample2()
        {
            var tasks = new List<Task>();
            var ba = new BankAccount();
            var ba2 = new BankAccount();
            Mutex mutex = new Mutex();
            Mutex mutex2 = new Mutex();
            // This operation(s) are not atomic
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Factory.StartNew(() => {
                    for (int i = 0; i < 1000; i++)
                    {
                        bool haveLock = mutex.WaitOne();
                        try
                        {
                            ba.Deposit(1);
                        }
                        finally
                        {
                            if (haveLock) mutex.ReleaseMutex();
                        }
                    }
                }));

                tasks.Add(Task.Factory.StartNew(() => {

                    for (int k = 0; k < 1000; k++)
                    {
                        bool haveLock = mutex2.WaitOne();
                        try
                        {
                            ba2.Deposit(1);
                        }
                        finally
                        {
                            if (haveLock) mutex2.ReleaseMutex();
                        }
                    }
                }));
                tasks.Add(Task.Factory.StartNew(() => {
                    for (int j = 0; j < 1000; j++)
                    {
                        bool haveLock = WaitHandle.WaitAll(new[] { mutex, mutex2 });
                        try
                        {
                            ba.Transfer(ba2, 1);
                        }
                        finally
                        {
                            if (haveLock)
                            {
                                mutex.ReleaseMutex();
                                mutex2.ReleaseMutex();
                            }
                        }
                    }
                }));
            }
            Task.WaitAll(tasks.ToArray());

            Console.WriteLine($"Final balance ba is {ba.Balance}.");
            Console.WriteLine($"Final balance ba2 is {ba2.Balance}.");
        }

        static void SharedMutex()
        {
            const string appName = "MyApp";
            Mutex mutex;
            try
            {
                mutex = Mutex.OpenExisting(appName);
                Console.WriteLine($"Sorry, {appName} is already running");
            }
            catch(WaitHandleCannotBeOpenedException e)
            {
                Console.WriteLine("We can run the program just fine.");
                mutex = new Mutex(false, appName);
            }
            finally
            {

            }

            Console.ReadKey();
            mutex.ReleaseMutex();
        }

        #endregion

        #region Reader-Writer Locks
        static ReaderWriterLockSlim padLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        static void ReaderWriterLock()
        {
            int x = 0;

            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Factory.StartNew(() => {
                    // padLock.EnterReadLock();
                    padLock.EnterUpgradeableReadLock();

                    if(i%2 ==0)
                    {
                        padLock.EnterWriteLock();
                        x = 123;
                        padLock.ExitWriteLock();
                    }

                    Console.WriteLine($"Entered read lock, x = {x}");
                    Thread.Sleep(5000);
                    //padLock.ExitReadLock();
                    padLock.ExitUpgradeableReadLock();

                    Console.WriteLine($"Exited read lock, x = {x}.");
                }));
            }

            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException ae)
            {
                ae.Handle(e => {
                    Console.WriteLine(e);
                    return true;
                });                  
            }

            Random random = new Random();
            while (true)
            {
                Console.ReadKey();
                padLock.EnterWriteLock();
                Console.WriteLine("Write lock acquired");
                int newValue = random.Next(10);
                x = newValue;
                Console.WriteLine($"Set x = {x}");
                padLock.ExitWriteLock();
                Console.WriteLine("Write lock has been released");
            }
        }
        #endregion
        static void Main(string[] args)
        {
            // Critical1();           
            //CriticalSpinLock();
            //  LockRecursion(5);
            // MutexExample();
            // MutexExample2();
            // SharedMutex();
            ReaderWriterLock();


        }
    }
}
