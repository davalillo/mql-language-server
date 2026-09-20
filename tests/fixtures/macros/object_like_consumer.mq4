// Issue #38 fixture: object-like (parameterless) user macros used in
// expressions. The constants must expand at the invocation position so the
// parse tree resolves them instead of leaving unresolved identifiers.
#define MAX_LOTS 0.5
#define True true

double MaxLots = MAX_LOTS;

int OnStart()
{
   bool flag = True;
   if (flag && MaxLots > 0)
   {
      return 1;
   }

   return 0;
}
