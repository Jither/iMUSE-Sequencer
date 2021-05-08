namespace Jither.Imuse.Scripting.Ast
{
    public enum NodeType
    {
        Script,

        DefineDeclaration,
        SoundsDeclaration,
        TriggerDeclaration,

        SoundDeclarator,

        IfStatement,
        ForStatement,
        CallStatement,
        EnqueueStatement,
        AssignmentStatement,

        FunctionCallExpression,
        BinaryExpression,
        UnaryExpression,
        UpdateExpression,
        Identifier,
        Literal,
    }
}
