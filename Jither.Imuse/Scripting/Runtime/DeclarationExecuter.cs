using Jither.Imuse.Scripting.Ast;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Scripting.Runtime
{
    public abstract class DeclarationExecuter : Executer
    {
        public static DeclarationExecuter Build(Declaration declaration)
        {
            return declaration switch
            {
                DefineDeclaration define => new DefineDeclarationExecuter(define),
                SoundsDeclaration sounds => new SoundsDeclarationExecuter(sounds),
                ActionDeclaration action => new ActionDeclarationExecuter(action),
                _ => ErrorHelper.ThrowUnknownNode<DeclarationExecuter>(declaration)
            };
        }
    }
}
