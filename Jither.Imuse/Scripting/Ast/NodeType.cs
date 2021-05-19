namespace Jither.Imuse.Scripting.Ast
{
    public enum NodeType
    {
        Script,

        DefineDeclaration,
        VariableDeclaration,
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

        // Script suspension (multitasking)
        BreakHereStatement,

        // Pseudo statement used during flattening/"compilation"
        Label, 

        // Statements used for transforming flow control statements (and enqueue)
        ConditionalJumpStatement, 
        JumpStatement,
        EnqueueStartStatement,
        EnqueueEndStatement
    }
}
