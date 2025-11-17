grammar Mql4Grammar;

// === Comments ===
COMMENT_BLOCK : '/*' .*? '*/' -> skip;
COMMENT_LINE  : '//' ~[\r\n]* -> skip;
WS            : [ \t\r\n]+ -> skip;

// === Numbers ===
INTEGER : [0-9]+;
DOUBLE  : [0-9]+ '.' [0-9]+;
HEX     : '0' [xX] [0-9a-fA-F]+;

// === Strings ===
STRING : '"' (ESC | ~["\\])* '"';
CHAR   : '\'' (ESC | ~['\\]) '\'';

fragment ESC : '\\' [abfnrtv\\'"0];

// === Identifiers and Keywords ===
IDENTIFIER : [a-zA-Z_] [a-zA-Z0-9_]*;

// Keywords
K_INT      : 'int';
K_DOUBLE   : 'double';
K_STRING   : 'string';
K_BOOL     : 'bool';
K_VOID     : 'void';
K_DATETIME : 'datetime';
K_COLOR    : 'color';
K_STATIC   : 'static';
K_EXTERN   : 'extern';
K_IF       : 'if';
K_ELSE     : 'else';
K_WHILE    : 'while';
K_FOR      : 'for';
K_DO       : 'do';
K_SWITCH   : 'switch';
K_CASE     : 'case';
K_DEFAULT  : 'default';
K_BREAK    : 'break';
K_CONTINUE : 'continue';
K_RETURN   : 'return';
K_TRUE     : 'true';
K_FALSE    : 'false';

// Directives
DIRECTIVE_INCLUDE  : '#include';
DIRECTIVE_PROPERTY : '#property';

// Operators
ASSIGN : '=';
ADD    : '+';
SUB    : '-';
MUL    : '*';
DIV    : '/';
MOD    : '%';
INC    : '++';
DEC    : '--';
EQ     : '==';
NEQ    : '!=';
LT     : '<';
LTE    : '<=';
GT     : '>';
GTE    : '>=';
K_AND  : '&&';
K_OR   : '||';
K_NOT  : '!';

// Punctuation
LPAREN  : '(';
RPAREN  : ')';
LBRACE  : '{';
RBRACE  : '}';
LBRACKET: '[';
RBRACKET: ']';
SEMICOLON: ';';
COMMA   : ',';
DOT     : '.';
COLON   : ':';
QUESTION: '?';

// === Parser Rules ===
compilationUnit
    : directive* globalDeclaration* EOF
    ;

directive
    : includeDirective
    | propertyDirective
    ;

includeDirective
    : DIRECTIVE_INCLUDE STRING
    ;

propertyDirective
    : DIRECTIVE_PROPERTY IDENTIFIER (STRING | INTEGER)?
    ;

globalDeclaration
    : functionDeclaration
    | variableDeclaration
    ;

functionDeclaration
    : dataType IDENTIFIER LPAREN parameterList? RPAREN block
    ;

parameterList
    : parameter (COMMA parameter)*
    ;

parameter
    : dataType IDENTIFIER?
    ;

variableDeclaration
    : dataType IDENTIFIER (ASSIGN expression)? SEMICOLON
    ;

dataType
    : K_INT | K_DOUBLE | K_STRING | K_BOOL | K_VOID | K_DATETIME | K_COLOR | IDENTIFIER
    ;

block
    : LBRACE statement* RBRACE
    ;

statement
    : block
    | variableDeclaration
    | expressionStatement
    | ifStatement
    | whileStatement
    | forStatement
    | returnStatement
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

forStatement
    : K_FOR LPAREN expression? SEMICOLON expression? SEMICOLON expression? RPAREN statement
    ;

returnStatement
    : K_RETURN expression? SEMICOLON
    ;

expression
    : logicalOrExpression
    ;

logicalOrExpression
    : logicalAndExpression (K_OR logicalAndExpression)*
    ;

logicalAndExpression
    : equalityExpression (K_AND equalityExpression)*
    ;

equalityExpression
    : relationalExpression (EQ relationalExpression | NEQ relationalExpression)*
    ;

relationalExpression
    : additiveExpression (LT additiveExpression | LTE additiveExpression | GT additiveExpression | GTE additiveExpression)*
    ;

additiveExpression
    : multiplicativeExpression (ADD multiplicativeExpression | SUB multiplicativeExpression)*
    ;

multiplicativeExpression
    : unaryExpression (MUL unaryExpression | DIV unaryExpression | MOD unaryExpression)*
    ;

unaryExpression
    : K_NOT unaryExpression
    | INC unaryExpression
    | DEC unaryExpression
    | postfixExpression
    ;

postfixExpression
    : primaryExpression
    | postfixExpression LBRACKET expression RBRACKET
    | postfixExpression LPAREN argumentList? RPAREN
    | postfixExpression DOT IDENTIFIER
    | postfixExpression INC
    | postfixExpression DEC
    ;

argumentList
    : expression (COMMA expression)*
    ;

primaryExpression
    : IDENTIFIER
    | literal
    | LPAREN expression RPAREN
    ;

literal
    : INTEGER | DOUBLE | HEX | STRING | CHAR | K_TRUE | K_FALSE
    ;
