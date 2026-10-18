namespace CDataTranspiler;

public enum BuiltinType
{
    Void, Bool,
    Char, SignedChar, UnsignedChar,
    Short, UnsignedShort,
    Int, UnsignedInt,
    Long, UnsignedLong,
    LongLong, UnsignedLongLong,
    Float, Double, LongDouble,
}

public enum AggregateKind
{
    Struct, Union, Enum, Soa, Aos,
}

public abstract record AstNode(int FirstToken, int LastToken)
{
    // patched as the parser consumes more tokens
    public int LastToken { get; set; } = LastToken;
}

public sealed record TranslationUnit(int FirstToken, int LastToken, List<AstNode> Decls)
    : AstNode(FirstToken, LastToken);

public sealed record IncludeDecl(int FirstToken, int LastToken, int IncludeToken)
    : AstNode(FirstToken, LastToken);

public sealed record TypedefDecl(int FirstToken, int LastToken, TypeRef Type, int NameToken)
    : AstNode(FirstToken, LastToken);

// one record per aggregate flavor — pattern matching is cleaner this way
public sealed record StructDef(int FirstToken, int LastToken, int NameToken, List<FieldDecl> Fields)
    : AstNode(FirstToken, LastToken);
public sealed record UnionDef(int FirstToken, int LastToken, int NameToken, List<FieldDecl> Fields)
    : AstNode(FirstToken, LastToken);
public sealed record SoaDef(int FirstToken, int LastToken, int NameToken, List<FieldDecl> Fields)
    : AstNode(FirstToken, LastToken);
public sealed record AosDef(int FirstToken, int LastToken, int NameToken, List<FieldDecl> Fields)
    : AstNode(FirstToken, LastToken);

public sealed record StructFwd(int FirstToken, int LastToken, int NameToken) : AstNode(FirstToken, LastToken);
public sealed record UnionFwd(int FirstToken, int LastToken, int NameToken)  : AstNode(FirstToken, LastToken);
public sealed record SoaFwd(int FirstToken, int LastToken, int NameToken)    : AstNode(FirstToken, LastToken);
public sealed record AosFwd(int FirstToken, int LastToken, int NameToken)    : AstNode(FirstToken, LastToken);

public sealed record EnumDef(int FirstToken, int LastToken, int NameToken, List<EnumValue> Values)
    : AstNode(FirstToken, LastToken);
public sealed record EnumFwd(int FirstToken, int LastToken, int NameToken)
    : AstNode(FirstToken, LastToken);

public sealed record EnumValue(int FirstToken, int LastToken, int NameToken)
    : AstNode(FirstToken, LastToken);

public sealed record FunctionDecl(
    int FirstToken, int LastToken,
    TypeRef ReturnType, int NameToken, List<ParamDecl> Params)
    : AstNode(FirstToken, LastToken);

public sealed record FunctionDef(
    int FirstToken, int LastToken,
    TypeRef ReturnType, int NameToken, List<ParamDecl> Params, AstNode? Body)
    : AstNode(FirstToken, LastToken);

public sealed record VarDecl(
    int FirstToken, int LastToken,
    TypeRef Type, int NameToken,
    Expr? Init = null)
    : AstNode(FirstToken, LastToken);

public sealed record FieldDecl(int FirstToken, int LastToken, TypeRef Type, int NameToken)
    : AstNode(FirstToken, LastToken);

public sealed record ParamDecl(int FirstToken, int LastToken, TypeRef Type, int? NameToken)
    : AstNode(FirstToken, LastToken)
{
    public bool IsNamed => NameToken.HasValue;
}

// pointer or array modifier; stored flat inside TypeRef
public abstract record TypeModifier;
public sealed record PointerMod() : TypeModifier;
public sealed record ArrayMod(int SizeToken) : TypeModifier;

// three base flavors + modifier list; modifiers in source order: char** -> [P,P]
public sealed record TypeRef(int FirstToken, int LastToken) : AstNode(FirstToken, LastToken)
{
    public BuiltinType?    Builtin            { get; init; }
    public AggregateKind?  Aggregate          { get; init; }
    public int?            AggregateNameToken { get; init; }
    public int?            TypedefNameToken   { get; init; }
    public List<TypeModifier> Modifiers { get; } = new();

    public bool IsBuiltin     => Builtin.HasValue;
    public bool IsAggregate   => Aggregate.HasValue;
    public bool IsTypedefName => TypedefNameToken.HasValue;
}

// ---------------------------------------------------------------------------
// operators
// ---------------------------------------------------------------------------

public enum UnaryOp
{
    Negate,  // -x
    Plus,    // +x
    Not,     // !x
    BitNot,  // ~x
    Deref,   // *x
    AddrOf,  // &x
    PreInc,  // ++x
    PreDec,  // --x
    PostInc, // x++
    PostDec, // x--
}

public enum BinaryOp
{
    Add, Sub, Mul, Div, Mod,
    Eq, Neq, Lt, Gt, Leq, Geq,
    And, Or,
    BitAnd, BitOr, BitXor,
    LShift, RShift,
}

public enum AssignOp
{
    Assign,       // =
    AddAssign,    // +=
    SubAssign,    // -=
    MulAssign,    // *=
    DivAssign,    // /=
}

// ---------------------------------------------------------------------------
// expressions
// ---------------------------------------------------------------------------

public abstract record Expr(int FirstToken, int LastToken) : AstNode(FirstToken, LastToken);

public sealed record IntLiteralExpr   (int FirstToken, int LastToken, int LitToken) : Expr(FirstToken, LastToken);
public sealed record FloatLiteralExpr (int FirstToken, int LastToken, int LitToken) : Expr(FirstToken, LastToken);
public sealed record StringLiteralExpr(int FirstToken, int LastToken, int LitToken) : Expr(FirstToken, LastToken);
public sealed record CharLiteralExpr  (int FirstToken, int LastToken, int LitToken) : Expr(FirstToken, LastToken);

public sealed record BoolLiteralExpr(int FirstToken, int LastToken, bool Value)
    : Expr(FirstToken, LastToken);

public sealed record NullptrLiteralExpr(int FirstToken, int LastToken)
    : Expr(FirstToken, LastToken);

public sealed record IdentExpr(int FirstToken, int LastToken, int NameToken)
    : Expr(FirstToken, LastToken);

public sealed record UnaryExpr(int FirstToken, int LastToken, UnaryOp Op, Expr Operand)
    : Expr(FirstToken, LastToken);

public sealed record BinaryExpr(int FirstToken, int LastToken, BinaryOp Op, Expr Left, Expr Right)
    : Expr(FirstToken, LastToken);

public sealed record AssignExpr(int FirstToken, int LastToken, AssignOp Op, Expr Target, Expr Value)
    : Expr(FirstToken, LastToken);

public sealed record CallExpr(int FirstToken, int LastToken, Expr Callee, List<Expr> Args)
    : Expr(FirstToken, LastToken);

public sealed record IndexExpr(int FirstToken, int LastToken, Expr Target, Expr Index)
    : Expr(FirstToken, LastToken);

public sealed record MemberExpr(int FirstToken, int LastToken, Expr Left, int FieldToken, bool IsArrow)
    : Expr(FirstToken, LastToken);

public sealed record SizeofExpr(int FirstToken, int LastToken, Expr Operand)
    : Expr(FirstToken, LastToken);

// ---------------------------------------------------------------------------
// statements
// ---------------------------------------------------------------------------

public abstract record Stmt(int FirstToken, int LastToken) : AstNode(FirstToken, LastToken);

// block items are either decls or stmts — mixed since C99 allows
// declarations anywhere inside a block
public abstract record BlockItem(int FirstToken, int LastToken) : AstNode(FirstToken, LastToken);
public sealed record StmtItem(int FirstToken, int LastToken, Stmt Inner)     : BlockItem(FirstToken, LastToken);
public sealed record DeclItem(int FirstToken, int LastToken, AstNode Decl)   : BlockItem(FirstToken, LastToken);

public sealed record BlockStmt(int FirstToken, int LastToken, List<BlockItem> Items)
    : Stmt(FirstToken, LastToken);

public sealed record ExprStmt(int FirstToken, int LastToken, Expr Expression)
    : Stmt(FirstToken, LastToken);

public sealed record ReturnStmt(int FirstToken, int LastToken, Expr? Value)
    : Stmt(FirstToken, LastToken);

public sealed record BreakStmt   (int FirstToken, int LastToken) : Stmt(FirstToken, LastToken);
public sealed record ContinueStmt(int FirstToken, int LastToken) : Stmt(FirstToken, LastToken);

public sealed record IfStmt(
    int FirstToken, int LastToken,
    Expr Condition, Stmt Then, Stmt? Else)
    : Stmt(FirstToken, LastToken);

public sealed record WhileStmt(int FirstToken, int LastToken, Expr Condition, Stmt Body)
    : Stmt(FirstToken, LastToken);

public sealed record DoWhileStmt(int FirstToken, int LastToken, Stmt Body, Expr Condition)
    : Stmt(FirstToken, LastToken);

// init can be a VarDecl, ExprStmt, or null
public sealed record ForStmt(
    int FirstToken, int LastToken,
    AstNode? Init,
    Expr?    Condition,
    Expr?    Post,
    Stmt     Body)
    : Stmt(FirstToken, LastToken);

public sealed record CastExpr(int FirstToken, int LastToken, TypeRef TargetType, Expr Operand)
    : Expr(FirstToken, LastToken);

public sealed record SoaPtrAccess(int FirstToken, int LastToken, Expr Pointer, Expr Count)
    : Expr(FirstToken, LastToken);

public sealed record SizeofSoaExpr(int FirstToken, int LastToken, int SoaNameToken)
    : Expr(FirstToken, LastToken);
