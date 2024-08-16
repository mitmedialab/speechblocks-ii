public class CoroutineResult<T>
{
    public void Reset()
    {
        success = false;
        result = default(T);
        errorCode = null;
    }

    public void Set(T result)
    {
        success = true;
        this.result = result;
        errorCode = null;
    }

    public void SetErrorCode(string errorCode)
    {
        success = false;
        result = default(T);
        this.errorCode = errorCode;
    }

    public bool WasSuccessful() { return success; }

    public T GetResult() { return result; }

    public string GetErrorCode() { return errorCode; }

    private bool success = false;
    private T result = default(T);
    private string errorCode = null;
}
