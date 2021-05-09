namespace Jither.Imuse.Scripting.Runtime
{
    public abstract class Executer
    {
        protected readonly ExecutionContext context;

        protected abstract ExecutionResult Execute(ExecutionContext context);
    }
}
