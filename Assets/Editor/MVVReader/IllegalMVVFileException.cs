namespace Assets.Editor.MVVReader
{
    /// <summary>
    /// Defines an error in the MVV file
    /// </summary>
    public class IllegalMVVFileException : System.Exception
    {
        public IllegalMVVFileException() : base() { }
        public IllegalMVVFileException(string message) : base(message) { }
        public IllegalMVVFileException(string message, System.Exception inner) : base(message, inner) { }
    }
}