//+------------------------------------------------------------------+
//| Fixture: Classes_Tutorial.mq5 (real-world MQL5 from mql5.com)    |
//| Article: https://www.mql5.com/es/articles/16765                  |
//| Author: CODE X (Daniel Jose)                                     |
//| Title: Del basico al intermedio: Clases (III)                    |
//| Constructs: class C_Regression, constructors with string param,  |
//|   destructors (~C_Regression()), new/delete operators, const     |
//|   methods, pass-by-reference (&channel), #define, StringFormat,  |
//|   ObjectCreate OBJ_REGRESSION, CopyTime, __FUNCTION__/__FILE__   |
//| License: MetaQuotes Ltd (educational use)                        |
//+------------------------------------------------------------------+
#property copyright "Daniel Jose"
//+------------------------------------------------------------------+
#define def_NameChannel    "Demo"
//+------------------------------------------------------------------+
//--- Inline class definition (originally in Include/Tutorial/File 03.mqh)
class C_Regression
{
    private :
//+----------------+
    public  :
//+----------------+
        C_Regression(const string msg)
        {
            datetime    dt0 = TimeCurrent(),
                        dt1[20];

            CopyTime(NULL, NULL, dt0, dt1.Size(), dt1);
            ObjectCreate(0, def_NameChannel, OBJ_REGRESSION, 0, dt1[0], 0, dt0, 0);
            ChartRedraw();
            PrintMsg(StringFormat("Running %s in %s in line %d", __FUNCTION__, __FILE__, __LINE__));
            PrintMsg("Message received: " + msg);
        }
//+----------------+
        ~C_Regression()
        {
            ObjectDelete(0, def_NameChannel);
            ChartRedraw();
            PrintMsg(StringFormat("Running %s in %s in line %d", __FUNCTION__, __FILE__, __LINE__));
        }
//+----------------+
        void PrintMsg(const string msg) const
        {
            Print(msg);
        }
//+----------------+
};
//+------------------------------------------------------------------+
//--- Script that exercises new/delete + pass-by-reference (originally
//--- Scripts/Code 04.mq5). Class definition is inlined above instead
//--- of being pulled via #include <Tutorial\File 03.mqh>.
//+------------------------------------------------------------------+
void OnStart(void)
{
    C_Regression *channel = new C_Regression(StringFormat("Init in line %d", __LINE__));
    
    (*channel).PrintMsg(StringFormat("Running %s in %s in line %d", __FUNCTION__, __FILE__, __LINE__));

    Checking(channel);

    Sleep(2000);

    delete channel;
}
//+------------------------------------------------------------------+
void Checking(const C_Regression &channel)
{
    channel.PrintMsg("Demonstrating the passage from a class to a procedure.");
}
//+------------------------------------------------------------------+