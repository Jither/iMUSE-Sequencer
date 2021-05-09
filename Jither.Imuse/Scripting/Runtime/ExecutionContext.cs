namespace Jither.Imuse.Scripting.Runtime
{
    public class ExecutionContext
    {
        public ImuseEngine Engine { get; }

        public ExecutionContext(ImuseEngine engine)
        {
            Engine = engine;
        }
    }
}
