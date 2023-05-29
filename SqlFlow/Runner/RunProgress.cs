namespace SqlFlow.Runner;

public struct RunProgress
{
    public RunProgress(int percent, string message)
    {
        Percent = percent;
        Message = message;
    }

    public int Percent { get; }
    public string Message { get; }
}
