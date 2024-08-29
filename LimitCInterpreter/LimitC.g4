grammar LimitC;

prog: gdecl* EOF;

gdecl: (funcDef | varDef | label);

funcDef:  type ID '(' paramListDef? ')' (codeBlock | ';');

paramListDef: paramDef (',' paramDef)*;
paramDef: (type ID?);

varDef: type varAssignDef (',' varAssignDef)* ';';

varAssignDef: ID ('=' expr)?;

funcCall: ID '(' paramList? ')';
paramList: param (',' param)*;
param: expr;

expr: constant                          # constantExpression
    | lvalue                            # lValExpression
    | funcCall                          # funcCallExpression
    | '(' expr ')'                      # parenthesesExpression
    | ADDOP expr                        # unaryPlusExpression
    | SUBOP expr                        # unaryNegationExpression
    | '(' type ')' expr                 # castExpression
    | expr (AST | DIVOP) expr           # mulDivExpression
    | expr (ADDOP | SUBOP) expr         # addSubExpression
    | assOp                             # additionalAssignmentExpression
    ;

lvalue: ID                                  # varExpression
    | INCR lvalue                           # preIncrementExpression
    | DECR lvalue                           # preDecrementExpression
    | '(' lvalue ')'                        # lparExpression
    | lvalue INCR                           # postIncrementExpression
    | lvalue DECR                           # postDecrementExpression
    | AMP lvalue                            # ampExpression
    | AST lvalue                            # astExpression
    ;

constant: INTEGER               # integerConstant
    | DOUBLE                    # doubleConstant
    | CHAR                      # charConstant
    | STR                       # stringConstant
    ;

codeBlock: '{' codeStateList* '}';

codeStateList: codeBlock | termExpr | label;

termExpr: varDef                # varDefExpression
    | expr ';'                  # looseExpression
    | RETSTAT expr ';'          # returnExpression
    ;

assOp: lvalue '=' expr          # varAssignment
    | lvalue '+=' expr          # addAssignment
    | lvalue '-=' expr          # subAssignment
    | lvalue '*=' expr          # multAssignment
    | lvalue '/=' expr          # divAssignment
    ;

type: typeLit AST*;

typeLit : 'void'
    |   'char'
    |   'short'
    |   'int'
    |   'long'
    |   'float'
    |   'double'
    ;

label: LABEL;

STR: '"' .*? '"';
CHAR: '\'' .*? '\'';
DOUBLE: NUM* '.' NUM+;
INTEGER: NUM+;

AST: '*';
AMP: '&';

INCR: '++';
DECR: '--';
DIVOP: '/';
ADDOP: '+';
SUBOP: '-';

RETSTAT: 'return';

ID:   LETTER (LETTER | NUM)*;

LETTER: [a-zA-Z_];

NUM: [0-9];

COMSTART: '/*';
COMEND: '*/'; 

WS: [ \t] -> skip;

LINEBREAK: ('\r' '\n'? | '\n') -> skip;

LABEL: COMSTART WS* LABELLIT WS* INTEGER WS* COMEND;
LABELLIT: [Ll] [Aa] [Bb] [Ee] [Ll];

MULTILINECOMMENT: COMSTART .*? COMEND -> skip;

COMMENT: '//' .*? LINEBREAK -> skip;

DIR: '#' .*? LINEBREAK -> skip;