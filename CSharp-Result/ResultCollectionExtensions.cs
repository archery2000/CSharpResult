using System;
using System.Collections.Generic;
using System.Linq;

namespace CSharp_Result
{
    /// <summary>
    /// A set of extension methods to convert between Results of Collections and Collections of Results
    /// </summary>
    public static class ResultCollectionExtensions
    {
        /// <summary>
        /// Checks if all the results in the Collection are not Failures.
        /// </summary>
        /// <param name="collection">Collection of Results to check</param>
        /// <typeparam name="T">Type contained in Success</typeparam>
        /// <returns>True if all results are not Failures</returns>
        public static bool AllSucceed<T>(this IEnumerable<Result<T>> collection) 
            where T : notnull
        {
            return collection.All(x => x.IsSuccess());
        }
        
        /// <summary>
        /// Checks if at least one result in the Collection is not a Failure
        /// </summary>
        /// <param name="collection">Collection of Results to check</param>
        /// <typeparam name="T">Type contained in Success</typeparam>
        /// <returns>True if at least one result is not a Failure</returns>
        public static bool AnySucceed<T>(this IEnumerable<Result<T>> collection) 
            where T : notnull
        {
            return collection.Any(x => x.IsSuccess());
        }
        
        /// <summary>
        /// Checks if all the results in the Collection are Failures.
        /// </summary>
        /// <param name="collection">Collection of Results to check</param>
        /// <typeparam name="T">Type contained in Success</typeparam>
        /// <returns>True if all results are Failures</returns>
        public static bool AllFail<T>(this IEnumerable<Result<T>> collection) 
            where T : notnull
        {
            return collection.All(x => x.IsFailure());
        }
        
        /// <summary>
        /// Checks if at least one result in the Collection is a Failure
        /// </summary>
        /// <param name="collection">Collection of Results to check</param>
        /// <typeparam name="T">Type contained in Success</typeparam>
        /// <returns>True if at least one result is a Failure</returns>
        public static bool AnyFail<T>(this IEnumerable<Result<T>> collection) 
            where T : notnull
        {
            return collection.Any(x => x.IsFailure());
        }

        /// <summary>
        /// Returns all the contents of the underlying Results if all elements succeed, otherwise throw an aggregate exception with all failures
        /// </summary>
        /// <param name="collection">The collection to parse</param>
        /// <typeparam name="TSucc">The type of the success wrapped</typeparam>
        /// <returns>The collection of successes if they all succeed</returns>
        /// <exception cref="AggregateException">The collection of exceptions if any fail</exception>
        public static IEnumerable<TSucc> Get<TSucc>(this IEnumerable<Result<TSucc>> collection)
            where TSucc : notnull
        {
            var enumerable = collection.ToList();
            var failures = enumerable.GetFailures().ToList();
            if (failures.Any())
            {
                throw new AggregateException(failures);
            }
            return enumerable.GetSuccesses();
        }

        /// <summary>
        /// Gets all the Failures 
        /// </summary>
        /// <param name="collection">Collection of Results to filter</param>
        /// <typeparam name="T">Type contained in Success</typeparam>
        /// <returns>IEnumerable of all the Failures in the Collection</returns>
        public static IEnumerable<Exception> GetFailures<T>(this IEnumerable<Result<T>> collection) 
            where T : notnull
        {
            return collection.Where(x => x.IsFailure()).Select(x => x.FailureOrDefault());
        }
        
        /// <summary>
        /// Gets all the Successes 
        /// </summary>
        /// <param name="collection">Collection of Results to filter</param>
        /// <typeparam name="T">Type contained in Success</typeparam>
        /// <returns>IEnumerable of all the Successes in the Collection</returns>
        public static IEnumerable<T> GetSuccesses<T>(this IEnumerable<Result<T>> collection) 
            where T : notnull
        {
            return collection.Where(x => x.IsSuccess()).Select(x => x.SuccessOrDefault());
        }

        /// <summary>
        /// Converts a Sequence of Results into a Result of Sequence.
        /// </summary>
        /// <param name="collection">Collection of Results to filter</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>A Result of a sequence</returns>
        public static Result<IEnumerable<T>> ToResultOfSeq<T>(this IEnumerable<Result<T>> collection) 
            where T : notnull
        {
            var enumerable = collection.ToList();
            var errors = enumerable.GetFailures();
            var innerExceptions = errors.ToList();
            if (innerExceptions.Any())
            {
                return new AggregateException(innerExceptions);
            }
            return new Success<IEnumerable<T>>(enumerable.GetSuccesses());
        }
        
        /// <summary>
        /// Converts a Result of a Sequence into a Sequence of Results
        /// </summary>
        /// <param name="collection">Result of collection to process</param>
        /// <typeparam name="T">Type contained in sequence</typeparam>
        /// <returns>A sequence of results</returns>
        public static IEnumerable<Result<T>> ToSeqOfResults<T>(this Result<IEnumerable<T>> collection) 
            where T : notnull
        {
            if (collection.IsFailure())
            {
                return new List<Result<T>>{collection.FailureOrDefault()};
            }
            else
            {
                return collection.SuccessOrDefault().Select(r => new Success<T>(r));
            }
        }
        
        /// <summary>
        /// Executes Do on each element of the collection
        /// </summary>
        /// <param name="results">Input Async Result Collection</param>
        /// <param name="function">The function to execute</param>
        /// <typeparam name="TSucc">Input type</typeparam>
        /// <typeparam name="TResult">The type of the result of the computation (unused)</typeparam>
        /// <returns>Collection after executing function on each element</returns>
        public static IEnumerable<Result<TSucc>> DoEach<TSucc, TResult>(this IEnumerable<Result<TSucc>> results,
            Func<TSucc, Result<TResult>> function)
            where TSucc: notnull
            where TResult: notnull
        {
            return results.Select(x => x.Do(function));
        }
        
        /// <summary>
        /// Executes Do on each element of the collection
        /// </summary>
        /// <param name="results">Input Async Result Collection</param>
        /// <param name="function">The function to execute</param>
        /// <param name="mapException">The mapping function for the error</param>
        /// <typeparam name="TSucc">Input type</typeparam>
        /// <typeparam name="TResult">The type of the result of the computation (unused)</typeparam>
        /// <returns>Collection after executing function on each element</returns>
        public static IEnumerable<Result<TSucc>> DoEach<TSucc, TResult>(this IEnumerable<Result<TSucc>> results,
            Func<TSucc, TResult> function, Func<Exception, Exception> mapException)
            where TSucc: notnull
        {
            return results.Select(x => x.Do(function, mapException));
        }
        
        /// <summary>
        /// Executes Do on each element of the collection
        /// </summary>
        /// <param name="results">Input Async Result Collection</param>
        /// <param name="function">The function to execute</param>
        /// <param name="mapException">The mapping function for the error</param>
        /// <typeparam name="TSucc">Input type</typeparam>
        /// <returns>Collection after executing function on each element</returns>
        public static IEnumerable<Result<TSucc>> DoEach<TSucc>(this IEnumerable<Result<TSucc>> results,
            Action<TSucc> function, Func<Exception, Exception> mapException)
            where TSucc: notnull
        {
            return results.Select(x => x.Do(function, mapException));
        }

        /// <summary>
        /// Executes Then on each element of the collection
        /// </summary>
        /// <param name="results">Input Async Result Collection</param>
        /// <param name="function">The function to execute</param>
        /// <typeparam name="TSucc">Input type</typeparam>
        /// <typeparam name="TResult">The type of the result of the computation</typeparam>
        /// <returns>Collection after executing function on each element</returns>
        public static IEnumerable<Result<TResult>> ThenEach<TSucc, TResult>(this IEnumerable<Result<TSucc>> results,
            Func<TSucc, Result<TResult>> function)
            where TSucc: notnull
            where TResult: notnull
        {
            return results.Select(x => x.Then(function));
        }
        
        /// <summary>
        /// Executes Then on each element of the collection
        /// </summary>
        /// <param name="results">Input Async Result Collection</param>
        /// <param name="function">The function to execute</param>
        /// <param name="mapException">The mapping function for the error</param>
        /// <typeparam name="TSucc">Input type</typeparam>
        /// <typeparam name="TResult">The type of the result of the computation</typeparam>
        /// <returns>Collection after executing function on each element</returns>
        public static IEnumerable<Result<TResult>> ThenEach<TSucc, TResult>(this IEnumerable<Result<TSucc>> results,
            Func<TSucc, TResult> function, Func<Exception, Exception> mapException)
            where TSucc: notnull
            where TResult: notnull
        {
            return results.Select(x => x.Then(function, mapException));
        }
        
        /// <summary>
        /// Executes Then on each element of the collection
        /// </summary>
        /// <param name="results">Input Async Result Collection</param>
        /// <param name="function">The function to execute</param>
        /// <param name="mapException">The mapping function for the error</param>
        /// <typeparam name="TSucc">Input type</typeparam>
        /// <returns>Collection after executing function on each element</returns>
        public static IEnumerable<Result<Unit>> ThenEach<TSucc>(this IEnumerable<Result<TSucc>> results,
            Action<TSucc> function, Func<Exception, Exception> mapException)
            where TSucc: notnull
        {
            return results.Select(x => x.Then(function, mapException));
        }
        
        /// <summary>
        /// Executes If on each element of the collection
        /// </summary>
        /// <param name="results">Input Async Result Collection</param>
        /// <param name="function">The function to execute</param>
        /// <typeparam name="TSucc">Input type</typeparam>
        /// <returns>Collection after executing function on each element</returns>
        public static IEnumerable<Result<TSucc>> IfEach<TSucc>(this IEnumerable<Result<TSucc>> results,
            Func<TSucc, Result<bool>> function)
            where TSucc: notnull
        {
            return results.Select(x => x.If(function));
        }
        
        /// <summary>
        /// Executes If on each element of the collection
        /// </summary>
        /// <param name="results">Input Async Result Collection</param>
        /// <param name="function">The function to execute</param>
        /// <param name="mapException">The mapping function for the error</param>
        /// <typeparam name="TSucc">Input type</typeparam>
        /// <returns>Collection after executing function on each element</returns>
        public static IEnumerable<Result<TSucc>> IfEach<TSucc>(this IEnumerable<Result<TSucc>> results,
            Func<TSucc, bool> function, Func<Exception, Exception> mapException)
            where TSucc: notnull
        {
            return results.Select(x => x.If(function, mapException));
        }

        /// <summary>
        /// Executes Match on each element of the collection
        /// </summary>
        /// <param name="results">Input Async Result Collection</param>
        /// <param name="Success">The function to execute if Result is Success</param>
        /// <param name="Failure">The function to execute if Result is Error</param>
        /// <typeparam name="TSucc">Input type</typeparam>
        /// <typeparam name="TResult">Return type</typeparam>
        /// <returns>Collection after executing function on each element</returns>
        public static IEnumerable<TResult> MatchEach<TSucc, TResult>(this IEnumerable<Result<TSucc>> results,
            Func<TSucc, TResult> Success, Func<Exception, TResult> Failure)
        {
            return results.Select(x => x.Match(Success, Failure));
        }
        
        /// <summary>
        /// Executes Match on each element of the collection
        /// </summary>
        /// <param name="results">Input Async Result Collection</param>
        /// <param name="Success">The function to execute if Result is Success</param>
        /// <param name="Failure">The function to execute if Result is Error</param>
        /// <typeparam name="TSucc">Input type</typeparam>
        /// <returns>Collection after executing function on each element</returns>
        public static void MatchEach<TSucc>(this IEnumerable<Result<TSucc>> results,
            Action<TSucc> Success, Action<Exception> Failure)
        {
            foreach(var x in results)
            {
                x.Match(Success, Failure);
            }
        }

        /// <summary>
        /// Converts a regular collection to an IEnumerable of Results
        /// </summary>
        /// <param name="collection">collection to convert</param>
        /// <typeparam name="T">type contained in collection</typeparam>
        /// <returns>Collection of Results</returns>
        public static IEnumerable<Result<T>> ToResultCollection<T>(this IEnumerable<T> collection)
        {
            return collection.Select(x => x.ToResult());
        }
    }
}