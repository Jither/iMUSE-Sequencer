using Jither.Imuse.Files;
using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
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

        public override RuntimeValue Execute(ExecutionContext context)
        {
            int soundId = id.Execute(context).AsInteger(id);
            string name = this.name.Execute(context).AsString(this.name);
            context.Engine.RegisterSound(
                soundId,
                context.FileProvider.Load(name)
            );

            return RuntimeValue.Void;
        }
    }
}
