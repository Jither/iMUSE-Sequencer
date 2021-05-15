using Jither.Imuse.Files;
using Jither.Imuse.Scripting.Ast;
using System;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class SoundDeclaratorExecuter : Executer
    {
        private readonly LiteralExecuter name;
        private readonly ExpressionExecuter id;

        public SoundDeclaratorExecuter(SoundDeclarator sound) : base(sound)
        {
            name = new LiteralExecuter(sound.Name);
            id = ExpressionExecuter.Build(sound.Id);
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            int soundId = id.GetValue(context).AsInteger(id);
            string name = this.name.GetValue(context).AsString(this.name);
            context.Engine.RegisterSound(
                soundId,
                // TODO: The actual loading of the SoundFile should be the Engine's job
                context.FileProvider.Load(name)
            );

            return ExecutionResult.Void;
        }
    }
}
