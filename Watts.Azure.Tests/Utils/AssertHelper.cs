namespace Watts.Azure.Tests.Utils
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public static class AssertHelper
    {
        /// <summary>
        /// Assert that the given action throws an exception of type ExceptionType
        /// </summary>
        /// <typeparam name="ExceptionType">The exception type</typeparam>
        /// <param name="func">The action to check</param>
        public static void Throws<ExceptionType>(Action func) where ExceptionType : Exception
        {
            var exceptionThrown = false;
            try
            {
                func.Invoke();
            }
            catch (Exception ex)
            {
                if (ex is ExceptionType || ex.InnerException is ExceptionType)
                {
                    exceptionThrown = true;
                }
            }

            if (!exceptionThrown)
            {
                throw new AssertFailedException(
                    $"An exception of type {typeof(ExceptionType)} was expected, but not thrown");
            }
        }

        /// <summary>
        /// Assert that the given action throws an exception of type ExceptionType
        /// </summary>
        /// <typeparam name="ExceptionType">The exception type</typeparam>
        /// <param name="func">The action to check</param>
        public static void DoesNotThrow<ExceptionType>(Action func) where ExceptionType : Exception
        {
            var exceptionThrown = false;
            try
            {
                func.Invoke();
            }
            catch (Exception ex)
            {
                if (ex is ExceptionType || ex.InnerException is ExceptionType)
                {
                    exceptionThrown = true;
                }
            }

            if (exceptionThrown)
            {
                throw new AssertFailedException(
                    $"An exception of type {typeof(ExceptionType)} was unexpected");
            }
        }
    }
}