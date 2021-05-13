using Jither.Imuse.Scripting.Runtime.Executers;

namespace Jither.Imuse.Scripting.Types
{
    public class ImuseAction
    {
        public string Name { get; }
        public int? During { get; }
        public StatementExecuter BodyExecuter { get; }

        public ImuseAction(string name, int? during, StatementExecuter bodyExecuter)
        {
            Name = name;
            During = during;
            BodyExecuter = bodyExecuter;
        }
    }
}
