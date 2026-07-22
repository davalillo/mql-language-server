//+------------------------------------------------------------------+
//| Fixture: ORB_Breakout.mq5 (real-world MQL5 from mql5.com article) |
//| Article: https://www.mql5.com/es/articles/19886                  |
//| Author: Israel Pelumi Abioye (ALGOYIN LTD)                       |
//| Constructs: input params, arrays, CopyOpen/Close/Low/High/Time,  |
//|   ObjectCreate, CTrade.Buy/Sell, for loops, if conditions, Bars()|
//| License: MetaQuotes Ltd (educational use)                        |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//|                                               Project 15 0RB.mq5 |
//|                                             Abioye Israel Pelumi |
//|                             https://linktr.ee/abioyeisraelpelumi |
//+------------------------------------------------------------------+
#property copyright "Abioye Israel Pelumi"
#property link      "https://linktr.ee/abioyeisraelpelumi"
#property version   "1.00"


#include <Trade/Trade.mqh>
CTrade trade;
int MagicNumber = 533930;  // Unique Number
input double RRR= 2; // RRR
input double lot_size = 0.2;
input int pos_num = 2; // Number of Positions to Open 



string open_time_string = "9:30";
datetime open_time;

ulong chart_id = ChartID();

string open_time_bar_close_string = "9:45";
datetime open_time_bar_close;


double m15_high[];
double m15_low[];
double m15_close[];
double m15_open[];

double m5_high[];
double m5_low[];
double m5_close[];
double m5_open[];

double ask_price;
double take_profit;
datetime lastTradeBarTime = 0;


int open_to_current;
bool isBreakout = false;

string close_time_string = "15:00";
datetime close_time;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---

   ArraySetAsSeries(m5_high,true);
   ArraySetAsSeries(m5_low,true);
   ArraySetAsSeries(m5_close,true);
   ArraySetAsSeries(m5_open,true);

   trade.SetExpertMagicNumber(MagicNumber);


//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---

   ObjectsDeleteAll(chart_id);


  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---

   open_time = StringToTime(open_time_string);
   close_time = StringToTime(close_time_string);



   ObjectCreate(chart_id,"OPEN TIME",OBJ_VLINE,0,open_time,0);
   ObjectSetInteger(chart_id,"OPEN TIME",OBJPROP_COLOR,clrBlue);
   ObjectSetInteger(chart_id,"OPEN TIME",OBJPROP_STYLE,STYLE_DASH);
   ObjectSetInteger(chart_id,"OPEN TIME",OBJPROP_WIDTH,2);
   
   ObjectCreate(chart_id,"CLOSE TIME",OBJ_VLINE,0,close_time,0);
   ObjectSetInteger(chart_id,"CLOSE TIME",OBJPROP_COLOR,clrRed);
   ObjectSetInteger(chart_id,"CLOSE TIME",OBJPROP_STYLE,STYLE_DASH);
   ObjectSetInteger(chart_id,"CLOSE TIME",OBJPROP_WIDTH,2);


   open_time_bar_close = StringToTime(open_time_bar_close_string);

   if(TimeCurrent() >= open_time_bar_close)
     {

      CopyHigh(_Symbol,PERIOD_M15,open_time,1,m15_high);
      CopyLow(_Symbol,PERIOD_M15,open_time,1,m15_low);
      CopyClose(_Symbol,PERIOD_M15,open_time,1,m15_close);
      CopyOpen(_Symbol,PERIOD_M15,open_time,1,m15_open);

      ObjectCreate(chart_id,"High",OBJ_TREND,0,open_time,m15_high[0],TimeCurrent(),m15_high[0]);
      ObjectSetInteger(chart_id,"High",OBJPROP_COLOR,clrBlue);
      ObjectSetInteger(chart_id,"High",OBJPROP_WIDTH,2);

      ObjectCreate(chart_id,"Low",OBJ_TREND,0,open_time,m15_low[0],TimeCurrent(),m15_low[0]);
      ObjectSetInteger(chart_id,"Low",OBJPROP_COLOR,clrBlue);
      ObjectSetInteger(chart_id,"Low",OBJPROP_WIDTH,2);

      open_to_current = Bars(_Symbol,PERIOD_M5,open_time_bar_close,TimeCurrent());

      CopyHigh(_Symbol,PERIOD_M5,1,open_to_current,m5_high);
      CopyLow(_Symbol,PERIOD_M5,1,open_to_current,m5_low);
      CopyClose(_Symbol,PERIOD_M5,1,open_to_current,m5_close);
      CopyOpen(_Symbol,PERIOD_M5,1,open_to_current,m5_open);




     }




   datetime currentBarTime = iTime(_Symbol, PERIOD_M5, 0);
   ask_price = SymbolInfoDouble(_Symbol,SYMBOL_ASK);


   if(TimeCurrent() >= open_time_bar_close && TimeCurrent() <= close_time && m5_close[0] > m15_high[0] && m5_close[1] < m15_high[0] && m5_close[0] > m5_open[0] && currentBarTime != lastTradeBarTime && isBreakout == false)
     {

      //BUY
      take_profit = MathAbs(ask_price + ((ask_price - m15_low[0]) * RRR));
      
      for(int i = 0; i < pos_num; i++)  // open 3 trades
        {

         trade.Buy(lot_size,_Symbol,ask_price,m15_low[0],take_profit);

        }
      lastTradeBarTime = currentBarTime;

     }

   if(TimeCurrent() >= open_time_bar_close && TimeCurrent() <= close_time && m5_close[0] < m15_low[0] && m5_close[1] > m15_low[0] && m5_close[0] < m5_open[0] && currentBarTime != lastTradeBarTime && isBreakout == false)
     {

      //SELL

      take_profit = MathAbs(ask_price - ((m15_high[0] - ask_price) * RRR));
      
      for(int i = 0; i < pos_num; i++)  // open 3 trades
        {
         trade.Sell(lot_size,_Symbol,ask_price,m15_high[0],take_profit);

        }


      lastTradeBarTime = currentBarTime;

     }

   if(TimeCurrent() >= open_time_bar_close)
     {

      for(int i = 0; i < open_to_current; i++)
        {
         if(i + 1  < open_to_current)
           {
            if((m5_close[i] > m15_high[0] && m5_close[i + 1] < m15_high[0]) || (m5_close[i] < m15_low[0] && m5_close[i + 1] > m15_low[0]))
              {

               isBreakout = true;

               break;
              }

           }

        }


     }


   if(TimeCurrent() < open_time)
     {

      isBreakout = false;


     }



  // Comment(isBreakout);
  
  for(int i = 0; i < PositionsTotal(); i++)
     {
      ulong ticket = PositionGetTicket(i);

      if(PositionGetInteger(POSITION_MAGIC) == MagicNumber  && PositionGetString(POSITION_SYMBOL) == ChartSymbol(chart_id))
        {
         

         if(TimeCurrent() >= close_time)
           {
            // Close the position
            trade.PositionClose(ticket);

           }

 
        }
     }



  }