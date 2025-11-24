grammar Mql4Grammar;

// ======================================================
// LEXER RULES
// ======================================================

// --- Channels ---
// Channel 0: DEFAULT (Parser lo ve - Includes, Properties, Código)
// Channel 1: PREPROCESSOR (Parser lo ignora - Defines, Ifdefs) -> Útil para LSP via TokenStream
// Channel 2: COMMENTS (Parser lo ignora - Highlighting)

// --- Comments ---
COMMENT_BLOCK : '/*' .*? '*/' -> channel(2);
COMMENT_LINE  : '//' ~[\r\n]* -> channel(2);

// --- Whitespace ---
WS            : [ \t\r\n\u000C]+ -> skip;

// --- Preprocessor Hybrid Strategy ---

// GRUPO A: Directivas Estructurales (Visibles para el Parser)
// Se mantienen aquí para que tus Visitors de C# sigan detectando includes/imports.
PRE_INCLUDE : '#include' ~[\r\n]*;
PRE_PROPERTY: '#property' ~[\r\n]*;
PRE_IMPORT  : '#import' ~[\r\n]*;

// GRUPO B: Directivas Lógicas (Ocultas al Parser -> Canal 1)
// Se ocultan para evitar que macros complejas rompan la sintaxis de funciones/clases.
PRE_DEFINE  : '#define' ~[\r\n]* -> channel(1);
PRE_IFDEF   : '#ifdef' ~[\r\n]* -> channel(1);
PRE_IFNDEF  : '#ifndef' ~[\r\n]* -> channel(1);
PRE_ELSE    : '#else' ~[\r\n]* -> channel(1);
PRE_ENDIF   : '#endif' ~[\r\n]* -> channel(1);
PRE_UNDEF   : '#undef' ~[\r\n]* -> channel(1);

// --- MQL4 Specific Literals ---
LITERAL_DATE  : 'D\'' ~[']* '\'';
LITERAL_COLOR : 'C\'' ~[']* '\'';

// --- Numbers ---
HEX           : '0' [xX] [0-9a-fA-F]+;
DOUBLE        : [0-9]+ '.' [0-9]* EXP?
              | '.' [0-9]+ EXP?
              | [0-9]+ EXP
              ;
INTEGER       : [0-9]+;

fragment EXP  : [Ee] [+-]? [0-9]+;

// --- Strings & Chars ---
STRING        : '"' (ESC | ~["\\])* '"';
CHAR          : '\'' (ESC | ~['\\]) '\'';
fragment ESC  : '\\' [abfnrtv\\'"0?];

// --- Keywords ---
K_INT       : 'int';
K_DOUBLE    : 'double';
K_STRING    : 'string';
K_BOOL      : 'bool';
K_VOID      : 'void';
K_DATETIME  : 'datetime';
K_COLOR     : 'color';
K_CHAR      : 'char';
K_UCHAR     : 'uchar';
K_SHORT     : 'short';
K_USHORT    : 'ushort';
K_UINT      : 'uint';
K_LONG      : 'long';
K_ULONG     : 'ulong';
K_FLOAT     : 'float';

K_STATIC    : 'static';
K_EXTERN    : 'extern';
K_INPUT     : 'input';
K_SINPUT    : 'sinput';
K_CONST     : 'const';
K_VIRTUAL   : 'virtual';
K_OVERRIDE  : 'override';

K_CLASS     : 'class';
K_STRUCT    : 'struct';
K_PUBLIC    : 'public';
K_PRIVATE   : 'private';
K_PROTECTED : 'protected';
K_TEMPLATE  : 'template';
K_TYPENAME  : 'typename';
K_OPERATOR  : 'operator';

K_ENUM      : 'enum';

K_NEW       : 'new';
K_DELETE    : 'delete';
K_SIZEOF    : 'sizeof';

K_IF        : 'if';
K_ELSE      : 'else';
K_WHILE     : 'while';
K_FOR       : 'for';
K_DO        : 'do';
K_SWITCH    : 'switch';
K_CASE      : 'case';
K_DEFAULT   : 'default';
K_BREAK     : 'break';
K_CONTINUE  : 'continue';
K_RETURN    : 'return';

K_TRUE      : 'true';
K_FALSE     : 'false';
K_NULL      : 'NULL';

// --- Operators ---
SCOPE       : '::';
ASSIGN      : '=';
ASSIGN_ADD  : '+=';
ASSIGN_SUB  : '-=';
ASSIGN_MUL  : '*=';
ASSIGN_DIV  : '/=';
ASSIGN_MOD  : '%=';
ASSIGN_AND  : '&=';
ASSIGN_OR   : '|=';
ASSIGN_XOR  : '^=';
ASSIGN_LSH  : '<<=';
ASSIGN_RSH  : '>>=';

ARROW       : '->';
INC         : '++';
DEC         : '--';
ADD         : '+';
SUB         : '-';
MUL         : '*';
DIV         : '/';
MOD         : '%';

EQ          : '==';
NEQ         : '!=';
LTE         : '<=';
GTE         : '>=';
LT          : '<';
GT          : '>';

LOG_AND     : '&&';
LOG_OR      : '||';
LOG_NOT     : '!';

BIT_AND     : '&';
BIT_OR      : '|';
BIT_XOR     : '^';
BIT_NOT     : '~';
SHIFT_L     : '<<';
SHIFT_R     : '>>';

QUESTION    : '?';
COLON       : ':';
SEMICOLON   : ';';
COMMA       : ',';
DOT         : '.';

LPAREN      : '(';
RPAREN      : ')';
LBRACE      : '{';
RBRACE      : '}';
LBRACKET    : '[';
RBRACKET    : ']';

// --- Identifiers ---
IDENTIFIER  : [a-zA-Z_] [a-zA-Z0-9_]*;


// ======================================================
// PARSER RULES
// ======================================================

compilationUnit
    : translationUnit* EOF
    ;

translationUnit
    : directive                     // Mantenemos directivas visibles (includes)
    | classDeclaration
    | structDeclaration
    | enumDeclaration
    | functionDeclaration
    | variableDeclarationStatement
    | semicolon
    ;

semicolon
    : SEMICOLON
    ;

// --- Preprocessor Directive Wrapper ---
directive
    : PRE_INCLUDE
    | PRE_PROPERTY
    | PRE_IMPORT
    ;

// --- Types ---
type
    : modifiers? baseType (LT type (COMMA type)* GT)? (BIT_AND)?
    ;

baseType
    : K_INT | K_DOUBLE | K_STRING | K_BOOL | K_VOID
    | K_DATETIME | K_COLOR
    | K_CHAR | K_UCHAR | K_SHORT | K_USHORT | K_UINT | K_LONG | K_ULONG | K_FLOAT
    | qualifiedName
    ;

modifiers
    : (K_CONST | K_STATIC | K_INPUT | K_SINPUT | K_EXTERN | K_VIRTUAL)+
    ;

qualifiedName
    : (SCOPE)? IDENTIFIER (SCOPE IDENTIFIER)*
    ;

// --- Declarations ---

variableDeclarationStatement
    : variableDeclaration SEMICOLON
    ;

variableDeclaration
    : modifiers? type variableDeclarator (COMMA variableDeclarator)*
    ;

variableDeclarator
    : IDENTIFIER arraySpecifier? (ASSIGN initializer)?
    ;

arraySpecifier
    : LBRACKET expression? RBRACKET (LBRACKET expression? RBRACKET)*
    ;

initializer
    : expression
    | arrayInitializer
    ;

arrayInitializer
    : LBRACE (initializer (COMMA initializer)*)? COMMA? RBRACE
    ;

// --- Functions ---
functionDeclaration
    : templateDefinition? modifiers? type IDENTIFIER LPAREN parameterList? RPAREN (block | SEMICOLON)
    ;

templateDefinition
    : K_TEMPLATE LT (K_TYPENAME | K_CLASS | type) IDENTIFIER GT
    ;

parameterList
    : parameter (COMMA parameter)*
    ;

parameter
    : modifiers? type BIT_AND? IDENTIFIER? arraySpecifier? (ASSIGN expression)?
    ;

// --- Classes & Structs ---
classDeclaration
    : K_CLASS IDENTIFIER (COLON accessModifier qualifiedName)? LBRACE classBody RBRACE SEMICOLON
    ;

structDeclaration
    : K_STRUCT IDENTIFIER LBRACE classBody RBRACE SEMICOLON
    ;

classBody
    : classMember*
    ;

classMember
    : accessModifier COLON
    | constructorDeclaration
    | destructorDeclaration
    | functionDeclaration
    | variableDeclarationStatement
    | directive   // Permitimos #include dentro de clases (raro pero posible)
    | semicolon
    ;

accessModifier
    : K_PUBLIC | K_PRIVATE | K_PROTECTED
    ;

constructorDeclaration
    : IDENTIFIER LPAREN parameterList? RPAREN (initializationList)? (block | SEMICOLON)
    ;

destructorDeclaration
    : BIT_NOT IDENTIFIER LPAREN RPAREN (block | SEMICOLON)
    ;

initializationList
    : COLON constructorInitializer (COMMA constructorInitializer)*
    ;

constructorInitializer
    : IDENTIFIER LPAREN expression? RPAREN
    ;

// --- Enums ---
enumDeclaration
    : K_ENUM IDENTIFIER LBRACE enumMember (COMMA enumMember)* RBRACE SEMICOLON
    ;

enumMember
    : IDENTIFIER (ASSIGN expression)?
    ;

// --- Statements ---
block
    : LBRACE statement* RBRACE
    ;

statement
    : block
    | variableDeclarationStatement
    | expressionStatement
    | ifStatement
    | whileStatement
    | doWhileStatement
    | forStatement
    | switchStatement
    | flowControlStatement
    | directive   // Permitimos #include dentro de funciones
    | semicolon
    ;

expressionStatement
    : expression? SEMICOLON
    ;

ifStatement
    : K_IF LPAREN expression RPAREN statement (K_ELSE statement)?
    ;

whileStatement
    : K_WHILE LPAREN expression RPAREN statement
    ;

doWhileStatement
    : K_DO statement K_WHILE LPAREN expression RPAREN SEMICOLON
    ;

forStatement
    : K_FOR LPAREN forInit expression? SEMICOLON expression? RPAREN statement
    ;

forInit
    : variableDeclaration SEMICOLON
    | expression? SEMICOLON
    ;

switchStatement
    : K_SWITCH LPAREN expression RPAREN LBRACE switchBlock* RBRACE
    ;

switchBlock
    : (K_CASE expression | K_DEFAULT) COLON statement*
    ;

flowControlStatement
    : K_BREAK SEMICOLON
    | K_CONTINUE SEMICOLON
    | K_RETURN expression? SEMICOLON
    ;

// --- Expressions ---
expression
    : primaryExpression                                     # atomExpr
    | expression (DOT | ARROW) IDENTIFIER                   # memberAccessExpr
    | expression LBRACKET expression RBRACKET               # arrayIndexExpr
    | expression LPAREN argumentList? RPAREN                # functionCallExpr
    | expression (INC | DEC)                                # postfixExpr
    | (INC | DEC) expression                                # prefixExpr
    | (ADD | SUB | BIT_NOT | LOG_NOT) expression            # unaryExpr
    | LPAREN type RPAREN expression                         # castExpr
    | K_SIZEOF LPAREN (type | expression) RPAREN            # sizeofExpr
    | K_NEW type (LPAREN argumentList? RPAREN)?             # newExpr
    | K_DELETE expression                                   # deleteExpr
    | expression (MUL | DIV | MOD) expression               # mulDivExpr
    | expression (ADD | SUB) expression                     # addSubExpr
    | expression (SHIFT_L | SHIFT_R) expression             # bitShiftExpr
    | expression (LT | LTE | GT | GTE) expression           # relationalExpr
    | expression (EQ | NEQ) expression                      # equalityExpr
    | expression BIT_AND expression                         # bitAndExpr
    | expression BIT_XOR expression                         # bitXorExpr
    | expression BIT_OR expression                          # bitOrExpr
    | expression LOG_AND expression                         # logAndExpr
    | expression LOG_OR expression                          # logOrExpr
    | <assoc=right> expression QUESTION expression COLON expression # ternaryExpr
    | <assoc=right> expression assignmentOp expression      # assignmentExpr
    ;

primaryExpression
    : literal
    | qualifiedName
    | LPAREN expression RPAREN
    ;

argumentList
    : expression (COMMA expression)*
    ;

assignmentOp
    : ASSIGN | ASSIGN_ADD | ASSIGN_SUB | ASSIGN_MUL | ASSIGN_DIV | ASSIGN_MOD
    | ASSIGN_AND | ASSIGN_OR | ASSIGN_XOR | ASSIGN_LSH | ASSIGN_RSH
    ;

literal
    : INTEGER
    | DOUBLE
    | HEX
    | STRING
    | CHAR
    | LITERAL_DATE
    | LITERAL_COLOR
    | K_TRUE
    | K_FALSE
    | K_NULL
    ;
