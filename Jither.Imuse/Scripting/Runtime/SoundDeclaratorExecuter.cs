using Jither.Imuse.Scripting.Ast;
using System;

namespace Jither.Imuse.Scripting.Runtime
{
    public class SoundDeclaratorExecuter : Executer
    {
        private readonly LiteralExecuter name;
        private readonly ExpressionExecuter id;

        public SoundDeclaratorExecuter(SoundDeclarator sound)
        {
            name = new LiteralExecuter(sound.Name);
            id = ExpressionExecuter.Build(sound.Id);
        }

        protected override ExecutionResult Execute(ExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }
}
