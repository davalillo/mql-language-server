// MQL4 fixture: constructor/destructor + initialization list
// Covers Mql4Grammar constructs: constructorDeclaration, destructorDeclaration,
//   initializationList, constructorInitializer
class Point {
private:
    int _x;
    int _y;
public:
    Point() : _x(0), _y(0) { }
    Point(int x, int y) : _x(x), _y(y) { }
    ~Point(void);
    ~Point() { }
};

Point::~Point(void) { }

void OnTick()
{
    Point p;
}