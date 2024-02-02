namespace Misty.Remapping;

public class MapParserException : MistyException
{
    public MapParserException(string message, int lineNumber) : base($"{message} at line {lineNumber}")
    {
    }
}
