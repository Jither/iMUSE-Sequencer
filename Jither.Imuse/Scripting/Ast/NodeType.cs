namespace Jither.Imuse.Scripting.Ast
{
    public enum NodeType
    {
        Script,

        DefineDeclaration,
        SoundsDeclaration,
        ActionDeclaration,
        EventDeclaration,

        SoundDeclarator,
        KeyPressEventDeclarator,
        TimeEventDeclarator,
        StartEventDeclarator,

        BlockStatement,
        BreakStatement,
        ExpressionStatement,
        IfStatement,
        DoStatement,
        WhileStatement,
        ForStatement,
        CaseStatement,
        CaseDefinition,
        EnqueueStatement,
        AssignmentStatement,

        CallExpression,
        BinaryExpression,
        UnaryExpression,
        UpdateExpression,
        Identifier,
        Literal,
    }
}
