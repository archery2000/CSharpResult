﻿using System;
using System.Threading.Tasks;

namespace CSharp_Result
{
   /// <summary>
   /// Static class with extension methods to support Async Result processing
   /// </summary>
   public static class AsyncResult 
   {
      /// <summary>
      /// Converts an object into an Async Success
      /// </summary>
      /// <param name="obj">Object to convert</param>
      /// <typeparam name="TSucc">Type of the object</typeparam>
      /// <returns>An Async Success containing the object</returns>
      public static async Task<Result<TSucc>> ToAsyncResult<TSucc>(this TSucc obj) 
         where TSucc : notnull
      {
         return obj;
      }
      /// <summary>
      /// Converts a Result into an Async Result
      /// </summary>
      /// <param name="obj">Result to convert</param>
      /// <typeparam name="TSucc">Type of the Success</typeparam>
      /// <returns>An Async Result containing the object</returns>
      public static async Task<Result<TSucc>> ToAsyncResult<TSucc>(this Result<TSucc> obj) 
         where TSucc : notnull
      {
         return obj;
      }
      
      /// <summary>
      /// Converts a normal single argument function into a new function that outputs a Result.
      /// </summary>
      /// <param name="func">Function to execute</param>
      /// <param name="mapException">Exceptions to catch and map to Failure</param>
      /// <typeparam name="TInput">Input type</typeparam>
      /// <typeparam name="TSucc">Output type</typeparam>
      /// <returns>Async Function that outputs a Success if successful, converting relevant Exceptions into Failure</returns>
      public static Func<TInput, Task<Result<TSucc>>> ToAsyncResultFunc<TInput, TSucc>(this Func<TInput, Task<TSucc>> func, Func<Exception, Exception> mapException) 
         where TSucc : notnull
      {
         return (async x =>
         {
            try
            {
               return await func(x);
            }
            catch(Exception e)
            {
               return mapException(e);
            }
         });
      }
      
      /// <summary>
      /// Converts a normal single argument void function into a new function that outputs a Result.
      /// </summary>
      /// <param name="func">Function to execute</param>
      /// <param name="mapException">Exceptions to catch and map to Failure</param>
      /// <typeparam name="TInput">Input type</typeparam>
      /// <returns>Async Function that outputs a Success of Unit if successful, converting relevant Exceptions into Failure</returns>
      public static Func<TInput, Task<Result<Unit>>> ToAsyncResultFunc<TInput>(this Func<TInput, Task> func, Func<Exception, Exception> mapException) 
      {
         return (async x =>
         {
            try
            {
               await func(x);
               return new Unit();
            }
            catch(Exception e)
            {
               return mapException(e);
            }
         });
      }

      /// <summary>
      /// Checks if value contained in Async Result is not null
      /// </summary>
      /// <param name="result">Result to check</param>
      /// <typeparam name="TSucc">Type of the value contained</typeparam>
      /// <returns>If value in Result is null, returns a NulLReferenceException as Failure</returns>
      public static async Task<Result<TSucc>> IsNotNull<TSucc>(this Task<Result<TSucc>> result)
      {
         return (await result).Match(
            Success: x => (Result<TSucc>)x?? (Result<TSucc>)new NullReferenceException(),
            Failure: x => x
         );
      }
      
      /// <summary>
      /// Checks if Async Result is a Failure
      /// </summary>
      /// <returns>True if Async Result is a Failure</returns>
      public static Task<bool> IsFailure<TSucc>(this Task<Result<TSucc>> result)
         where TSucc : notnull
      {
         return result.Match(Success:s => false, Failure: e => true);
      }
        
      /// <summary>
      /// Checks if Async Result contains a Success
      /// </summary>
      /// <returns>True if Async Result is contains a Success</returns>
      public static async Task<bool> IsSuccess<TSucc>(this Task<Result<TSucc>> result)
         where TSucc : notnull
      {
         return !await result.IsFailure();
      }
      
      
      /// <summary>
      /// Returns the contents of the Async Result if successful, or throws the exception if it failed.
      /// </summary>
      /// <returns></returns>
      public static Task<TSucc> Get<TSucc>(this Task<Result<TSucc>> result)
         where TSucc : notnull
      {
         return result.Match(
            x => x,
            err => throw err
         );
      }


      /// <summary>
      /// Unwraps and returns the Success if it exists, otherwise returns a default value
      /// </summary>
      /// <param name="defaultImpl">Optional default value to return</param>
      /// <returns>Success or default value</returns>
      public static Task<TSucc> SuccessOrDefault<TSucc>(this Task<Result<TSucc>> result, TSucc defaultImpl = default)
         where TSucc : notnull
      {
         return result.Match(
            Success: s => s,
            Failure: e => defaultImpl
         );
      }

      /// <summary>
      /// Unwraps and returns the Failure if it exists, otherwise returns a default value
      /// </summary>
      /// <param name="defaultImpl">Optional default value to return</param>
      /// <returns>Failure or default value</returns>
      public static Task<Exception> FailureOrDefault<TSucc>(this Task<Result<TSucc>> result, Exception defaultImpl = default)
         where TSucc : notnull
      {
         return result.Match(
            Success: s => defaultImpl,
            Failure: e => e
         );
      }
   }
   /// <summary>
   /// Static class for extensions methods manipulating Async Results
   /// </summary>
   public static class AsyncResultExtensions
   {
      /// <summary>
      /// If Async Result is a Failure, passes the Failure through a mapper function.
      /// </summary>
      /// <param name="res">Input Async Result</param>
      /// <param name="mapper">Maps Failure to other Failures</param>
      /// <returns>Mapped Failure if Result is Failure</returns>
      public static async Task<Result<TSucc>> MapFailure<TSucc>(this Task<Result<TSucc>> res, Func<Exception, Exception> mapper) 
         where TSucc : notnull
      {
         var result = await res;
         return result.MapFailure(mapper);
      }

      /// <summary>
      /// Define continuations on the success and failure cases.
      /// If Result is Success, executes Success
      /// Else, executes Failure
      /// </summary>
      /// <param name="res">Input Result</param>
      /// <param name="Success">Async function to execute on Success</param>
      /// <param name="Failure">Async function to execute on Failure</param>
      /// <typeparam name="TInput">Type of the input Result</typeparam>
      /// <typeparam name="T">Return type of the function</typeparam>
      /// <returns>Object of type T</returns>
      public static async Task<T> MatchAwait<TInput, T>(this Task<Result<TInput>> res, Func<TInput, Task<T>> Success, Func<Exception, Task<T>> Failure)
         where TInput : notnull
      {
         var result = await res switch
         {
            Success<TInput> s => Success(s),
            Failure<TInput> e => Failure(e),
            _ => throw new NotSupportedException("Unable to match Result with non Success or Failure!")
         };
         return await result;
      }
      
      /// <summary>
      /// Define continuations on the success and failure cases.
      /// If Result is Success, executes Success
      /// Else, executes Failure
      /// </summary>
      /// <param name="res">Input Result</param>
      /// <param name="Success">Async function to execute on Success</param>
      /// <param name="Failure">Async function to execute on Failure</param>
      /// <typeparam name="TInput">Type of the input Result</typeparam>
      public static async Task MatchAwait<TInput>(this Task<Result<TInput>> res, Func<TInput, Task> Success, Func<Exception, Task> Failure)
         where TInput : notnull
      {
         var result = await res switch
         {
            Success<TInput> s => Success(s),
            Failure<TInput> e => Failure(e),
            _ => throw new NotSupportedException("Unable to match Result with non Success or Failure!")
         };
         await result;
      }
      
      /// <summary>
      /// Define continuations on the success and failure cases.
      /// If Result is Success, executes Success
      /// Else, executes Failure
      /// </summary>
      /// <param name="res">Input Result</param>
      /// <param name="Success">Function to execute on Success</param>
      /// <param name="Failure">Function to execute on Failure</param>
      /// <typeparam name="TInput">Type of the input Result</typeparam>
      /// <typeparam name="T">Return type of the function</typeparam>
      /// <returns>Object of type T</returns>
      public static async Task<T> Match<TInput, T>(this Task<Result<TInput>> res, Func<TInput, T> Success, Func<Exception, T> Failure)
         where TInput : notnull
      {
         return (await res).Match(Success, Failure);
      }
      
      /// <summary>
      /// Define continuations on the success and failure cases.
      /// If Result is Success, executes Success
      /// Else, executes Failure
      /// </summary>
      /// <param name="res">Input Result</param>
      /// <param name="Success">Function to execute on Success</param>
      /// <param name="Failure">Function to execute on Failure</param>
      /// <typeparam name="TInput">Type of the input Result</typeparam>
      public static async Task Match<TInput>(this Task<Result<TInput>> res, Action<TInput> Success, Action<Exception> Failure)
         where TInput : notnull
      {
         (await res).Match(Success, Failure);
      }

      /// <summary>
      /// If holding a Success, Executes the async function with the result as input.
      /// </summary>
      /// <param name="res">Input Async Result</param>
      /// <param name="function">The function to execute</param>
      /// <typeparam name="TSucc">Input type</typeparam>
      /// <typeparam name="TResult">The type of the result of the computation (unused)</typeparam>
      /// <returns>Async of either the Success, or a Failure</returns>
      public static Task<Result<TSucc>> DoAwait<TSucc, TResult>(this Task<Result<TSucc>> res, Func<TSucc, Task<Result<TResult>>> function) 
         where TSucc : notnull
         where TResult : notnull
      {
         return res.MatchAwait(
            Success: s =>
            {
               return function(s).Then(x => s, Errors.MapNone);
            },
            Failure: e => Task.FromResult((Result<TSucc>)e)
         );
      }

      /// <summary>
      /// If holding a Success, Executes the async function with the result as input.
      /// If any exception is thrown, it is mapped by the mapper function.
      /// </summary>
      /// <param name="res">Input Async Result</param>
      /// <param name="function">The function to execute</param>
      /// <param name="mapException">The mapping function for the error</param>
      /// <typeparam name="TSucc">Input type</typeparam>
      /// <typeparam name="TResult">The type of the result of the computation (unused)</typeparam>
      /// <returns>Async of either the Success, or a Failure</returns>
      public static Task<Result<TSucc>> DoAwait<TSucc, TResult>(this Task<Result<TSucc>> res, Func<TSucc, Task<TResult>> function, Func<Exception, Exception> mapException) 
         where TSucc : notnull
         where TResult : notnull
      {
         return res.DoAwait(function.ToAsyncResultFunc(mapException));
      }
      
      /// <summary>
      /// If holding a Success, Executes the async function with the result as input.
      /// If any exception is thrown, it is mapped by the mapper function.
      /// </summary>
      /// <param name="res">Input Async Result</param>
      /// <param name="function">The function to execute</param>
      /// <param name="mapException">The mapping function for the error</param>
      /// <typeparam name="TSucc">Input type</typeparam>
      /// <returns>Async of either a Success of Unit, or a Failure</returns>
      public static Task<Result<TSucc>> DoAwait<TSucc>(this Task<Result<TSucc>> res, Func<TSucc, Task> function, Func<Exception, Exception> mapException) 
         where TSucc : notnull
      {
         return res.DoAwait(function.ToAsyncResultFunc(mapException));
      }
      
      /// <summary>
      /// If holding a Success, Executes the function with the result as input.
      /// </summary>
      /// <param name="res">Input Async Result</param>
      /// <param name="function">The function to execute</param>
      /// <typeparam name="TSucc">Input type</typeparam>
      /// <typeparam name="TResult">The type of the result of the computation (unused)</typeparam>
      /// <returns>Async of either the Success, or a Failure</returns>
      public static async Task<Result<TSucc>> Do<TSucc, TResult>(this Task<Result<TSucc>> res, Func<TSucc, Result<TResult>> function) 
         where TSucc : notnull
      {
         var result = await res;
         return result.Do(function);
      }

      /// <summary>
      /// If holding a Success, Executes the function with the result as input.
      /// If any exception is thrown, it is mapped by the mapper function.
      /// </summary>
      /// <param name="res">Input Async Result</param>
      /// <param name="function">The function to execute</param>
      /// <param name="mapException">The mapping function for the error</param>
      /// <typeparam name="TSucc">Input type</typeparam>
      /// <typeparam name="TResult">The type of the result of the computation (unused)</typeparam>
      /// <returns>Async of either the Success, or a Failure</returns>
      public static Task<Result<TSucc>> Do<TSucc, TResult>(this Task<Result<TSucc>> res, Func<TSucc, TResult> function, Func<Exception, Exception> mapException) 
         where TSucc : notnull
      {
         return res.Do(function.ToResultFunc(mapException));
      }
      
      /// <summary>
      /// If holding a Success, Executes the function with the result as input.
      /// If any exception is thrown, it is mapped by the mapper function.
      /// </summary>
      /// <param name="res">Input Async Result</param>
      /// <param name="function">The function to execute</param>
      /// <param name="mapException">The mapping function for the error</param>
      /// <typeparam name="TSucc">Input type</typeparam>
      /// <returns>Async of either a Success of Unit, or a Failure</returns>
      public static Task<Result<TSucc>> Do<TSucc>(this Task<Result<TSucc>> res, Action<TSucc> function, Func<Exception, Exception> mapException) 
         where TSucc : notnull
      {
         return res.Do(function.ToResultFunc(mapException));
      }

      /// <summary>
      /// If holding a Success, Executes the async function with the result as input.
      /// Returns the result of the computation.
      /// </summary>
      /// <param name="res">Input Async Result</param>
      /// <param name="function">The function to execute</param>
      /// <typeparam name="TSucc">Input type</typeparam>
      /// <typeparam name="TResult">The type of the result of the computation</typeparam>
      /// <returns>Async of either a Success from the computation, or a Failure</returns>
      public static Task<Result<TResult>> ThenAwait<TSucc, TResult>(this Task<Result<TSucc>> res, Func<TSucc, Task<Result<TResult>>> function) 
         where TSucc : notnull
         where TResult : notnull
      {
         return res.MatchAwait(
            Success: function,
            Failure: e => Task.FromResult((Result<TResult>)e)
            );
      }
      
      /// <summary>
      /// If holding a Success, Executes the async function with the result as input.
      /// If any exception is thrown, it is mapped by the mapper function, otherwise returns the result of the computation.
      /// </summary>
      /// <param name="res">Input Async Result</param>
      /// <param name="function">The function to execute</param>
      /// <param name="mapException">The mapping function for the error</param>
      /// <typeparam name="TSucc">Input type</typeparam>
      /// <typeparam name="TResult">The type of the result of the computation</typeparam>
      /// <returns>Async of either a Success from the computation, or a Failure</returns>
      public static Task<Result<TResult>> ThenAwait<TSucc, TResult>(this Task<Result<TSucc>> res, Func<TSucc, Task<TResult>> function, Func<Exception, Exception> mapException) 
         where TSucc : notnull
         where TResult : notnull
      {
         return res.ThenAwait(function.ToAsyncResultFunc(mapException));
      }
      
      /// <summary>
      /// If holding a Success, Executes the async function with the result as input.
      /// If any exception is thrown, it is mapped by the mapper function, otherwise returns Unit.
      /// </summary>
      /// <param name="res">Input Async Result</param>
      /// <param name="function">The function to execute</param>
      /// <param name="mapException">The mapping function for the error</param>
      /// <typeparam name="TSucc">Input type</typeparam>
      /// <returns>Async of either a Success containing Unit, or a Failure</returns>
      public static Task<Result<Unit>> ThenAwait<TSucc>(this Task<Result<TSucc>> res, Func<TSucc, Task> function, Func<Exception, Exception> mapException) 
         where TSucc : notnull
      {
         return res.ThenAwait(function.ToAsyncResultFunc(mapException));
      }
      
      /// <summary>
      /// If holding a Success, Executes the function with the result as input.
      /// Returns the result of the computation.
      /// </summary>
      /// <param name="res">Input Async Result</param>
      /// <param name="function">The function to execute</param>
      /// <typeparam name="TSucc">Input type</typeparam>
      /// <typeparam name="TResult">The type of the result of the computation</typeparam>
      /// <returns>Async of either a Success from the computation, or a Failure</returns>
      public static async Task<Result<TResult>> Then<TSucc, TResult>(this Task<Result<TSucc>> res, Func<TSucc, Result<TResult>> function) 
         where TSucc : notnull
         where TResult : notnull
      {
         var result = await res;
         return result.Then(function);
      }
      
      /// <summary>
      /// If holding a Success, Executes the async function with the result as input.
      /// If any exception is thrown, it is mapped by the mapper function, otherwise returns the result of the computation.
      /// </summary>
      /// <param name="res">Input Async Result</param>
      /// <param name="function">The function to execute</param>
      /// <param name="mapException">The mapping function for the error</param>
      /// <typeparam name="TSucc">Input type</typeparam>
      /// <typeparam name="TResult">The type of the result of the computation</typeparam>
      /// <returns>Async of either a Success from the computation, or a Failure</returns>
      public static Task<Result<TResult>> Then<TSucc, TResult>(this Task<Result<TSucc>> res, Func<TSucc, TResult> function, Func<Exception, Exception> mapException) 
         where TSucc : notnull
         where TResult : notnull
      {
         return res.Then(function.ToResultFunc(mapException));
      }
      
      /// <summary>
      /// If holding a Success, Executes the async function with the result as input.
      /// If any exception is thrown, it is mapped by the mapper function, otherwise returns Unit.
      /// </summary>
      /// <param name="res">Input Async Result</param>
      /// <param name="function">The function to execute</param>
      /// <param name="mapException">The mapping function for the error</param>
      /// <typeparam name="TSucc">Input type</typeparam>
      /// <returns>Async of either a Success containing Unit, or a Failure</returns>
      public static Task<Result<Unit>> Then<TSucc>(this Task<Result<TSucc>> res, Action<TSucc> function, Func<Exception, Exception> mapException) 
         where TSucc : notnull
      {
         return res.Then(function.ToResultFunc(mapException));
      }
      
      /// <summary>
      /// If holding a Success, Executes the async function with the result as input. If result is false, returns Failure.
      /// </summary>
      /// <param name="res">Input Async Result</param>
      /// <param name="function">The function to execute</param>
      /// <typeparam name="TSucc">Input type</typeparam>
      /// <returns>Async of either the Success, or a Failure</returns>
      public static Task<Result<TSucc>> IfAwait<TSucc>(this Task<Result<TSucc>> res, Func<TSucc, Task<Result<bool>>> function) 
         where TSucc : notnull
      {
         return res.MatchAwait(
            Success: s =>
            {
               return function(s).Then(x => x?(Result<TSucc>)s:(Result<TSucc>)new Exception("Predicate returned false!"));
            },
            Failure: e => Task.FromResult((Result<TSucc>)e)
         );
      }

      /// <summary>
      /// If holding a Success, Executes the async function with the result as input. If result is false, returns Failure.
      /// If any exception is thrown, it is mapped by the mapper function.
      /// </summary>
      /// <param name="res">Input Async Result</param>
      /// <param name="function">The function to execute</param>
      /// <param name="mapException">The mapping function for the error</param>
      /// <typeparam name="TSucc">Input type</typeparam>
      /// <returns>Async of either the Success, or a Failure</returns>
      public static Task<Result<TSucc>> IfAwait<TSucc>(this Task<Result<TSucc>> res, Func<TSucc, Task<bool>> function, Func<Exception, Exception> mapException) 
         where TSucc : notnull
      {
         return res.IfAwait(function.ToAsyncResultFunc(mapException));
      }

      /// <summary>
      /// If holding a Success, Executes the function with the result as input. If result is false, returns Failure.
      /// </summary>
      /// <param name="res">Input Async Result</param>
      /// <param name="function">The function to execute</param>
      /// <typeparam name="TSucc">Input type</typeparam>
      /// <returns>Async of either the Success, or a Failure</returns>
      public static async Task<Result<TSucc>> If<TSucc>(this Task<Result<TSucc>> res, Func<TSucc, Result<bool>> function) 
         where TSucc : notnull
      {
         var result = await res;
         return result.If(function);
      }

      /// <summary>
      /// If holding a Success, Executes the function with the result as input. If result is false, returns Failure.
      /// If any exception is thrown, it is mapped by the mapper function.
      /// </summary>
      /// <param name="res">Input Async Result</param>
      /// <param name="function">The function to execute</param>
      /// <param name="mapException">The mapping function for the error</param>
      /// <typeparam name="TSucc">Input type</typeparam>
      /// <returns>Async of either the Success, or a Failure</returns>
      public static Task<Result<TSucc>> If<TSucc>(this Task<Result<TSucc>> res, Func<TSucc, bool> function, Func<Exception, Exception> mapException) 
         where TSucc : notnull
      {
         return res.If(function.ToResultFunc(mapException));
      }
   }
}