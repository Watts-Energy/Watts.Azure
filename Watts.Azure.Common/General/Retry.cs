namespace Watts.Azure.Common.General
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    public class Retry
    {
        private int numberOfRetries;
        private int maxRetries = 1;
        private int delayInMs = 1000;

        private bool randomDelay = false;
        private int minDelayInMs = 1000;
        private int maxDelayInMs = 5000;

        private readonly Random rand = new Random(DateTime.Now.Millisecond);

        private readonly List<Func<bool>> retryMethod;

        private Retry(Func<bool> retryMethod)
        {
            this.retryMethod = new List<Func<bool>>() { retryMethod };
        }

        public static Retry Do(Func<bool> method)
        {
            return new Retry(method);
        }

        public Retry Then(Func<bool> method)
        {
            this.retryMethod.Add(method);
            return this;
        }

        public Retry WithDelayInMs(int milliseconds)
        {
            this.delayInMs = milliseconds;
            return this;
        }

        public Retry WithRandomDelay(int minMs, int maxMs)
        {
            this.minDelayInMs = minMs;
            this.maxDelayInMs = maxMs;
            return this;
        }

        public Retry MaxTimes(int maxTimes)
        {
            this.maxRetries = maxTimes;
            return this;
        }

        public bool Go()
        {
            bool retVal = false;
            foreach (var method in this.retryMethod)
            {
                while (!retVal && this.numberOfRetries < this.maxRetries)
                {
                    retVal = method();
                    this.numberOfRetries++;

                    if (!retVal)
                    {
                        int delay = this.randomDelay
                            ? (int)(this.minDelayInMs + this.rand.NextDouble() * (this.maxDelayInMs - this.minDelayInMs))
                            : this.delayInMs;
                        Thread.Sleep(delay);
                    }
                }

                // If we've retried too many times, return false.
                if (this.numberOfRetries >= this.maxRetries)
                {
                    return false;
                }
                else {
                    // Reset numberOfRetries
                    this.numberOfRetries = 0;
                }
            }

            return retVal;
        }
    }
}