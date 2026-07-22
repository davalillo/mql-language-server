//+------------------------------------------------------------------+
//| Fixture: SupportResistance_Zones.mq5 (reconstructed from article)|
//| Article: https://www.mql5.com/es/articles/20021                  |
//| Author: Israel Pelumi Abioye (ALGOYIN LTD)                       |
//| Title: Introduccion a MQL5 (Parte 26): EA basado en zonas de     |
//|        soporte y resistencia                                     |
//| Constructs: input ENUM_TIMEFRAMES, CopyOpen/Close/Low/High/Time, |
//|   swing low/high detection (IsSwingLow/IsSwingHigh), second      |
//|   swing confirmation, breakout validation (ArrayMinimum/Maximum),|
//|   CHOCH detection (deep nested for loops, 6+ levels), ObjectCreate|
//|   OBJ_RECTANGLE/OBJ_TEXT/OBJ_TREND, Bars(), MathMin/MathMax,     |
//|   #include <Trade/Trade.mqh>, CTrade.Buy/Sell, multi-timeframe   |
//| License: MetaQuotes Ltd (educational use)                        |
//+------------------------------------------------------------------+
#property copyright "Israel Pelumi Abioye"
#property link      "https://www.mql5.com/es/users/13467913"
#property version   "1.00"

#include <Trade/Trade.mqh>
CTrade trade;

input ENUM_TIMEFRAMES timeframe = PERIOD_W1;      // SUPPORT AND RESISTANCE TIMEFRAME
input ENUM_TIMEFRAMES exe_timeframe = PERIOD_M30; // EXECUTION TIMEFRAME
input double lot_size = 0.2;                      // Lot Size
input double RRR = 3;                             // RRR

double open[];
double close[];
double low[];
double high[];
datetime time[];

double exe_open[];
double exe_close[];
double exe_low[];
double exe_high[];
datetime exe_time[];

int bars_check = 200;
int exe_total_symbol_bars;
int total_symbol_bars;
int z = 7;

double first_sup_price;
double first_sup_min_body_price;
datetime first_sup_time;

double second_sup_price;
datetime second_sup_time;

string support_object;
ulong chart_id = ChartID();

int sup_bars;
int sup_min_low_index;
double sup_min_low_price;

string first_low_txt;
string second_low_txt;

double lower_high;
datetime lower_high_time;
double lower_low;
datetime lower_low_time;

double low_l;
datetime low_time;
double high_h;
datetime high_time;

datetime start_choch_time;

double first_res_price;
double first_res_max_body_price;
datetime first_res_time;

double second_res_price;
datetime second_res_time;

string resistance_object;
int res_bars;
int res_max_high_index;
double res_max_high_price;

string first_high_txt;
string second_high_txt;

double higher_high;
datetime higher_high_time;
double higher_low;
datetime higher_low_time;

double ask_price;
double take_profit;
datetime lastTradeBarTime = 0;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   ArraySetAsSeries(exe_close,true);
   ArraySetAsSeries(exe_open,true);
   ArraySetAsSeries(exe_high,true);
   ArraySetAsSeries(exe_low,true);
   ArraySetAsSeries(exe_time,true);
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   ObjectsDeleteAll(chart_id);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   CopyOpen(_Symbol, timeframe, TimeCurrent(), bars_check, open);
   CopyClose(_Symbol, timeframe, TimeCurrent(), bars_check, close);
   CopyLow(_Symbol, timeframe, TimeCurrent(), bars_check, low);
   CopyHigh(_Symbol, timeframe, TimeCurrent(), bars_check, high);
   CopyTime(_Symbol, timeframe, TimeCurrent(), bars_check, time);

   CopyOpen(_Symbol, exe_timeframe, TimeCurrent(), bars_check, exe_open);
   CopyClose(_Symbol, exe_timeframe, TimeCurrent(), bars_check, exe_close);
   CopyLow(_Symbol, exe_timeframe, TimeCurrent(), bars_check, exe_low);
   CopyHigh(_Symbol, exe_timeframe, TimeCurrent(), bars_check, exe_high);
   CopyTime(_Symbol, exe_timeframe, TimeCurrent(), bars_check, exe_time);

   total_symbol_bars = Bars(_Symbol, timeframe);
   exe_total_symbol_bars = Bars(_Symbol, exe_timeframe);

   datetime currentBarTime = iTime(_Symbol, exe_timeframe, 0);
   ask_price = SymbolInfoDouble(_Symbol,SYMBOL_ASK);

   //--- SUPPORT ZONE DETECTION + BULLISH CHOCH + BUY EXECUTION
   if(total_symbol_bars >= bars_check)
     {
      for(int i = z ; i < bars_check - z; i++)
        {
         if(IsSwingLow(low, i, z))
           {
            first_sup_price = low[i];
            first_sup_min_body_price = MathMin(close[i], open[i]);
            first_sup_time = time[i];

            for(int j = i+1; j < bars_check - z; j++)
              {
               if(IsSwingLow(low, j, z) && low[j] <= first_sup_min_body_price && low[j] >= first_sup_price)
                 {
                  second_sup_price = low[j];
                  second_sup_time = time[j];
                  start_choch_time = time[j+z];

                  sup_bars = Bars(_Symbol,timeframe,first_sup_time,TimeCurrent());
                  sup_min_low_index = ArrayMinimum(low,i,sup_bars);
                  sup_min_low_price = low[sup_min_low_index];

                  if(sup_min_low_price >= first_sup_price)
                    {
                     support_object = StringFormat("SUPPORT %f",first_sup_price);
                     ObjectCreate(chart_id,support_object,OBJ_RECTANGLE,0,first_sup_time,first_sup_price,TimeCurrent(),first_sup_min_body_price);
                     ObjectSetInteger(chart_id,support_object,OBJPROP_COLOR,clrBlue);
                     ObjectSetInteger(chart_id,support_object,OBJPROP_BACK,true);
                     ObjectSetInteger(chart_id,support_object,OBJPROP_FILL,true);

                     first_low_txt = StringFormat("FIRST LOW%d",i);
                     ObjectCreate(chart_id,first_low_txt,OBJ_TEXT,0,first_sup_time,first_sup_price);
                     ObjectSetString(chart_id,first_low_txt,OBJPROP_TEXT,"1");

                     second_low_txt = StringFormat("SECOND LOW%d",i);
                     ObjectCreate(chart_id,second_low_txt,OBJ_TEXT,0,second_sup_time,second_sup_price);
                     ObjectSetString(chart_id,second_low_txt,OBJPROP_TEXT,"2");

                     if(exe_total_symbol_bars >= bars_check)
                       {
                        for(int k = 4; k < bars_check-3; k++)
                          {
                           if(IsSwingLow(exe_low, k, 3))
                             {
                              lower_low = exe_low[k];
                              lower_low_time = exe_time[k];

                              for(int l = k; l < bars_check-3; l++)
                                {
                                 if(IsSwingHigh(exe_high,l,3))
                                   {
                                    lower_high = exe_high[l];
                                    lower_high_time = exe_time[l];

                                    for(int m = l; m < bars_check-3; m++)
                                      {
                                       if(IsSwingLow(exe_low, m, 3))
                                         {
                                          low_l = exe_low[m];
                                          low_time = exe_time[m];

                                          for(int n = m; n < bars_check-3; n++)
                                            {
                                             if(IsSwingHigh(exe_high,n,3))
                                               {
                                                high_h = exe_high[n];
                                                high_time = exe_time[n];

                                                for(int o = k; o > 0; o--)
                                                  {
                                                   if(exe_close[o] > lower_high && exe_open[o] < lower_high)
                                                     {
                                                      if(lower_high > lower_low && low_l < lower_high && low_l > lower_low && high_h > low_l && high_h > lower_high && lower_low <= first_sup_min_body_price && lower_low >= first_sup_price
                                                         && high_time > start_choch_time)
                                                        {
                                                         ObjectCreate(chart_id,"LLLH",OBJ_TREND,0,lower_low_time,lower_low,lower_high_time,lower_high);
                                                         ObjectSetInteger(chart_id,"LLLH",OBJPROP_COLOR,clrRed);
                                                         ObjectSetInteger(chart_id,"LLLH",OBJPROP_WIDTH,2);

                                                         ObjectCreate(chart_id,"LHL",OBJ_TREND,0,lower_high_time,lower_high,low_time,low_l);
                                                         ObjectSetInteger(chart_id,"LHL",OBJPROP_COLOR,clrRed);
                                                         ObjectSetInteger(chart_id,"LHL",OBJPROP_WIDTH,2);

                                                         ObjectCreate(chart_id,"LH",OBJ_TREND,0,low_time,low_l,high_time,high_h);
                                                         ObjectSetInteger(chart_id,"LH",OBJPROP_COLOR,clrRed);
                                                         ObjectSetInteger(chart_id,"LH",OBJPROP_WIDTH,2);

                                                         ObjectCreate(chart_id,"S Cross Line",OBJ_TREND,0,lower_high_time,lower_high,exe_time[o],lower_high);
                                                         ObjectSetInteger(chart_id,"S Cross Line",OBJPROP_COLOR,clrRed);
                                                         ObjectSetInteger(chart_id,"S Cross Line",OBJPROP_WIDTH,2);

                                                         if(exe_time[1] == exe_time[o] && currentBarTime != lastTradeBarTime)
                                                           {
                                                            take_profit = MathAbs(ask_price + ((ask_price - lower_low) * RRR));
                                                            trade.Buy(lot_size,_Symbol,ask_price,lower_low,take_profit);
                                                            lastTradeBarTime = currentBarTime;
                                                           }
                                                        }
                                                      break;
                                                     }
                                                  }
                                               break;
                                              }
                                            }
                                          break;
                                         }
                                      }
                                    break;
                                   }
                                }
                              break;
                             }
                          }
                       }
                    }
                  break;
                 }
              }
           }
        }
     }

   //--- RESISTANCE ZONE DETECTION + BEARISH CHOCH + SELL EXECUTION
   for(int i = z ; i < bars_check - z; i++)
     {
      if(IsSwingHigh(high, i, z))
        {
         first_res_price = high[i];
         first_res_max_body_price = MathMax(close[i], open[i]);
         first_res_time = time[i];

         for(int j = i+1; j < bars_check - z; j++)
           {
            if(IsSwingHigh(high, j, z) && high[j] >= first_res_max_body_price && high[j] <= first_res_price)
              {
               second_res_price = high[j];
               second_res_time = time[j];
               start_choch_time = time[j+z];

               resistance_object = StringFormat("RESISTANCE %f",first_res_price);

               res_bars = Bars(_Symbol,timeframe,first_res_time,TimeCurrent());
               res_max_high_index = ArrayMaximum(high,i,res_bars);
               res_max_high_price = high[res_max_high_index];

               if(res_max_high_price <= first_res_price)
                 {
                  ObjectCreate(chart_id,resistance_object,OBJ_RECTANGLE,0,first_res_time,first_res_price,TimeCurrent(),first_res_max_body_price);
                  ObjectSetInteger(chart_id,resistance_object,OBJPROP_COLOR,clrGreen);
                  ObjectSetInteger(chart_id,resistance_object,OBJPROP_BACK,true);
                  ObjectSetInteger(chart_id,resistance_object,OBJPROP_FILL,true);

                  first_high_txt = StringFormat("FIRST HIGH%d",i);
                  ObjectCreate(chart_id,first_high_txt,OBJ_TEXT,0,first_res_time,first_res_price);
                  ObjectSetString(chart_id,first_high_txt,OBJPROP_TEXT,"1");

                  second_high_txt = StringFormat("SECOND HIGH%d",i);
                  ObjectCreate(chart_id,second_high_txt,OBJ_TEXT,0,second_res_time,second_res_price);
                  ObjectSetString(chart_id,second_high_txt,OBJPROP_TEXT,"2");

                  if(exe_total_symbol_bars >= bars_check)
                    {
                     for(int k = 4; k < bars_check-3; k++)
                       {
                        if(IsSwingHigh(exe_high, k, 3))
                          {
                           higher_high = exe_high[k];
                           higher_high_time = exe_time[k];

                           for(int l = k; l < bars_check-3; l++)
                             {
                              if(IsSwingLow(exe_low,l,3))
                                {
                                 higher_low = exe_low[l];
                                 higher_low_time = exe_time[l];

                                 for(int m = l; m < bars_check-3; m++)
                                   {
                                    if(IsSwingHigh(exe_high, m, 3))
                                      {
                                       high_h = exe_high[m];
                                       high_time = exe_time[m];

                                       for(int n = m; n < bars_check-3; n++)
                                         {
                                          if(IsSwingLow(exe_low,n,3))
                                            {
                                             low_l = exe_low[n];
                                             low_time = exe_time[n];

                                             for(int o = k; o > 0; o--)
                                               {
                                                if(exe_close[o] < higher_low && exe_open[o] > higher_low)
                                                  {
                                                   if(higher_low < higher_high && high_h > higher_low && high_h < higher_high && low_l < high_h && low_l < higher_low && higher_high >= first_res_max_body_price
                                                      && higher_high <= first_res_price && low_time > start_choch_time)
                                                     {
                                                      ObjectCreate(chart_id,"HHHL",OBJ_TREND,0,higher_high_time,higher_high,higher_low_time,higher_low);
                                                      ObjectSetInteger(chart_id,"HHHL",OBJPROP_COLOR,clrRed);
                                                      ObjectSetInteger(chart_id,"HHHL",OBJPROP_WIDTH,2);

                                                      ObjectCreate(chart_id,"HLH",OBJ_TREND,0,higher_low_time,higher_low,high_time,high_h);
                                                      ObjectSetInteger(chart_id,"HLH",OBJPROP_COLOR,clrRed);
                                                      ObjectSetInteger(chart_id,"HLH",OBJPROP_WIDTH,2);

                                                      ObjectCreate(chart_id,"HL",OBJ_TREND,0,high_time,high_h,low_time,low_l);
                                                      ObjectSetInteger(chart_id,"HL",OBJPROP_COLOR,clrRed);
                                                      ObjectSetInteger(chart_id,"HL",OBJPROP_WIDTH,2);

                                                      ObjectCreate(chart_id,"R Cross Line",OBJ_TREND,0,higher_low_time,higher_low,exe_time[o],higher_low);
                                                      ObjectSetInteger(chart_id,"R Cross Line",OBJPROP_COLOR,clrRed);
                                                      ObjectSetInteger(chart_id,"R Cross Line",OBJPROP_WIDTH,2);

                                                      if(exe_time[1] == exe_time[o] && currentBarTime != lastTradeBarTime)
                                                        {
                                                         take_profit = MathAbs(ask_price - ((high_h - ask_price) * RRR));
                                                         trade.Sell(lot_size,_Symbol,ask_price,higher_high,take_profit);
                                                         lastTradeBarTime = currentBarTime;
                                                        }
                                                     }
                                                   break;
                                                  }
                                               }
                                            break;
                                           }
                                         }
                                      break;
                                     }
                                   }
                                break;
                               }
                             }
                          break;
                         }
                       }
                    }
                 }
              break;
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| FUNCTION FOR SWING LOW                                           |
//+------------------------------------------------------------------+
bool IsSwingLow(const double &low_price[], int index, int lookback)
  {
   for(int i = 1; i <= lookback; i++)
     {
      if(low_price[index] > low_price[index - i] || low_price[index] > low_price[index + i])
         return false;
     }
   return true;
  }
//+------------------------------------------------------------------+
//| FUNCTION FOR SWING HIGH                                          |
//+------------------------------------------------------------------+
bool IsSwingHigh(const double &high_price[], int index, int lookback)
  {
   for(int i = 1; i <= lookback; i++)
     {
      if(high_price[index] < high_price[index - i] || high_price[index] < high_price[index + i])
         return false;
     }
   return true;
  }
//+------------------------------------------------------------------+