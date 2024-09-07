namespace AbyssCLI.Tool
{
    public interface IError
    {
        string Message { get; }
    }

    public class StringError : IError
    {
        public StringError(string message)
        {
            Message = message;
        }

        public string Message { get; private set; }
    }
}
