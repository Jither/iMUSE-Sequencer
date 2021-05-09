using Jither.Imuse.Scripting.Ast;
using System;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Runtime
{
    public class SoundsDeclarationExecuter : DeclarationExecuter
    {
        private readonly List<SoundDeclaratorExecuter> sounds = new();

        public SoundsDeclarationExecuter(SoundsDeclaration sounds)
        {
            foreach (var sound in sounds.Sounds)
            {
                this.sounds.Add(new SoundDeclaratorExecuter(sound));
            }
        }

        protected override ExecutionResult Execute(ExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }
}
