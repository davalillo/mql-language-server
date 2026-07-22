// MQL4 fixture: global constructor/destructor (outside class body)
// Covers Mql4Grammar constructs: globalConstructorDeclaration, globalDestructorDeclaration
// Syntax: Crypter::Crypter() {} and Crypter::~Crypter() {}
// These are function declarations where qualifiedName = Crypter::Crypter
// The visitor extracts them as functions with the full qualified name.

class Crypter {
public:
    Crypter();
    ~Crypter();
};

Crypter::Crypter()
{
}

Crypter::~Crypter()
{
}