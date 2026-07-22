//+------------------------------------------------------------------+
//| Fixture: Fractal_Breakout_EMA.mq5 (real-world MQL5 from mql5.com)|
//| Article: https://www.mql5.com/es/articles/18297                  |
//| Author: Christian Benjamin                                       |
//| Constructs: iFractals, iMA, CopyBuffer, ArraySetAsSeries,        |
//|   ArrayResize, EMPTY_VALUE, OBJ_HLINE, OBJ_ARROW, OBJ_TEXT,      |
//|   StringFormat, Alert, PlaySound, input ENUM_TIMEFRAMES          |
//| License: MetaQuotes Ltd (educational use)                        |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//|                              Fractal Breakout, EMA 14 and EMA 200|
//|                                   Copyright 2025, MetaQuotes Ltd.|
//|                           https://www.mql5.com/en/users/lynnchris|
//+------------------------------------------------------------------+
#property copyright "Copyright 2025, MetaQuotes Ltd."
#property link      "https://www.mql5.com/en/users/lynnchris"
#property version   "1.0"
#property strict

//---inputs
input ENUM_TIMEFRAMES InpTimeframe      = PERIOD_CURRENT;  // chart TF
input int             InpHistoryBars    = 200;             // bars back to scan
input int             InpEMA14Period    = 14;              // fast EMA
input int             InpEMA200Period   = 200;             // slow EMA
input color           InpBullColor      = clrLime;         // bullish arrow color
input color           InpBearColor      = clrRed;          // bearish arrow color
input string          InpBullText       = "BULL Break";    // bullish label
input string          InpBearText       = "BEAR Break";    // bearish label
input int             InpArrowOffset    = 20;              // offset in points for label
input int             InpArrowFontSize  = 12;              // size of the arrow glyph
input bool            InpAlertsEnabled  = true;            // show pop-up alerts
input string          InpAlertSoundFile = "alert.wav";     // sound file in /Sounds/

//---indicator handles & buffers
int  hFractals, hEMA14, hEMA200;
double fractalUp[], fractalDown[];

//+------------------------------------------------------------------+
//| Expert initialization                                           |
//+------------------------------------------------------------------+
int OnInit()
  {
   hFractals = iFractals(_Symbol, InpTimeframe);
   hEMA14    = iMA(_Symbol, InpTimeframe, InpEMA14Period, 0, MODE_EMA, PRICE_CLOSE);
   hEMA200   = iMA(_Symbol, InpTimeframe, InpEMA200Period,0, MODE_EMA, PRICE_CLOSE);
   if(hFractals==INVALID_HANDLE || hEMA14==INVALID_HANDLE || hEMA200==INVALID_HANDLE)
      return(INIT_FAILED);

   ArraySetAsSeries(fractalUp,   true);
   ArraySetAsSeries(fractalDown, true);
   ArrayResize(fractalUp,   InpHistoryBars);
   ArrayResize(fractalDown, InpHistoryBars);
   return(INIT_SUCCEEDED);
  }

//+------------------------------------------------------------------+
//| Expert deinitialization                                         |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   if(hFractals!=INVALID_HANDLE)
      IndicatorRelease(hFractals);
   if(hEMA14   !=INVALID_HANDLE)
      IndicatorRelease(hEMA14);
   if(hEMA200  !=INVALID_HANDLE)
      IndicatorRelease(hEMA200);

   for(int i=ObjectsTotal(0)-1; i>=0; i--)
     {
      string n = ObjectName(0,i);
      if(StringFind(n,"FB_")>=0)
         ObjectDelete(0,n);
     }
  }

//+------------------------------------------------------------------+
//| Tick handler                                                    |
//+------------------------------------------------------------------+
void OnTick()
  {
// 1) Read current EMAs
   double ema14Arr[1], ema200Arr[1];
   if(CopyBuffer(hEMA14,0,0,1,ema14Arr)<=0 || CopyBuffer(hEMA200,0,0,1,ema200Arr)<=0)
      return;
   double ema14_now  = ema14Arr[0];
   double ema200_now = ema200Arr[0];

// 2) Read fractal history (skip current bar)
   if(CopyBuffer(hFractals,0,1,InpHistoryBars,fractalUp)<=0 ||
      CopyBuffer(hFractals,1,1,InpHistoryBars,fractalDown)<=0)
      return;

// 3) Find most recent valid fractals (ignore EMPTY_VALUE)
   int upShift=-1, downShift=-1;
   for(int i=1; i<InpHistoryBars; i++)
     {
      if(fractalUp[i]   != EMPTY_VALUE && upShift<0)
         upShift   = i+1;
      if(fractalDown[i] != EMPTY_VALUE && downShift<0)
         downShift = i+1;
      if(upShift>0 && downShift>0)
         break;
     }

// 4) Levels
   double lvlUp   = (upShift>0)   ? fractalUp[upShift-1]     : 0.0;
   double lvlDown = (downShift>0) ? fractalDown[downShift-1] : 0.0;

// 5) Draw level lines
   DrawHLine("FB_LevelUp",   lvlUp,   InpBullColor);
   DrawHLine("FB_LevelDown", lvlDown, InpBearColor);

// 6) DEBUG print
   double prevC = iClose(_Symbol,InpTimeframe,1);
   double currC = iClose(_Symbol,InpTimeframe,0);
   PrintFormat(
      "DBG lvlUp=%.5f lvlDown=%.5f prevC=%.5f currC=%.5f EMA14=%.5f EMA200=%.5f",
      lvlUp, lvlDown, prevC, currC, ema14_now, ema200_now
   );

// 7) Breakouts on the last closed candle (shift=1)

// Bearish breakout
   if(lvlDown>0
      && prevC>=lvlDown && currC<lvlDown
      && currC<ema14_now && currC<ema200_now
      && ema200_now>ema14_now)
     {
      Print(">>> Bear breakout triggered");
      Signal(false, 1, lvlDown, ema14_now, ema200_now);
     }
// Bullish breakout
   if(lvlUp>0
      && prevC<=lvlUp && currC>lvlUp
      && currC>ema14_now && currC>ema200_now
      && ema14_now>ema200_now)
     {
      Print(">>> Bull breakout triggered");
      Signal(true, 1, lvlUp, ema14_now, ema200_now);
     }
  }

//+------------------------------------------------------------------+
//| Draw or update a dotted HLine                                   |
//+------------------------------------------------------------------+
void DrawHLine(string name, double price, color clr)
  {
   if(price<=0)
      return;
   if(ObjectFind(0,name)<0)
     {
      ObjectCreate(0,name,OBJ_HLINE,0,0,price);
      ObjectSetInteger(0,name,OBJPROP_COLOR,clr);
      ObjectSetInteger(0,name,OBJPROP_STYLE,STYLE_DOT);
      ObjectSetInteger(0,name,OBJPROP_WIDTH,1);
     }
   else
      ObjectSetDouble(0,name,OBJPROP_PRICE,price);
  }

//+------------------------------------------------------------------+
//| Plot arrow, label, log & alert                                   |
//+------------------------------------------------------------------+
void Signal(bool isBull, int shift, double level, double ema14, double ema200)
  {
   datetime t     = iTime(_Symbol,InpTimeframe,shift);
   double   price = level;
   string   side  = isBull ? "Bull" : "Bear";

// Arrow
   string arrowName = StringFormat("FB_%sArrow_%d", side, (int)t);
   ObjectCreate(0, arrowName, OBJ_ARROW, 0, t, price);
   ObjectSetInteger(0, arrowName, OBJPROP_ARROWCODE,    isBull?233:234);
   ObjectSetInteger(0, arrowName, OBJPROP_COLOR,        isBull?InpBullColor:InpBearColor);
   ObjectSetInteger(0, arrowName, OBJPROP_WIDTH,        2);
   ObjectSetInteger(0, arrowName, OBJPROP_FONTSIZE,     InpArrowFontSize);

// Label
   string lab = arrowName + "_L";
   double offset = (isBull ? InpArrowOffset : -InpArrowOffset) * _Point;
   ObjectCreate(0, lab, OBJ_TEXT, 0, t, price + offset);
   ObjectSetString(0, lab, OBJPROP_TEXT,    isBull?InpBullText:InpBearText);
   ObjectSetInteger(0, lab, OBJPROP_COLOR,  isBull?InpBullColor:InpBearColor);
   ObjectSetInteger(0, lab, OBJPROP_FONTSIZE, 11);

// Expert log
   string msg = StringFormat(
                   "%s breakout at %s | Level=%.5f | Close=%.5f | EMA14=%.5f | EMA200=%.5f",
                   side, TimeToString(t, TIME_DATE|TIME_SECONDS),
                   level, iClose(_Symbol,InpTimeframe,shift),
                   ema14, ema200
                );
   Print(msg);

// Pop-up alert + optional sound
   if(InpAlertsEnabled)
     {
      Alert(msg);
      if(StringLen(InpAlertSoundFile)>0)
         PlaySound(InpAlertSoundFile);
     }
  }
//+------------------------------------------------------------------+
