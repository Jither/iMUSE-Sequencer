namespace Jither.Imuse.Scripting.Ast
{
    public enum NodeType
    {
        Script,

        DefineDeclaration,
        SoundsDeclaration,
        ActionDeclaration,

        SoundDeclarator,

        BlockStatement,
        BreakStatement,
        IfStatement,
        DoStatement,
        WhileStatement,
        ForStatement,
        CaseStatement,
        CaseDefinition,
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
