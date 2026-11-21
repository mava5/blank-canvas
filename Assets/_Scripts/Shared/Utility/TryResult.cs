
using System;

public struct TryResult
{
    public bool Success;
    public string ErrorMessage;
    
    internal TryResult(bool success, string potentialErrorMessage)
    {
        Success = success;
        ErrorMessage = potentialErrorMessage;
    }

    public static implicit operator bool(TryResult result) => result.Success;

    public static TryResult Ok() => new(true, "");
    public static TryResult Fail() => new(false, "");
    public static TryResult Fail(string errorMessage) => new(false, errorMessage);

    public static TryResult<T> Ok<T>(T value = default) => new(value, true, "");
    public static TryResult<T> Fail<T>() => new(default, false, "");
    public static TryResult<T> Fail<T>(string errorMessage) => new(default, false, errorMessage);
    public static TryResult<T> Fail<T>(T value, string errorMessage) => new(value, false, errorMessage);
}


public struct TryResult<T>
{
    public T Value;
    public bool Success;
    public string ErrorMessage;

    internal TryResult(T value, bool success, string potentialErrorMessage)
    {
        Value = value;
        Success = success;
        ErrorMessage = potentialErrorMessage;
    }

    public static implicit operator bool(TryResult<T> result) => result.Success;
    public static implicit operator TryResult<T>(TryResult result) => new(default, result.Success, result.ErrorMessage);
}
