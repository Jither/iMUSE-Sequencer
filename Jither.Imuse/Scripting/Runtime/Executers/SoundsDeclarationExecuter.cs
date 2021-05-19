using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
using System;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class SoundsDeclarationExecuter : DeclarationExecuter
    {
        private readonly List<SoundDeclaratorExecuter> sounds = new();

        public SoundsDeclarationExecuter(SoundsDeclaration sounds) : base(sounds)
        {
            foreach (var sound in sounds.Sounds)
            {
                this.sounds.Add(new SoundDeclaratorExecuter(sound));
            }
        }

        public override RuntimeValue Execute(ExecutionContext context)
        {
            foreach (var sound in sounds)
            {
                sound.Execute(context);
            }

            return RuntimeValue.Void;
        }
    }
}
