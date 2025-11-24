//+------------------------------------------------------------------+
//|                                                   DucibusPro.mq4 |
//|                Copyright 2019-2023, Pro Investing Associates S.L.|
//|                                       https://www.botvesting.com |
//+------------------------------------------------------------------+

// OJO!!  Esta asignación de UsaCSV está tambien en Optimator y hay que activar las dos
//#ifndef UsaCSV
//   #define UsaCSV
//#endif
//
//#ifndef WalkForfardPro
//   #define WalkForfardPro
//#endif
//
//#ifndef WalkForfardPro_Modo_Null
//   #define WalkForfardPro_Modo_Null
//#endif


#include <WinUser32.mqh>
#include <Common/Constants.mqh>
#include <Common/Enums.mqh>
#include <Common/Types.mqh>

#define WM_COPYDATA 0x004A
#define INFO 0x01
#define SendMessage SendMessageW
struct Copydatastruct
  {
   int               dwData;
   int               cbData;
   int               lpData;
  };
struct Infostruct
  {
   double            valorDouble;
  };
#property icon "icono_botvesting.ico"
#property copyright "Copyright 2023 Proinvesting Associates S.L."
#property link      "https://botvesting.com"
//#property version   "1.00"

#property description   "===== Lidera a Julius Caesar, Alexander Magnus y Hannibal Barca ======"

//##########################################
//## Walk Forward Pro Header Start (MQL4) ##
//##########################################
#ifdef WalkForfardPro

#import "TSMWFP.ex4"
void WFA_Initialise();
void WFA_UpdateValues();
void WFA_PerformCalculations(double &dCustomPerformanceCriterion);
#import

#endif
//########################################
//## Walk Forward Pro Header End (MQL4) ##
//########################################

#import "kernel32.dll"
int GetModuleHandleA(string lpString);
int FreeLibrary(int hModule);
int DeleteFileA(string file);
int DeleteFileW(string file);

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int GlobalAlloc(int Flags, int Size);
int GlobalLock(int hMem);
int GlobalUnlock(int hMem);
int GlobalFree(int hMem);
int lstrcpyW(int ptrhMem, string Text);
#import



#include "Botlidator_ver_2_90.mqh"


#import "User32.dll"
int GetParent(int hWnd);
int GetTopWindow(int hWnd);
int GetWindowRect(int hWnd, int &rect[4]);
int PostMessageA(int hWnd, int msg, int wparam, int lparam);
ushort GetKeyState(int nVirtKey);
ushort GetAsyncKeyState(int nVirtKey);

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int FindWindow(string&, int);
int SendMessage(int, int, int, Copydatastruct&);

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OpenClipboard(int hOwnerWindow);
int EmptyClipboard();
int CloseClipboard();
int SetClipboardData(int Format, int hMem);


#import "msvcrt.dll"
int memcpy(Copydatastruct&, Copydatastruct&, int);
int memcpy(Infostruct&, Infostruct&, int);
#import

Infostruct info;
Copydatastruct copy;
string sDispatch = "mywindow";
int hwDispatch = 0;

//#define WM_CLOSE 16

#define GMEM_MOVEABLE   2
#define CF_UNICODETEXT  13

datetime bloqueoTemporal;

datetime tiempoMinuto=0;
datetime tiempoHora=0;

datetime tiempodesDeInicio=0;

int LB = 200;
int maxBarsForPeriod = 500;
bool showM01 = TRUE;
bool showM05 = TRUE;
bool showM15 = TRUE;
bool showM30 = TRUE;
bool showH01 = TRUE;
bool showH04 = TRUE;
bool showD01 = TRUE;
bool showW01 = TRUE;
bool showMN1 = TRUE;
int lastBarTime_M1 = 0;
int lastBarTime_M5 = 0;
int lastBarTime_M15 = 0;
int lastBarTime_M30 = 0;
int lastBarTime_H1 = 0;
int lastBarTime_H4 = 0;
int lastBarTime_D1 = 0;
int lastBarTime_W1 = 0;
int lastBarTime_MN1 = 0;
int indicatorBuffer_M1 = 0;
int indicatorBuffer_M5 = 0;
int indicatorBuffer_M15 = 0;
int indicatorBuffer_M30 = 0;
int indicatorBuffer_H1 = 0;
int indicatorBuffer_H4 = 0;
int indicatorBuffer_D1 = 0;
int indicatorBuffer_W1 = 0;
int indicatorBuffer_MN1 = 0;
double maxBuffer_M1[501];
double maxBuffer_M5[501];
double maxBuffer_M15[501];
double maxBuffer_M30[501];
double maxBuffer_H1[501];
double maxBuffer_H4[501];
double maxBuffer_D1[501];
double maxBuffer_W1[501];
double maxBuffer_MN1[501];
double minBuffer_M1[501];
double minBuffer_M5[501];
double minBuffer_M15[501];
double minBuffer_M30[501];
double minBuffer_H1[501];
double minBuffer_H4[501];
double minBuffer_D1[501];
double minBuffer_W1[501];
double minBuffer_MN1[501];

double soportes[10];
double resistencias[10];
string soportesNames[10];
string resistenciasNames[10];
int sopFuerza[10];
int resFuerza[10];

int sopFuerte=1;
int resFuerte=1;

ushort altKey=0;
bool pulsadaAltKey=True;

string iniPar=Symbol();

int hWnd1back=0;

string coletaPar="";

double multiplicadorCierreParcialLocal = 1.0;
double multiplicadorCierreParcialLocalCiclo = 1.0;
double lotajePrimeraCaesar=0.01;
double lotajePrimeraAlexander=0.01;
double lotajePrimeraHannibal=0.01;

bool    CaesarOrdenEnEstaVela = false;
bool    AlexanderOrdenEnEstaVela = false;
bool    HannibalOrdenEnEstaVela = false;
datetime CaesarNewCandleTime = TimeCurrent();
datetime AlexanderNewCandleTime = TimeCurrent();
datetime HannibalNewCandleTime = TimeCurrent();

int cargCar=0;
datetime tiempoCargadaCartera=0;

double  media0 = 0;
double  media1 = 0;

double  atrTP = 0;
double  atrTPCaesar = 0;

double  atrValue = 0;

double  atrValueJ = 0;
double  atrParcialValueJ = 0;

double  atrValueA = 0;
double  atrParcialValueA = 0;

double  atrValueH = 0;
double  atrParcialValueH = 0;

double    prevPriceBid;
double    prevPriceAsk;

uint tiempoEnTimer = 0;
uint tiempoEnTimer2 = 0;
bool     enTick=false;

double nivelDeActivacionJ=Bid;
double nivelDeActivacionA=Bid;
double nivelDeActivacionH=Bid;
double nivelDeActivacionM=Bid;

int retorno =0;

double CaesarGlobalTP=0;
double AlexanderGlobalTP=0;
double HannibalGlobalTP=0;

int colorLines[] = {0xB4019E, 0x4B11C3, 0x0822FF, 0x0052FE, 0x009AFF, 0x00BDFE, 0x00FEFE, 0x00EAC6, 0x19B21B, 0xD19201, 0xFF4600, 0xA90045, 0xFEFEFE, 0x909090, 0x000000};

int lastColorCruceLineaJ1;
int lastColorStopCruceLineaJ1;
int lastColorCruceLineaJ2;
int lastColorStopCruceLineaJ2;

int lastColorCruceLineaA1;
int lastColorStopCruceLineaA1;
int lastColorCruceLineaA2;
int lastColorStopCruceLineaA2;

int lastColorCruceLineaH1;
int lastColorStopCruceLineaH1;
int lastColorCruceLineaH2;
int lastColorStopCruceLineaH2;

int lastColorCruceLineaM1;
int lastColorStopCruceLineaM1;
int lastColorCruceLineaM2;
int lastColorStopCruceLineaM2;

double lotajeLineaM=0;

double lastAsk=-1;

double SMA=0.00;
double SMA2=0.00;

double GranH=0.00;
double GranL=0.00;
double GranH2=0.00;
double GranL2=0.00;
int HH=0;
int LL=0;
int HH2=0;
int LL2=0;

double divisorMM=1;
double mediaMM=200;
double offsetMM=200;
double mediaMMask=200;
double offsetMMask=200;

double divisorGG=1;
double mediaGG=50;
double offsetGG=50;

int mouseCoorX=0;
int mouseCoorY=0;

datetime mouseTime=0;
double mousePrice=0;

int lastCaesarPipStep=0;
int lastAlexanderPipStep=0;
int lastHannibalPipStep=0;


bool puedeReEntrarCaesar=true;
bool puedeReEntrarAlexander=true;
bool puedeReEntrarHannibal=true;

bool debeMostrarLineaAsk=true;


//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+

enum intTipoFormacion
  {
   Peloton = 0, //Pelotón
   Escuadron = 1, //Escuadrón
   Comando = 2, //Comando
  };


//extern string LicenceTO = "CaptainFX";
extern string Info1 = "                                       ";//.
extern string Info2 = "                                       ";//.
extern string Info3 = "  ¡OJO! NO TOCAR NADA AQUÍ!";//.
extern string Info4 = "                                       ";//.
extern string Info5 = "TODO SE AJUSTA DESDE EL PANEL";//.
extern string Info6 = "                                       ";//.
extern string Info7 = "                                       ";//.
extern string Info8 = "                                       ";//.
extern string ArmeriaDeCapitanes = "< < < + + Arma a tus caudillos hasta los dientes + + > > >";
extern string nombreCapitan = "Mi nombre";
extern string lemaCapitan = "Lo importante no es participar ...";

enum intTipoPanel
  {
   Ocultar = 1, //Ocultar
   Normal = 2, //Standard
  };

intTipoPanel mostrarPanel = Normal; //Mostrar Panel

int AceleraBTPanel = 0;
int conteoBT = 0;



enum intTipoCuenta
  {
   Cent = 100,
   Standard = 1,
   Roboforex = 0,
   Por_Defecto = -1
  };
extern static intTipoCuenta tipoCuenta = Por_Defecto;

int static lastTipoCuenta=Por_Defecto;


enum intOpcionesModo
  {
   Modo_Gladiador = 0, //Modo Gladiador
   Modo_Elite = 1, //Modo Élite
   Modo_Centurion = 2, //Modo Centurión
   Modo_Minerva = 3, //Modo Minerva
  };

enum intTipoCierres
  {
   Solo_Par = 0,
   Toda_Cuenta = 1,
  };

extern intOpcionesModo modoOperativa = Modo_Elite;

// --¯¯--_-Lacerta Cauda-¯--__-->>>
extern bool lacertaCauda = false;
extern double lacertaCaudaFlotante = 100.00;
//extern bool lacertaCaudaEsDinero = false;
extern int lotdecimal = 2;
extern double slipPage = 3.0;
extern bool   limiteSpread = True;
extern double maxSpread = 40.0;
extern bool   limiteSeparacion = True;
extern double minSeparacionCaudillos = 100.0;
extern intTipoFormacion tipoFormacion = Peloton;

extern bool   InteresCompuesto = False;
extern double   BalanceBase = 0.00;
extern bool   LotExact = True;

int    BuysErroneas = 0;
int    SellsErroneas = 0;




extern string ARMERIA_CENTURION = "< < < + + Armeria Centurion + + > > >";

extern double centuApertura = -1.0;
extern double centuCierre = 0.6;

extern string ARMERIA_TGA = "< < < + + Armeria Trailing Ganancia + + > > >";

extern bool   TrailingGAActivo = false;
extern double TrailingGAStart = 2.0;
extern double TrailingGAStop = 2.0;
extern double TrailingGABeneDia = 2.0;
extern intTipoCierres TrailingGATipoCierre = Solo_Par;

extern string ARMERIA_TRE = "< < < + + Armeria Trailing Reduccion + + > > >";

extern bool   TrailingREActivo = False;
extern double TrailingREStop = 2.0;
extern double TrailingREStart = 2.0;
extern double TrailingREMinima = 2.0;
extern intTipoCierres TrailingRETipoCierres = Solo_Par;

double TrailingREStopCALC = 1.0;
double TrailingREStartCALC = 1.0;


enum intOpcionesTF
  {
   TF_M1 = 1, //M1
   TF_M5 = 5, //M5
   TF_M15 = 15, //M15
   TF_M30 = 30, //M30
   TF_H1 = 60, //H1
   TF_H4 = 240, //H4
   TF_D1 = 1440, //D1
   TF_W1 = 10080, //W1
   TF_MN = 43200, //MN
   TF_0 = 0, //Gráfica
  };

enum intOpcionesInterTF
  {
   iTF_M1 = 1, //M1
   iTF_M5 = 5, //M5
   iTF_M15 = 15, //M15
   iTF_M30 = 30, //M30
   iTF_H1 = 60, //H1
   iTF_H4 = 240, //H4
   iTF_D1 = 1440, //D1
   iTF_W1 = 10080, //W1
   iTF_MN = 43200, //MN
   iTF_0 = 0, //Gráfica
   iTF_Tick = -1, //Tick
  };

enum intOpcionesTipoOperacion
  {
   Tipo_Buy = 0, //Buy
   Tipo_Sell = 1, //Sell
   Tipo_Manual = 2, //Manual
   Tipo_Tendencial = 3, //Tendencial
   Tipo_AntiTenden = 4, //Anti-Tendencial
  };

enum intOpcionesActividad
  {
   Terminando = 0, //Terminando
   Apagado = 1,
   Encendido = 2,
  };

enum intModoPipStep
  {
   PS_Fijo = 0,
   PS_Incrementa = 1,
   PS_Variable = 2,
  };

enum intModoTp
  {
   TP_Fijo = 0,
   TP_Decrementa = 1,
  };

enum intModoCL
  {
   CL_Alza = 0,
   CL_Baja = 1,
   CL_Alza_y_Baja = 2,
   CL_Baja_y_Alza = 3,
  };

enum intDirecCL
  {
   CL_NoInicio = 0,
   CL_DibujaInicio = 1,
   CL_ActivaBUY =  2,
   CL_ActivaSELL = 3,
   CL_ActivaOtra = 4,
  };


enum intStopCL
  {
   CL_NoStop = 0,
   CL_DibujaStop = 1,
   CL_ActivaCloseAll = 2,
   CL_ActivaAlta = 3,
   CL_ActivaBaja = 4,
   CL_ActivaCloseBuys = 5,
   CL_ActivaCloseSells = 6,
   CL_ActivaCloseTicket = 7,
  };

enum intColorCL
  {
   CL_Color0 = 0,
   CL_Color1 = 1,
   CL_Color2 = 2,
   CL_Color3 = 3,
   CL_Color4 = 4,
   CL_Color5 = 5,
   CL_Color6 = 6,
   CL_Color7 = 7,
   CL_Color8 = 8,
   CL_Color9 = 9,
   CL_Color10 = 10,
   CL_Color11 = 11,
   CL_Color12 = 12,
   CL_Color13 = 13,
   CL_Color14 = 14,
   CL_Color15 = 15,
  };

int cuentaParo=0;

int lastReason = -1;

int mes = 0;

int saltaMes = 0;

int diaActual = 0;

int noOpHorarioJ = 0;
int noOpHorarioA = 0;
int noOpHorarioH = 0;

int count_orders_account = 0;


int primeraPosJ1 = 0;
int segundaPosJ1 = 0;
int terceraPosJ1 = 0;
int primeraPosStopJ1 = 0;
int segundaPosStopJ1 = 0;
int terceraPosStopJ1 = 0;
int primeraPosJ2 = 0;
int segundaPosJ2 = 0;
int terceraPosJ2 = 0;
int primeraPosStopJ2 = 0;
int segundaPosStopJ2 = 0;
int terceraPosStopJ2 = 0;

int primeraPosA1 = 0;
int segundaPosA1 = 0;
int terceraPosA1 = 0;
int primeraPosStopA1 = 0;
int segundaPosStopA1 = 0;
int terceraPosStopA1 = 0;
int primeraPosA2 = 0;
int segundaPosA2 = 0;
int terceraPosA2 = 0;
int primeraPosStopA2 = 0;
int segundaPosStopA2 = 0;
int terceraPosStopA2 = 0;

int primeraPosH1 = 0;
int segundaPosH1 = 0;
int terceraPosH1 = 0;
int primeraPosStopH1 = 0;
int segundaPosStopH1 = 0;
int terceraPosStopH1 = 0;
int primeraPosH2 = 0;
int segundaPosH2 = 0;
int terceraPosH2 = 0;
int primeraPosStopH2 = 0;
int segundaPosStopH2 = 0;
int terceraPosStopH2 = 0;


int primeraPosM1 = 0;
int segundaPosM1 = 0;
int terceraPosM1 = 0;
int primeraPosStopM1 = 0;
int segundaPosStopM1 = 0;
int terceraPosStopM1 = 0;
int primeraPosM2 = 0;
int segundaPosM2 = 0;
int terceraPosM2 = 0;
int primeraPosStopM2 = 0;
int segundaPosStopM2 = 0;
int terceraPosStopM2 = 0;

long ChID=0;

bool lacertaCaudaEsDinero = false;

bool    actualizadoTPCaesar=false;
bool    actualizadoTPAlexander=false;
bool    actualizadoTPHannibal=false;

int     selectedCaudillo=0;
bool    desplazado=false;

bool    noMargenParaBuy=false;
bool    noMargenParaSell=false;

datetime tiempoInicializacion=0;

bool    allowTA=false;

double  CaesarLotsLast=0.00;
double  AlexanderLotsLast=0.00;
double  HannibalLotsLast=0.00;

int scale_factor;

int lastCountTradesCaesar=-1;
int lastCountTradesAlexander=-1;
int lastCountTradesHannibal=-1;

bool          autoNuevoModo = false;
datetime      erroneasTimeCaesar = 0;
datetime      erroneasTimeAlexander = 0;
datetime      erroneasTimeHannibal = 0;

datetime tiempoLocalElapsed=0;



double        tiempoCierre = 0;
double        tiempoInicioCicloA = 0;
double        tiempoInicioCicloH = 0;

double        alto, bajo, medio;

double        multiAlexanderClose = 1;
double        multiHannibalClose = 1;

double        CaesarLotExponentIni;
double        AlexanderLotExponentIni;
double        HannibalLotExponentIni;

double        centurionFlotUltJ;
double        centurionFlotUltA;
double        centurionFlotUltH;
int           tendenciaInicial;

double        CaesarLotsIni;
double        AlexanderLotsIni;
double        HannibalLotsIni;
double        CaesarLotsIni2;
double        AlexanderLotsIni2;
double        HannibalLotsIni2;
double        balanceInicial;

double        centuAperturaIni = centuApertura;
double        centuCierreIni = centuCierre;
double        TrailingGAStartIni = TrailingGAStart;
double        TrailingGAStopIni = TrailingGAStop;
double        TrailingGABeneDiaIni = TrailingGABeneDia;

double        TrailingREStartIni = TrailingREStart;
double        TrailingREStopIni = TrailingREStop;
double        TrailingREMinimaIni = TrailingREMinima;

double        beneHoy = 0;

double        LimitCloseIni = 0.0;//LimitClose;

int           valorTimer=75;

bool          conseguido = false;
double        profitX = 0;
double        maxProfit = 0;
double        minProfit = 0;
double        diferencia = 1;
double        taraProfit = 0.0;
bool          haBajadoProfit = false;
bool          haSubidoProfit = false;
int           parte = 0;
int           CaesarTipoAlerta;
int           AlexTipoAlerta;
int           HannibalTipoAlerta;
int           CaesarGatilloAlerta;
int           AlexGatilloAlerta;
int           HannibalGatilloAlerta;
int           CaesarFrecuenciaAlerta;
int           AlexFrecuenciaAlerta;
int           HannibalFrecuenciaAlerta;
double        CaesarValorAlerta;
double        AlexValorAlerta;
double        HannibalValorAlerta;
double        MargenValorAlerta;
double        LacertaValorAlerta;
int           MargenGatilloAlerta;
int           MargenFrecuenciaAlerta;
int           LacertaGatilloAlerta;
int           LacertaFrecuenciaAlerta;
int           MensajeGatilloAlerta;
int           MensajeFrecuenciaAlerta;

int           AlertaMovilActiva;
int           AlertaMailActiva;
int           AlertaSoundActiva = 1;

datetime      tiempoAlertaMensaje = 0;
datetime      tiempoAlertaMargen = 0;
datetime      tiempoAlertaLacerta = 0;
datetime      tiempoAlertaCaesar = 0;
datetime      tiempoAlertaAlexander = 0;
datetime      tiempoAlertaHannibal = 0;
string        lastMensajeBot = "";

double        incre = 1;
double        incre2 = 0.001;
datetime      lastMinute = 0;
datetime      horaInicio = 0;
int          primerBotlidator = 0;


bool errorFijo = true;
bool errorFijo2 = true;

int modificaMN = 0;


extern string ARMERIA_CAESAR = "< < < + + Armeria de Julius Caesar + + > > >"; //AF-FiboScalper
extern intOpcionesActividad CaesarActividad = Apagado;
extern intOpcionesTipoOperacion CaesarTipoOperacion = Tipo_Manual;
extern intOpcionesTF CaesarSeleccionTF = TF_H1;
extern intOpcionesInterTF CaesarIntervaloTF = iTF_Tick;
extern double CaesarLots = 0.01;
extern double CaesarLotExponent = 1.67;
extern bool   CaesarCambiaExpActivo = False;
extern double CaesarCambiaExpValor = 0.01;
extern int    CaesarCambiaExpDesdeMartingala = 0;
extern double maxLotsCaesar = 500.0;
extern double CaesarPipStep = 200.0;
extern intModoPipStep CaesarModoPipStep = PS_Fijo;
extern double CaesarTakeProfit = 100.0;
extern intModoTp CaesarModoTp = TP_Fijo;
extern bool CaesarUseTrailingStop = FALSE;
extern double CaesarTrailStart = 75.0;
extern double CaesarTrailStop = 35.0;
extern int MaxTrades_Caesar = 18;
extern bool CaesarAutoPriceAverage = TRUE;
int PeriodRsiCaesar = 14;
double LevelRsiLowCaesar = 30.00;
double LevelRsiHighCaesar = 70.00;
extern int VolatilidadAdjJ = 0; //Nivel Volatilidad Caesar
extern int HolguraAdjJ = 0; //Holgura Tendencia Caesar

double valorInicialcaesarTPReEntVari=0;
double valorInicialalexanderTPReEntVari=0;
double valorInicialhannibalTPReEntVari=0;

double lastDecreCaesarTP=0.0;
double lastDecreAlexanderTP=0.0;
double lastDecreHannibalTP=0.0;

double caesarTPReEntVari=0;
double alexanderTPReEntVari=0;
double hannibalTPReEntVari=0;

extern bool       reEntradasCaesar      = false;
extern int        reStopCaesar   = 2;
extern double     reDistanciaCaesar  = 0.25;
extern double     reLotajeCaesar     = 0.5;

bool puedeCoberturearCaesar = true;
bool hazReentradaCaesar = false;
double precioPrimeraCaesar = 0;

double preLastOpenPriceCaesar = 0;
double preLastOpenPriceAlexander = 0;
double preLastOpenPriceHannibal = 0;

int lastCaesarTakeProfit = (int)CaesarTakeProfit;

int MagicNumber_Caesar = 10278 + modificaMN;


int conta = 0;

double flotanteLacertaCauda = 0;
double flotanteParActual = 0;
double lastFlotanteParActual = 0;

double saludPorcen = 1000000;
double saludMin = 0;
double saludMax = 1000000;

int exit = -1;

datetime checktime;
int lastOrdersTotal = 100000;

int opCaesar = -1;
int opAlexander = -1;
int opHannibal = -1;

bool CaesarActivo = false;
double trailingStopLevel = 48.0; //nuevos
double caesarTakeProfitPromediado;
double atrStopLevel;
double priceFilter;
double promedioPrecioCaesar = 0.00;
double caesarBidPrice;
double caesarAskPrice;
double caesarUltimoPrecioOperacionBuy;
double caesarUltimoPrecioOperacionSell;
double caesarPrimerPrecioOperacionBuy;
double caesarPrimerPrecioOperacionSell;
bool CaesarActualizarTP;
string comentarioCaesar = "Julius Caesar";
int operationCounter;
double lotajeActualizadoCaesar;
int posicionOrden;
int numeroOperacionesCaesar = 0;
int numeroOperacionesAlexander = 0;
int numeroOperacionesHannibal = 0;
bool debeAbrirseOperacionCaesar = FALSE;
int resultadoAbirOrdenCaesar;
bool CaesarResultAbirOperacion = FALSE;
int CaesarCambioMinuto = 1;
datetime CaesarWaitSecondsTPRe = TimeCurrent();
int CaesarOperacionAbiertasBuy = 0;
int CaesarOperacionAbiertasSell = 0;

/*  */
extern string ARMERIA_ALEXANDER = "< < < + + Armeria de Alexander Magnus + + > > >"; //AF-Scalper
extern intOpcionesActividad AlexanderActividad = Apagado;
extern intOpcionesTipoOperacion AlexanderTipoOperacion = Tipo_Manual;
extern intOpcionesTF AlexanderSeleccionTF = TF_H1;
extern intOpcionesInterTF AlexanderIntervaloTF = iTF_Tick;
extern double AlexanderLots = 0.01;
extern double AlexanderLotExponent = 1.67;
extern bool   AlexanderCambiaExpActivo = False;
extern double AlexanderCambiaExpValor = 0.01;
extern int    AlexanderCambiaExpDesdeMartingala = 0;
extern double maxLotsAlexander = 500.0;
extern double AlexanderPipStep = 200.0;
extern intModoPipStep AlexanderModoPipStep = PS_Fijo;
extern double AlexanderTakeProfit = 100.0;
extern intModoTp AlexanderModoTp = TP_Fijo;
extern bool AlexanderUseTrailingStop = FALSE;
extern double AlexanderTrailStart = 75.0;
extern double AlexanderTrailStop = 35.0;
extern int MaxTrades_Alexander = 18;
extern bool AlexanderAutoPriceAverage = TRUE;
extern int VolatilidadAdjA = 0; //Nivel Volatilidad Alexander
extern int HolguraAdjA = 0; //Holgura Tendencia Alexander

extern bool       reEntradasAlexander      = false;
extern int        reStopAlexander   = 2;
extern double     reDistanciaAlexander  = 0.25;
extern double     reLotajeAlexander     = 0.5;

bool puedeCoberturearAlexander = true;
bool hazReentradaAlexander = false;
double precioPrimeraAlexander = 0;

int lastAlexanderTakeProfit = (int)AlexanderTakeProfit;

int MagicNumber_Alexander = 22324 + modificaMN;
/*  */

bool AlexanderActivo = false;
double g_pips_412 = 40.0;
bool alexanderOrderInProgress = FALSE;
double alexanderTrailingStopLevel = 48.0;
double alexanderTakeProfitPromediado;
double promedioPrecioAlexander = 0.00;
double alexanderBidPrice;
double alexanderAskPrice;
double alexanderUltimoPrecioOperacionBuy;
double alexanderUltimoPrecioOperacionSell;
double alexanderPrimerPrecioOperacionBuy;
double alexanderPrimerPrecioOperacionSell;
bool AlexanderActualizarTP;
string comentarioAlexander = "Alexander Magnus";
int alexanderBufferCounter1 = 0;
int alexanderBufferCounter2;
double lotajeActualizadoAlexander;
int alexanderPosition = 0;
bool debeAbrirseOperacionAlexander = FALSE;
bool AlexanderTipoBuy = FALSE;
bool AlexanderTipoSell = FALSE;
int resultadoAbirOrdenAlexander;
bool AlexanderResultAbirOperacion = FALSE;
double alexanderPriceBuffer1;
double alexanderPriceBuffer2;
int alexanderDateTime = 1;
int AlexanderCambioMinuto = 1;
datetime AlexanderWaitSecondsTPRe = TimeCurrent();

int AlexanderOperacionAbiertasBuy = 0;
int AlexanderOperacionAbiertasSell = 0;

/*  */
extern string ARMERIA_HANNIBAL = "< < < + + Armeria de Hannibal Barca + + > > >"; //AF-TrendKiller
extern intOpcionesActividad HannibalActividad = Apagado;
extern intOpcionesTipoOperacion HannibalTipoOperacion = Tipo_Manual;
extern intOpcionesTF HannibalSeleccionTF = TF_H1;
extern intOpcionesInterTF HannibalIntervaloTF = iTF_Tick;
extern double HannibalLots = 0.01;
extern double HannibalLotExponent = 1.67;
extern bool   HannibalCambiaExpActivo = False;
extern double HannibalCambiaExpValor = 0.01;
extern int    HannibalCambiaExpDesdeMartingala = 0;
extern double maxLotsHannibal = 500.0;
extern double HannibalPipStep = 200.0;
extern intModoPipStep HannibalModoPipStep = PS_Fijo;
extern double HannibalTakeProfit = 100.0;
extern intModoTp HannibalModoTp = TP_Fijo;
extern bool HannibalUseTrailingStop = FALSE;
extern double HannibalTrailStart = 75.0;
extern double HannibalTrailStop = 35.0;
extern int MaxTrades_Hannibal = 18;
extern bool HannibalAutoPriceAverage = TRUE;
int PeriodRsiHannibal = 14;
double LevelRsiLowHannibal = 30.00;
double LevelRsiHighHannibal = 70.00;
extern int VolatilidadAdjH = 0; //Nivel Volatilidad Hannibal
extern int HolguraAdjH = 0; //Holgura Tendencia Hannibal

extern bool       reEntradasHannibal      = false;
extern int        reStopHannibal          = 2;
extern double     reDistanciaHannibal     = 0.25;
extern double     reLotajeHannibal        = 0.5;

bool puedeCoberturearHannibal = true;
bool hazReentradaHannibal = false;
double precioPrimeraHannibal = 0;

int lastHannibalTakeProfit = (int)HannibalTakeProfit;

int MagicNumber_Hannibal = 23794 + modificaMN;
/*  */

int ManualOperacionAbiertasBuy=0;
int ManualOperacionAbiertasSell=0;


bool HannibalActivo = false;
int hannibalTimeframe = PERIOD_M1;
bool hannibalOrderInProgress = FALSE;
double hannibalTrailingStopLevel = 48.0;
double hannibalTakeProfitPromediado;
double promedioPrecioHannibal = 0.00;
double hannibalBidPrice;
double hannibalAskPrice;
double hannibalUltimoPrecioOperacionBuy;
double hannibalUltimoPrecioOperacionSell;
double hannibalPrimerPrecioOperacionBuy;
double hannibalPrimerPrecioOperacionSell;
bool HannibalActualizarTP;
string comentarioHannibal = "Hannibal Barca";
int hannibalBufferCounter1 = 0;
int hannibalBufferCounter2;
double lotajeActualizadoHannibal;
int hannibalPosition = 0;
bool debeAbrirseOperacionHannibal = FALSE;
bool HannibalTipoBuy = FALSE;
bool HannibalTipoSell = FALSE;
int resultadoAbirOrdenHannibal;
int HannibalCambioMinuto = 1;
datetime HannibalWaitSecondsTPRe = TimeCurrent();

int HannibalOperacionAbiertasBuy = 0;
int HannibalOperacionAbiertasSell = 0;


bool HannibalResultAbirOperacion = FALSE;
bool cg = FALSE;
double equidadActual;
double equidadAnterior;
int hannibalDateTime = 1;

string botvestingUrl;
double capitanFlotante = 0.0;
double capitanPorcentajeFlotante = 0.0;
double CaesarFlotante =  0.0;
double caesarPorcentajeFlotante = 0.0;
double capitanLotaje = 0.0;
double caesarLotaje = 0.0;
double alexanderLotaje = 0.0;
double hannibalLotaje = 0.0;
double AlexanderFlotante =  0.0;
double alexanderPorcentajeFlotante = 0.0;
double HannibalFlotante =  0.0;
double hannibalPorcentajeFlotante = 0.0;
double capitanBenefObjTP = 0.0;
double caesarBenefObjTP = 0.0;
double alexanderBenefObjTP = 0.0;
double hannibalBenefObjTP = 0.0;
double capitanBenefObjSL = 0.0;
double caesarBenefObjSL = 0.0;
double alexanderBenefObjSL = 0.0;
double hannibalBenefObjSL = 0.0;
double swapCapitan = 0.0;
double swapCaesar = 0.0;
double swapAlexander = 0.0;
double swapHannibal = 0.0;
double comisionCapitan = 0.0;
double comisionCaesar = 0.0;
double comisionAlexander = 0.0;
double comisionHannibal = 0.0;
double lotajeTotalPar = 0.0;
double lotajeTotalCuenta = 0.0;
double dineroPorTickLotPar = 0.0;
double dineroPorTickLotCuenta = 0.0;

double dineroPorTickLotCapitan = 0.0;
double dineroPorTickLotCaesar = 0.0;
double dineroPorTickLotAlexander = 0.0;
double dineroPorTickLotHannibal = 0.0;

int operacionesCont = 0;
int operacionesContPar = 0;
double flotantePar = 0.0;
double porcentajeFlotantePar = 0.0;
double flotanteCuenta = 0.0;
double flotanteCuentaPorcentaje = 0.0;
double benefObjParTP = 0.0;
double benefObjParSL = 0.0;
double swapPar = 0.0;
double comisionPar = 0.0;
string salud = "";
string spread = "";
string flotanteCaesar = "";
double valorCadaTick = 0.0;
double beneficioTPObjetivoAcumulado;
double beneficioSLObjetivoAcumulado;
double swapAcumulado;
double comisionAcumulado;
double lotajeAcumulado = 0.0;

double lotajeTotalCuentaBuys = 0.0;
double lotajeTotalCuentaSells = 0.0;
double lotajeTotalParBuys = 0.0;
double lotajeTotalParSells = 0.0;

double lotajeTotalAcumulado = 0.0;
double profitAcumulado = 0.0;
double profitAcumuladoMonth = 0.0;
double profitActualTotal = 0.0;
double ticksRecorridos = 0.0;
double spreadActual = 0.0;
double spreadAnt1 = 0.0;
double spreadAnt2 = 0.0;
double spreadAnt3 = 0.0;
double spreadAnt4 = 0.0;
double spreadAnt5 = 0.0;
double spreadAnt6 = 0.0;
double spreadAnt7 = 0.0;
double spreadAnt8 = 0.0;
double spreadAnt9 = 0.0;
double spreadAnt10 = 0.0;
double spreadAnt11 = 0.0;
double spreadAnt12 = 0.0;
double spreadAnt13 = 0.0;
double spreadAnt14 = 0.0;
double spreadAnt15 = 0.0;
double spreadAnt16 = 0.0;
double spreadAnt17 = 0.0;
double spreadAnt18 = 0.0;
double spreadAnt19 = 0.0;
double spreadAnt20 = 0.0;

double tipoCuentaDouble = 1.0;

color colorDatosCaesar = White;
color colorDatosAlexander = White;
color colorDatosHannibal = White;
string valorSpread = "";

int numRobot = 0;
int mercadoAbierto = -2;
datetime tiempoInicio;
datetime tiempoUpdatePanel;
datetime tiempoMetatrader;
bool primeraVez = true;
bool refrescar = false;

int otraMartinCaesar = 0;
int otraMartinAlexander = 0;
int otraMartinHannibal = 0;

int CaesarOrdenManual = 0;
int AlexanderOrdenManual = 0;
int HannibalOrdenManual = 0;

int OrdenManual = 0;

double acumCicloJ = 0;
double acumCicloA = 0;
double acumCicloH = 0;

datetime acumCicloJTime = 0;
datetime acumCicloATime = 0;
datetime acumCicloHTime = 0;

double infoLotStep = 0.01;
double infoLotMax = 100;
double infoLotMin = 0.1;

int infoStopMin = 40;

int tendencia = 0;
int lastTendencia = 0;

double ultimaCaesar = 0;
double ultimaCaesarRe = 0;
double ultimaAlexander = 0;
double ultimaAlexanderRe = 0;
double ultimaHannibal = 0;
double ultimaHannibalRe = 0;
double ultimaManual = 0;
double ultimaManualRe = 0;


double lastOpenPriceCaesar = 0;
double lastOpenPriceAlexander = 0;
double lastOpenPriceHannibal = 0;
double lastOpenPriceManual = 0;
double lastOpenPriceManualRe = 0;

int ultimaTicketCaesar = -1;
int ultimaTicketAlexander = -1;
int ultimaTicketHannibal = -1;
int ultimaTicketManual = -1;

int ultimaTicketCaesarRe = -1;
int ultimaTicketAlexanderRe = -1;
int ultimaTicketHannibalRe = -1;
int ultimaTicketManualRe = -1;

int masAltaTicketCaesar = -1;
int masAltaTicketAlexander = -1;
int masAltaTicketHannibal = -1;
int masAltaTicketManual = -1;

int masBajaTicketCaesar = -1;
int masBajaTicketAlexander = -1;
int masBajaTicketHannibal = -1;
int masBajaTicketManual = -1;


int      ticketOrdenMasAltaCaesar;
int      ticketOrdenMasBajaCaesar;
double   flotanteOrdenMasAltaCaesar;
double   flotanteOrdenMasBajaCaesar;
double   precioOrdenMasAltaCaesar;
double   precioOrdenMasBajaCaesar;
double   prePrecioOrdenMasAltaCaesar;
double   prePrecioOrdenMasBajaCaesar;

int      ticketOrdenMasAltaAlexander;
double   flotanteOrdenMasAltaAlexander;
int      ticketOrdenMasBajaAlexander;
double   flotanteOrdenMasBajaAlexander;
double   precioOrdenMasAltaAlexander;
double   precioOrdenMasBajaAlexander;
double   prePrecioOrdenMasAltaAlexander;
double   prePrecioOrdenMasBajaAlexander;

int      ticketOrdenMasAltaHannibal;
double   flotanteOrdenMasAltaHannibal;
int      ticketOrdenMasBajaHannibal;
double   flotanteOrdenMasBajaHannibal;
double   precioOrdenMasAltaHannibal;
double   precioOrdenMasBajaHannibal;
double   prePrecioOrdenMasAltaHannibal;
double   prePrecioOrdenMasBajaHannibal;

int      ticketOrdenMasAltaManual;
double   flotanteOrdenMasAltaManual;
int      ticketOrdenMasBajaManual;
double   flotanteOrdenMasBajaManual;
double   precioOrdenMasAltaManual;
double   precioOrdenMasBajaManual;
double   prePrecioOrdenMasAltaManual;
double   prePrecioOrdenMasBajaManual;

double flotanteUltimaCaesar = 0.0;
double flotanteUltimaCaesarRe = 0.0;
double flotanteUltimaAlexander = 0.0;
double flotanteUltimaHannibal = 0.0;

double maxPriceCaesar = 0;
double maxPriceAlexander = 0;
double maxPriceHannibal = 0;

double minPriceCaesar = 999999;
double minPriceAlexander = 999999;
double minPriceHannibal = 999999;

int     countTradesCaesarRe = 0;
int     countTradesAlexanderRe = 0;
int     countTradesHannibalRe = 0;

int     countTradesCaesarVar = 0;
int     countTradesAlexanderVar = 0;
int     countTradesHannibalVar = 0;
int     countTradesCaptainVar = 0;
int     countTradesTotalParVar = 0;
int     lastCountTradesTotalParVar = 0;

int     segundosMercadoCerrado = 0;
int     segundosMercadoAbierto = 0;

bool primerTick = true;



extern string  LIMITES_TEMPORALES = "< < < + + Limites Temporales + + > > >";
extern bool   Enero = true;
extern bool   Febrero = true;
extern bool   Marzo = true;
extern bool   Abril = true;
extern bool   Mayo = true;
extern bool   Junio = true;
extern bool   Julio = true;
extern bool   Agosto = true;
extern bool   Septiembre = true;
extern bool   Octubre = true;
extern bool   Noviembre = true;
extern bool   Diciembre = true;
extern bool   AlsoClose = false;
extern double LimitClose = 0.0;


extern bool   LunesJ = True;
extern bool   MartesJ = True;
extern bool   MiercolesJ = True;
extern bool   JuevesJ = True;
extern bool   ViernesJ = True;
extern bool   SabadoJ = True;
extern bool   DomingoJ = True;

extern int    desdeDiaJ = 1;
extern int    hastaDiaJ = 31;

extern int    desdeHoraJ = 0;
extern int    desdeMinutoJ = 0;
extern int    hastaHoraJ = 23;
extern int    hastaMinutoJ = 59;

extern bool   InvierteTiempoJ = false;

extern bool   LunesA = True;
extern bool   MartesA = True;
extern bool   MiercolesA = True;
extern bool   JuevesA = True;
extern bool   ViernesA = True;
extern bool   SabadoA = True;
extern bool   DomingoA = True;

extern int    desdeDiaA = 1;
extern int    hastaDiaA = 31;

extern int    desdeHoraA = 0;
extern int    desdeMinutoA = 0;
extern int    hastaHoraA = 23;
extern int    hastaMinutoA = 59;

extern bool   InvierteTiempoA = false;

extern bool   LunesH = True;
extern bool   MartesH = True;
extern bool   MiercolesH = True;
extern bool   JuevesH = True;
extern bool   ViernesH = True;
extern bool   SabadoH = True;
extern bool   DomingoH = True;

extern int    desdeDiaH = 1;
extern int    hastaDiaH = 31;

extern int    desdeHoraH = 0;
extern int    desdeMinutoH = 0;
extern int    hastaHoraH = 23;
extern int    hastaMinutoH = 59;

extern bool   InvierteTiempoH = false;



//+------------------------------------------------------------------+
//|  GLOBALES OPTIMIZACION                                           |
//+------------------------------------------------------------------+

#include "Optimator_ver_2_00.mqh"

int MyPoint = 1;


//string  LINEAS_OPERACION = "< < < + + Lineas de Operacion Manual + + > > >";

intDirecCL direcCruceLineaJ1     = CL_NoInicio;
intModoCL  tipoCruceLineaJ1      = CL_Alza;
intColorCL colorCruceLineaJ1     = CL_Color0;
intStopCL  stopCruceLineaJ1      = CL_NoStop;
intColorCL colorStopCruceLineaJ1 = CL_Color1;

intDirecCL direcCruceLineaJ2     = CL_NoInicio;
intModoCL  tipoCruceLineaJ2      = CL_Baja;
intColorCL colorCruceLineaJ2     = CL_Color2;
intStopCL  stopCruceLineaJ2      = CL_NoStop;
intColorCL colorStopCruceLineaJ2 = CL_Color3;


intDirecCL direcCruceLineaA1     = CL_NoInicio;
intModoCL  tipoCruceLineaA1      = CL_Alza;
intColorCL colorCruceLineaA1     = CL_Color4;
intStopCL  stopCruceLineaA1      = CL_NoStop;
intColorCL colorStopCruceLineaA1 = CL_Color5;

intDirecCL direcCruceLineaA2     = CL_NoInicio;
intModoCL  tipoCruceLineaA2      = CL_Baja;
intColorCL colorCruceLineaA2     = CL_Color6;
intStopCL  stopCruceLineaA2      = CL_NoStop;
intColorCL colorStopCruceLineaA2 = CL_Color7;


intDirecCL direcCruceLineaH1     = CL_NoInicio;
intModoCL  tipoCruceLineaH1      = CL_Alza;
intColorCL colorCruceLineaH1     = CL_Color9;
intStopCL  stopCruceLineaH1      = CL_NoStop;
intColorCL colorStopCruceLineaH1 = CL_Color10;

intDirecCL direcCruceLineaH2     = CL_NoInicio;
intModoCL  tipoCruceLineaH2      = CL_Baja;
intColorCL colorCruceLineaH2     = CL_Color11;
intStopCL  stopCruceLineaH2      = CL_NoStop;
intColorCL colorStopCruceLineaH2 = CL_Color12;



intDirecCL direcCruceLineaM1     = CL_NoInicio;
intModoCL  tipoCruceLineaM1      = CL_Alza;
intColorCL colorCruceLineaM1     = CL_Color9;
intStopCL  stopCruceLineaM1      = CL_NoStop;
intColorCL colorStopCruceLineaM1 = CL_Color10;

intDirecCL direcCruceLineaM2     = CL_NoInicio;
intModoCL  tipoCruceLineaM2      = CL_Baja;
intColorCL colorCruceLineaM2     = CL_Color11;
intStopCL  stopCruceLineaM2      = CL_NoStop;
intColorCL colorStopCruceLineaM2 = CL_Color12;







// Copies the specified text to the clipboard, returning true if successful
bool CopyTextToClipboard(string Text)
  {
   bool bReturnvalue = false;

// Try grabbing ownership of the clipboard
   if(OpenClipboard(0) != 0)
     {
      // Try emptying the clipboard
      if(EmptyClipboard() != 0)
        {
         // Try allocating a block of global memory to hold the text
         int lnString = StringLen(Text);
         int hMem = GlobalAlloc(GMEM_MOVEABLE, (lnString * 2) + 2);
         if(hMem != 0)
           {
            // Try locking the memory, so that we can copy into it
            int ptrMem = GlobalLock(hMem);
            if(ptrMem != 0)
              {
               // Copy the string into the global memory
               lstrcpyW(ptrMem, Text);
               // Release ownership of the global memory (but don't discard it)
               GlobalUnlock(hMem);

               // Try setting the clipboard contents using the global memory
               if(SetClipboardData(CF_UNICODETEXT, hMem) != 0)
                 {
                  // Okay
                  bReturnvalue = true;
                 }
               else
                 {
                  // Failed to set the clipboard using the global memory
                  GlobalFree(hMem);
                 }
              }
            else
              {
               // Memory allocated but not locked
               GlobalFree(hMem);
              }
           }
         else
           {
            // Failed to allocate memory to hold string
           }
        }
      else
        {
         // Failed to empty clipboard
        }
      // Always release the clipboard, even if the copy failed
      CloseClipboard();
     }
   else
     {
      // Failed to open clipboard
     }

   return (bReturnvalue);
  }





double OnTester()

//+------------------------------------------------------------------+
  {

#ifdef UsaCSV
   return InTester();
#endif

  }


#ifdef UsaCSV

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double AlvortCoeff(double profit, double dd, int timelapse)
  {
   if(timelapse > 0.0)
     {
      if(dd > 0.0)
        {
         return (profit / (double) timelapse) / dd;
        }
     }
   return 0.0;
  }



//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double potencia(double valor, int exponente)
  {
   if(valor < 0)
      return MathPow(-1, exponente - 1) * MathPow(valor, exponente);
   else
      return MathPow(valor, exponente);
  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double beneficioriesgo()
  {
   double  profit = TesterStatistics(STAT_PROFIT);
   double  max_dd = TesterStatistics(STAT_EQUITY_DD);
   if(max_dd != 0.0)
      return profit / max_dd;
   else
      return -1.0;
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double sqn()
  {
   int hstTotal = OrdersHistoryTotal();
   double tradesProfits[];
   double average = 0.0;
   ArrayResize(tradesProfits, hstTotal);
   double totalProfit = 0.0;
   for(int i = 0; i < hstTotal; i++)
     {
      int comprobacion = OrderSelect(i, SELECT_BY_POS, MODE_HISTORY);
      tradesProfits[i] = OrderProfit();
      totalProfit += tradesProfits[i];
     }

   if(hstTotal != 0)
      average = totalProfit / hstTotal;

   double stdDev = standardDeviation(tradesProfits, average);
   if(stdDev != 0.0)
      return (average / stdDev) * MathSqrt(hstTotal);
   else
      return -1.0;
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double ganadoras()
  {
   int total_trades = (int)TesterStatistics(STAT_TRADES);
   int winning_trades = (int)TesterStatistics(STAT_PROFIT_TRADES);
   double percentage = (double)(winning_trades) / (double)(total_trades);
   return percentage * TesterStatistics(STAT_PROFIT_FACTOR);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double divergencia()
  {
   double average_profit = TesterStatistics(STAT_GROSS_PROFIT) / TesterStatistics(STAT_PROFIT_TRADES);
   double average_loss = TesterStatistics(STAT_GROSS_LOSS) / TesterStatistics(STAT_LOSS_TRADES);
   int total_trades = (int)TesterStatistics(STAT_TRADES);
   int winning_trades = (int)TesterStatistics(STAT_PROFIT_TRADES);
   double percentage = (double)(winning_trades) / (double)(total_trades);
   return percentage - (1 - average_profit / (average_profit + MathAbs(average_loss)));
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double kratio()
  {
   double acumPips = 0.0;
   double acumPipsArr[];
   ArrayResize(acumPipsArr, OrdersHistoryTotal());
   for(int j = 0; j < OrdersHistoryTotal(); j++)
     {
      int Orden = OrderSelect(j, SELECT_BY_POS, MODE_HISTORY);

      if(OrderType() == OP_BUY)
        {
         acumPips += (OrderClosePrice() - OrderOpenPrice()) / (Point * MyPoint);
        }
      else
         if(OrderType() == OP_SELL)
           {
            acumPips += (OrderOpenPrice() - OrderClosePrice()) / (Point * MyPoint);
           }

      acumPipsArr[j] = acumPips;

     }

   return getKRatio(acumPipsArr);
  }



// --------------------------------- KRATIO FUNCTION --------------------------------------------------------
class RegLinModel
  {
public:
   double            slope;
   double            errStd;
   double            intercept;

                     RegLinModel(void) {slope = 0.0; errStd = 0.0; intercept = 0.0;};
                     RegLinModel(const RegLinModel &foo)
     {
      slope = foo.slope;
      errStd = foo.errStd;
      intercept = foo.intercept;
     };
                    ~RegLinModel() {};
  };

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double getKRatio(double& arr[])
  {

   RegLinModel rg = regLin(arr);

   return rg.slope / (rg.errStd * ArraySize(arr));

  }

//y=a+bx  ,donde b es la pendiente
RegLinModel regLin(double& arr[])
  {



   RegLinModel rl = new RegLinModel();
   double sumy = 0,
          sumx = 0,
          sumxy = 0,
          sumxx = 0,
          a = 0,
          b = 0;

   int nElements = ArraySize(arr);
   double val[];
   ArrayResize(val, nElements);

   if(nElements == 0)
     {
      Print("regLin" + ": array size error");
      return rl;
     }


   for(int i = 0; i < nElements; i++)
     {
      sumy += arr[i];
      sumxy += arr[i] * i;
      sumx += i;
      sumxx += i * i;
     }

   b = (nElements * sumxy - sumx * sumy) / (nElements * sumxx - sumx * sumx);
   a = (sumy - (b * sumx)) / nElements;

   for(int j = 0; j < nElements; j++)
     {
      val[j] = a + (b * j);
     }

   rl.slope = b;
   rl.intercept = a;
   rl.errStd = getSlopeStdErr(arr, val);

   return rl;
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double getSlopeStdErr(double &val[], double &estimations[])
  {

   int arrSize = ArraySize(val);

   double ind[];
   ArrayResize(ind, arrSize);
   double indMedia = 0.0;
   double valMedia = 0.0;
   for(int i = 0; i < arrSize; i++)
     {
      indMedia += i;
      valMedia += val[i];
      ind[i] = i;
     }
   indMedia = indMedia / arrSize;
   valMedia = valMedia / arrSize;

   double indDev = desviacion(ind, indMedia);
   double valDev = desviacion(val, valMedia);
   double indValDev = codesviaciones(ind, indMedia, val, valMedia);

   if(indDev == 0.0)
      return 2147483600.0;
   if(arrSize == 2)
      return 2147483600.0;

   double error = MathSqrt((valDev - (indValDev / indDev)) / (arrSize - 2)) / MathSqrt(indDev);

   return error;
  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double codesviaciones(double &arr1[], double avg1, double &arr2[], double avg2)
  {

   double res = 0.0;
   for(int i = 0; i < ArraySize(arr1); i++)
     {
      res += (arr1[i] - avg1) * (arr2[i] - avg2);
     }

   return MathPow(res, 2);

  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double desviacion(double &arr[], double mx)
  {
   int size = ArraySize(arr);

   if(size <= 1)
     {
      Print("desviacion" + ": array size error");
      return(EMPTY_VALUE);
     }
   double sum = 0.0;
   for(int i = 0; i < size; i++)
     {
      sum += MathPow(arr[i] - mx, 2);
     }

   return(sum);
  }


#endif // UsaCSV   


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void PrintText(string name, string message,datetime time, double price)
  {
   string Horizontal_Offset="   ";

   double Vertical_Offset= (Point * scale_factor)/100;
   string objectname = "textabove_" + name;
   ObjectCreate(objectname, OBJ_TEXT, 0, time, price+ Vertical_Offset);
   ObjectSetText(objectname,Horizontal_Offset+message,7,"Arial", clrWhiteSmoke);
   ObjectSetInteger(0,objectname,OBJPROP_ANCHOR,ANCHOR_LEFT_LOWER);

  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CalculaPosNextOrd(int caudillo, int ps_caesar=-1, int ps_alexander=-1, int ps_hannibal=-1)
  {

   intModoPipStep modoPipStep;
   int countTradesCaudillo;
   int pipStep;
   int tipoOperacion;
   double ultimoPrecioOperacionBuy;
   double ultimoPrecioOperacionSell;

   switch(caudillo)
     {
      case  0:
         modoPipStep=CaesarModoPipStep;
         if(ps_caesar<0)
           {
            pipStep=CaesarPipStep;
           }
         else
           {
            pipStep=ps_caesar;
           }
         countTradesCaudillo=countTradesCaesarVar;
         ultimoPrecioOperacionBuy = FindLastBuyPrice_Caesar();
         ultimoPrecioOperacionSell = FindLastSellPrice_Caesar();
         tipoOperacion = tipoOperacionCaudillo(MagicNumber_Caesar);
         break;
      case  1:
         modoPipStep=AlexanderModoPipStep;
         if(ps_alexander<0)
           {
            pipStep=AlexanderPipStep;
           }
         else
           {
            pipStep=ps_alexander;
           }
         countTradesCaudillo=countTradesAlexanderVar;
         ultimoPrecioOperacionBuy = FindLastBuyPrice_Alexander();
         ultimoPrecioOperacionSell = FindLastSellPrice_Alexander();
         tipoOperacion = tipoOperacionCaudillo(MagicNumber_Alexander);
         break;
      case  2:
         modoPipStep=HannibalModoPipStep;
         if(ps_hannibal<0)
           {
            pipStep=HannibalPipStep;
           }
         else
           {
            pipStep=ps_hannibal;
           }
         countTradesCaudillo=countTradesHannibalVar;
         ultimoPrecioOperacionBuy = FindLastBuyPrice_Hannibal();
         ultimoPrecioOperacionSell = FindLastSellPrice_Hannibal();
         tipoOperacion = tipoOperacionCaudillo(MagicNumber_Hannibal);
         break;
      default:
         break;
     }



   double posLinNextOrd=0.00;

   double atrPS = 0;
   if(modoPipStep == PS_Incrementa && countTradesCaudillo > 0)
     {
      double multiPipStep = (pipStep / 2) * (countTradesCaudillo - 1);
      atrPS = 0;
     }
   else
     {
      multiPipStep = 0;
      atrPS = 0;
     }
   if(modoPipStep == PS_Fijo)
     {
      multiPipStep = 0;
      atrPS = 0;
     }
   if(modoPipStep == PS_Variable)
     {
      multiPipStep = 0;
      atrPS = atrValueJ;
     }



   if(tipoOperacion == 0)
     {
      posLinNextOrd=ultimoPrecioOperacionBuy-((multiPipStep + (pipStep + atrPS)) * Point);
     }
   else
     {
      if(tipoOperacion == 1)
        {
         posLinNextOrd=ultimoPrecioOperacionSell+((multiPipStep + (pipStep + atrPS)) * Point);
        }
     }

   return posLinNextOrd;

  }
//+------------------------------------------------------------------+


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CalculaPosReEnt(int caudillo)
  {

   switch(caudillo)
     {
      case  0:
         int nOper=countTradesCaesarVar;
         if(reEntradasCaesar && nOper > 0 && (!puedeCoberturearCaesar || nOper==1))
           {
            if((CaesarOperacionAbiertasSell > 0))
              {
               return (precioOrdenMasBajaCaesar - ((CaesarPipStep * reDistanciaCaesar))*Point);
              }
            else
              {
               if(CaesarOperacionAbiertasBuy > 0)
                 {
                  return (precioOrdenMasAltaCaesar + ((CaesarPipStep * reDistanciaCaesar))*Point);
                 }
              }
           }

         break;
      case  1:
         nOper=countTradesAlexanderVar;
         if(reEntradasAlexander && nOper > 0 && (!puedeCoberturearAlexander || nOper==1))
           {
            if((AlexanderOperacionAbiertasSell > 0))
              {
               return (precioOrdenMasBajaAlexander - ((AlexanderPipStep * reDistanciaAlexander))*Point);
              }
            else
              {
               if(AlexanderOperacionAbiertasBuy > 0)
                 {
                  return (precioOrdenMasAltaAlexander + ((AlexanderPipStep * reDistanciaAlexander))*Point);
                 }
              }
           }

         break;
      case  2:
         nOper=countTradesHannibalVar;
         if(reEntradasHannibal && nOper > 0 && (!puedeCoberturearHannibal || nOper==1))
           {
            if((HannibalOperacionAbiertasSell > 0))
              {
               return (precioOrdenMasBajaHannibal - ((HannibalPipStep * reDistanciaHannibal))*Point);
              }
            else
              {
               if(HannibalOperacionAbiertasBuy > 0)
                 {
                  return (precioOrdenMasAltaHannibal + ((HannibalPipStep * reDistanciaHannibal))*Point);
                 }
              }
           }

         break;
     }



   return(-1);

  }




//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void PintaOperacionesAbiertasManuales()
  {

   static int lastCountTradesCapitan=-1;
   static int lastManuNumOrder=-2;
//if (countTradesCaptainVar!=lastCountTradesCapitan)
//{

   countTradesCaptainVar=CountTrades_CapitanX();


   for(int i=ObjectsTotal(ChartID()); i>=0; i--)
     {
      string name = ObjectName(ChartID(), i);
      //if(robotAcMan.ManuNumOrder != lastManuNumOrder || (countTradesCaptainVar!=lastCountTradesCapitan) )
      //   {
      if(StringSubstr(name,0,5) == "Order")
        {
         ObjectDelete(ChartID(), name);
        }
      if(StringSubstr(name,0,9) == "textabove")
        {
         ObjectDelete(ChartID(), name);
        }
      if(StringSubstr(name,1,9) == "BreakEven")
        {
         ObjectDelete(ChartID(), name);
        }
      //}
      if(name == "TStart_Alexander")
        {
         ObjectDelete(ChartID(), name);
        }
      if(name == "TStart_Hannibal")
        {
         ObjectDelete(ChartID(), name);
        }
      if(name == "TStart_Caesar")
        {
         ObjectDelete(ChartID(), name);
        }
     }

   lastManuNumOrder=robotAcMan.ManuNumOrder;
   lastCountTradesCapitan=countTradesCaptainVar;

   double manualTP=0;
   double manualSL=0;

   bool os=false;
   bool os2=false;
   bool os3=false;

   datetime   lastOpenTime=0;
   string     guardaTickets="";
   int        nextOrderSelected=-1;

   int selec=0;
   int ordTot=OrdersTotal();



   for(int pos = ordTot; pos >= 0; pos--)
     {

      lastOpenTime=0;
      nextOrderSelected=-1;

      for(int pos2 = ordTot-1; pos2 >= 0; pos2--)
        {

         os2 = OrderSelect(pos2, SELECT_BY_POS, MODE_TRADES);
         if(OrderSymbol() != Symbol() || OrderMagicNumber() != 0 || !os2)
            continue;

         if(OrderOpenTime()>lastOpenTime && StringFind(guardaTickets,""+OrderTicket())<0)
           {
            lastOpenTime=OrderOpenTime();
            nextOrderSelected=OrderTicket();
           }
        }


      os3 = OrderSelect(nextOrderSelected, SELECT_BY_TICKET, MODE_TRADES);


      if(nextOrderSelected>=0 && os3)
        {

         guardaTickets+=" "+nextOrderSelected;


         selec+=1;
         if(robotAcMan.ManuNumOrder>0)
           {
            if((countTradesCaptainVar+1) - robotAcMan.ManuNumOrder!=selec)
               continue;
           }


         string ticket = ""+OrderTicket();
         if(StringFind(OrderComment(),"to #")>=0 || StringFind(OrderComment(),"from #")>=0)
           {string coletilla=" P";}
         else
           {coletilla="";}


         if(robotAcMan.ManuOrderLots!=OrderLots())
           {
            robotAcMan.ManuOrderLots=OrderLots();
            //robotAcMan.ManuModificadoPorDLL=0;
           }

         if(OrderType() == OP_SELL)
           {
            if(ObjectFind("Order_Sell_Open_"+ticket)>=0)
              {
               ObjectDelete(ChartID(), "Order_Sell_Open_"+ticket);
              }
            HLineCreate(0, "Order_Sell_Open_"+ticket, 0, OrderOpenPrice(),clrLightGreen,3,1,false,false);
            PrintText(ticket,"#"+ticket+" SELL "+DoubleToString(OrderLots(),2)+coletilla,Time[WindowBarsPerChart()-2],OrderOpenPrice());

            manualTP=OrderTakeProfit();
            if(manualTP>0)
              {
               double objetivoTP=NormalizeDouble(beneficioTPObjetivoSymbolCaudillo(0, nextOrderSelected), 2) / tipoCuentaDouble;
               robotAcMan.ManuObjetivoTP=objetivoTP;

               if(ObjectFind("Order_Sell_TP_"+ticket)>=0)
                 {
                  ObjectDelete(ChartID(), "Order_Sell_TP_"+ticket);
                 }
               HLineCreate(0, "Order_Sell_TP_"+ticket, 0, manualTP,clrGreen,3,1,true,false);
               PrintText(ticket+"TP","                                 OBJETIVO "+DoubleToString(objetivoTP,2)+"   "+DoubleToString(PorcentageDeCantidadSobreBalance(objetivoTP*tipoCuentaDouble),2)+"%",Time[WindowBarsPerChart()-2],manualTP);
              }
            else
              {
               robotAcMan.ManuObjetivoTP=0.00;
              }

            manualSL=OrderStopLoss();
            if(manualSL>0)
              {
               double objetivoSL=NormalizeDouble(beneficioSLObjetivoSymbolCaudillo(0, nextOrderSelected), 2) / tipoCuentaDouble;
               robotAcMan.ManuObjetivoSL=objetivoSL;

               if(ObjectFind("Order_Sell_SL_"+ticket)>=0)
                 {
                  ObjectDelete(ChartID(), "Order_Sell_SL_"+ticket);
                 }
               HLineCreate(0, "Order_Sell_SL_"+ticket, 0, manualSL,clrRed,3,1,true,false);
               PrintText(ticket+"SL","                                 RIESGO "+DoubleToString(objetivoSL,2)+"   "+DoubleToString(PorcentageDeCantidadSobreBalance(objetivoSL*tipoCuentaDouble),2)+"%",Time[WindowBarsPerChart()-2],manualSL);
              }
            else
              {
               robotAcMan.ManuObjetivoSL=0.00;
              }

           }



         if(OrderType() == OP_BUY)
           {
            if(ObjectFind("Order_Buy_Open_"+ticket)>=0)
              {
               ObjectDelete(ChartID(), "Order_Buy_Open_"+ticket);
              }
            HLineCreate(0, "Order_Buy_Open_"+ticket, 0, OrderOpenPrice(),clrLightGreen,3,1,false,false);
            PrintText(ticket,"#"+ticket+" BUY "+DoubleToString(OrderLots(),2)+coletilla,Time[WindowBarsPerChart()-2],OrderOpenPrice());

            manualTP=OrderTakeProfit();
            if(manualTP>0)
              {
               objetivoTP=NormalizeDouble(beneficioTPObjetivoSymbolCaudillo(0, nextOrderSelected), 2) / tipoCuentaDouble;
               robotAcMan.ManuObjetivoTP=objetivoTP;

               if(ObjectFind("Order_Buy_TP_"+ticket)>=0)
                 {
                  ObjectDelete(ChartID(), "Order_Buy_TP_"+ticket);
                 }
               HLineCreate(0, "Order_Buy_TP_"+ticket, 0, manualTP,clrGreen,3,1,true,false);
               PrintText(ticket+"TP","                                 OBJETIVO "+DoubleToString(objetivoTP,2)+"   "+DoubleToString(PorcentageDeCantidadSobreBalance(objetivoTP*tipoCuentaDouble),2)+"%",Time[WindowBarsPerChart()-2],manualTP);
              }
            else
              {
               robotAcMan.ManuObjetivoTP=0.00;
              }

            manualSL=OrderStopLoss();
            if(manualSL>0)
              {
               objetivoSL=NormalizeDouble(beneficioSLObjetivoSymbolCaudillo(0, nextOrderSelected), 2) / tipoCuentaDouble;
               robotAcMan.ManuObjetivoSL=objetivoSL;

               if(ObjectFind("Order_Buy_SL_"+ticket)>=0)
                 {
                  ObjectDelete(ChartID(), "Order_Buy_SL_"+ticket);
                 }
               HLineCreate(0, "Order_Buy_SL_"+ticket, 0, manualSL,clrRed,3,1,true,false);
               PrintText(ticket+"SL","                                 RIESGO "+DoubleToString(objetivoSL,2)+"   "+DoubleToString(PorcentageDeCantidadSobreBalance(objetivoSL*tipoCuentaDouble),2)+"%",Time[WindowBarsPerChart()-2],manualSL);
              }
            else
              {
               robotAcMan.ManuObjetivoSL=0.00;
              }

           }
         robotAcMan.ManuFlotante=OrderProfit();
         robotAcMan.ManuOpType=OrderType();
         if ( MathAbs(OrderSwap())>0.00)
            robotAcMan.ManuSwap=OrderSwap() / tipoCuentaDouble;
            else
            robotAcMan.ManuSwap=0.00;
         if(OrderCommission()>0.00)
            robotAcMan.ManuComi=OrderCommission() / tipoCuentaDouble;
            else
            robotAcMan.ManuComi=0.00;
         if(robotAcMan.ManuNumOrder>0)
           {
            robotAcMan.ManuTicket=OrderTicket();
           }
         else
           {
            robotAcMan.ManuTicket=0;
           }
        }


      //}
     }
   robotAcMan.ManuNumOrderEnEx4 = robotAcMan.ManuNumOrder;
   robotAcMan.ManuOrderPrice = OrderOpenPrice();


   ChartRedraw();
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void PintaOperacionesAbiertasCaesar()
  {

   lastCountTradesAlexander=-1;
   lastCountTradesHannibal=-1;

//if (countTradesCaesarVar!=lastCountTradesCaesar || actualizadoTPCaesar || CaesarPipStep!=lastCaesarPipStep)
//{

   lastCaesarPipStep=CaesarPipStep;

   lastCountTradesCaesar=countTradesCaesarVar;

   actualizadoTPCaesar=false;

   for(int i=ObjectsTotal(ChartID()); i>=0; i--)
     {
      string name = ObjectName(ChartID(), i);
      if(StringSubstr(name,0,5) == "Order")
        {
         ObjectDelete(ChartID(), name);
        }
      if(StringSubstr(name,0,9) == "textabove")
        {
         ObjectDelete(ChartID(), name);
        }
      if(StringSubstr(name,1,9) == "BreakEven")
        {
         ObjectDelete(ChartID(), name);
        }
      if(name == "TStart_Alexander")
        {
         ObjectDelete(ChartID(), name);
        }
      if(name == "TStart_Hannibal")
        {
         ObjectDelete(ChartID(), name);
        }

     }

   bool os=false;
   double posLinNextOrd;

   if(countTradesCaesarVar>0)
     {
      // PINTA SIGUIENTE ORDEN -------------------------------
      posLinNextOrd=CalculaPosNextOrd(0);
      if(countTradesCaesarVar>=MaxTrades_Caesar)
         int colorNext=clrRed;
      else
         colorNext=clrGray;

      HLineCreate(0, "Order_Next", 0, posLinNextOrd,colorNext,2,1,true,false);
      PrintText("Order_Next_Text","NEXT ORDER: "+DoubleToString(posLinNextOrd,Digits()),Time[WindowBarsPerChart()-2],posLinNextOrd);
      //------------------------------------------------------
      //PINTA REENTRADAS -------------------------------------
      double posLinReOrd=CalculaPosReEnt(0);
      if(posLinReOrd>=0)
        {
         HLineCreate(0, "Order_ReEnt", 0, posLinReOrd,colorNext,2,1,true,false);
         PrintText("Order_ReEnt_Text","RE-ENTRY: "+DoubleToString(posLinReOrd,Digits())+"  Lot:"+DoubleToStr(reLotajeCaesar,2),Time[WindowBarsPerChart()-2],posLinReOrd);
        }
      //------------------------------------------------------
      //PINTA BreakEven --------------------------------------
      double BE=CaesarBEPos();
      if(BE>=0)
        {
         HLineCreate(0, "JBreakEven_Line", 0, BE,clrDarkGreen,STYLE_DOT,1,true,false);
         PrintText("JBreakEven_Text","BE: "+DoubleToString(BE,Digits()),Time[WindowBarsPerChart()-2],BE);
        }
      //------------------------------------------------------
     }
   int ot=OrdersTotal() - 1;
   for(int pos = ot; pos >= 0; pos--)
     {
      os = OrderSelect(pos, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol() != Symbol() || OrderMagicNumber() != MagicNumber_Caesar || !os)
         continue;

      string ticket = ""+OrderTicket();
      if(StringFind(OrderComment(),"to #")>=0 || StringFind(OrderComment(),"from #")>=0)
        {string coletilla=" P";}
      else
        {coletilla="";}
      if(OrderType() == OP_SELL)
        {
         HLineCreate(0, "Order_Sell_Open_"+ticket, 0, OrderOpenPrice(),clrLightGreen,3,1,false,false);
         PrintText(ticket,"#"+ticket+" SELL "+DoubleToString(OrderLots(),2)+coletilla,Time[WindowBarsPerChart()-2],OrderOpenPrice());
         HLineCreate(0, "Order_Sell_TP_"+ticket, 0, OrderTakeProfit(),clrRed,3,1,true,false);
         CaesarGlobalTP=OrderTakeProfit();
         PrintText(ticket+"TP","                                    OBJETIVO "+DoubleToString(caesarBenefObjTP / tipoCuentaDouble,2)+"   "+DoubleToString(PorcentageDeCantidadSobreBalance(caesarBenefObjTP / tipoCuentaDouble),2)+"%",Time[WindowBarsPerChart()-2],OrderTakeProfit());

        }
      if(OrderType() == OP_BUY)
        {
         HLineCreate(0, "Order_Buy_Open_"+ticket, 0, OrderOpenPrice(),clrLightGreen,3,1,false,false);
         PrintText(ticket,"#"+ticket+" BUY "+DoubleToString(OrderLots(),2)+coletilla,Time[WindowBarsPerChart()-2],OrderOpenPrice());
         HLineCreate(0, "Order_Buy_TP_"+ticket, 0, OrderTakeProfit(),clrRed,3,1,true,false);
         CaesarGlobalTP=OrderTakeProfit();
         PrintText(ticket+"TP","                                    OBJETIVO "+DoubleToString(caesarBenefObjTP / tipoCuentaDouble,2)+"   "+DoubleToString(PorcentageDeCantidadSobreBalance(caesarBenefObjTP / tipoCuentaDouble),2)+"%",Time[WindowBarsPerChart()-2],OrderTakeProfit());

        }
     }


   ChartRedraw();
//}

  }



//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void PintaOperacionesAbiertasAlexander()
  {

   lastCountTradesCaesar=-1;
   lastCountTradesHannibal=-1;

//if (countTradesAlexanderVar!=lastCountTradesAlexander || actualizadoTPAlexander || AlexanderPipStep!=lastAlexanderPipStep)
//{
   lastCountTradesAlexander=countTradesAlexanderVar;

   lastAlexanderPipStep=AlexanderPipStep;


   actualizadoTPAlexander=false;

   for(int i=ObjectsTotal(ChartID()); i>=0; i--)
     {
      string name = ObjectName(ChartID(), i);
      if(StringSubstr(name,0,5) == "Order")
        {
         ObjectDelete(ChartID(), name);
        }
      if(StringSubstr(name,0,9) == "textabove")
        {
         ObjectDelete(ChartID(), name);
        }
      if(StringSubstr(name,1,9) == "BreakEven")
        {
         ObjectDelete(ChartID(), name);
        }
      if(name == "TStart_Caesar")
        {
         ObjectDelete(ChartID(), name);
        }
      if(name == "TStart_Hannibal")
        {
         ObjectDelete(ChartID(), name);
        }
     }

   bool os=false;

   if(countTradesAlexanderVar>0)
     {
      // PINTA SIGUIENTE ORDEN -------------------------------
      double posLinNextOrd=CalculaPosNextOrd(1);
      if(countTradesAlexanderVar>=MaxTrades_Alexander)
         int colorNext=clrRed;
      else
         colorNext=clrGray;

      HLineCreate(0, "Order_Next", 0, posLinNextOrd,colorNext,2,1,true,false);
      PrintText("Order_Next_Text","NEXT ORDER "+DoubleToString(posLinNextOrd,Digits()),Time[WindowBarsPerChart()-2],posLinNextOrd);

      //PINTA REENTRADAS -------------------------------------
      double posLinReOrd=CalculaPosReEnt(1);
      if(posLinReOrd>=0)
        {
         HLineCreate(0, "Order_ReEnt", 0, posLinReOrd,colorNext,2,1,true,false);
         PrintText("Order_ReEnt_Text","RE-ENTRY: "+DoubleToString(posLinReOrd,Digits())+"  Lot:"+DoubleToStr(reLotajeAlexander,2),Time[WindowBarsPerChart()-2],posLinReOrd);
        }
      //------------------------------------------------------
      //PINTA BreakEven --------------------------------------
      double BE=AlexanderBEPos();
      if(BE>=0)
        {
         HLineCreate(0, "ABreakEven_Line", 0, BE,clrDarkGreen,STYLE_DOT,1,true,false);
         PrintText("ABreakEven_Text","BE: "+DoubleToString(BE,Digits()),Time[WindowBarsPerChart()-2],BE);
        }
      //------------------------------------------------------
     }

   int ot=OrdersTotal() - 1;

   for(int pos = ot; pos >= 0; pos--)
     {
      os = OrderSelect(pos, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol() != Symbol() || OrderMagicNumber() != MagicNumber_Alexander || !os)
         continue;

      string ticket = ""+OrderTicket();
      if(StringFind(OrderComment(),"to #")>=0 || StringFind(OrderComment(),"from #")>=0)
        {string coletilla=" P";}
      else
        {coletilla="";}
      if(OrderType() == OP_SELL)
        {
         HLineCreate(0, "Order_Sell_Open_"+ticket, 0, OrderOpenPrice(),clrLightGreen,3,1,false,false);
         PrintText(ticket,"#"+ticket+" SELL "+DoubleToString(OrderLots(),2)+coletilla,Time[WindowBarsPerChart()-2],OrderOpenPrice());
         HLineCreate(0, "Order_Sell_TP_"+ticket, 0, OrderTakeProfit(),clrRed,3,1,false,false);
         PrintText(ticket+"TP","                                    OBJETIVO "+DoubleToString(alexanderBenefObjTP / tipoCuentaDouble,2)+"   "+DoubleToString(PorcentageDeCantidadSobreBalance(alexanderBenefObjTP / tipoCuentaDouble),2)+"%",Time[WindowBarsPerChart()-2],OrderTakeProfit());

        }
      if(OrderType() == OP_BUY)
        {
         HLineCreate(0, "Order_Buy_Open_"+ticket, 0, OrderOpenPrice(),clrLightGreen,3,1,false,false);
         PrintText(ticket,"#"+ticket+" BUY "+DoubleToString(OrderLots(),2)+coletilla,Time[WindowBarsPerChart()-2],OrderOpenPrice());
         HLineCreate(0, "Order_Buy_TP_"+ticket, 0, OrderTakeProfit(),clrRed,3,1,false,false);
         PrintText(ticket+"TP","                                    OBJETIVO "+DoubleToString(alexanderBenefObjTP / tipoCuentaDouble,2)+"   "+DoubleToString(PorcentageDeCantidadSobreBalance(alexanderBenefObjTP / tipoCuentaDouble),2)+"%",Time[WindowBarsPerChart()-2],OrderTakeProfit());

        }
     }
   ChartRedraw();
//}

  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void PintaOperacionesAbiertasHannibal()
  {

   lastCountTradesAlexander=-1;
   lastCountTradesCaesar=-1;

//if (countTradesHannibalVar!=lastCountTradesHannibal || actualizadoTPHannibal || HannibalPipStep!=lastHannibalPipStep)
//{
   lastCountTradesHannibal=countTradesHannibalVar;


   lastHannibalPipStep=HannibalPipStep;

   actualizadoTPHannibal=false;

   for(int i=ObjectsTotal(ChartID()); i>=0; i--)
     {
      string name = ObjectName(ChartID(), i);
      if(StringSubstr(name,0,5) == "Order")
        {
         ObjectDelete(ChartID(), name);
        }
      if(StringSubstr(name,0,9) == "textabove")
        {
         ObjectDelete(ChartID(), name);
        }
      if(StringSubstr(name,1,9) == "BreakEven")
        {
         ObjectDelete(ChartID(), name);
        }
      if(name == "TStart_Alexander")
        {
         ObjectDelete(ChartID(), name);
        }
      if(name == "TStart_Caesar")
        {
         ObjectDelete(ChartID(), name);
        }
     }

   bool os=false;

   if(countTradesHannibalVar>0)
     {
      // PINTA SIGUIENTE ORDEN -------------------------------
      double posLinNextOrd=CalculaPosNextOrd(2);
      if(countTradesHannibalVar>=MaxTrades_Hannibal)
         int colorNext=clrRed;
      else
         colorNext=clrGray;

      HLineCreate(0, "Order_Next", 0, posLinNextOrd,colorNext,2,1,true,false);
      PrintText("Order_Next_Text","NEXT ORDER "+DoubleToString(posLinNextOrd,Digits()),Time[WindowBarsPerChart()-2],posLinNextOrd);

      //PINTA REENTRADAS -------------------------------------
      double posLinReOrd=CalculaPosReEnt(2);
      if(posLinReOrd>=0)
        {
         HLineCreate(0, "Order_ReEnt", 0, posLinReOrd,colorNext,2,1,true,false);
         PrintText("Order_ReEnt_Text","RE-ENTRY: "+DoubleToString(posLinReOrd,Digits())+"  Lot:"+DoubleToStr(reLotajeHannibal,2),Time[WindowBarsPerChart()-2],posLinReOrd);
        }
      //------------------------------------------------------
      //PINTA BreakEven --------------------------------------
      double BE=HannibalBEPos();
      if(BE>=0)
        {
         HLineCreate(0, "HBreakEven_Line", 0, BE,clrDarkGreen,STYLE_DOT,1,true,false);
         PrintText("HBreakEven_Text","BE: "+DoubleToString(BE,Digits()),Time[WindowBarsPerChart()-2],BE);
        }
      //------------------------------------------------------
     }

   int ot=OrdersTotal() - 1;

   for(int pos = ot; pos >= 0; pos--)
     {
      os = OrderSelect(pos, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol() != Symbol() || OrderMagicNumber() != MagicNumber_Hannibal || !os)
         continue;

      string ticket = ""+OrderTicket();
      if(StringFind(OrderComment(),"to #")>=0 || StringFind(OrderComment(),"from #")>=0)
        {string coletilla=" P";}
      else
        {coletilla="";}
      if(OrderType() == OP_SELL)
        {
         HLineCreate(0, "Order_Sell_Open_"+ticket, 0, OrderOpenPrice(),clrLightGreen,3,1,false,false);
         PrintText(ticket,"#"+ticket+" SELL "+DoubleToString(OrderLots(),2)+coletilla,Time[WindowBarsPerChart()-2],OrderOpenPrice());
         HLineCreate(0, "Order_Sell_TP_"+ticket, 0, OrderTakeProfit(),clrRed,3,1,false,false);
         PrintText(ticket+"TP","                                    OBJETIVO "+DoubleToString(hannibalBenefObjTP / tipoCuentaDouble,2)+"   "+DoubleToString(PorcentageDeCantidadSobreBalance(hannibalBenefObjTP / tipoCuentaDouble),2)+"%",Time[WindowBarsPerChart()-2],OrderTakeProfit());

        }
      if(OrderType() == OP_BUY)
        {
         HLineCreate(0, "Order_Buy_Open_"+ticket, 0, OrderOpenPrice(),clrLightGreen,3,1,false,false);
         PrintText(ticket,"#"+ticket+" BUY "+DoubleToString(OrderLots(),2)+coletilla,Time[WindowBarsPerChart()-2],OrderOpenPrice());
         HLineCreate(0, "Order_Buy_TP_"+ticket, 0, OrderTakeProfit(),clrRed,3,1,false,false);
         PrintText(ticket+"TP","                                    OBJETIVO "+DoubleToString(hannibalBenefObjTP / tipoCuentaDouble,2)+"   "+DoubleToString(PorcentageDeCantidadSobreBalance(hannibalBenefObjTP / tipoCuentaDouble),2)+"%",Time[WindowBarsPerChart()-2],OrderTakeProfit());

        }
     }
   ChartRedraw();
//}

  }




//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OperaLineaJ1()
  {
// if(direcCruceLineaJ1==0)return;
   static bool pintadaLineaJ1;

   if(ObjectFind("DProLineIniJ1") >= 0)
     {
      if(lastColorCruceLineaJ1 != colorCruceLineaJ1)
        {
         ObjectSetInteger(0, "DProLineIniJ1", OBJPROP_COLOR, colorLines[colorCruceLineaJ1]);
         lastColorCruceLineaJ1 = colorCruceLineaJ1;
         ObjectSetInteger(0, "DProLineIniJ1", OBJPROP_WIDTH, 5);
         ChartRedraw();
         Sleep(200);
        }
      else
        {
         ObjectSetInteger(0, "DProLineIniJ1", OBJPROP_WIDTH, 1);
        }


      if(direcCruceLineaJ1 == CL_DibujaInicio)
        {
         ObjectSetInteger(0, "DProLineIniJ1", OBJPROP_SELECTED, true);
         if(!pintadaLineaJ1)
           {
            ObjectSetInteger(0, "DProLineIniJ1", OBJPROP_WIDTH, 5);
            ChartRedraw();
            Sleep(200);
           }
         else
           {
            ObjectSetInteger(0, "DProLineIniJ1", OBJPROP_WIDTH, 1);
           }

         pintadaLineaJ1=true;

        }
      else
        {
         ObjectSetInteger(0, "DProLineIniJ1", OBJPROP_SELECTED, false);
         if(pintadaLineaJ1)
           {
            pintadaLineaJ1=false;
            primeraPosJ1 = 0;
            segundaPosJ1 = 0;
            terceraPosJ1 = 0;
           }
        }

      //        if(countTradesCaesarVar == 0 && direcCruceLineaJ1 > CL_DibujaInicio && CaesarActividad == Encendido) // && CaesarTipoOperacion == Tipo_Manual
      if(direcCruceLineaJ1 > CL_DibujaInicio) // && CaesarTipoOperacion == Tipo_Manual
        {
         double DProLineOpenJ1 = ObjectGetValueByShift("DProLineIniJ1", 0);


         // ELIGE CON QUÉ PRECIO (bid-ask DEBE ACTIVARSE LA LÍNEA. ========
         if(direcCruceLineaJ1 == CL_ActivaBUY) //BUY
           {
            nivelDeActivacionJ = Ask;
           }
         if(direcCruceLineaJ1 == CL_ActivaSELL) //SELL
           {
            nivelDeActivacionJ = Bid;
           }
         if(direcCruceLineaJ1 == CL_ActivaOtra) //+ Orden
           {
            if(CaesarOperacionAbiertasBuy>CaesarOperacionAbiertasSell)
              {nivelDeActivacionJ = Ask;}
            else {nivelDeActivacionJ = Bid;}
           }
         //=================================================================


         if(nivelDeActivacionJ > DProLineOpenJ1)
           {

            if(primeraPosJ1 == 0)
              {
               primeraPosJ1 = 1; // 1= por encima
              }
            else
              {
               if(segundaPosJ1 == 0 && primeraPosJ1 != 1)
                 {
                  segundaPosJ1 = 1; // 1= por encima
                 }
               else
                 {
                  if(terceraPosJ1 == 0 && segundaPosJ1 != 1 && segundaPosJ1 != 0)
                    {
                     terceraPosJ1 = 1; // 1= por encima
                    }
                 }
              }

           }
         if(nivelDeActivacionJ < DProLineOpenJ1)
           {

            if(primeraPosJ1 == 0)
              {
               primeraPosJ1 = 2; // 2 = por debajo
              }
            else
              {
               if(segundaPosJ1 == 0 && primeraPosJ1 != 2)
                 {
                  segundaPosJ1 = 2; // 2 = por debajo
                 }
               else
                 {
                  if(terceraPosJ1 == 0 && segundaPosJ1 != 2 && segundaPosJ1 != 0)
                    {
                     terceraPosJ1 = 2; // 2 = por debajo
                    }
                 }
              }
           }


         //Comment(primeraPosJ1, " ", segundaPosJ1, " ", terceraPosJ1);




         //tipoCruceLinea:  0 = cruza al alza
         if(tipoCruceLineaJ1 == 0)
           {
            if((primeraPosJ1 == 2 && segundaPosJ1 == 1))
              {
               if(direcCruceLineaJ1 == CL_ActivaBUY) //BUY
                 {
                  CaesarOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaJ1 == CL_ActivaSELL) //SELL
                 {
                  CaesarOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaJ1 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinCaesar = 1;
                  direcCruceLineaJ1=CL_DibujaInicio;
                  robotReturn.direcCruceLineaJ1=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;

                 }
               primeraPosJ1 = 0;
               segundaPosJ1 = 0;
               terceraPosJ1 = 0;
              }
            else
              {
               if(primeraPosJ1 != 2)
                 {
                  primeraPosJ1 = 0;
                  segundaPosJ1 = 0;
                  terceraPosJ1 = 0;
                 }
              }
           }


         //tipoCruceLinea:  1 = cruza a la baja
         if(tipoCruceLineaJ1 == 1)
           {
            if(primeraPosJ1 == 1 && segundaPosJ1 == 2)
              {
               if(direcCruceLineaJ1 == CL_ActivaBUY) //BUY
                 {
                  CaesarOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaJ1 == CL_ActivaSELL) //SELL
                 {
                  CaesarOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaJ1 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinCaesar = 1;
                  direcCruceLineaJ1=CL_DibujaInicio;
                  robotReturn.direcCruceLineaJ1=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                 }
               primeraPosJ1 = 0;
               segundaPosJ1 = 0;
               terceraPosJ1 = 0;
              }
            else
              {
               if(primeraPosJ1 != 1)
                 {
                  primeraPosJ1 = 0;
                  segundaPosJ1 = 0;
                  terceraPosJ1 = 0;
                 }
              }
           }


         //tipoCruceLinea:  2 = cruza al alza y despues a la baja
         if(tipoCruceLineaJ1 == 2)
           {
            if(primeraPosJ1 == 2 && segundaPosJ1 == 1 && terceraPosJ1 == 2)
              {
               if(direcCruceLineaJ1 == CL_ActivaBUY) //BUY
                 {
                  CaesarOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaJ1 == CL_ActivaSELL) //SELL
                 {
                  CaesarOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaJ1 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinCaesar = 1;
                  direcCruceLineaJ1=CL_DibujaInicio;
                  robotReturn.direcCruceLineaJ1=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                 }
               primeraPosJ1 = 0;
               segundaPosJ1 = 0;
               terceraPosJ1 = 0;
              }
            else
              {
               if(primeraPosJ1 != 2)
                 {
                  primeraPosJ1 = 0;
                  segundaPosJ1 = 0;
                  terceraPosJ1 = 0;
                 }
              }
           }


         //tipoCruceLinea:  3 = cruza a la baja y despues al alza
         if(tipoCruceLineaJ1 == 3)
           {
            if(primeraPosJ1 == 1 && segundaPosJ1 == 2 && terceraPosJ1 == 1)
              {
               if(direcCruceLineaJ1 == CL_ActivaBUY) //BUY
                 {
                  CaesarOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaJ1 == CL_ActivaSELL) //SELL
                 {
                  CaesarOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaJ1 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinCaesar = 1;
                  direcCruceLineaJ1=CL_DibujaInicio;
                  robotReturn.direcCruceLineaJ1=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                 }
               primeraPosJ1 = 0;
               segundaPosJ1 = 0;
               terceraPosJ1 = 0;
              }
            else
              {
               if(primeraPosJ1 != 1)
                 {
                  primeraPosJ1 = 0;
                  segundaPosJ1 = 0;
                  terceraPosJ1 = 0;
                 }
              }
           }
        }
      else
        {
         primeraPosJ1 = 0;
         segundaPosJ1 = 0;
         terceraPosJ1 = 0;



         if(direcCruceLineaJ1 == CL_NoInicio)
           {
            ObjectDelete("DProLineIniJ1");
           }
        }
     }
   else
     {
      if(direcCruceLineaJ1 > CL_NoInicio)
        {
         // PINTA NUEVA LINEA
         int cantiBars = WindowBarsPerChart() / 2;
         double precioCentro = WindowPriceMax(0) - ((WindowPriceMax(0) - WindowPriceMin(0)) / 5.0);
         if(mousePrice!=0)
            precioCentro=mousePrice;

         TrendCreate(0, "DProLineIniJ1", 0, iTime(NULL, PERIOD_CURRENT, cantiBars), precioCentro, iTime(NULL, PERIOD_CURRENT, 0), precioCentro, colorLines[colorCruceLineaJ1]);
         lastColorCruceLineaJ1 = colorCruceLineaJ1;
         primeraPosJ1 = 0;
         segundaPosJ1 = 0;
         terceraPosJ1 = 0;
         EmptyClipboard();
         //---
        }
     }
  }




//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OperaLineaStopJ1()
  {
// if(direcCruceLineaJ1==0)return;
   static bool pintadaLineaStopJ1;
   if(ObjectFind("DProStopLineJ1") >= 0)
     {
      if(stopCruceLineaJ1 == CL_NoStop)
        {
         ObjectDelete("DProStopLineJ1");
         pintadaLineaStopJ1=false;
         return;
        }

      if(lastColorStopCruceLineaJ1 != colorStopCruceLineaJ1)
        {
         ObjectSetInteger(0, "DProStopLineJ1", OBJPROP_COLOR, colorLines[colorStopCruceLineaJ1]);
         lastColorStopCruceLineaJ1 = colorStopCruceLineaJ1;
         ObjectSetInteger(0, "DProStopLineJ1", OBJPROP_WIDTH, 5);
         ChartRedraw();
         Sleep(200);
        }
      else
        {
         ObjectSetInteger(0, "DProStopLineJ1", OBJPROP_WIDTH, 1);
        }
      if(stopCruceLineaJ1 == CL_DibujaStop)
        {
         ObjectSetInteger(0, "DProStopLineJ1", OBJPROP_SELECTED, true);
         if(!pintadaLineaStopJ1)
           {
            ObjectSetInteger(0, "DProStopLineJ1", OBJPROP_WIDTH, 5);
            ChartRedraw();
            Sleep(200);
           }
         else
           {
            ObjectSetInteger(0, "DProStopLineJ1", OBJPROP_WIDTH, 1);
           }

         pintadaLineaStopJ1=true;
        }
      else
        {
         ObjectSetInteger(0, "DProStopLineJ1", OBJPROP_SELECTED, false);
         if(pintadaLineaStopJ1)
           {
            pintadaLineaStopJ1=false;
            primeraPosStopJ1 = 0;
            segundaPosStopJ1 = 0;
            terceraPosStopJ1 = 0;
           }
        }



      if(countTradesCaesarVar > 0)
        {
         double DProLineStopJ1 = ObjectGetValueByShift("DProStopLineJ1", 0);

         if(CaesarOperacionAbiertasBuy>=CaesarOperacionAbiertasSell)
           {nivelDeActivacionJ = Bid;}
         else {nivelDeActivacionJ = Ask;}


         if(nivelDeActivacionJ > DProLineStopJ1)
           {

            if(primeraPosStopJ1 == 0)
              {
               primeraPosStopJ1 = 1; // 1= por encima
              }
            else
              {
               if(segundaPosStopJ1 == 0 && primeraPosStopJ1 != 1)
                 {
                  segundaPosStopJ1 = 1; // 1= por encima
                 }
               else
                 {
                  if(terceraPosStopJ1 == 0 && segundaPosStopJ1 != 1 && segundaPosStopJ1 != 0)
                    {
                     terceraPosStopJ1 = 1; // 1= por encima
                    }
                 }
              }

           }
         if(nivelDeActivacionJ < DProLineStopJ1)
           {

            if(primeraPosStopJ1 == 0)
              {
               primeraPosStopJ1 = 2; // 2 = por debajo
              }
            else
              {
               if(segundaPosStopJ1 == 0 && primeraPosStopJ1 != 2)
                 {
                  segundaPosStopJ1 = 2; // 2 = por debajo
                 }
               else
                 {
                  if(terceraPosStopJ1 == 0 && segundaPosStopJ1 != 2 && segundaPosStopJ1 != 0)
                    {
                     terceraPosStopJ1 = 2; // 2 = por debajo
                    }
                 }
              }
           }


         //Comment(primeraPosStopJ1, " ", segundaPosStopJ1, " ", terceraPosStopJ1);



         //tipoCruceLinea:  0 = cruza al alza
         if(stopCruceLineaJ1 == CL_ActivaCloseBuys)
           {
            if((primeraPosStopJ1 == 2 && segundaPosStopJ1 == 1) || (primeraPosStopJ1 == 1 && segundaPosStopJ1 == 2))
              {
               cerrarTodasOperacionesCiclo(MagicNumber_Caesar, OP_BUY);
               primeraPosStopJ1 = 0;
               segundaPosStopJ1 = 0;
               terceraPosStopJ1 = 0;
              }
           }
         if(stopCruceLineaJ1 == CL_ActivaCloseSells)
           {
            if((primeraPosStopJ1 == 2 && segundaPosStopJ1 == 1) || (primeraPosStopJ1 == 1 && segundaPosStopJ1 == 2))
              {
               cerrarTodasOperacionesCiclo(MagicNumber_Caesar, OP_SELL);
               primeraPosStopJ1 = 0;
               segundaPosStopJ1 = 0;
               terceraPosStopJ1 = 0;
              }
           }
         if(stopCruceLineaJ1 == CL_ActivaCloseAll)
           {
            if((primeraPosStopJ1 == 2 && segundaPosStopJ1 == 1) || (primeraPosStopJ1 == 1 && segundaPosStopJ1 == 2))
              {
               cerrarTodasOperacionesCiclo(MagicNumber_Caesar);
               primeraPosStopJ1 = 0;
               segundaPosStopJ1 = 0;
               terceraPosStopJ1 = 0;
              }
           }
         if(stopCruceLineaJ1 == CL_ActivaAlta)// && CaesarTipoOperacion == Tipo_Manual
           {
            if((primeraPosStopJ1 == 2 && segundaPosStopJ1 == 1) || (primeraPosStopJ1 == 1 && segundaPosStopJ1 == 2))
              {
               cerrarTicket(ticketOrdenMasAltaCaesar);
               primeraPosStopJ1 = 0;
               segundaPosStopJ1 = 0;
               terceraPosStopJ1 = 0;
               stopCruceLineaJ1=CL_DibujaStop;
               robotReturn.stopCruceLineaJ1=CL_DibujaStop;
               robotSend.actualizarGadgets=1;
               pintadaLineaStopJ1=true;
               retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
              }
           }
         if(stopCruceLineaJ1 == CL_ActivaBaja)// && CaesarTipoOperacion == Tipo_Manual
           {
            if((primeraPosStopJ1 == 2 && segundaPosStopJ1 == 1) || (primeraPosStopJ1 == 1 && segundaPosStopJ1 == 2))
              {
               cerrarTicket(ticketOrdenMasBajaCaesar);
               primeraPosStopJ1 = 0;
               segundaPosStopJ1 = 0;
               terceraPosStopJ1 = 0;
               stopCruceLineaJ1=CL_DibujaStop;
               robotReturn.stopCruceLineaJ1=CL_DibujaStop;
               robotSend.actualizarGadgets=1;
               pintadaLineaStopJ1=true;
               retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
              }
           }
        }
     }
   else
     {
      if(stopCruceLineaJ1 > CL_NoStop)
        {
         // PINTA NUEVA LINEA
         int cantiBars = WindowBarsPerChart() / 2;
         double BE=CaesarBEPos();
         if(BE>=0.00)
           {
            double precioCentro = BE;
           }
         else
           {
            precioCentro = WindowPriceMax(0) - ((WindowPriceMax(0) - WindowPriceMin(0)) / 5.0) * 2.0;
           }

         if(mousePrice!=0)
            precioCentro=mousePrice;

         TrendCreate(0, "DProStopLineJ1", 0, iTime(NULL, PERIOD_CURRENT, cantiBars), precioCentro, iTime(NULL, PERIOD_CURRENT, 0), precioCentro, colorLines[colorStopCruceLineaJ1], STYLE_DASH);
         lastColorStopCruceLineaJ1 = colorStopCruceLineaJ1;
         primeraPosStopJ1 = 0;
         segundaPosStopJ1 = 0;
         terceraPosStopJ1 = 0;
         EmptyClipboard();

        }
     }
  }





//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OperaLineaJ2()
  {
// if(direcCruceLineaJ2==0)return;
   static bool pintadaLineaJ2;

   if(ObjectFind("DProLineIniJ2") >= 0)
     {
      if(lastColorCruceLineaJ2 != colorCruceLineaJ2)
        {
         ObjectSetInteger(0, "DProLineIniJ2", OBJPROP_COLOR, colorLines[colorCruceLineaJ2]);
         lastColorCruceLineaJ2 = colorCruceLineaJ2;
         ObjectSetInteger(0, "DProLineIniJ2", OBJPROP_WIDTH, 5);
         ChartRedraw();
         Sleep(200);
        }
      else
        {
         ObjectSetInteger(0, "DProLineIniJ2", OBJPROP_WIDTH, 1);
        }

      if(direcCruceLineaJ2 == CL_DibujaInicio)
        {
         ObjectSetInteger(0, "DProLineIniJ2", OBJPROP_SELECTED, true);
         if(!pintadaLineaJ2)
           {
            ObjectSetInteger(0, "DProLineIniJ2", OBJPROP_WIDTH, 5);
            ChartRedraw();
            Sleep(200);
           }
         else
           {
            ObjectSetInteger(0, "DProLineIniJ2", OBJPROP_WIDTH, 1);
           }
         pintadaLineaJ2=true;
        }
      else
        {
         ObjectSetInteger(0, "DProLineIniJ2", OBJPROP_SELECTED, false);
         if(pintadaLineaJ2)
           {
            pintadaLineaJ2=false;
            primeraPosJ2 = 0;
            segundaPosJ2 = 0;
            terceraPosJ2 = 0;
           }
        }

      //        if(countTradesCaesarVar == 0 && direcCruceLineaJ2 > CL_DibujaInicio && CaesarActividad == Encendido) // && CaesarTipoOperacion == Tipo_Manual
      if(direcCruceLineaJ2 > CL_DibujaInicio) // && CaesarTipoOperacion == Tipo_Manual
        {
         double DProLineOpenJ2 = ObjectGetValueByShift("DProLineIniJ2", 0);


         // ELIGE CONQ UÉ PRECIO (bid-ask DEBE ACTIVARSE LA LÍNEA. ========
         if(direcCruceLineaJ2 == CL_ActivaBUY) //BUY
           {
            nivelDeActivacionJ = Ask;
           }
         if(direcCruceLineaJ2 == CL_ActivaSELL) //SELL
           {
            nivelDeActivacionJ = Bid;
           }
         if(direcCruceLineaJ2 == CL_ActivaOtra) //+ Orden
           {
            if(CaesarOperacionAbiertasBuy>CaesarOperacionAbiertasSell)
              {nivelDeActivacionJ = Ask;}
            else {nivelDeActivacionJ = Bid;}
           }
         //=================================================================



         if(nivelDeActivacionJ > DProLineOpenJ2)
           {

            if(primeraPosJ2 == 0)
              {
               primeraPosJ2 = 1; // 1= por encima
              }
            else
              {
               if(segundaPosJ2 == 0 && primeraPosJ2 != 1)
                 {
                  segundaPosJ2 = 1; // 1= por encima
                 }
               else
                 {
                  if(terceraPosJ2 == 0 && segundaPosJ2 != 1 && segundaPosJ2 != 0)
                    {
                     terceraPosJ2 = 1; // 1= por encima
                    }
                 }
              }

           }
         if(nivelDeActivacionJ < DProLineOpenJ2)
           {

            if(primeraPosJ2 == 0)
              {
               primeraPosJ2 = 2; // 2 = por debajo
              }
            else
              {
               if(segundaPosJ2 == 0 && primeraPosJ2 != 2)
                 {
                  segundaPosJ2 = 2; // 2 = por debajo
                 }
               else
                 {
                  if(terceraPosJ2 == 0 && segundaPosJ2 != 2 && segundaPosJ2 != 0)
                    {
                     terceraPosJ2 = 2; // 2 = por debajo
                    }
                 }
              }
           }


         //Comment(primeraPosJ2, " ", segundaPosJ2, " ", terceraPosJ2);



         //tipoCruceLinea:  0 = cruza al alza
         if(tipoCruceLineaJ2 == 0)
           {
            if((primeraPosJ2 == 2 && segundaPosJ2 == 1))
              {
               if(direcCruceLineaJ2 == CL_ActivaBUY) //BUY
                 {
                  CaesarOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaJ2 == CL_ActivaSELL) //SELL
                 {
                  CaesarOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaJ2 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinCaesar = 1;
                  direcCruceLineaJ2=CL_DibujaInicio;
                  robotReturn.direcCruceLineaJ2=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                 }
               primeraPosJ2 = 0;
               segundaPosJ2 = 0;
               terceraPosJ2 = 0;
              }
            else
              {
               if(primeraPosJ2 != 2)
                 {
                  primeraPosJ2 = 0;
                  segundaPosJ2 = 0;
                  terceraPosJ2 = 0;
                 }
              }
           }

         //tipoCruceLinea:  1 = cruza a la baja
         if(tipoCruceLineaJ2 == 1)
           {
            if(primeraPosJ2 == 1 && segundaPosJ2 == 2)
              {
               if(direcCruceLineaJ2 == CL_ActivaBUY) //BUY
                 {
                  CaesarOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaJ2 == CL_ActivaSELL) //SELL
                 {
                  CaesarOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaJ2 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinCaesar = 1;
                  direcCruceLineaJ2=CL_DibujaInicio;
                  robotReturn.direcCruceLineaJ2=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                 }
               primeraPosJ2 = 0;
               segundaPosJ2 = 0;
               terceraPosJ2 = 0;
              }
            else
              {
               if(primeraPosJ2 != 1)
                 {
                  primeraPosJ2 = 0;
                  segundaPosJ2 = 0;
                  terceraPosJ2 = 0;
                 }
              }
           }


         //tipoCruceLinea:  2 = cruza al alza y despues a la baja
         if(tipoCruceLineaJ2 == 2)
           {
            if(primeraPosJ2 == 2 && segundaPosJ2 == 1 && terceraPosJ2 == 2)
              {
               if(direcCruceLineaJ2 == CL_ActivaBUY) //BUY
                 {
                  CaesarOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaJ2 == CL_ActivaSELL) //SELL
                 {
                  CaesarOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaJ2 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinCaesar = 1;
                  direcCruceLineaJ2=CL_DibujaInicio;
                  robotReturn.direcCruceLineaJ2=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                 }
               primeraPosJ2 = 0;
               segundaPosJ2 = 0;
               terceraPosJ2 = 0;
              }
            else
              {
               if(primeraPosJ2 != 2)
                 {
                  primeraPosJ2 = 0;
                  segundaPosJ2 = 0;
                  terceraPosJ2 = 0;
                 }
              }
           }
         //tipoCruceLinea:  3 = cruza a la baja y despues al alza
         if(tipoCruceLineaJ2 == 3)
           {
            if(primeraPosJ2 == 1 && segundaPosJ2 == 2 && terceraPosJ2 == 1)
              {
               if(direcCruceLineaJ2 == CL_ActivaBUY) //BUY
                 {
                  CaesarOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaJ2 == CL_ActivaSELL) //SELL
                 {
                  CaesarOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaJ2 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinCaesar = 1;
                  direcCruceLineaJ2=CL_DibujaInicio;
                  robotReturn.direcCruceLineaJ2=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                 }
               primeraPosJ2 = 0;
               segundaPosJ2 = 0;
               terceraPosJ2 = 0;
              }
            else
              {
               if(primeraPosJ2 != 1)
                 {
                  primeraPosJ2 = 0;
                  segundaPosJ2 = 0;
                  terceraPosJ2 = 0;
                 }
              }
           }
        }
      else
        {
         primeraPosJ2 = 0;
         segundaPosJ2 = 0;
         terceraPosJ2 = 0;
         if(direcCruceLineaJ2 == CL_NoInicio)
           {
            ObjectDelete("DProLineIniJ2");
           }
        }
     }
   else
     {
      if(direcCruceLineaJ2 > CL_NoInicio)
        {
         // PINTA NUEVA LINEA
         int cantiBars = WindowBarsPerChart() / 2;
         double precioCentro = WindowPriceMax(0) - ((WindowPriceMax(0) - WindowPriceMin(0)) / 5.0) * 3;

         if(mousePrice!=0)
            precioCentro=mousePrice;

         TrendCreate(0, "DProLineIniJ2", 0, iTime(NULL, PERIOD_CURRENT, cantiBars), precioCentro, iTime(NULL, PERIOD_CURRENT, 0), precioCentro, colorLines[colorCruceLineaJ2]);
         lastColorCruceLineaJ2 = colorCruceLineaJ2;
         primeraPosJ2 = 0;
         segundaPosJ2 = 0;
         terceraPosJ2 = 0;
         EmptyClipboard();

        }
     }
  }




//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OperaLineaStopJ2()
  {
// if(direcCruceLineaJ2==0)return;
   static bool pintadaLineaStopJ2;

   if(ObjectFind("DProStopLineJ2") >= 0)
     {
      if(stopCruceLineaJ2 == CL_NoStop)
        {
         ObjectDelete("DProStopLineJ2");
         pintadaLineaStopJ2=false;
         return;
        }

      if(lastColorStopCruceLineaJ2 != colorStopCruceLineaJ2)
        {
         ObjectSetInteger(0, "DProStopLineJ2", OBJPROP_COLOR, colorLines[colorStopCruceLineaJ2]);
         lastColorStopCruceLineaJ2 = colorStopCruceLineaJ2;
         ObjectSetInteger(0, "DProStopLineJ2", OBJPROP_WIDTH, 5);
         ChartRedraw();
         Sleep(200);
        }
      else
        {
         ObjectSetInteger(0, "DProStopLineJ2", OBJPROP_WIDTH, 1);
        }
      if(stopCruceLineaJ2 == CL_DibujaStop)
        {
         ObjectSetInteger(0, "DProStopLineJ2", OBJPROP_SELECTED, true);
         if(!pintadaLineaStopJ2)
           {
            ObjectSetInteger(0, "DProStopLineJ2", OBJPROP_WIDTH, 5);
            ChartRedraw();
            Sleep(200);
           }
         else
           {
            ObjectSetInteger(0, "DProStopLineJ2", OBJPROP_WIDTH, 1);
           }
         pintadaLineaStopJ2=true;
        }
      else
        {
         ObjectSetInteger(0, "DProStopLineJ2", OBJPROP_SELECTED, false);
         if(pintadaLineaStopJ2)
           {
            pintadaLineaStopJ2=false;
            primeraPosStopJ2 = 0;
            segundaPosStopJ2 = 0;
            terceraPosStopJ2 = 0;
           }
        }


      if(countTradesCaesarVar > 0)
        {
         double DProLineStopJ2 = ObjectGetValueByShift("DProStopLineJ2", 0);

         if(CaesarOperacionAbiertasBuy>=CaesarOperacionAbiertasSell)
           {nivelDeActivacionJ = Bid;}
         else {nivelDeActivacionJ = Ask;}

         if(nivelDeActivacionJ > DProLineStopJ2)
           {

            if(primeraPosStopJ2 == 0)
              {
               primeraPosStopJ2 = 1; // 1= por encima
              }
            else
              {
               if(segundaPosStopJ2 == 0 && primeraPosStopJ2 != 1)
                 {
                  segundaPosStopJ2 = 1; // 1= por encima
                 }
               else
                 {
                  if(terceraPosStopJ2 == 0 && segundaPosStopJ2 != 1 && segundaPosStopJ2 != 0)
                    {
                     terceraPosStopJ2 = 1; // 1= por encima
                    }
                 }
              }

           }
         if(nivelDeActivacionJ < DProLineStopJ2)
           {

            if(primeraPosStopJ2 == 0)
              {
               primeraPosStopJ2 = 2; // 2 = por debajo
              }
            else
              {
               if(segundaPosStopJ2 == 0 && primeraPosStopJ2 != 2)
                 {
                  segundaPosStopJ2 = 2; // 2 = por debajo
                 }
               else
                 {
                  if(terceraPosStopJ2 == 0 && segundaPosStopJ2 != 2 && segundaPosStopJ2 != 0)
                    {
                     terceraPosStopJ2 = 2; // 2 = por debajo
                    }
                 }
              }
           }


         //Comment(primeraPosStopJ2, " ", segundaPosStopJ2, " ", terceraPosStopJ2);



         //tipoCruceLinea:  0 = cruza al alza
         if(stopCruceLineaJ2 == CL_ActivaCloseBuys)
           {
            if((primeraPosStopJ2 == 2 && segundaPosStopJ2 == 1) || (primeraPosStopJ2 == 1 && segundaPosStopJ2 == 2))
              {
               cerrarTodasOperacionesCiclo(MagicNumber_Caesar, OP_BUY);
               primeraPosStopJ2 = 0;
               segundaPosStopJ2 = 0;
               terceraPosStopJ2 = 0;
              }
           }
         if(stopCruceLineaJ2 == CL_ActivaCloseSells)// && CaesarTipoOperacion == Tipo_Manual
           {
            if((primeraPosStopJ2 == 2 && segundaPosStopJ2 == 1) || (primeraPosStopJ2 == 1 && segundaPosStopJ2 == 2))
              {
               cerrarTodasOperacionesCiclo(MagicNumber_Caesar, OP_SELL);
               primeraPosStopJ2 = 0;
               segundaPosStopJ2 = 0;
               terceraPosStopJ2 = 0;
              }
           }
         if(stopCruceLineaJ2 == CL_ActivaCloseAll)// && CaesarTipoOperacion == Tipo_Manual
           {
            if((primeraPosStopJ2 == 2 && segundaPosStopJ2 == 1) || (primeraPosStopJ2 == 1 && segundaPosStopJ2 == 2))
              {
               cerrarTodasOperacionesCiclo(MagicNumber_Caesar);
               primeraPosStopJ2 = 0;
               segundaPosStopJ2 = 0;
               terceraPosStopJ2 = 0;
              }
           }
         if(stopCruceLineaJ2 == CL_ActivaAlta)// && CaesarTipoOperacion == Tipo_Manual
           {
            if((primeraPosStopJ2 == 2 && segundaPosStopJ2 == 1) || (primeraPosStopJ2 == 1 && segundaPosStopJ2 == 2))
              {
               cerrarTicket(ticketOrdenMasAltaCaesar);
               primeraPosStopJ2 = 0;
               segundaPosStopJ2 = 0;
               terceraPosStopJ2 = 0;
               stopCruceLineaJ2=CL_DibujaStop;
               robotReturn.stopCruceLineaJ2=CL_DibujaStop;
               robotSend.actualizarGadgets=1;
               retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());

              }
           }
         if(stopCruceLineaJ2 == CL_ActivaBaja)// && CaesarTipoOperacion == Tipo_Manual
           {
            if((primeraPosStopJ2 == 2 && segundaPosStopJ2 == 1) || (primeraPosStopJ2 == 1 && segundaPosStopJ2 == 2))
              {
               cerrarTicket(ticketOrdenMasBajaCaesar);
               primeraPosStopJ2 = 0;
               segundaPosStopJ2 = 0;
               terceraPosStopJ2 = 0;
               stopCruceLineaJ2=CL_DibujaStop;
               robotReturn.stopCruceLineaJ2=CL_DibujaStop;
               robotSend.actualizarGadgets=1;
               retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());

              }
           }

        }
     }
   else
     {
      if(stopCruceLineaJ2 > CL_NoStop)
        {
         // PINTA NUEVA LINEA STOP 2
         int cantiBars = WindowBarsPerChart() / 2;

         double BE=CaesarBEPos();
         if(BE>=0.00)
           {
            double precioCentro = BE;
           }
         else
           {
            precioCentro = WindowPriceMax(0) - ((WindowPriceMax(0) - WindowPriceMin(0)) / 5.0) * 4.0;
           }

         if(mousePrice!=0)
            precioCentro=mousePrice;

         TrendCreate(0, "DProStopLineJ2", 0, iTime(NULL, PERIOD_CURRENT, cantiBars), precioCentro, iTime(NULL, PERIOD_CURRENT, 0), precioCentro, colorLines[colorStopCruceLineaJ2], STYLE_DASH);
         lastColorStopCruceLineaJ2 = colorStopCruceLineaJ2;
         primeraPosStopJ2 = 0;
         segundaPosStopJ2 = 0;
         terceraPosStopJ2 = 0;
         EmptyClipboard();

        }
     }
  }













//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OperaLineaA1()
  {
// if(direcCruceLineaA1==0)return;
   static bool pintadaLineaA1;

   if(ObjectFind("DProLineIniA1") >= 0)
     {
      if(lastColorCruceLineaA1 != colorCruceLineaA1)
        {
         ObjectSetInteger(0, "DProLineIniA1", OBJPROP_COLOR, colorLines[colorCruceLineaA1]);
         lastColorCruceLineaA1 = colorCruceLineaA1;
         ObjectSetInteger(0, "DProLineIniA1", OBJPROP_WIDTH, 5);
         ChartRedraw();
         Sleep(200);
        }
      else
        {
         ObjectSetInteger(0, "DProLineIniA1", OBJPROP_WIDTH, 1);
        }
      if(direcCruceLineaA1 == CL_DibujaInicio)
        {
         ObjectSetInteger(0, "DProLineIniA1", OBJPROP_SELECTED, true);
         if(!pintadaLineaA1)
           {
            ObjectSetInteger(0, "DProLineIniA1", OBJPROP_WIDTH, 5);
            ChartRedraw();
            Sleep(200);
           }
         else
           {
            ObjectSetInteger(0, "DProLineIniA1", OBJPROP_WIDTH, 1);
           }
         pintadaLineaA1=true;
        }
      else
        {
         ObjectSetInteger(0, "DProLineIniA1", OBJPROP_SELECTED, false);
         if(pintadaLineaA1)
           {
            pintadaLineaA1=false;
            primeraPosA1 = 0;
            segundaPosA1 = 0;
            terceraPosA1 = 0;
           }
        }

      //        if(countTradesAlexanderVar == 0 && direcCruceLineaA1 > 1 && AlexanderActividad == Encendido) // && AlexanderTipoOperacion == Tipo_Manual
      if(direcCruceLineaA1 > 1) // && AlexanderTipoOperacion == Tipo_Manual
        {
         double DProLineOpenA1 = ObjectGetValueByShift("DProLineIniA1", 0);


         // ELIGE CONQ UÉ PRECIO (bid-ask DEBE ACTIVARSE LA LÍNEA. ========
         if(direcCruceLineaA1 == CL_ActivaBUY) //BUY
           {
            nivelDeActivacionA = Ask;
           }
         if(direcCruceLineaA1 == CL_ActivaSELL) //SELL
           {
            nivelDeActivacionA = Bid;
           }
         if(direcCruceLineaA1 == CL_ActivaOtra) //+ Orden
           {
            if(AlexanderOperacionAbiertasBuy>AlexanderOperacionAbiertasSell)
              {nivelDeActivacionA = Ask;}
            else {nivelDeActivacionA = Bid;}
           }
         //=================================================================



         if(nivelDeActivacionA > DProLineOpenA1)
           {

            if(primeraPosA1 == 0)
              {
               primeraPosA1 = 1; // 1= por encima
              }
            else
              {
               if(segundaPosA1 == 0 && primeraPosA1 != 1)
                 {
                  segundaPosA1 = 1; // 1= por encima
                 }
               else
                 {
                  if(terceraPosA1 == 0 && segundaPosA1 != 1 && segundaPosA1 != 0)
                    {
                     terceraPosA1 = 1; // 1= por encima
                    }
                 }
              }

           }
         if(nivelDeActivacionA < DProLineOpenA1)
           {

            if(primeraPosA1 == 0)
              {
               primeraPosA1 = 2; // 2 = por debajo
              }
            else
              {
               if(segundaPosA1 == 0 && primeraPosA1 != 2)
                 {
                  segundaPosA1 = 2; // 2 = por debajo
                 }
               else
                 {
                  if(terceraPosA1 == 0 && segundaPosA1 != 2 && segundaPosA1 != 0)
                    {
                     terceraPosA1 = 2; // 2 = por debajo
                    }
                 }
              }
           }


         //Comment(primeraPosA1, " ", segundaPosA1, " ", terceraPosA1);



         //tipoCruceLinea:  0 = cruza al alza
         if(tipoCruceLineaA1 == 0)
           {
            if((primeraPosA1 == 2 && segundaPosA1 == 1))
              {
               if(direcCruceLineaA1 == 2) //BUY
                 {
                  AlexanderOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaA1 == 3) //SELL
                 {
                  AlexanderOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaA1 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinAlexander = 1;
                  direcCruceLineaA1=CL_DibujaInicio;
                  robotReturn.direcCruceLineaA1=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;

                 }
               primeraPosA1 = 0;
               segundaPosA1 = 0;
               terceraPosA1 = 0;
              }
            else
              {
               if(primeraPosA1 != 2)
                 {
                  primeraPosA1 = 0;
                  segundaPosA1 = 0;
                  terceraPosA1 = 0;
                 }
              }
           }

         //tipoCruceLinea:  1 = cruza a la baja
         if(tipoCruceLineaA1 == 1)
           {
            if(primeraPosA1 == 1 && segundaPosA1 == 2)
              {
               if(direcCruceLineaA1 == 2) //BUY
                 {
                  AlexanderOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaA1 == 3) //SELL
                 {
                  AlexanderOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaA1 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinAlexander = 1;
                  direcCruceLineaA1=CL_DibujaInicio;
                  robotReturn.direcCruceLineaA1=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;

                 }
               primeraPosA1 = 0;
               segundaPosA1 = 0;
               terceraPosA1 = 0;
              }
            else
              {
               if(primeraPosA1 != 1)
                 {
                  primeraPosA1 = 0;
                  segundaPosA1 = 0;
                  terceraPosA1 = 0;
                 }
              }
           }


         //tipoCruceLinea:  2 = cruza al alza y despues a la baja
         if(tipoCruceLineaA1 == 2)
           {
            if(primeraPosA1 == 2 && segundaPosA1 == 1 && terceraPosA1 == 2)
              {
               if(direcCruceLineaA1 == 2) //BUY
                 {
                  AlexanderOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaA1 == 3) //SELL
                 {
                  AlexanderOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaA1 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinAlexander = 1;
                  direcCruceLineaA1=CL_DibujaInicio;
                  robotReturn.direcCruceLineaA1=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;

                 }
               primeraPosA1 = 0;
               segundaPosA1 = 0;
               terceraPosA1 = 0;
              }
            else
              {
               if(primeraPosA1 != 2)
                 {
                  primeraPosA1 = 0;
                  segundaPosA1 = 0;
                  terceraPosA1 = 0;
                 }
              }
           }
         //tipoCruceLinea:  3 = cruza a la baja y despues al alza
         if(tipoCruceLineaA1 == 3)
           {
            if(primeraPosA1 == 1 && segundaPosA1 == 2 && terceraPosA1 == 1)
              {
               if(direcCruceLineaA1 == 2) //BUY
                 {
                  AlexanderOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaA1 == 3) //SELL
                 {
                  AlexanderOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaA1 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinAlexander = 1;
                  direcCruceLineaA1=CL_DibujaInicio;
                  robotReturn.direcCruceLineaA1=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;

                 }
               primeraPosA1 = 0;
               segundaPosA1 = 0;
               terceraPosA1 = 0;
              }
            else
              {
               if(primeraPosA1 != 1)
                 {
                  primeraPosA1 = 0;
                  segundaPosA1 = 0;
                  terceraPosA1 = 0;
                 }
              }
           }
        }
      else
        {
         primeraPosA1 = 0;
         segundaPosA1 = 0;
         terceraPosA1 = 0;
         if(direcCruceLineaA1 == 0)
           {
            ObjectDelete("DProLineIniA1");
           }
        }
     }
   else
     {
      if(direcCruceLineaA1 > 0)
        {
         // PINTA NUEVA LINEA
         int cantiBars = WindowBarsPerChart() / 2;
         double precioCentro = WindowPriceMax(0) - ((WindowPriceMax(0) - WindowPriceMin(0)) / 5.0);
         //double bajoPriceAnt = iClose(NULL, PERIOD_CURRENT, iLowest(NULL, PERIOD_CURRENT, MODE_CLOSE, cantiBars, cantiBars));
         //double bajoPriceAct = iClose(NULL, PERIOD_CURRENT, iLowest(NULL, PERIOD_CURRENT, MODE_CLOSE, cantiBars, 0));
         //double bajoTimeAnt = iTime(NULL, PERIOD_CURRENT, iLowest(NULL, PERIOD_CURRENT, MODE_CLOSE, cantiBars, cantiBars));
         //double bajoTimeAct = iTime(NULL, PERIOD_CURRENT, iLowest(NULL, PERIOD_CURRENT, MODE_CLOSE, cantiBars, 0));
         //double bajo = iLow(NULL, PERIOD_M1, iLowest(NULL, PERIOD_M1, MODE_LOW, 10, 1));

         if(mousePrice!=0)
            precioCentro=mousePrice;

         TrendCreate(0, "DProLineIniA1", 0, iTime(NULL, PERIOD_CURRENT, cantiBars), precioCentro, iTime(NULL, PERIOD_CURRENT, 0), precioCentro, colorLines[colorCruceLineaA1]);
         lastColorCruceLineaA1 = colorCruceLineaA1;
         primeraPosA1 = 0;
         segundaPosA1 = 0;
         terceraPosA1 = 0;
         EmptyClipboard();

         //---
        }
     }
  }




//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OperaLineaStopA1()
  {
// if(direcCruceLineaA1==0)return;
   static bool pintadaLineaStopA1;

   if(ObjectFind("DProStopLineA1") >= 0)
     {
      if(stopCruceLineaA1 == 0)
        {
         ObjectDelete("DProStopLineA1");
         pintadaLineaStopA1=false;
         return;
        }

      if(lastColorStopCruceLineaA1 != colorStopCruceLineaA1)
        {
         ObjectSetInteger(0, "DProStopLineA1", OBJPROP_COLOR, colorLines[colorStopCruceLineaA1]);
         lastColorStopCruceLineaA1 = colorStopCruceLineaA1;
         ObjectSetInteger(0, "DProStopLineA1", OBJPROP_WIDTH, 5);
         ChartRedraw();
         Sleep(200);
        }
      else
        {
         ObjectSetInteger(0, "DProStopLineA1", OBJPROP_WIDTH, 1);
        }
      if(stopCruceLineaA1 == CL_DibujaStop)
        {
         ObjectSetInteger(0, "DProStopLineA1", OBJPROP_SELECTED, true);
         if(!pintadaLineaStopA1)
           {
            ObjectSetInteger(0, "DProStopLineA1", OBJPROP_WIDTH, 5);
            ChartRedraw();
            Sleep(200);
           }
         else
           {
            ObjectSetInteger(0, "DProStopLineA1", OBJPROP_WIDTH, 1);
           }
         pintadaLineaStopA1=true;
        }
      else
        {
         ObjectSetInteger(0, "DProStopLineA1", OBJPROP_SELECTED, false);
         if(pintadaLineaStopA1)
           {
            pintadaLineaStopA1=false;
            primeraPosStopA1 = 0;
            segundaPosStopA1 = 0;
            terceraPosStopA1 = 0;
           }
        }


      if(countTradesAlexanderVar > 0)
        {
         double DProLineStopA1 = ObjectGetValueByShift("DProStopLineA1", 0);

         if(AlexanderOperacionAbiertasBuy>=AlexanderOperacionAbiertasSell)
           {nivelDeActivacionA = Bid;}
         else {nivelDeActivacionA = Ask;}

         if(nivelDeActivacionA > DProLineStopA1)
           {

            if(primeraPosStopA1 == 0)
              {
               primeraPosStopA1 = 1; // 1= por encima
              }
            else
              {
               if(segundaPosStopA1 == 0 && primeraPosStopA1 != 1)
                 {
                  segundaPosStopA1 = 1; // 1= por encima
                 }
               else
                 {
                  if(terceraPosStopA1 == 0 && segundaPosStopA1 != 1 && segundaPosStopA1 != 0)
                    {
                     terceraPosStopA1 = 1; // 1= por encima
                    }
                 }
              }

           }
         if(nivelDeActivacionA < DProLineStopA1)
           {

            if(primeraPosStopA1 == 0)
              {
               primeraPosStopA1 = 2; // 2 = por debajo
              }
            else
              {
               if(segundaPosStopA1 == 0 && primeraPosStopA1 != 2)
                 {
                  segundaPosStopA1 = 2; // 2 = por debajo
                 }
               else
                 {
                  if(terceraPosStopA1 == 0 && segundaPosStopA1 != 2 && segundaPosStopA1 != 0)
                    {
                     terceraPosStopA1 = 2; // 2 = por debajo
                    }
                 }
              }
           }


         //Comment(primeraPosStopA1, " ", segundaPosStopA1, " ", terceraPosStopA1);



         //tipoCruceLinea:  0 = cruza al alza
         if(stopCruceLineaA1 == CL_ActivaCloseBuys)
           {
            if((primeraPosStopA1 == 2 && segundaPosStopA1 == 1) || (primeraPosStopA1 == 1 && segundaPosStopA1 == 2))
              {
               cerrarTodasOperacionesCiclo(MagicNumber_Alexander, OP_BUY);
               primeraPosStopA1 = 0;
               segundaPosStopA1 = 0;
               terceraPosStopA1 = 0;
              }
           }
         if(stopCruceLineaA1 == CL_ActivaCloseSells)
           {
            if((primeraPosStopA1 == 2 && segundaPosStopA1 == 1) || (primeraPosStopA1 == 1 && segundaPosStopA1 == 2))
              {
               cerrarTodasOperacionesCiclo(MagicNumber_Alexander, OP_SELL);
               primeraPosStopA1 = 0;
               segundaPosStopA1 = 0;
               terceraPosStopA1 = 0;
              }
           }
         if(stopCruceLineaA1 == CL_ActivaCloseAll)// && AlexanderTipoOperacion == Tipo_Manual
           {
            if((primeraPosStopA1 == 2 && segundaPosStopA1 == 1) || (primeraPosStopA1 == 1 && segundaPosStopA1 == 2))
              {
               cerrarTodasOperacionesCiclo(MagicNumber_Alexander);
               primeraPosStopA1 = 0;
               segundaPosStopA1 = 0;
               terceraPosStopA1 = 0;
              }
           }
         if(stopCruceLineaA1 == CL_ActivaAlta)// && AlexanderTipoOperacion == Tipo_Manual
           {
            if((primeraPosStopA1 == 2 && segundaPosStopA1 == 1) || (primeraPosStopA1 == 1 && segundaPosStopA1 == 2))
              {
               cerrarTicket(ticketOrdenMasAltaAlexander);
               primeraPosStopA1 = 0;
               segundaPosStopA1 = 0;
               terceraPosStopA1 = 0;
               stopCruceLineaA1=CL_DibujaStop;
               robotReturn.stopCruceLineaA1=CL_DibujaStop;
               robotSend.actualizarGadgets=1;
               retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());

              }
           }
         if(stopCruceLineaA1 == CL_ActivaBaja)// && AlexanderTipoOperacion == Tipo_Manual
           {
            if((primeraPosStopA1 == 2 && segundaPosStopA1 == 1) || (primeraPosStopA1 == 1 && segundaPosStopA1 == 2))
              {
               cerrarTicket(ticketOrdenMasBajaAlexander);
               primeraPosStopA1 = 0;
               segundaPosStopA1 = 0;
               terceraPosStopA1 = 0;
               stopCruceLineaA1=CL_DibujaStop;
               robotReturn.stopCruceLineaA1=CL_DibujaStop;
               robotSend.actualizarGadgets=1;
               retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());

              }
           }
        }
     }
   else
     {
      if(stopCruceLineaA1 > 0)
        {
         // PINTA NUEVA LINEA
         int cantiBars = WindowBarsPerChart() / 2;
         double BE=AlexanderBEPos();
         if(BE>=0.00)
           {
            double precioCentro = BE;
           }
         else
           {
            precioCentro = WindowPriceMax(0) - ((WindowPriceMax(0) - WindowPriceMin(0)) / 5.0) * 2.0;
           }

         if(mousePrice!=0)
            precioCentro=mousePrice;

         TrendCreate(0, "DProStopLineA1", 0, iTime(NULL, PERIOD_CURRENT, cantiBars), precioCentro, iTime(NULL, PERIOD_CURRENT, 0), precioCentro, colorLines[colorStopCruceLineaA1], STYLE_DASH);
         lastColorStopCruceLineaA1 = colorStopCruceLineaA1;
         primeraPosStopA1 = 0;
         segundaPosStopA1 = 0;
         terceraPosStopA1 = 0;
         EmptyClipboard();
        }
     }
  }





//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OperaLineaA2()
  {
// if(direcCruceLineaA2==0)return;
   static bool pintadaLineaA2;

   if(ObjectFind("DProLineIniA2") >= 0)
     {
      if(lastColorCruceLineaA2 != colorCruceLineaA2)
        {
         ObjectSetInteger(0, "DProLineIniA2", OBJPROP_COLOR, colorLines[colorCruceLineaA2]);
         lastColorCruceLineaA2 = colorCruceLineaA2;
         ObjectSetInteger(0, "DProLineIniA2", OBJPROP_WIDTH, 5);
         ChartRedraw();
         Sleep(200);
        }
      else
        {
         ObjectSetInteger(0, "DProLineIniA2", OBJPROP_WIDTH, 1);
        }
      if(direcCruceLineaA2 == CL_DibujaInicio)
        {
         ObjectSetInteger(0, "DProLineIniA2", OBJPROP_SELECTED, true);
         if(!pintadaLineaA2)
           {
            ObjectSetInteger(0, "DProLineIniA2", OBJPROP_WIDTH, 5);
            ChartRedraw();
            Sleep(200);
           }
         else
           {
            ObjectSetInteger(0, "DProLineIniA2", OBJPROP_WIDTH, 1);
           }
         pintadaLineaA2=true;
        }
      else
        {
         ObjectSetInteger(0, "DProLineIniA2", OBJPROP_SELECTED, false);
         if(pintadaLineaA2)
           {
            pintadaLineaA2=false;
            primeraPosA2 = 0;
            segundaPosA2 = 0;
            terceraPosA2 = 0;
           }
        }

      //        if(countTradesAlexanderVar == 0 && direcCruceLineaA2 > 1 && AlexanderActividad == Encendido) // && AlexanderTipoOperacion == Tipo_Manual
      if(direcCruceLineaA2 > 1) // && AlexanderTipoOperacion == Tipo_Manual
        {
         double DProLineOpenA2 = ObjectGetValueByShift("DProLineIniA2", 0);


         // ELIGE CONQ UÉ PRECIO (bid-ask DEBE ACTIVARSE LA LÍNEA. ========
         if(direcCruceLineaA2 == CL_ActivaBUY) //BUY
           {
            nivelDeActivacionA = Ask;
           }
         if(direcCruceLineaA2 == CL_ActivaSELL) //SELL
           {
            nivelDeActivacionA = Bid;
           }
         if(direcCruceLineaA2 == CL_ActivaOtra) //+ Orden
           {
            if(AlexanderOperacionAbiertasBuy>AlexanderOperacionAbiertasSell)
              {nivelDeActivacionA = Ask;}
            else {nivelDeActivacionA = Bid;}
           }
         //=================================================================

         if(nivelDeActivacionA > DProLineOpenA2)
           {

            if(primeraPosA2 == 0)
              {
               primeraPosA2 = 1; // 1= por encima
              }
            else
              {
               if(segundaPosA2 == 0 && primeraPosA2 != 1)
                 {
                  segundaPosA2 = 1; // 1= por encima
                 }
               else
                 {
                  if(terceraPosA2 == 0 && segundaPosA2 != 1 && segundaPosA2 != 0)
                    {
                     terceraPosA2 = 1; // 1= por encima
                    }
                 }
              }

           }
         if(nivelDeActivacionA < DProLineOpenA2)
           {

            if(primeraPosA2 == 0)
              {
               primeraPosA2 = 2; // 2 = por debajo
              }
            else
              {
               if(segundaPosA2 == 0 && primeraPosA2 != 2)
                 {
                  segundaPosA2 = 2; // 2 = por debajo
                 }
               else
                 {
                  if(terceraPosA2 == 0 && segundaPosA2 != 2 && segundaPosA2 != 0)
                    {
                     terceraPosA2 = 2; // 2 = por debajo
                    }
                 }
              }
           }


         //Comment(primeraPosA2, " ", segundaPosA2, " ", terceraPosA2);



         //tipoCruceLinea:  0 = cruza al alza
         if(tipoCruceLineaA2 == 0)
           {
            if((primeraPosA2 == 2 && segundaPosA2 == 1))
              {
               if(direcCruceLineaA2 == 2) //BUY
                 {
                  AlexanderOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaA2 == 3) //SELL
                 {
                  AlexanderOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaA2 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinAlexander = 1;
                  direcCruceLineaA2=CL_DibujaInicio;
                  robotReturn.direcCruceLineaA2=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;

                 }
               primeraPosA2 = 0;
               segundaPosA2 = 0;
               terceraPosA2 = 0;
              }
            else
              {
               if(primeraPosA2 != 2)
                 {
                  primeraPosA2 = 0;
                  segundaPosA2 = 0;
                  terceraPosA2 = 0;
                 }
              }
           }

         //tipoCruceLinea:  1 = cruza a la baja
         if(tipoCruceLineaA2 == 1)
           {
            if(primeraPosA2 == 1 && segundaPosA2 == 2)
              {
               if(direcCruceLineaA2 == 2) //BUY
                 {
                  AlexanderOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaA2 == 3) //SELL
                 {
                  AlexanderOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaA2 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinAlexander = 1;
                  direcCruceLineaA2=CL_DibujaInicio;
                  robotReturn.direcCruceLineaA2=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;

                 }
               primeraPosA2 = 0;
               segundaPosA2 = 0;
               terceraPosA2 = 0;
              }
            else
              {
               if(primeraPosA2 != 1)
                 {
                  primeraPosA2 = 0;
                  segundaPosA2 = 0;
                  terceraPosA2 = 0;
                 }
              }
           }


         //tipoCruceLinea:  2 = cruza al alza y despues a la baja
         if(tipoCruceLineaA2 == 2)
           {
            if(primeraPosA2 == 2 && segundaPosA2 == 1 && terceraPosA2 == 2)
              {
               if(direcCruceLineaA2 == 2) //BUY
                 {
                  AlexanderOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaA2 == 3) //SELL
                 {
                  AlexanderOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaA2 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinAlexander = 1;
                  direcCruceLineaA2=CL_DibujaInicio;
                  robotReturn.direcCruceLineaA2=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;

                 }
               primeraPosA2 = 0;
               segundaPosA2 = 0;
               terceraPosA2 = 0;
              }
            else
              {
               if(primeraPosA2 != 2)
                 {
                  primeraPosA2 = 0;
                  segundaPosA2 = 0;
                  terceraPosA2 = 0;
                 }
              }
           }
         //tipoCruceLinea:  3 = cruza a la baja y despues al alza
         if(tipoCruceLineaA2 == 3)
           {
            if(primeraPosA2 == 1 && segundaPosA2 == 2 && terceraPosA2 == 1)
              {
               if(direcCruceLineaA2 == 2) //BUY
                 {
                  AlexanderOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaA2 == 3) //SELL
                 {
                  AlexanderOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaA2 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinAlexander = 1;
                  direcCruceLineaA2=CL_DibujaInicio;
                  robotReturn.direcCruceLineaA2=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;

                 }
               primeraPosA2 = 0;
               segundaPosA2 = 0;
               terceraPosA2 = 0;
              }
            else
              {
               if(primeraPosA2 != 1)
                 {
                  primeraPosA2 = 0;
                  segundaPosA2 = 0;
                  terceraPosA2 = 0;
                 }
              }
           }
        }
      else
        {
         primeraPosA2 = 0;
         segundaPosA2 = 0;
         terceraPosA2 = 0;
         if(direcCruceLineaA2 == 0)
           {
            ObjectDelete("DProLineIniA2");
           }
        }
     }
   else
     {
      if(direcCruceLineaA2 > 0)
        {
         // PINTA NUEVA LINEA
         int cantiBars = WindowBarsPerChart() / 2;
         double precioCentro = WindowPriceMax(0) - ((WindowPriceMax(0) - WindowPriceMin(0)) / 5.0) * 3;

         if(mousePrice!=0)
            precioCentro=mousePrice;

         TrendCreate(0, "DProLineIniA2", 0, iTime(NULL, PERIOD_CURRENT, cantiBars), precioCentro, iTime(NULL, PERIOD_CURRENT, 0), precioCentro, colorLines[colorCruceLineaA2]);
         lastColorCruceLineaA2 = colorCruceLineaA2;
         primeraPosA2 = 0;
         segundaPosA2 = 0;
         terceraPosA2 = 0;
         EmptyClipboard();

        }
     }
  }




//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OperaLineaStopA2()
  {
// if(direcCruceLineaA2==0)return;
   static bool pintadaLineaStopA2;

   if(ObjectFind("DProStopLineA2") >= 0)
     {
      if(stopCruceLineaA2 == 0)
        {
         ObjectDelete("DProStopLineA2");
         pintadaLineaStopA2=false;
         return;
        }

      if(lastColorStopCruceLineaA2 != colorStopCruceLineaA2)
        {
         ObjectSetInteger(0, "DProStopLineA2", OBJPROP_COLOR, colorLines[colorStopCruceLineaA2]);
         lastColorStopCruceLineaA2 = colorStopCruceLineaA2;
         ObjectSetInteger(0, "DProStopLineA2", OBJPROP_WIDTH, 5);
         ChartRedraw();
         Sleep(200);
        }
      else
        {
         ObjectSetInteger(0, "DProStopLineA2", OBJPROP_WIDTH, 1);
        }
      if(stopCruceLineaA2 == CL_DibujaStop)
        {
         ObjectSetInteger(0, "DProStopLineA2", OBJPROP_SELECTED, true);
         if(!pintadaLineaStopA2)
           {
            ObjectSetInteger(0, "DProStopLineA2", OBJPROP_WIDTH, 5);
            ChartRedraw();
            Sleep(200);
           }
         else
           {
            ObjectSetInteger(0, "DProStopLineA2", OBJPROP_WIDTH, 1);
           }
         pintadaLineaStopA2=true;
        }
      else
        {
         ObjectSetInteger(0, "DProStopLineA2", OBJPROP_SELECTED, false);
         if(pintadaLineaStopA2)
           {
            pintadaLineaStopA2=false;
            primeraPosStopA2 = 0;
            segundaPosStopA2 = 0;
            terceraPosStopA2 = 0;
           }
        }


      if(countTradesAlexanderVar > 0)
        {
         double DProLineStopA2 = ObjectGetValueByShift("DProStopLineA2", 0);

         if(AlexanderOperacionAbiertasBuy>=AlexanderOperacionAbiertasSell)
           {nivelDeActivacionA = Bid;}
         else {nivelDeActivacionA = Ask;}

         if(nivelDeActivacionA > DProLineStopA2)
           {

            if(primeraPosStopA2 == 0)
              {
               primeraPosStopA2 = 1; // 1= por encima
              }
            else
              {
               if(segundaPosStopA2 == 0 && primeraPosStopA2 != 1)
                 {
                  segundaPosStopA2 = 1; // 1= por encima
                 }
               else
                 {
                  if(terceraPosStopA2 == 0 && segundaPosStopA2 != 1 && segundaPosStopA2 != 0)
                    {
                     terceraPosStopA2 = 1; // 1= por encima
                    }
                 }
              }

           }
         if(nivelDeActivacionA < DProLineStopA2)
           {

            if(primeraPosStopA2 == 0)
              {
               primeraPosStopA2 = 2; // 2 = por debajo
              }
            else
              {
               if(segundaPosStopA2 == 0 && primeraPosStopA2 != 2)
                 {
                  segundaPosStopA2 = 2; // 2 = por debajo
                 }
               else
                 {
                  if(terceraPosStopA2 == 0 && segundaPosStopA2 != 2 && segundaPosStopA2 != 0)
                    {
                     terceraPosStopA2 = 2; // 2 = por debajo
                    }
                 }
              }
           }


         //Comment(primeraPosStopA2, " ", segundaPosStopA2, " ", terceraPosStopA2);



         //tipoCruceLinea:  0 = cruza al alza
         if(stopCruceLineaA2 == CL_ActivaCloseBuys)
           {
            if((primeraPosStopA2 == 2 && segundaPosStopA2 == 1) || (primeraPosStopA2 == 1 && segundaPosStopA2 == 2))
              {
               cerrarTodasOperacionesCiclo(MagicNumber_Alexander, OP_BUY);
               primeraPosStopA2 = 0;
               segundaPosStopA2 = 0;
               terceraPosStopA2 = 0;
              }
           }
         if(stopCruceLineaA2 == CL_ActivaCloseSells)
           {
            if((primeraPosStopA2 == 2 && segundaPosStopA2 == 1) || (primeraPosStopA2 == 1 && segundaPosStopA2 == 2))
              {
               cerrarTodasOperacionesCiclo(MagicNumber_Alexander, OP_SELL);
               primeraPosStopA2 = 0;
               segundaPosStopA2 = 0;
               terceraPosStopA2 = 0;
              }
           }
         if(stopCruceLineaA2 == CL_ActivaCloseAll)
           {
            if((primeraPosStopA2 == 2 && segundaPosStopA2 == 1) || (primeraPosStopA2 == 1 && segundaPosStopA2 == 2))
              {
               cerrarTodasOperacionesCiclo(MagicNumber_Alexander);
               primeraPosStopA2 = 0;
               segundaPosStopA2 = 0;
               terceraPosStopA2 = 0;
              }
           }
         if(stopCruceLineaA2 == CL_ActivaAlta)// && AlexanderTipoOperacion == Tipo_Manual
           {
            if((primeraPosStopA2 == 2 && segundaPosStopA2 == 1) || (primeraPosStopA2 == 1 && segundaPosStopA2 == 2))
              {
               cerrarTicket(ticketOrdenMasAltaAlexander);
               primeraPosStopA2 = 0;
               segundaPosStopA2 = 0;
               terceraPosStopA2 = 0;
               stopCruceLineaA2=CL_DibujaStop;
               robotReturn.stopCruceLineaA2=CL_DibujaStop;
               robotSend.actualizarGadgets=1;
               retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());

              }
           }
         if(stopCruceLineaA2 == CL_ActivaBaja)// && AlexanderTipoOperacion == Tipo_Manual
           {
            if((primeraPosStopA2 == 2 && segundaPosStopA2 == 1) || (primeraPosStopA2 == 1 && segundaPosStopA2 == 2))
              {
               cerrarTicket(ticketOrdenMasBajaAlexander);
               primeraPosStopA2 = 0;
               segundaPosStopA2 = 0;
               terceraPosStopA2 = 0;
               stopCruceLineaA2=CL_DibujaStop;
               robotReturn.stopCruceLineaA2=CL_DibujaStop;
               robotSend.actualizarGadgets=1;
               retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());

              }
           }

        }
     }
   else
     {
      if(stopCruceLineaA2 > 0)
        {
         // PINTA NUEVA LINEA STOP 2
         int cantiBars = WindowBarsPerChart() / 2;
         double BE=AlexanderBEPos();
         if(BE>=0.00)
           {
            double precioCentro = BE;
           }
         else
           {
            precioCentro = WindowPriceMax(0) - ((WindowPriceMax(0) - WindowPriceMin(0)) / 5.0) * 4.0;
           }

         if(mousePrice!=0)
            precioCentro=mousePrice;

         TrendCreate(0, "DProStopLineA2", 0, iTime(NULL, PERIOD_CURRENT, cantiBars), precioCentro, iTime(NULL, PERIOD_CURRENT, 0), precioCentro, colorLines[colorStopCruceLineaA2], STYLE_DASH);
         lastColorStopCruceLineaA2 = colorStopCruceLineaA2;
         primeraPosStopA2 = 0;
         segundaPosStopA2 = 0;
         terceraPosStopA2 = 0;
         EmptyClipboard();
        }
     }
  }



















//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OperaLineaH1()
  {
// if(direcCruceLineaH1==0)return;
   static bool pintadaLineaH1;

   if(ObjectFind("DProLineIniH1") >= 0)
     {
      if(lastColorCruceLineaH1 != colorCruceLineaH1)
        {
         ObjectSetInteger(0, "DProLineIniH1", OBJPROP_COLOR, colorLines[colorCruceLineaH1]);
         lastColorCruceLineaH1 = colorCruceLineaH1;
         ObjectSetInteger(0, "DProLineIniH1", OBJPROP_WIDTH, 5);
         ChartRedraw();
         Sleep(200);
        }
      else
        {
         ObjectSetInteger(0, "DProLineIniH1", OBJPROP_WIDTH, 1);
        }
      if(direcCruceLineaH1 == CL_DibujaInicio)
        {
         ObjectSetInteger(0, "DProLineIniH1", OBJPROP_SELECTED, true);
         if(!pintadaLineaH1)
           {
            ObjectSetInteger(0, "DProLineIniH1", OBJPROP_WIDTH, 5);
            ChartRedraw();
            Sleep(200);
           }
         else
           {
            ObjectSetInteger(0, "DProLineIniH1", OBJPROP_WIDTH, 1);
           }
         pintadaLineaH1=true;
        }
      else
        {
         ObjectSetInteger(0, "DProLineIniH1", OBJPROP_SELECTED, false);
         if(pintadaLineaH1)
           {
            pintadaLineaH1=false;
            primeraPosH1 = 0;
            segundaPosH1 = 0;
            terceraPosH1 = 0;
           }
        }

      //        if(countTradesHannibalVar == 0 && direcCruceLineaH1 > 1 && HannibalActividad == Encendido) // && HannibalTipoOperacion == Tipo_Manual
      if(direcCruceLineaH1 > 1) // && HannibalTipoOperacion == Tipo_Manual
        {
         double DProLineOpenH1 = ObjectGetValueByShift("DProLineIniH1", 0);

         // ELIGE CONQ UÉ PRECIO (bid-ask DEBE ACTIVARSE LA LÍNEA. ========
         if(direcCruceLineaH1 == CL_ActivaBUY) //BUY
           {
            nivelDeActivacionH = Ask;
           }
         if(direcCruceLineaH1 == CL_ActivaSELL) //SELL
           {
            nivelDeActivacionH = Bid;
           }
         if(direcCruceLineaH1 == CL_ActivaOtra) //+ Orden
           {
            if(HannibalOperacionAbiertasBuy>HannibalOperacionAbiertasSell)
              {nivelDeActivacionH = Ask;}
            else {nivelDeActivacionH = Bid;}
           }
         //=================================================================


         if(nivelDeActivacionH > DProLineOpenH1)
           {

            if(primeraPosH1 == 0)
              {
               primeraPosH1 = 1; // 1= por encima
              }
            else
              {
               if(segundaPosH1 == 0 && primeraPosH1 != 1)
                 {
                  segundaPosH1 = 1; // 1= por encima
                 }
               else
                 {
                  if(terceraPosH1 == 0 && segundaPosH1 != 1 && segundaPosH1 != 0)
                    {
                     terceraPosH1 = 1; // 1= por encima
                    }
                 }
              }

           }
         if(nivelDeActivacionH < DProLineOpenH1)
           {

            if(primeraPosH1 == 0)
              {
               primeraPosH1 = 2; // 2 = por debajo
              }
            else
              {
               if(segundaPosH1 == 0 && primeraPosH1 != 2)
                 {
                  segundaPosH1 = 2; // 2 = por debajo
                 }
               else
                 {
                  if(terceraPosH1 == 0 && segundaPosH1 != 2 && segundaPosH1 != 0)
                    {
                     terceraPosH1 = 2; // 2 = por debajo
                    }
                 }
              }
           }


         //Comment(primeraPosH1, " ", segundaPosH1, " ", terceraPosH1);



         //tipoCruceLinea:  0 = cruza al alza
         if(tipoCruceLineaH1 == 0)
           {
            if((primeraPosH1 == 2 && segundaPosH1 == 1))
              {
               if(direcCruceLineaH1 == 2) //BUY
                 {
                  HannibalOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaH1 == 3) //SELL
                 {
                  HannibalOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaH1 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinHannibal = 1;
                  direcCruceLineaH1=CL_DibujaInicio;
                  robotReturn.direcCruceLineaH1=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;

                 }
               primeraPosH1 = 0;
               segundaPosH1 = 0;
               terceraPosH1 = 0;
              }
            else
              {
               if(primeraPosH1 != 2)
                 {
                  primeraPosH1 = 0;
                  segundaPosH1 = 0;
                  terceraPosH1 = 0;
                 }
              }
           }

         //tipoCruceLinea:  1 = cruza a la baja
         if(tipoCruceLineaH1 == 1)
           {
            if(primeraPosH1 == 1 && segundaPosH1 == 2)
              {
               if(direcCruceLineaH1 == 2) //BUY
                 {
                  HannibalOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaH1 == 3) //SELL
                 {
                  HannibalOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaH1 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinHannibal = 1;
                  direcCruceLineaH1=CL_DibujaInicio;
                  robotReturn.direcCruceLineaH1=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;

                 }
               primeraPosH1 = 0;
               segundaPosH1 = 0;
               terceraPosH1 = 0;
              }
            else
              {
               if(primeraPosH1 != 1)
                 {
                  primeraPosH1 = 0;
                  segundaPosH1 = 0;
                  terceraPosH1 = 0;
                 }
              }
           }


         //tipoCruceLinea:  2 = cruza al alza y despues a la baja
         if(tipoCruceLineaH1 == 2)
           {
            if(primeraPosH1 == 2 && segundaPosH1 == 1 && terceraPosH1 == 2)
              {
               if(direcCruceLineaH1 == 2) //BUY
                 {
                  HannibalOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaH1 == 3) //SELL
                 {
                  HannibalOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaH1 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinHannibal = 1;
                  direcCruceLineaH1=CL_DibujaInicio;
                  robotReturn.direcCruceLineaH1=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;

                 }
               primeraPosH1 = 0;
               segundaPosH1 = 0;
               terceraPosH1 = 0;
              }
            else
              {
               if(primeraPosH1 != 2)
                 {
                  primeraPosH1 = 0;
                  segundaPosH1 = 0;
                  terceraPosH1 = 0;
                 }
              }
           }
         //tipoCruceLinea:  3 = cruza a la baja y despues al alza
         if(tipoCruceLineaH1 == 3)
           {
            if(primeraPosH1 == 1 && segundaPosH1 == 2 && terceraPosH1 == 1)
              {
               if(direcCruceLineaH1 == 2) //BUY
                 {
                  HannibalOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaH1 == 3) //SELL
                 {
                  HannibalOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaH1 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinHannibal = 1;
                  direcCruceLineaH1=CL_DibujaInicio;
                  robotReturn.direcCruceLineaH1=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;

                 }
               primeraPosH1 = 0;
               segundaPosH1 = 0;
               terceraPosH1 = 0;
              }
            else
              {
               if(primeraPosH1 != 1)
                 {
                  primeraPosH1 = 0;
                  segundaPosH1 = 0;
                  terceraPosH1 = 0;
                 }
              }
           }
        }
      else
        {
         primeraPosH1 = 0;
         segundaPosH1 = 0;
         terceraPosH1 = 0;
         if(direcCruceLineaH1 == 0)
           {
            ObjectDelete("DProLineIniH1");
           }
        }
     }
   else
     {
      if(direcCruceLineaH1 > 0)
        {
         // PINTA NUEVA LINEA
         int cantiBars = WindowBarsPerChart() / 2;
         double precioCentro = WindowPriceMax(0) - ((WindowPriceMax(0) - WindowPriceMin(0)) / 5.0);
         //double bajoPriceAnt = iClose(NULL, PERIOD_CURRENT, iLowest(NULL, PERIOD_CURRENT, MODE_CLOSE, cantiBars, cantiBars));
         //double bajoPriceAct = iClose(NULL, PERIOD_CURRENT, iLowest(NULL, PERIOD_CURRENT, MODE_CLOSE, cantiBars, 0));
         //double bajoTimeAnt = iTime(NULL, PERIOD_CURRENT, iLowest(NULL, PERIOD_CURRENT, MODE_CLOSE, cantiBars, cantiBars));
         //double bajoTimeAct = iTime(NULL, PERIOD_CURRENT, iLowest(NULL, PERIOD_CURRENT, MODE_CLOSE, cantiBars, 0));
         //double bajo = iLow(NULL, PERIOD_M1, iLowest(NULL, PERIOD_M1, MODE_LOW, 10, 1));

         if(mousePrice!=0)
            precioCentro=mousePrice;

         TrendCreate(0, "DProLineIniH1", 0, iTime(NULL, PERIOD_CURRENT, cantiBars), precioCentro, iTime(NULL, PERIOD_CURRENT, 0), precioCentro, colorLines[colorCruceLineaH1]);
         lastColorCruceLineaH1 = colorCruceLineaH1;
         primeraPosH1 = 0;
         segundaPosH1 = 0;
         terceraPosH1 = 0;
         EmptyClipboard();

         //---
        }
     }
  }




//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OperaLineaStopH1()
  {
// if(direcCruceLineaH1==0)return;
   static bool pintadaLineaStopH1;

   if(ObjectFind("DProStopLineH1") >= 0)
     {
      if(stopCruceLineaH1 == 0)
        {
         ObjectDelete("DProStopLineH1");
         pintadaLineaStopH1=false;
         return;
        }

      if(lastColorStopCruceLineaH1 != colorStopCruceLineaH1)
        {
         ObjectSetInteger(0, "DProStopLineH1", OBJPROP_COLOR, colorLines[colorStopCruceLineaH1]);
         lastColorStopCruceLineaH1 = colorStopCruceLineaH1;
         ObjectSetInteger(0, "DProStopLineH1", OBJPROP_WIDTH, 5);
         ChartRedraw();
         Sleep(200);
        }
      else
        {
         ObjectSetInteger(0, "DProStopLineH1", OBJPROP_WIDTH, 1);
        }
      if(stopCruceLineaH1 == CL_DibujaStop)
        {
         ObjectSetInteger(0, "DProStopLineH1", OBJPROP_SELECTED, true);
         if(!pintadaLineaStopH1)
           {
            ObjectSetInteger(0, "DProStopLineH1", OBJPROP_WIDTH, 5);
            ChartRedraw();
            Sleep(200);
           }
         else
           {
            ObjectSetInteger(0, "DProStopLineH1", OBJPROP_WIDTH, 1);
           }
         pintadaLineaStopH1=true;
        }
      else
        {
         ObjectSetInteger(0, "DProStopLineH1", OBJPROP_SELECTED, false);
         if(pintadaLineaStopH1)
           {
            pintadaLineaStopH1=false;
            primeraPosStopH1 = 0;
            segundaPosStopH1 = 0;
            terceraPosStopH1 = 0;
           }
        }


      if(countTradesHannibalVar > 0)
        {
         double DProLineStopH1 = ObjectGetValueByShift("DProStopLineH1", 0);

         if(HannibalOperacionAbiertasBuy>=HannibalOperacionAbiertasSell)
           {nivelDeActivacionH = Bid;}
         else {nivelDeActivacionH = Ask;}

         if(nivelDeActivacionH > DProLineStopH1)
           {

            if(primeraPosStopH1 == 0)
              {
               primeraPosStopH1 = 1; // 1= por encima
              }
            else
              {
               if(segundaPosStopH1 == 0 && primeraPosStopH1 != 1)
                 {
                  segundaPosStopH1 = 1; // 1= por encima
                 }
               else
                 {
                  if(terceraPosStopH1 == 0 && segundaPosStopH1 != 1 && segundaPosStopH1 != 0)
                    {
                     terceraPosStopH1 = 1; // 1= por encima
                    }
                 }
              }

           }
         if(nivelDeActivacionH < DProLineStopH1)
           {

            if(primeraPosStopH1 == 0)
              {
               primeraPosStopH1 = 2; // 2 = por debajo
              }
            else
              {
               if(segundaPosStopH1 == 0 && primeraPosStopH1 != 2)
                 {
                  segundaPosStopH1 = 2; // 2 = por debajo
                 }
               else
                 {
                  if(terceraPosStopH1 == 0 && segundaPosStopH1 != 2 && segundaPosStopH1 != 0)
                    {
                     terceraPosStopH1 = 2; // 2 = por debajo
                    }
                 }
              }
           }


         //Comment(primeraPosStopH1, " ", segundaPosStopH1, " ", terceraPosStopH1);



         //tipoCruceLinea:  0 = cruza al alza
         if(stopCruceLineaH1 == CL_ActivaCloseBuys)
           {
            if((primeraPosStopH1 == 2 && segundaPosStopH1 == 1) || (primeraPosStopH1 == 1 && segundaPosStopH1 == 2))
              {
               cerrarTodasOperacionesCiclo(MagicNumber_Hannibal, OP_BUY);
               primeraPosStopH1 = 0;
               segundaPosStopH1 = 0;
               terceraPosStopH1 = 0;
              }
           }
         if(stopCruceLineaH1 == CL_ActivaCloseSells)
           {
            if((primeraPosStopH1 == 2 && segundaPosStopH1 == 1) || (primeraPosStopH1 == 1 && segundaPosStopH1 == 2))
              {
               cerrarTodasOperacionesCiclo(MagicNumber_Hannibal, OP_SELL);
               primeraPosStopH1 = 0;
               segundaPosStopH1 = 0;
               terceraPosStopH1 = 0;
              }
           }
         if(stopCruceLineaH1 == CL_ActivaCloseAll)// && HannibalTipoOperacion == Tipo_Manual
           {
            if((primeraPosStopH1 == 2 && segundaPosStopH1 == 1) || (primeraPosStopH1 == 1 && segundaPosStopH1 == 2))
              {
               cerrarTodasOperacionesCiclo(MagicNumber_Hannibal);
               primeraPosStopH1 = 0;
               segundaPosStopH1 = 0;
               terceraPosStopH1 = 0;
              }
           }
         if(stopCruceLineaH1 == CL_ActivaAlta)
           {
            if((primeraPosStopH1 == 2 && segundaPosStopH1 == 1) || (primeraPosStopH1 == 1 && segundaPosStopH1 == 2))
              {
               cerrarTicket(ticketOrdenMasAltaHannibal);
               primeraPosStopH1 = 0;
               segundaPosStopH1 = 0;
               terceraPosStopH1 = 0;
               stopCruceLineaH1=CL_DibujaStop;
               robotReturn.stopCruceLineaH1=CL_DibujaStop;
               robotSend.actualizarGadgets=1;
               retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());

              }
           }
         if(stopCruceLineaH1 == CL_ActivaBaja)
           {
            if((primeraPosStopH1 == 2 && segundaPosStopH1 == 1) || (primeraPosStopH1 == 1 && segundaPosStopH1 == 2))
              {
               cerrarTicket(ticketOrdenMasBajaHannibal);
               primeraPosStopH1 = 0;
               segundaPosStopH1 = 0;
               terceraPosStopH1 = 0;
               stopCruceLineaH1=CL_DibujaStop;
               robotReturn.stopCruceLineaH1=CL_DibujaStop;
               robotSend.actualizarGadgets=1;
               retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());

              }
           }
        }
     }
   else
     {
      if(stopCruceLineaH1 > 0)
        {
         // PINTA NUEVA LINEA
         int cantiBars = WindowBarsPerChart() / 2;
         double BE=HannibalBEPos();
         if(BE>=0.00)
           {
            double precioCentro = BE;
           }
         else
           {
            precioCentro = WindowPriceMax(0) - ((WindowPriceMax(0) - WindowPriceMin(0)) / 5.0) * 2.0;
           }

         if(mousePrice!=0)
            precioCentro=mousePrice;

         TrendCreate(0, "DProStopLineH1", 0, iTime(NULL, PERIOD_CURRENT, cantiBars), precioCentro, iTime(NULL, PERIOD_CURRENT, 0), precioCentro, colorLines[colorStopCruceLineaH1], STYLE_DASH);
         lastColorStopCruceLineaH1 = colorStopCruceLineaH1;
         primeraPosStopH1 = 0;
         segundaPosStopH1 = 0;
         terceraPosStopH1 = 0;
         EmptyClipboard();
        }
     }
  }





//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OperaLineaH2()
  {
// if(direcCruceLineaH2==0)return;
   static bool pintadaLineaH2;

   if(ObjectFind("DProLineIniH2") >= 0)
     {
      if(lastColorCruceLineaH2 != colorCruceLineaH2)
        {
         ObjectSetInteger(0, "DProLineIniH2", OBJPROP_COLOR, colorLines[colorCruceLineaH2]);
         lastColorCruceLineaH2 = colorCruceLineaH2;
         ObjectSetInteger(0, "DProLineIniH2", OBJPROP_WIDTH, 5);
         ChartRedraw();
         Sleep(200);
        }
      else
        {
         ObjectSetInteger(0, "DProLineIniH2", OBJPROP_WIDTH, 1);
        }
      if(direcCruceLineaH2 == CL_DibujaInicio)
        {
         ObjectSetInteger(0, "DProLineIniH2", OBJPROP_SELECTED, true);
         if(!pintadaLineaH2)
           {
            ObjectSetInteger(0, "DProLineIniH2", OBJPROP_WIDTH, 5);
            ChartRedraw();
            Sleep(200);
           }
         else
           {
            ObjectSetInteger(0, "DProLineIniH2", OBJPROP_WIDTH, 1);
           }
         pintadaLineaH2=true;
        }
      else
        {
         ObjectSetInteger(0, "DProLineIniH2", OBJPROP_SELECTED, false);
         if(pintadaLineaH2)
           {
            pintadaLineaH2=false;
            primeraPosH2 = 0;
            segundaPosH2 = 0;
            terceraPosH2 = 0;
           }
        }

      //        if(countTradesHannibalVar == 0 && direcCruceLineaH2 > 1 && HannibalActividad == Encendido) // && HannibalTipoOperacion == Tipo_Manual
      if(direcCruceLineaH2 > 1) // && HannibalTipoOperacion == Tipo_Manual
        {
         double DProLineOpenH2 = ObjectGetValueByShift("DProLineIniH2", 0);


         // ELIGE CONQ UÉ PRECIO (bid-ask DEBE ACTIVARSE LA LÍNEA. ========
         if(direcCruceLineaH2 == CL_ActivaBUY) //BUY
           {
            nivelDeActivacionH = Ask;
           }
         if(direcCruceLineaH2 == CL_ActivaSELL) //SELL
           {
            nivelDeActivacionH = Bid;
           }
         if(direcCruceLineaH2 == CL_ActivaOtra) //+ Orden
           {
            if(HannibalOperacionAbiertasBuy>HannibalOperacionAbiertasSell)
              {nivelDeActivacionH = Ask;}
            else {nivelDeActivacionH = Bid;}
           }
         //=================================================================


         if(nivelDeActivacionH > DProLineOpenH2)
           {

            if(primeraPosH2 == 0)
              {
               primeraPosH2 = 1; // 1= por encima
              }
            else
              {
               if(segundaPosH2 == 0 && primeraPosH2 != 1)
                 {
                  segundaPosH2 = 1; // 1= por encima
                 }
               else
                 {
                  if(terceraPosH2 == 0 && segundaPosH2 != 1 && segundaPosH2 != 0)
                    {
                     terceraPosH2 = 1; // 1= por encima
                    }
                 }
              }

           }
         if(nivelDeActivacionH < DProLineOpenH2)
           {

            if(primeraPosH2 == 0)
              {
               primeraPosH2 = 2; // 2 = por debajo
              }
            else
              {
               if(segundaPosH2 == 0 && primeraPosH2 != 2)
                 {
                  segundaPosH2 = 2; // 2 = por debajo
                 }
               else
                 {
                  if(terceraPosH2 == 0 && segundaPosH2 != 2 && segundaPosH2 != 0)
                    {
                     terceraPosH2 = 2; // 2 = por debajo
                    }
                 }
              }
           }


         //Comment(primeraPosH2, " ", segundaPosH2, " ", terceraPosH2);



         //tipoCruceLinea:  0 = cruza al alza
         if(tipoCruceLineaH2 == 0)
           {
            if((primeraPosH2 == 2 && segundaPosH2 == 1))
              {
               if(direcCruceLineaH2 == 2) //BUY
                 {
                  HannibalOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaH2 == 3) //SELL
                 {
                  HannibalOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaH2 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinHannibal = 1;
                  direcCruceLineaH2=CL_DibujaInicio;
                  robotReturn.direcCruceLineaH2=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;

                 }
               primeraPosH2 = 0;
               segundaPosH2 = 0;
               terceraPosH2 = 0;
              }
            else
              {
               if(primeraPosH2 != 2)
                 {
                  primeraPosH2 = 0;
                  segundaPosH2 = 0;
                  terceraPosH2 = 0;
                 }
              }
           }

         //tipoCruceLinea:  1 = cruza a la baja
         if(tipoCruceLineaH2 == 1)
           {
            if(primeraPosH2 == 1 && segundaPosH2 == 2)
              {
               if(direcCruceLineaH2 == 2) //BUY
                 {
                  HannibalOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaH2 == 3) //SELL
                 {
                  HannibalOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaH2 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinHannibal = 1;
                  direcCruceLineaH2=CL_DibujaInicio;
                  robotReturn.direcCruceLineaH2=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;

                 }
               primeraPosH2 = 0;
               segundaPosH2 = 0;
               terceraPosH2 = 0;
              }
            else
              {
               if(primeraPosH2 != 1)
                 {
                  primeraPosH2 = 0;
                  segundaPosH2 = 0;
                  terceraPosH2 = 0;
                 }
              }
           }


         //tipoCruceLinea:  2 = cruza al alza y despues a la baja
         if(tipoCruceLineaH2 == 2)
           {
            if(primeraPosH2 == 2 && segundaPosH2 == 1 && terceraPosH2 == 2)
              {
               if(direcCruceLineaH2 == 2) //BUY
                 {
                  HannibalOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaH2 == 3) //SELL
                 {
                  HannibalOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaH2 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinHannibal = 1;
                  direcCruceLineaH2=CL_DibujaInicio;
                  robotReturn.direcCruceLineaH2=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;

                 }
               primeraPosH2 = 0;
               segundaPosH2 = 0;
               terceraPosH2 = 0;
              }
            else
              {
               if(primeraPosH2 != 2)
                 {
                  primeraPosH2 = 0;
                  segundaPosH2 = 0;
                  terceraPosH2 = 0;
                 }
              }
           }
         //tipoCruceLinea:  3 = cruza a la baja y despues al alza
         if(tipoCruceLineaH2 == 3)
           {
            if(primeraPosH2 == 1 && segundaPosH2 == 2 && terceraPosH2 == 1)
              {
               if(direcCruceLineaH2 == 2) //BUY
                 {
                  HannibalOrdenManual = 2; //2=Buy
                 }
               if(direcCruceLineaH2 == 3) //SELL
                 {
                  HannibalOrdenManual = 1; //1=SELL
                 }
               if(direcCruceLineaH2 == CL_ActivaOtra) //+ Orden
                 {
                  otraMartinHannibal = 1;
                  direcCruceLineaH2=CL_DibujaInicio;
                  robotReturn.direcCruceLineaH2=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;

                 }
               primeraPosH2 = 0;
               segundaPosH2 = 0;
               terceraPosH2 = 0;
              }
            else
              {
               if(primeraPosH2 != 1)
                 {
                  primeraPosH2 = 0;
                  segundaPosH2 = 0;
                  terceraPosH2 = 0;
                 }
              }
           }
        }
      else
        {
         primeraPosH2 = 0;
         segundaPosH2 = 0;
         terceraPosH2 = 0;
         if(direcCruceLineaH2 == 0)
           {
            ObjectDelete("DProLineIniH2");
           }
        }
     }
   else
     {
      if(direcCruceLineaH2 > 0)
        {
         // PINTA NUEVA LINEA
         int cantiBars = WindowBarsPerChart() / 2;
         double precioCentro = WindowPriceMax(0) - ((WindowPriceMax(0) - WindowPriceMin(0)) / 5.0) * 3;

         if(mousePrice!=0)
            precioCentro=mousePrice;

         TrendCreate(0, "DProLineIniH2", 0, iTime(NULL, PERIOD_CURRENT, cantiBars), precioCentro, iTime(NULL, PERIOD_CURRENT, 0), precioCentro, colorLines[colorCruceLineaH2]);
         lastColorCruceLineaH2 = colorCruceLineaH2;
         primeraPosH2 = 0;
         segundaPosH2 = 0;
         terceraPosH2 = 0;
         EmptyClipboard();
        }
     }
  }




//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OperaLineaStopH2()
  {
// if(direcCruceLineaH2==0)return;
   static bool pintadaLineaStopH2;

   if(ObjectFind("DProStopLineH2") >= 0)
     {
      if(stopCruceLineaH2 == 0)
        {
         ObjectDelete("DProStopLineH2");
         pintadaLineaStopH2=false;
         return;
        }

      if(lastColorStopCruceLineaH2 != colorStopCruceLineaH2)
        {
         ObjectSetInteger(0, "DProStopLineH2", OBJPROP_COLOR, colorLines[colorStopCruceLineaH2]);
         lastColorStopCruceLineaH2 = colorStopCruceLineaH2;
         ObjectSetInteger(0, "DProStopLineH2", OBJPROP_WIDTH, 5);
         ChartRedraw();
         Sleep(200);
        }
      else
        {
         ObjectSetInteger(0, "DProStopLineH2", OBJPROP_WIDTH, 1);
        }
      if(stopCruceLineaH2 == CL_DibujaStop)
        {
         ObjectSetInteger(0, "DProStopLineH2", OBJPROP_SELECTED, true);
         if(!pintadaLineaStopH2)
           {
            ObjectSetInteger(0, "DProStopLineH2", OBJPROP_WIDTH, 5);
            ChartRedraw();
            Sleep(200);
           }
         else
           {
            ObjectSetInteger(0, "DProStopLineH2", OBJPROP_WIDTH, 1);
           }
         pintadaLineaStopH2=true;
        }
      else
        {
         ObjectSetInteger(0, "DProStopLineH2", OBJPROP_SELECTED, false);
         if(pintadaLineaStopH2)
           {
            pintadaLineaStopH2=false;
            primeraPosStopH2 = 0;
            segundaPosStopH2 = 0;
            terceraPosStopH2 = 0;
           }
        }

      if(countTradesHannibalVar > 0)
        {
         double DProLineStopH2 = ObjectGetValueByShift("DProStopLineH2", 0);

         if(HannibalOperacionAbiertasBuy>=HannibalOperacionAbiertasSell)
           {nivelDeActivacionH = Bid;}
         else {nivelDeActivacionH = Ask;}

         if(nivelDeActivacionH > DProLineStopH2)
           {

            if(primeraPosStopH2 == 0)
              {
               primeraPosStopH2 = 1; // 1= por encima
              }
            else
              {
               if(segundaPosStopH2 == 0 && primeraPosStopH2 != 1)
                 {
                  segundaPosStopH2 = 1; // 1= por encima
                 }
               else
                 {
                  if(terceraPosStopH2 == 0 && segundaPosStopH2 != 1 && segundaPosStopH2 != 0)
                    {
                     terceraPosStopH2 = 1; // 1= por encima
                    }
                 }
              }

           }
         if(nivelDeActivacionH < DProLineStopH2)
           {

            if(primeraPosStopH2 == 0)
              {
               primeraPosStopH2 = 2; // 2 = por debajo
              }
            else
              {
               if(segundaPosStopH2 == 0 && primeraPosStopH2 != 2)
                 {
                  segundaPosStopH2 = 2; // 2 = por debajo
                 }
               else
                 {
                  if(terceraPosStopH2 == 0 && segundaPosStopH2 != 2 && segundaPosStopH2 != 0)
                    {
                     terceraPosStopH2 = 2; // 2 = por debajo
                    }
                 }
              }
           }


         //Comment(primeraPosStopH2, " ", segundaPosStopH2, " ", terceraPosStopH2);



         //tipoCruceLinea:  0 = cruza al alza
         if(stopCruceLineaH2 == CL_ActivaCloseBuys)
           {
            if((primeraPosStopH2 == 2 && segundaPosStopH2 == 1) || (primeraPosStopH2 == 1 && segundaPosStopH2 == 2))
              {
               cerrarTodasOperacionesCiclo(MagicNumber_Hannibal, OP_BUY);
               primeraPosStopH2 = 0;
               segundaPosStopH2 = 0;
               terceraPosStopH2 = 0;
              }
           }
         if(stopCruceLineaH2 == CL_ActivaCloseSells)
           {
            if((primeraPosStopH2 == 2 && segundaPosStopH2 == 1) || (primeraPosStopH2 == 1 && segundaPosStopH2 == 2))
              {
               cerrarTodasOperacionesCiclo(MagicNumber_Hannibal, OP_SELL);
               primeraPosStopH2 = 0;
               segundaPosStopH2 = 0;
               terceraPosStopH2 = 0;
              }
           }
         if(stopCruceLineaH2 == CL_ActivaCloseAll)// && HannibalTipoOperacion == Tipo_Manual
           {
            if((primeraPosStopH2 == 2 && segundaPosStopH2 == 1) || (primeraPosStopH2 == 1 && segundaPosStopH2 == 2))
              {
               cerrarTodasOperacionesCiclo(MagicNumber_Hannibal);
               primeraPosStopH2 = 0;
               segundaPosStopH2 = 0;
               terceraPosStopH2 = 0;
              }
           }
         if(stopCruceLineaH2 == CL_ActivaAlta)// && HannibalTipoOperacion == Tipo_Manual
           {
            if((primeraPosStopH2 == 2 && segundaPosStopH2 == 1) || (primeraPosStopH2 == 1 && segundaPosStopH2 == 2))
              {
               cerrarTicket(ticketOrdenMasAltaHannibal);
               primeraPosStopH2 = 0;
               segundaPosStopH2 = 0;
               terceraPosStopH2 = 0;
               stopCruceLineaH2=CL_DibujaStop;
               robotReturn.stopCruceLineaH2=CL_DibujaStop;
               robotSend.actualizarGadgets=1;
               retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());

              }
           }
         if(stopCruceLineaH2 == CL_ActivaBaja)// && HannibalTipoOperacion == Tipo_Manual
           {
            if((primeraPosStopH2 == 2 && segundaPosStopH2 == 1) || (primeraPosStopH2 == 1 && segundaPosStopH2 == 2))
              {
               cerrarTicket(ticketOrdenMasBajaHannibal);
               primeraPosStopH2 = 0;
               segundaPosStopH2 = 0;
               terceraPosStopH2 = 0;
               stopCruceLineaH2=CL_DibujaStop;
               robotReturn.stopCruceLineaH2=CL_DibujaStop;
               robotSend.actualizarGadgets=1;
               retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());

              }
           }

        }
     }
   else
     {
      if(stopCruceLineaH2 > 0)
        {
         // PINTA NUEVA LINEA STOP 2
         int cantiBars = WindowBarsPerChart() / 2;
         double BE=HannibalBEPos();
         if(BE>=0.00)
           {
            double precioCentro = BE;
           }
         else
           {
            precioCentro = WindowPriceMax(0) - ((WindowPriceMax(0) - WindowPriceMin(0)) / 5.0) * 4.0;
           }

         if(mousePrice!=0)
            precioCentro=mousePrice;

         TrendCreate(0, "DProStopLineH2", 0, iTime(NULL, PERIOD_CURRENT, cantiBars), precioCentro, iTime(NULL, PERIOD_CURRENT, 0), precioCentro, colorLines[colorStopCruceLineaH2], STYLE_DASH);
         lastColorStopCruceLineaH2 = colorStopCruceLineaH2;
         primeraPosStopH2 = 0;
         segundaPosStopH2 = 0;
         terceraPosStopH2 = 0;
         EmptyClipboard();
        }
     }
  }





















//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OperaLineaM1()
  {
   if(lotajeLineaM>0.001)
      return;
   static bool pintadaLineaM1;

   if(ObjectFind("DProLineIniM1") >= 0)
     {
      if(lastColorCruceLineaM1 != colorCruceLineaM1)
        {
         ObjectSetInteger(0, "DProLineIniM1", OBJPROP_COLOR, colorLines[colorCruceLineaM1]);
         lastColorCruceLineaM1 = colorCruceLineaM1;
         ObjectSetInteger(0, "DProLineIniM1", OBJPROP_WIDTH, 5);
         ChartRedraw();
         Sleep(200);
        }
      else
        {
         ObjectSetInteger(0, "DProLineIniM1", OBJPROP_WIDTH, 1);
        }
      if(direcCruceLineaM1 == CL_DibujaInicio)
        {
         ObjectSetInteger(0, "DProLineIniM1", OBJPROP_SELECTED, true);
         if(!pintadaLineaM1)
           {
            ObjectSetInteger(0, "DProLineIniM1", OBJPROP_WIDTH, 5);
            ChartRedraw();
            Sleep(200);
           }
         else
           {
            ObjectSetInteger(0, "DProLineIniM1", OBJPROP_WIDTH, 1);
           }
         pintadaLineaM1=true;
        }
      else
        {
         ObjectSetInteger(0, "DProLineIniM1", OBJPROP_SELECTED, false);
         if(pintadaLineaM1)
           {
            pintadaLineaM1=false;
            primeraPosM1 = 0;
            segundaPosM1 = 0;
            terceraPosM1 = 0;
           }
        }

      //        if(countTradesManualVar == 0 && direcCruceLineaM1 > 1 && ManualActividad == Encendido) // && ManualTipoOperacion == Tipo_Manual
      if(direcCruceLineaM1 > 1) // && ManualTipoOperacion == Tipo_Manual
        {
         double DProLineOpenM1 = ObjectGetValueByShift("DProLineIniM1", 0);

         // ELIGE CONQ UÉ PRECIO (bid-ask DEBE ACTIVARSE LA LÍNEA. ========
         if(direcCruceLineaM1 == CL_ActivaBUY) //BUY
           {
            nivelDeActivacionM = Ask;
           }
         if(direcCruceLineaM1 == CL_ActivaSELL) //SELL
           {
            nivelDeActivacionM = Bid;
           }

         //=================================================================


         if(nivelDeActivacionM > DProLineOpenM1)
           {

            if(primeraPosM1 == 0)
              {
               primeraPosM1 = 1; // 1= por encima
              }
            else
              {
               if(segundaPosM1 == 0 && primeraPosM1 != 1)
                 {
                  segundaPosM1 = 1; // 1= por encima
                 }
               else
                 {
                  if(terceraPosM1 == 0 && segundaPosM1 != 1 && segundaPosM1 != 0)
                    {
                     terceraPosM1 = 1; // 1= por encima
                    }
                 }
              }

           }
         if(nivelDeActivacionM < DProLineOpenM1)
           {

            if(primeraPosM1 == 0)
              {
               primeraPosM1 = 2; // 2 = por debajo
              }
            else
              {
               if(segundaPosM1 == 0 && primeraPosM1 != 2)
                 {
                  segundaPosM1 = 2; // 2 = por debajo
                 }
               else
                 {
                  if(terceraPosM1 == 0 && segundaPosM1 != 2 && segundaPosM1 != 0)
                    {
                     terceraPosM1 = 2; // 2 = por debajo
                    }
                 }
              }
           }


         //Comment(primeraPosM1, " ", segundaPosM1, " ", terceraPosM1);



         //tipoCruceLinea:  0 = cruza al alza
         if(tipoCruceLineaM1 == 0)
           {
            if((primeraPosM1 == 2 && segundaPosM1 == 1))
              {
               if(direcCruceLineaM1 == CL_ActivaBUY) //BUY
                 {
                  direcCruceLineaM1=CL_DibujaInicio;
                  robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                  retorno=-1000;//PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                  int conta=0;
                  while(retorno==-1000 && conta<50)
                    {
                     conta=conta+1;
                     direcCruceLineaM1=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                     direcCruceLineaM1=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     Sleep(1);
                    }
                  Sleep(100);

                  lotajeLineaM=robotReturn.lotajeCruceLineaM1;
                  robotAcMan.ManuPoneOrden = 12; //2=Buy
                 }
               if(direcCruceLineaM1 == CL_ActivaSELL) //SELL
                 {
                  direcCruceLineaM1=CL_DibujaInicio;
                  robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                  retorno=-1000;//PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                  conta=0;
                  while(retorno==-1000 && conta<50)
                    {
                     conta=conta+1;
                     direcCruceLineaM1=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                     direcCruceLineaM1=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     Sleep(1);
                    }
                  Sleep(100);

                  lotajeLineaM=robotReturn.lotajeCruceLineaM1;
                  robotAcMan.ManuPoneOrden = 11; //2=Sell

                 }
               primeraPosM1 = 0;
               segundaPosM1 = 0;
               terceraPosM1 = 0;
              }
            else
              {
               if(primeraPosM1 != 2)
                 {
                  primeraPosM1 = 0;
                  segundaPosM1 = 0;
                  terceraPosM1 = 0;
                 }
              }
           }

         //tipoCruceLinea:  1 = cruza a la baja
         if(tipoCruceLineaM1 == 1)
           {
            if(primeraPosM1 == 1 && segundaPosM1 == 2)
              {
               if(direcCruceLineaM1 == CL_ActivaBUY) //BUY
                 {
                  direcCruceLineaM1=CL_DibujaInicio;
                  robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                  retorno=-1000;//PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                  conta=0;
                  while(retorno==-1000 && conta<50)
                    {
                     conta=conta+1;
                     direcCruceLineaM1=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                     direcCruceLineaM1=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     Sleep(1);
                    }
                  Sleep(100);

                  lotajeLineaM=robotReturn.lotajeCruceLineaM1;
                  robotAcMan.ManuPoneOrden = 12; //2=Buy

                 }
               if(direcCruceLineaM1 == CL_ActivaSELL) //SELL
                 {
                  direcCruceLineaM1=CL_DibujaInicio;
                  robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                  retorno=-1000;//PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                  conta=0;
                  while(retorno==-1000 && conta<50)
                    {
                     conta=conta+1;
                     direcCruceLineaM1=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                     direcCruceLineaM1=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     Sleep(1);
                    }
                  Sleep(100);

                  lotajeLineaM=robotReturn.lotajeCruceLineaM1;
                  robotAcMan.ManuPoneOrden = 11; //2=Sell

                 }
               primeraPosM1 = 0;
               segundaPosM1 = 0;
               terceraPosM1 = 0;
              }
            else
              {
               if(primeraPosM1 != 1)
                 {
                  primeraPosM1 = 0;
                  segundaPosM1 = 0;
                  terceraPosM1 = 0;
                 }
              }
           }


         //tipoCruceLinea:  2 = cruza al alza y despues a la baja
         if(tipoCruceLineaM1 == 2)
           {
            if(primeraPosM1 == 2 && segundaPosM1 == 1 && terceraPosM1 == 2)
              {
               if(direcCruceLineaM1 == CL_ActivaBUY) //BUY
                 {
                  direcCruceLineaM1=CL_DibujaInicio;
                  robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                  retorno=-1000;//PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                  conta=0;
                  while(retorno==-1000 && conta<50)
                    {
                     conta=conta+1;
                     direcCruceLineaM1=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                     direcCruceLineaM1=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     Sleep(1);
                    }
                  Sleep(100);

                  lotajeLineaM=robotReturn.lotajeCruceLineaM1;
                  robotAcMan.ManuPoneOrden = 12; //2=Buy

                 }
               if(direcCruceLineaM1 == CL_ActivaSELL) //SELL
                 {
                  direcCruceLineaM1=CL_DibujaInicio;
                  robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                  retorno=-1000;//PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                  conta=0;
                  while(retorno==-1000 && conta<50)
                    {
                     conta=conta+1;
                     direcCruceLineaM1=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                     direcCruceLineaM1=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     Sleep(1);
                    }
                  Sleep(100);

                  lotajeLineaM=robotReturn.lotajeCruceLineaM1;
                  robotAcMan.ManuPoneOrden = 11; //2=Sell

                 }
               primeraPosM1 = 0;
               segundaPosM1 = 0;
               terceraPosM1 = 0;
              }
            else
              {
               if(primeraPosM1 != 2)
                 {
                  primeraPosM1 = 0;
                  segundaPosM1 = 0;
                  terceraPosM1 = 0;
                 }
              }
           }
         //tipoCruceLinea:  3 = cruza a la baja y despues al alza
         if(tipoCruceLineaM1 == 3)
           {
            if(primeraPosM1 == 1 && segundaPosM1 == 2 && terceraPosM1 == 1)
              {
               if(direcCruceLineaM1 == CL_ActivaBUY) //BUY
                 {
                  direcCruceLineaM1=CL_DibujaInicio;
                  robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                  retorno=-1000;//PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                  conta=0;
                  while(retorno==-1000 && conta<50)
                    {
                     conta=conta+1;
                     direcCruceLineaM1=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                     direcCruceLineaM1=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     Sleep(1);
                    }
                  Sleep(100);

                  lotajeLineaM=robotReturn.lotajeCruceLineaM1;
                  robotAcMan.ManuPoneOrden = 12; //2=Buy

                 }
               if(direcCruceLineaM1 == CL_ActivaSELL) //SELL
                 {
                  direcCruceLineaM1=CL_DibujaInicio;
                  robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                  retorno=-1000;//PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                  conta=0;
                  while(retorno==-1000 && conta<50)
                    {
                     conta=conta+1;
                     direcCruceLineaM1=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                     direcCruceLineaM1=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM1=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     Sleep(1);
                    }
                  Sleep(100);

                  lotajeLineaM=robotReturn.lotajeCruceLineaM1;
                  robotAcMan.ManuPoneOrden = 11; //2=Sell

                 }
               primeraPosM1 = 0;
               segundaPosM1 = 0;
               terceraPosM1 = 0;
              }
            else
              {
               if(primeraPosM1 != 1)
                 {
                  primeraPosM1 = 0;
                  segundaPosM1 = 0;
                  terceraPosM1 = 0;
                 }
              }
           }
        }
      else
        {
         primeraPosM1 = 0;
         segundaPosM1 = 0;
         terceraPosM1 = 0;
         if(direcCruceLineaM1 == 0)
           {
            ObjectDelete("DProLineIniM1");
           }
        }
     }
   else
     {
      if(direcCruceLineaM1 > 0)
        {
         // PINTA NUEVA LINEA
         int cantiBars = WindowBarsPerChart() / 2;
         double precioCentro = WindowPriceMax(0) - ((WindowPriceMax(0) - WindowPriceMin(0)) / 5.0);
         //double bajoPriceAnt = iClose(NULL, PERIOD_CURRENT, iLowest(NULL, PERIOD_CURRENT, MODE_CLOSE, cantiBars, cantiBars));
         //double bajoPriceAct = iClose(NULL, PERIOD_CURRENT, iLowest(NULL, PERIOD_CURRENT, MODE_CLOSE, cantiBars, 0));
         //double bajoTimeAnt = iTime(NULL, PERIOD_CURRENT, iLowest(NULL, PERIOD_CURRENT, MODE_CLOSE, cantiBars, cantiBars));
         //double bajoTimeAct = iTime(NULL, PERIOD_CURRENT, iLowest(NULL, PERIOD_CURRENT, MODE_CLOSE, cantiBars, 0));
         //double bajo = iLow(NULL, PERIOD_M1, iLowest(NULL, PERIOD_M1, MODE_LOW, 10, 1));

         if(mousePrice!=0)
            precioCentro=mousePrice;

         TrendCreate(0, "DProLineIniM1", 0, iTime(NULL, PERIOD_CURRENT, cantiBars), precioCentro, iTime(NULL, PERIOD_CURRENT, 0), precioCentro, colorLines[colorCruceLineaM1]);
         lastColorCruceLineaM1 = colorCruceLineaM1;
         primeraPosM1 = 0;
         segundaPosM1 = 0;
         terceraPosM1 = 0;
         EmptyClipboard();

         //---
        }
     }
  }




//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OperaLineaStopM1()
  {
// if(direcCruceLineaM1==0)return;
   static bool pintadaLineaStopM1;

   if(ObjectFind("DProStopLineM1") >= 0)
     {
      if(stopCruceLineaM1 == 0)
        {
         ObjectDelete("DProStopLineM1");
         pintadaLineaStopM1=false;
         return;
        }

      if(lastColorStopCruceLineaM1 != colorStopCruceLineaM1)
        {
         ObjectSetInteger(0, "DProStopLineM1", OBJPROP_COLOR, colorLines[colorStopCruceLineaM1]);
         lastColorStopCruceLineaM1 = colorStopCruceLineaM1;
         ObjectSetInteger(0, "DProStopLineM1", OBJPROP_WIDTH, 5);
         ChartRedraw();
         Sleep(200);
        }
      else
        {
         ObjectSetInteger(0, "DProStopLineM1", OBJPROP_WIDTH, 1);
        }
      if(stopCruceLineaM1 == CL_DibujaStop)
        {
         ObjectSetInteger(0, "DProStopLineM1", OBJPROP_SELECTED, true);
         if(!pintadaLineaStopM1)
           {
            ObjectSetInteger(0, "DProStopLineM1", OBJPROP_WIDTH, 5);
            ChartRedraw();
            Sleep(200);
           }
         else
           {
            ObjectSetInteger(0, "DProStopLineM1", OBJPROP_WIDTH, 1);
           }
         pintadaLineaStopM1=true;
        }
      else
        {
         ObjectSetInteger(0, "DProStopLineM1", OBJPROP_SELECTED, false);
         if(pintadaLineaStopM1)
           {
            pintadaLineaStopM1=false;
            primeraPosStopM1 = 0;
            segundaPosStopM1 = 0;
            terceraPosStopM1 = 0;
           }
        }


      if(countTradesCaptainVar > 0)
        {
         double DProLineStopM1 = ObjectGetValueByShift("DProStopLineM1", 0);

         if(stopCruceLineaM1 == CL_ActivaCloseTicket)
           {
            if(OrderSelect(robotReturn.ticketStopLinea1,SELECT_BY_TICKET))
              {
               if(OrderType()==OP_BUY)
                 {nivelDeActivacionM = Bid;}
               else {nivelDeActivacionM = Ask;}
              }
           }
         else
           {
            if(ManualOperacionAbiertasBuy>=ManualOperacionAbiertasSell)
              {nivelDeActivacionM = Bid;}
            else {nivelDeActivacionM = Ask;}
           }

         if(nivelDeActivacionM > DProLineStopM1)
           {

            if(primeraPosStopM1 == 0)
              {
               primeraPosStopM1 = 1; // 1= por encima
              }
            else
              {
               if(segundaPosStopM1 == 0 && primeraPosStopM1 != 1)
                 {
                  segundaPosStopM1 = 1; // 1= por encima
                 }
               else
                 {
                  if(terceraPosStopM1 == 0 && segundaPosStopM1 != 1 && segundaPosStopM1 != 0)
                    {
                     terceraPosStopM1 = 1; // 1= por encima
                    }
                 }
              }

           }
         if(nivelDeActivacionM < DProLineStopM1)
           {

            if(primeraPosStopM1 == 0)
              {
               primeraPosStopM1 = 2; // 2 = por debajo
              }
            else
              {
               if(segundaPosStopM1 == 0 && primeraPosStopM1 != 2)
                 {
                  segundaPosStopM1 = 2; // 2 = por debajo
                 }
               else
                 {
                  if(terceraPosStopM1 == 0 && segundaPosStopM1 != 2 && segundaPosStopM1 != 0)
                    {
                     terceraPosStopM1 = 2; // 2 = por debajo
                    }
                 }
              }
           }


         //Comment(primeraPosStopM1, " ", segundaPosStopM1, " ", terceraPosStopM1);



         //tipoCruceLinea:  0 = cruza al alza
         if(stopCruceLineaM1 == CL_ActivaCloseBuys)
           {

            if((primeraPosStopM1 == 2 && segundaPosStopM1 == 1) || (primeraPosStopM1 == 1 && segundaPosStopM1 == 2))
              {
               cerrarTodasOperacionesCiclo(0, OP_BUY);
               primeraPosStopM1 = 0;
               segundaPosStopM1 = 0;
               terceraPosStopM1 = 0;
               //stopCruceLineaM1=CL_DibujaStop;
               //robotReturn.stopCruceLineaM1=CL_DibujaStop;
               //robotSend.actualizarGadgets=1;
               //
              }
           }
         if(stopCruceLineaM1 == CL_ActivaCloseSells)
           {

            if((primeraPosStopM1 == 2 && segundaPosStopM1 == 1) || (primeraPosStopM1 == 1 && segundaPosStopM1 == 2))
              {
               cerrarTodasOperacionesCiclo(0, OP_SELL);
               primeraPosStopM1 = 0;
               segundaPosStopM1 = 0;
               terceraPosStopM1 = 0;
               //stopCruceLineaM1=CL_DibujaStop;
               //robotReturn.stopCruceLineaM1=CL_DibujaStop;
               //robotSend.actualizarGadgets=1;
               //
              }
           }
         if(stopCruceLineaM1 == CL_ActivaCloseAll)// && ManualTipoOperacion == Tipo_Manual
           {

            if((primeraPosStopM1 == 2 && segundaPosStopM1 == 1) || (primeraPosStopM1 == 1 && segundaPosStopM1 == 2))
              {
               cerrarTodasOperacionesCiclo(0);
               primeraPosStopM1 = 0;
               segundaPosStopM1 = 0;
               terceraPosStopM1 = 0;
               //stopCruceLineaM1=CL_DibujaStop;
               //robotReturn.stopCruceLineaM1=CL_DibujaStop;
               //robotSend.actualizarGadgets=1;
               //
              }
           }
         if(stopCruceLineaM1 == CL_ActivaAlta)
           {
            if((primeraPosStopM1 == 2 && segundaPosStopM1 == 1) || (primeraPosStopM1 == 1 && segundaPosStopM1 == 2))
              {
               cerrarTicket(ticketOrdenMasAltaManual);
               primeraPosStopM1 = 0;
               segundaPosStopM1 = 0;
               terceraPosStopM1 = 0;
               stopCruceLineaM1=CL_DibujaStop;
               robotReturn.stopCruceLineaM1=CL_DibujaStop;
               robotSend.actualizarGadgets=1;
               retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());

              }
           }
         if(stopCruceLineaM1 == CL_ActivaBaja)
           {
            if((primeraPosStopM1 == 2 && segundaPosStopM1 == 1) || (primeraPosStopM1 == 1 && segundaPosStopM1 == 2))
              {
               cerrarTicket(ticketOrdenMasBajaManual);
               primeraPosStopM1 = 0;
               segundaPosStopM1 = 0;
               terceraPosStopM1 = 0;
               stopCruceLineaM1=CL_DibujaStop;
               robotReturn.stopCruceLineaM1=CL_DibujaStop;
               robotSend.actualizarGadgets=1;
               retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());

              }
           }
         if(stopCruceLineaM1 == CL_ActivaCloseTicket)
           {
            if((primeraPosStopM1 == 2 && segundaPosStopM1 == 1) || (primeraPosStopM1 == 1 && segundaPosStopM1 == 2))
              {
               cerrarTicket(robotReturn.ticketStopLinea1);
               robotReturn.ticketStopLinea1=0;
               robotAcMan.ManuTicket=0;
               primeraPosStopM1 = 0;
               segundaPosStopM1 = 0;
               terceraPosStopM1 = 0;
               stopCruceLineaM1=CL_DibujaStop;
               robotReturn.stopCruceLineaM1=CL_DibujaStop;
               robotSend.actualizarGadgets=1;
               retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());

              }
           }
        }
     }
   else
     {
      if(stopCruceLineaM1 > 0)
        {
         // PINTA NUEVA LINEA
         int cantiBars = WindowBarsPerChart() / 2;
         //double BE=ManualBEPos();
         //if (BE>=0.00)
         //{
         //    double precioCentro = BE;
         //}
         //else
         //{
         double precioCentro = WindowPriceMax(0) - ((WindowPriceMax(0) - WindowPriceMin(0)) / 5.0) * 2.0;
         //            }

         if(mousePrice!=0)
            precioCentro=mousePrice;

         TrendCreate(0, "DProStopLineM1", 0, iTime(NULL, PERIOD_CURRENT, cantiBars), precioCentro, iTime(NULL, PERIOD_CURRENT, 0), precioCentro, colorLines[colorStopCruceLineaM1], STYLE_DASH);
         lastColorStopCruceLineaM1 = colorStopCruceLineaM1;
         primeraPosStopM1 = 0;
         segundaPosStopM1 = 0;
         terceraPosStopM1 = 0;
         EmptyClipboard();
        }
     }
  }





//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OperaLineaM2()
  {
   if(lotajeLineaM>0.001)
      return;
   static bool pintadaLineaM2;

   if(ObjectFind("DProLineIniM2") >= 0)
     {
      if(lastColorCruceLineaM2 != colorCruceLineaM2)
        {
         ObjectSetInteger(0, "DProLineIniM2", OBJPROP_COLOR, colorLines[colorCruceLineaM2]);
         lastColorCruceLineaM2 = colorCruceLineaM2;
         ObjectSetInteger(0, "DProLineIniM2", OBJPROP_WIDTH, 5);
         ChartRedraw();
         Sleep(200);
        }
      else
        {
         ObjectSetInteger(0, "DProLineIniM2", OBJPROP_WIDTH, 1);
        }
      if(direcCruceLineaM2 == CL_DibujaInicio)
        {
         ObjectSetInteger(0, "DProLineIniM2", OBJPROP_SELECTED, true);
         if(!pintadaLineaM2)
           {
            ObjectSetInteger(0, "DProLineIniM2", OBJPROP_WIDTH, 5);
            ChartRedraw();
            Sleep(200);
           }
         else
           {
            ObjectSetInteger(0, "DProLineIniM2", OBJPROP_WIDTH, 1);
           }
         pintadaLineaM2=true;
        }
      else
        {
         ObjectSetInteger(0, "DProLineIniM2", OBJPROP_SELECTED, false);
         if(pintadaLineaM2)
           {
            pintadaLineaM2=false;
            primeraPosM2 = 0;
            segundaPosM2 = 0;
            terceraPosM2 = 0;
           }
        }

      //        if(countTradesManualVar == 0 && direcCruceLineaM2 > 1 && ManualActividad == Encendido) // && ManualTipoOperacion == Tipo_Manual
      if(direcCruceLineaM2 > 1) // && ManualTipoOperacion == Tipo_Manual
        {
         double DProLineOpenM2 = ObjectGetValueByShift("DProLineIniM2", 0);


         // ELIGE CONQ UÉ PRECIO (bid-ask DEBE ACTIVARSE LA LÍNEA. ========
         if(direcCruceLineaM2 == CL_ActivaBUY) //BUY
           {
            nivelDeActivacionM = Ask;
           }
         if(direcCruceLineaM2 == CL_ActivaSELL) //SELL
           {
            nivelDeActivacionM = Bid;
           }
         if(direcCruceLineaM2 == CL_ActivaOtra) //+ Orden
           {
            if(ManualOperacionAbiertasBuy>ManualOperacionAbiertasSell)
              {nivelDeActivacionM = Ask;}
            else {nivelDeActivacionM = Bid;}
           }
         //=================================================================


         if(nivelDeActivacionM > DProLineOpenM2)
           {

            if(primeraPosM2 == 0)
              {
               primeraPosM2 = 1; // 1= por encima
              }
            else
              {
               if(segundaPosM2 == 0 && primeraPosM2 != 1)
                 {
                  segundaPosM2 = 1; // 1= por encima
                 }
               else
                 {
                  if(terceraPosM2 == 0 && segundaPosM2 != 1 && segundaPosM2 != 0)
                    {
                     terceraPosM2 = 1; // 1= por encima
                    }
                 }
              }

           }
         if(nivelDeActivacionM < DProLineOpenM2)
           {

            if(primeraPosM2 == 0)
              {
               primeraPosM2 = 2; // 2 = por debajo
              }
            else
              {
               if(segundaPosM2 == 0 && primeraPosM2 != 2)
                 {
                  segundaPosM2 = 2; // 2 = por debajo
                 }
               else
                 {
                  if(terceraPosM2 == 0 && segundaPosM2 != 2 && segundaPosM2 != 0)
                    {
                     terceraPosM2 = 2; // 2 = por debajo
                    }
                 }
              }
           }


         //Comment(primeraPosM2, " ", segundaPosM2, " ", terceraPosM2);



         //tipoCruceLinea:  0 = cruza al alza
         if(tipoCruceLineaM2 == 0)
           {
            if((primeraPosM2 == 2 && segundaPosM2 == 1))
              {
               if(direcCruceLineaM2 == CL_ActivaBUY) //BUY
                 {
                  direcCruceLineaM2=CL_DibujaInicio;
                  robotReturn.direcCruceLineaM2=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                  retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                  conta=0;
                  while(retorno==-1000 && conta<50)
                    {
                     conta=conta+1;
                     direcCruceLineaM2=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM2=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                     Sleep(1);
                    }
                  lotajeLineaM=robotReturn.lotajeCruceLineaM2;
                  robotAcMan.ManuPoneOrden = 12; //2=Buy

                 }
               if(direcCruceLineaM2 == CL_ActivaSELL) //SELL
                 {
                  direcCruceLineaM2=CL_DibujaInicio;
                  robotReturn.direcCruceLineaM2=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                  retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                  conta=0;
                  while(retorno==-1000 && conta<50)
                    {
                     conta=conta+1;
                     direcCruceLineaM2=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM2=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                     Sleep(1);
                    }
                  lotajeLineaM=robotReturn.lotajeCruceLineaM2;
                  robotAcMan.ManuPoneOrden = 11; //1=Sell

                 }
               primeraPosM2 = 0;
               segundaPosM2 = 0;
               terceraPosM2 = 0;
              }
            else
              {
               if(primeraPosM2 != 2)
                 {
                  primeraPosM2 = 0;
                  segundaPosM2 = 0;
                  terceraPosM2 = 0;
                 }
              }
           }

         //tipoCruceLinea:  1 = cruza a la baja
         if(tipoCruceLineaM2 == 1)
           {
            if(primeraPosM2 == 1 && segundaPosM2 == 2)
              {
               if(direcCruceLineaM2 == CL_ActivaBUY) //BUY
                 {
                  direcCruceLineaM2=CL_DibujaInicio;
                  robotReturn.direcCruceLineaM2=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                  retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                  conta=0;
                  while(retorno==-1000 && conta<50)
                    {
                     conta=conta+1;
                     direcCruceLineaM2=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM2=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                     Sleep(1);
                    }

                  lotajeLineaM=robotReturn.lotajeCruceLineaM2;
                  robotAcMan.ManuPoneOrden = 12; //2=Buy

                 }
               if(direcCruceLineaM2 == CL_ActivaSELL) //SELL
                 {
                  direcCruceLineaM2=CL_DibujaInicio;
                  robotReturn.direcCruceLineaM2=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                  retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                  conta=0;
                  while(retorno==-1000 && conta<50)
                    {
                     conta=conta+1;
                     direcCruceLineaM2=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM2=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                     Sleep(1);
                    }

                  lotajeLineaM=robotReturn.lotajeCruceLineaM2;
                  robotAcMan.ManuPoneOrden = 11; //1=Sell

                 }
               primeraPosM2 = 0;
               segundaPosM2 = 0;
               terceraPosM2 = 0;
              }
            else
              {
               if(primeraPosM2 != 1)
                 {
                  primeraPosM2 = 0;
                  segundaPosM2 = 0;
                  terceraPosM2 = 0;
                 }
              }
           }


         //tipoCruceLinea:  2 = cruza al alza y despues a la baja
         if(tipoCruceLineaM2 == 2)
           {
            if(primeraPosM2 == 2 && segundaPosM2 == 1 && terceraPosM2 == 2)
              {
               if(direcCruceLineaM2 == CL_ActivaBUY) //BUY
                 {
                  direcCruceLineaM2=CL_DibujaInicio;
                  robotReturn.direcCruceLineaM2=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                  retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                  conta=0;
                  while(retorno==-1000 && conta<50)
                    {
                     conta=conta+1;
                     direcCruceLineaM2=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM2=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                     Sleep(1);
                    }

                  lotajeLineaM=robotReturn.lotajeCruceLineaM2;
                  robotAcMan.ManuPoneOrden = 12; //2=Buy

                 }
               if(direcCruceLineaM2 == CL_ActivaSELL) //SELL
                 {
                  direcCruceLineaM2=CL_DibujaInicio;
                  robotReturn.direcCruceLineaM2=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                  retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                  conta=0;
                  while(retorno==-1000 && conta<50)
                    {
                     conta=conta+1;
                     direcCruceLineaM2=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM2=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                     Sleep(1);
                    }

                  lotajeLineaM=robotReturn.lotajeCruceLineaM2;
                  robotAcMan.ManuPoneOrden = 11; //1=Sell

                 }
               primeraPosM2 = 0;
               segundaPosM2 = 0;
               terceraPosM2 = 0;
              }
            else
              {
               if(primeraPosM2 != 2)
                 {
                  primeraPosM2 = 0;
                  segundaPosM2 = 0;
                  terceraPosM2 = 0;
                 }
              }
           }
         //tipoCruceLinea:  3 = cruza a la baja y despues al alza
         if(tipoCruceLineaM2 == 3)
           {
            if(primeraPosM2 == 1 && segundaPosM2 == 2 && terceraPosM2 == 1)
              {
               if(direcCruceLineaM2 == CL_ActivaBUY) //BUY
                 {
                  direcCruceLineaM2=CL_DibujaInicio;
                  robotReturn.direcCruceLineaM2=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                  retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                  conta=0;
                  while(retorno==-1000 && conta<50)
                    {
                     conta=conta+1;
                     direcCruceLineaM2=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM2=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                     Sleep(1);
                    }

                  lotajeLineaM=robotReturn.lotajeCruceLineaM2;
                  robotAcMan.ManuPoneOrden = 12; //2=Buy

                 }
               if(direcCruceLineaM2 == CL_ActivaSELL) //SELL
                 {
                  direcCruceLineaM2=CL_DibujaInicio;
                  robotReturn.direcCruceLineaM2=CL_DibujaInicio;
                  robotSend.actualizarGadgets=1;
                  retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                  conta=0;
                  while(retorno==-1000 && conta<50)
                    {
                     conta=conta+1;
                     direcCruceLineaM2=CL_DibujaInicio;
                     robotReturn.direcCruceLineaM2=CL_DibujaInicio;
                     robotSend.actualizarGadgets=1;
                     retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
                     Sleep(1);
                    }

                  lotajeLineaM=robotReturn.lotajeCruceLineaM2;
                  robotAcMan.ManuPoneOrden = 11; //1=Sell

                 }
               primeraPosM2 = 0;
               segundaPosM2 = 0;
               terceraPosM2 = 0;
              }
            else
              {
               if(primeraPosM2 != 1)
                 {
                  primeraPosM2 = 0;
                  segundaPosM2 = 0;
                  terceraPosM2 = 0;
                 }
              }
           }
        }
      else
        {
         primeraPosM2 = 0;
         segundaPosM2 = 0;
         terceraPosM2 = 0;
         if(direcCruceLineaM2 == 0)
           {
            ObjectDelete("DProLineIniM2");
           }
        }
     }
   else
     {
      if(direcCruceLineaM2 > 0)
        {
         // PINTA NUEVA LINEA
         int cantiBars = WindowBarsPerChart() / 2;
         double precioCentro = WindowPriceMax(0) - ((WindowPriceMax(0) - WindowPriceMin(0)) / 5.0) * 3;

         if(mousePrice!=0)
            precioCentro=mousePrice;

         TrendCreate(0, "DProLineIniM2", 0, iTime(NULL, PERIOD_CURRENT, cantiBars), precioCentro, iTime(NULL, PERIOD_CURRENT, 0), precioCentro, colorLines[colorCruceLineaM2]);
         lastColorCruceLineaM2 = colorCruceLineaM2;
         primeraPosM2 = 0;
         segundaPosM2 = 0;
         terceraPosM2 = 0;
         EmptyClipboard();
        }
     }
  }




//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OperaLineaStopM2()
  {
// if(direcCruceLineaM2==0)return;
   static bool pintadaLineaStopM2;

   if(ObjectFind("DProStopLineM2") >= 0)
     {
      if(stopCruceLineaM2 == 0)
        {
         ObjectDelete("DProStopLineM2");
         pintadaLineaStopM2=false;
         return;
        }

      if(lastColorStopCruceLineaM2 != colorStopCruceLineaM2)
        {
         ObjectSetInteger(0, "DProStopLineM2", OBJPROP_COLOR, colorLines[colorStopCruceLineaM2]);
         lastColorStopCruceLineaM2 = colorStopCruceLineaM2;
         ObjectSetInteger(0, "DProStopLineM2", OBJPROP_WIDTH, 5);
         ChartRedraw();
         Sleep(200);
        }
      else
        {
         ObjectSetInteger(0, "DProStopLineM2", OBJPROP_WIDTH, 1);
        }
      if(stopCruceLineaM2 == CL_DibujaStop)
        {
         ObjectSetInteger(0, "DProStopLineM2", OBJPROP_SELECTED, true);
         if(!pintadaLineaStopM2)
           {
            ObjectSetInteger(0, "DProStopLineM2", OBJPROP_WIDTH, 5);
            ChartRedraw();
            Sleep(200);
           }
         else
           {
            ObjectSetInteger(0, "DProStopLineM2", OBJPROP_WIDTH, 1);
           }
         pintadaLineaStopM2=true;
        }
      else
        {
         ObjectSetInteger(0, "DProStopLineM2", OBJPROP_SELECTED, false);
         if(pintadaLineaStopM2)
           {
            pintadaLineaStopM2=false;
            primeraPosStopM2 = 0;
            segundaPosStopM2 = 0;
            terceraPosStopM2 = 0;
           }
        }

      if(countTradesCaptainVar > 0)
        {
         double DProLineStopM2 = ObjectGetValueByShift("DProStopLineM2", 0);

         if(stopCruceLineaM2 == CL_ActivaCloseTicket)
           {
            if(OrderSelect(robotReturn.ticketStopLinea2,SELECT_BY_TICKET))
              {
               if(OrderType()==OP_BUY)
                 {nivelDeActivacionM = Bid;}
               else {nivelDeActivacionM = Ask;}
              }
           }
         else
           {
            if(ManualOperacionAbiertasBuy>=ManualOperacionAbiertasSell)
              {nivelDeActivacionM = Bid;}
            else {nivelDeActivacionM = Ask;}
           }


         if(nivelDeActivacionM > DProLineStopM2)
           {

            if(primeraPosStopM2 == 0)
              {
               primeraPosStopM2 = 1; // 1= por encima
              }
            else
              {
               if(segundaPosStopM2 == 0 && primeraPosStopM2 != 1)
                 {
                  segundaPosStopM2 = 1; // 1= por encima
                 }
               else
                 {
                  if(terceraPosStopM2 == 0 && segundaPosStopM2 != 1 && segundaPosStopM2 != 0)
                    {
                     terceraPosStopM2 = 1; // 1= por encima
                    }
                 }
              }

           }
         if(nivelDeActivacionM < DProLineStopM2)
           {

            if(primeraPosStopM2 == 0)
              {
               primeraPosStopM2 = 2; // 2 = por debajo
              }
            else
              {
               if(segundaPosStopM2 == 0 && primeraPosStopM2 != 2)
                 {
                  segundaPosStopM2 = 2; // 2 = por debajo
                 }
               else
                 {
                  if(terceraPosStopM2 == 0 && segundaPosStopM2 != 2 && segundaPosStopM2 != 0)
                    {
                     terceraPosStopM2 = 2; // 2 = por debajo
                    }
                 }
              }
           }


         //Comment(primeraPosStopM2, " ", segundaPosStopM2, " ", terceraPosStopM2);



         //tipoCruceLinea:  0 = cruza al alza
         if(stopCruceLineaM2 == CL_ActivaCloseBuys)
           {
            if((primeraPosStopM2 == 2 && segundaPosStopM2 == 1) || (primeraPosStopM2 == 1 && segundaPosStopM2 == 2))
              {
               cerrarTodasOperacionesCiclo(0, OP_BUY);
               primeraPosStopM2 = 0;
               segundaPosStopM2 = 0;
               terceraPosStopM2 = 0;
               //stopCruceLineaM2=CL_DibujaStop;
               //robotReturn.stopCruceLineaM2=CL_DibujaStop;
               //robotSend.actualizarGadgets=1;
               //
              }
           }
         if(stopCruceLineaM2 == CL_ActivaCloseSells)
           {
            if((primeraPosStopM2 == 2 && segundaPosStopM2 == 1) || (primeraPosStopM2 == 1 && segundaPosStopM2 == 2))
              {
               cerrarTodasOperacionesCiclo(0, OP_SELL);
               primeraPosStopM2 = 0;
               segundaPosStopM2 = 0;
               terceraPosStopM2 = 0;
               //stopCruceLineaM2=CL_DibujaStop;
               //robotReturn.stopCruceLineaM2=CL_DibujaStop;
               //robotSend.actualizarGadgets=1;
               //
              }
           }
         if(stopCruceLineaM2 == CL_ActivaCloseAll)
           {
            if((primeraPosStopM2 == 2 && segundaPosStopM2 == 1) || (primeraPosStopM2 == 1 && segundaPosStopM2 == 2))
              {
               cerrarTodasOperacionesCiclo(0);
               primeraPosStopM2 = 0;
               segundaPosStopM2 = 0;
               terceraPosStopM2 = 0;
               //stopCruceLineaM2=CL_DibujaStop;
               //robotReturn.stopCruceLineaM2=CL_DibujaStop;
               //robotSend.actualizarGadgets=1;
              }
           }
         if(stopCruceLineaM2 == CL_ActivaAlta)// && ManualTipoOperacion == Tipo_Manual
           {
            if((primeraPosStopM2 == 2 && segundaPosStopM2 == 1) || (primeraPosStopM2 == 1 && segundaPosStopM2 == 2))
              {
               cerrarTicket(ticketOrdenMasAltaManual);
               primeraPosStopM2 = 0;
               segundaPosStopM2 = 0;
               terceraPosStopM2 = 0;
               stopCruceLineaM2=CL_DibujaStop;
               robotReturn.stopCruceLineaM2=CL_DibujaStop;
               robotSend.actualizarGadgets=1;
               retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());

              }
           }
         if(stopCruceLineaM2 == CL_ActivaBaja)// && ManualTipoOperacion == Tipo_Manual
           {
            if((primeraPosStopM2 == 2 && segundaPosStopM2 == 1) || (primeraPosStopM2 == 1 && segundaPosStopM2 == 2))
              {
               cerrarTicket(ticketOrdenMasBajaManual);
               primeraPosStopM2 = 0;
               segundaPosStopM2 = 0;
               terceraPosStopM2 = 0;
               stopCruceLineaM2=CL_DibujaStop;
               robotReturn.stopCruceLineaM2=CL_DibujaStop;
               robotSend.actualizarGadgets=1;
               retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());

              }
           }
         if(stopCruceLineaM2 == CL_ActivaCloseTicket)
           {
            if((primeraPosStopM2 == 2 && segundaPosStopM2 == 1) || (primeraPosStopM2 == 1 && segundaPosStopM2 == 2))
              {
               cerrarTicket(robotReturn.ticketStopLinea2);
               robotReturn.ticketStopLinea2=0;
               robotAcMan.ManuTicket=0;
               primeraPosStopM2 = 0;
               segundaPosStopM2 = 0;
               terceraPosStopM2 = 0;
               stopCruceLineaM2=CL_DibujaStop;
               robotReturn.stopCruceLineaM2=CL_DibujaStop;
               robotSend.actualizarGadgets=1;
               retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());

              }
           }
        }
     }
   else
     {
      if(stopCruceLineaM2 > 0)
        {
         // PINTA NUEVA LINEA STOP 2
         int cantiBars = WindowBarsPerChart() / 2;
         //double BE=ManualBEPos();
         //if (BE>=0.00)
         //{
         //    double precioCentro = BE;
         //}
         //else
         //{
         double precioCentro = WindowPriceMax(0) - ((WindowPriceMax(0) - WindowPriceMin(0)) / 5.0) * 4.0;
         //}

         if(mousePrice!=0)
            precioCentro=mousePrice;

         TrendCreate(0, "DProStopLineM2", 0, iTime(NULL, PERIOD_CURRENT, cantiBars), precioCentro, iTime(NULL, PERIOD_CURRENT, 0), precioCentro, colorLines[colorStopCruceLineaM2], STYLE_DASH);
         lastColorStopCruceLineaM2 = colorStopCruceLineaM2;
         primeraPosStopM2 = 0;
         segundaPosStopM2 = 0;
         terceraPosStopM2 = 0;
         EmptyClipboard();
        }
     }
  }






















//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool HayLimiteHorario(int caudillo = 0)
  {
   static int lastMinu = -1;
   static int lastSeco = -1;
   static bool res = false;

   datetime tiempoActual = TimeCurrent();
   int minutoActual = TimeMinute(tiempoActual);
   int horaActual = TimeHour(tiempoActual);
   int dayMonthActual = TimeDay(tiempoActual);

   int dayWeekActual = TimeDayOfWeek(tiempoActual);
   int monthActual = TimeMonth(tiempoActual);

   bool respuesta = false;

   switch(caudillo)
     {
      case 1: //Julius
         noOpHorarioJ = 0;
         datetime desdeTimeJ = StringToTime(""+Year()+"."+Month()+"."+Day()+" "+StringFormat("%02d",desdeHoraJ)+":"+StringFormat("%02d",desdeMinutoJ)+":00");
         datetime hastaTimeJ = StringToTime(""+Year()+"."+Month()+"."+Day()+" "+StringFormat("%02d",hastaHoraJ)+":"+StringFormat("%02d",hastaMinutoJ)+":59");


         if(tiempoActual<desdeTimeJ || tiempoActual>hastaTimeJ)
           {
            respuesta = true;
           }
         if(InvierteTiempoJ)
            respuesta= !respuesta;


         if(dayMonthActual < desdeDiaJ)
            respuesta = true;
         if(dayMonthActual > hastaDiaJ)
            respuesta = true;

         if(dayWeekActual == 1 && !LunesJ)
            respuesta = true;
         if(dayWeekActual == 2 && !MartesJ)
            respuesta = true;
         if(dayWeekActual == 3 && !MiercolesJ)
            respuesta = true;
         if(dayWeekActual == 4 && !JuevesJ)
            respuesta = true;
         if(dayWeekActual == 5 && !ViernesJ)
            respuesta = true;
         if(dayWeekActual == 6 && !SabadoJ)
            respuesta = true;
         if(dayWeekActual == 0 && !DomingoJ)
            respuesta = true;

         if(respuesta)
           {
            noOpHorarioJ = 1;
            CaesarOrdenManual = 0;
           }


         break;
      case 2: //Alexander

         noOpHorarioA = 0;

         datetime desdeTimeA = StringToTime(""+Year()+"."+Month()+"."+Day()+" "+StringFormat("%02d",desdeHoraA)+":"+StringFormat("%02d",desdeMinutoA)+":00");
         datetime hastaTimeA = StringToTime(""+Year()+"."+Month()+"."+Day()+" "+StringFormat("%02d",hastaHoraA)+":"+StringFormat("%02d",hastaMinutoA)+":59");


         if(tiempoActual<desdeTimeA || tiempoActual>hastaTimeA)
           {
            respuesta = true;
           }
         if(InvierteTiempoA)
            respuesta= !respuesta;

         if(dayMonthActual < desdeDiaA)
            respuesta = true;
         if(dayMonthActual > hastaDiaA)
            respuesta = true;

         if(dayWeekActual == 1 && !LunesA)
            respuesta = true;
         if(dayWeekActual == 2 && !MartesA)
            respuesta = true;
         if(dayWeekActual == 3 && !MiercolesA)
            respuesta = true;
         if(dayWeekActual == 4 && !JuevesA)
            respuesta = true;
         if(dayWeekActual == 5 && !ViernesA)
            respuesta = true;
         if(dayWeekActual == 6 && !SabadoA)
            respuesta = true;
         if(dayWeekActual == 0 && !DomingoA)
            respuesta = true;


         if(respuesta)
           {
            noOpHorarioA = 1;
            AlexanderOrdenManual = 0;
           }
         break;
      case 3: //Hannibal

         noOpHorarioH = 0;

         datetime desdeTimeH = StringToTime(""+Year()+"."+Month()+"."+Day()+" "+StringFormat("%02d",desdeHoraH)+":"+StringFormat("%02d",desdeMinutoH)+":00");
         datetime hastaTimeH = StringToTime(""+Year()+"."+Month()+"."+Day()+" "+StringFormat("%02d",hastaHoraH)+":"+StringFormat("%02d",hastaMinutoH)+":59");


         if(tiempoActual<desdeTimeH || tiempoActual>hastaTimeH)
           {
            respuesta = true;
           }
         if(InvierteTiempoH)
            respuesta= !respuesta;

         if(dayMonthActual < desdeDiaH)
            respuesta = true;
         if(dayMonthActual > hastaDiaH)
            respuesta = true;

         if(dayWeekActual == 1 && !LunesH)
            respuesta = true;
         if(dayWeekActual == 2 && !MartesH)
            respuesta = true;
         if(dayWeekActual == 3 && !MiercolesH)
            respuesta = true;
         if(dayWeekActual == 4 && !JuevesH)
            respuesta = true;
         if(dayWeekActual == 5 && !ViernesH)
            respuesta = true;
         if(dayWeekActual == 6 && !SabadoH)
            respuesta = true;
         if(dayWeekActual == 0 && !DomingoH)
            respuesta = true;


         if(respuesta)
           {
            noOpHorarioH = 1;
            HannibalOrdenManual = 0;
           }
         break;
     }

   if((
         (!Enero     && (monthActual == 1)) ||
         (!Febrero   && (monthActual == 2)) ||
         (!Marzo     && (monthActual == 3)) ||
         (!Abril     && (monthActual == 4)) ||
         (!Mayo      && (monthActual == 5)) ||
         (!Junio     && (monthActual == 6)) ||
         (!Julio     && (monthActual == 7)) ||
         (!Agosto    && (monthActual == 8)) ||
         (!Septiembre && (monthActual == 9)) ||
         (!Octubre   && (monthActual == 10)) ||
         (!Noviembre && (monthActual == 11)) ||
         (!Diciembre && (monthActual == 12))))
     {
      respuesta = true;
      noOpHorarioJ = 1;
      noOpHorarioA = 1;
      noOpHorarioH = 1;

     }


   if(respuesta && TimeSeconds(tiempoActual)<59 && (countTradesTotalParVar > 0 && (AlsoClose==1 && MathAbs(AccountProfit()) < LimitClose * tipoCuentaDouble)))
     {
      switch(caudillo)
        {
         case 1: //Julius

            noOpHorarioJ = 2;
            cerrarTodasOperacionesCiclo(MagicNumber_Caesar);
            CaesarOrdenManual = 0;
            break;

         case 2: //Alexander
            noOpHorarioA = 2;
            cerrarTodasOperacionesCiclo(MagicNumber_Alexander);
            AlexanderOrdenManual = 0;
            break;

         case 3: //Hannibal
            noOpHorarioH = 2;
            cerrarTodasOperacionesCiclo(MagicNumber_Hannibal);
            HannibalOrdenManual = 0;
            break;
        }
     }

//Comment(lugar);
   res = respuesta;
   return respuesta;
  }


void unload_dll()

  {
   int HMOD = GetModuleHandleA(nombreDLL);
   if(HMOD != 0)
      FreeLibrary(HMOD);
  }





//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string convertUTF8(string text, int CP = CP_ACP)
  {
//TO UTF-8 ///////////////////////////////////////////////
   uchar array[];
   StringToCharArray(text, array, 0, -1, CP);
   text = CharArrayToString(array, 0, -1, CP_UTF8);
   ArrayFree(array);
//FIN TO UTF-8 ///////////////////////////////////////////
   return text;
  }





//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
  
  
   coletaPar = StringSubstr(_Symbol, 6, 0);
   Sleep(1);
   if (GlobalVariableCheck("cargCar"))return INIT_FAILED;
            
   lotajeSumTotal = 0.00;

   scale_factor = (TerminalInfoInteger(TERMINAL_SCREEN_DPI) * 100) / 96;

//## Walk Forward Pro OnInit() code start (MQL4) ##
#ifdef WalkForfardPro
   if(MQLInfoInteger(MQL_TESTER))
      WFA_Initialise();
#endif
//## Walk Forward Pro OnInit() code end (MQL4) ##


//    InicioOpti = TimeCurrent(); // Rafa

   int loadSet;// = TerminalInfoInteger(TERMINAL_KEYSTATE_CONTROL);
   loadSet = GetKeyState(17); //CTRL
   if(loadSet > 1)
     {
      string nombreCapitan2 = convertUTF8(nombreCapitan);
      string lemaCapitan2 = convertUTF8(lemaCapitan);
      string ARMERIA_CAESAR2 = convertUTF8(ARMERIA_CAESAR);
      string ARMERIA_ALEXANDER2 = convertUTF8(ARMERIA_ALEXANDER);
      string ARMERIA_HANNIBAL2 = convertUTF8(ARMERIA_HANNIBAL);
      //if (StringFind(ARMERIA_CAESAR2,"í")>=0)
      // {
      nombreCapitan = nombreCapitan2;
      lemaCapitan = lemaCapitan2;
      ARMERIA_CAESAR = ARMERIA_CAESAR2;
      ARMERIA_ALEXANDER = ARMERIA_ALEXANDER2;
      ARMERIA_HANNIBAL = ARMERIA_HANNIBAL2;
      //}
     }

   infoLotStep = MarketInfo(Symbol(), MODE_LOTSTEP);
   infoLotMax = MarketInfo(Symbol(), MODE_MAXLOT);
   infoLotMin = MarketInfo(Symbol(), MODE_MINLOT);







   if(lastReason != REASON_TEMPLATE && lastReason != REASON_CHARTCHANGE)
     {
      lastReason = -1;


      int static autoRobo=0;

        if( (!autoRobo) && (tipoCuenta==Por_Defecto) && (StringFind(AccountCompany(), "RoboForex")>=0) && (!GlobalVariableCheck("tcuen-"+AccountInfoInteger(ACCOUNT_LOGIN))))
            {
            tipoCuenta=Roboforex;
            autoRobo=1;
            }
            else
            autoRobo=0;

      if(primeraVez || (loadSet > 1))
        {
        
        if(!IsOptimization()){
            KillCmd();
         }

        

         // INICIALIZA PARA INTERES COMPUESTO
         CaesarLotsIni = CaesarLots;
         AlexanderLotsIni = AlexanderLots;
         HannibalLotsIni = HannibalLots;
         CaesarLotsIni2 = CaesarLots;
         AlexanderLotsIni2 = AlexanderLots;
         HannibalLotsIni2 = HannibalLots;
         balanceInicial = AccountInfoDouble(ACCOUNT_BALANCE);

         // INICIALIZA PARA DECREMENTO EXPONENTE
         CaesarLotExponentIni = CaesarLotExponent;
         AlexanderLotExponentIni = AlexanderLotExponent;
         HannibalLotExponentIni = HannibalLotExponent;

         // INICIALIZA PARA OPERATIVA CENTURION
         centuAperturaIni = centuApertura;
         centuCierreIni = centuCierre;

         // INICIALIZA PARA CIERRE POR PROFIT DIARIO
         TrailingGAStartIni = TrailingGAStart;
         TrailingGAStopIni = TrailingGAStop;
         TrailingGABeneDiaIni = TrailingGABeneDia;

         LimitCloseIni = LimitClose;

         multiplicadorCierreParcialLocal = 1.0;


         CaesarLotsLast = CaesarLots;
         AlexanderLotsLast = AlexanderLots;
         HannibalLotsLast = HannibalLots;








         robotReturn.lotajeCruceLineaM1=infoLotMin;
         robotReturn.lotajeCruceLineaM2=infoLotMin;

         primeraVez = false;

         acumCicloJTime = TimeCurrent();
         acumCicloJ = 0;
         acumCicloATime = TimeCurrent();
         acumCicloA = 0;
         acumCicloHTime = TimeCurrent();
         acumCicloH = 0;


         tiempoInicio = TimeCurrent();


         exit = 0;
         
         
         
         
            
         if (tipoCuenta!=lastTipoCuenta)
         {   
         if (tipoCuenta<0)
            tipoCuenta=Roboforex;
            
         switch(tipoCuenta)
           {
            case  0:


    string brokerLower = AccountInfoString(ACCOUNT_COMPANY);
    StringToLower(brokerLower);
   
   
    if((StringFind(brokerLower, "roboforex") >= 0) && (StringFind(AccountServer(),"ProCent")<0))
        tipoCuen = "0"; // Auto-Deteccion de cuenta Standard (0) para Roboforex
    if((StringFind(brokerLower, "roboforex") >= 0) && (StringFind(AccountServer(),"ProCent")>=0))
        tipoCuen = "1"; // Auto-Deteccion de cuenta Cent (1) para Roboforex   
        

               if(tipoCuen == "1")
                 {
                  tipoCuentaDouble = 100;
                  tipoCuenta =100;
                  GlobalVariableSet("tcuen-"+AccountInfoInteger(ACCOUNT_LOGIN),tipoCuentaDouble);
                 }
               else
                 {
                  tipoCuentaDouble = 1;
                  tipoCuenta =1;
                  GlobalVariableSet("tcuen-"+AccountInfoInteger(ACCOUNT_LOGIN),tipoCuentaDouble);
                 }

               break;
            case  1:
               tipoCuen = "0";
               tipoCuentaDouble = 1.00;
               GlobalVariableSet("tcuen-"+AccountInfoInteger(ACCOUNT_LOGIN),tipoCuentaDouble);

               break;
            case  100:

               tipoCuen = "1";
               tipoCuentaDouble = 100.00;
               GlobalVariableSet("tcuen-"+AccountInfoInteger(ACCOUNT_LOGIN),tipoCuentaDouble);

               break;
            default:
               break;
           }
lastTipoCuenta=tipoCuenta;

if(!autoRobo && !IsOptimization() && !IsTesting())
{
   Sleep(200);
   TerminalClose(0);
   HayUpdate(-10);
   return;
}

}
   else
   {

                  
               if (!GlobalVariableGet("tcuen-"+AccountInfoInteger(ACCOUNT_LOGIN), tipoCuentaDouble))
               {
                  tipoCuentaDouble=1.00;
               }
               
               if ((int)tipoCuentaDouble==1)
                  tipoCuen="0";
               if ((int)tipoCuentaDouble==100)
                  tipoCuen="1";
                  
               tipoCuenta=tipoCuentaDouble;  
   }      
         

         int lacertaCaudaint = 0, CaesarAutoPriceAverageint = 0, AlexanderAutoPriceAverageint = 0, HannibalAutoPriceAverageint = 0, CaesarUseTrailingStopint = 0, AlexanderUseTrailingStopint = 0, HannibalUseTrailingStopint = 0;
         int limiteSpreadint = 0, limiteSeparacionint = 0, AceleraBTPanelint = 0, TipoCuentaRobot = 0;
         int reEntradasCaesarInt = 0, reEntradasAlexanderInt = 0, reEntradasHannibalInt = 0;
         int CaesarCambiaExpActivoInt = 0, AlexanderCambiaExpActivoInt = 0, HannibalCambiaExpActivoInt = 0;
         if(reEntradasCaesar && CaesarActivo)
           {
            reEntradasCaesarInt = 1;
           }
         if(reEntradasAlexander && AlexanderActivo)
           {
            reEntradasAlexanderInt = 1;
           }
         if(reEntradasHannibal && HannibalActivo)
           {
            reEntradasHannibalInt = 1;
           }
         if(lacertaCauda)
           {
            lacertaCaudaint = 1;
           }
         if(CaesarCambiaExpActivo)
           {
            CaesarCambiaExpActivoInt = 1;
           }
         if(AlexanderCambiaExpActivo)
           {
            AlexanderCambiaExpActivoInt = 1;
           }
         if(HannibalCambiaExpActivo)
           {
            HannibalCambiaExpActivoInt = 1;
           }
         if(CaesarAutoPriceAverage)
           {
            CaesarAutoPriceAverageint = 1;
           }
         if(AlexanderAutoPriceAverage)
           {
            AlexanderAutoPriceAverageint = 1;
           }
         if(HannibalAutoPriceAverage)
           {
            HannibalAutoPriceAverageint = 1;
           }
         if(CaesarUseTrailingStop)
           {
            CaesarUseTrailingStopint = 1;
           }
         if(AlexanderUseTrailingStop)
           {
            AlexanderUseTrailingStopint = 1;
           }
         if(HannibalUseTrailingStop)
           {
            HannibalUseTrailingStopint = 1;
           }
         if(limiteSpread)
           {
            limiteSpreadint = 1;
           }
         if(limiteSeparacion)
           {
            limiteSeparacionint = 1;
           }
         if(AceleraBTPanel)
           {
            AceleraBTPanelint = 1;
           }

         ZeroMemory(robotReturn);
         ZeroMemory(robotSend);

         robotReturn.lotajeManualCfg=infoLotMin;
         robotReturn.lotajeCruceLineaM1=infoLotMin;
         robotReturn.lotajeCruceLineaM2=infoLotMin;

         robotReturn.reEntradasCaesar = reEntradasCaesarInt;
         robotReturn.reEntradasAlexander = reEntradasAlexanderInt;
         robotReturn.reEntradasHannibal = reEntradasHannibalInt;

         robotReturn.reStopCaesar = reStopCaesar;
         robotReturn.reStopAlexander = reStopAlexander;
         robotReturn.reStopHannibal = reStopHannibal;

         robotReturn.reDistanciaCaesar = reDistanciaCaesar;
         robotReturn.reDistanciaAlexander = reDistanciaAlexander;
         robotReturn.reDistanciaHannibal = reDistanciaHannibal;

         robotReturn.reLotajeCaesar = reLotajeCaesar;
         robotReturn.reLotajeAlexander = reLotajeAlexander;
         robotReturn.reLotajeHannibal = reLotajeHannibal;

         robotReturn.CaesarActividad = (int)CaesarActividad;
         robotReturn.CaesarAutoPriceAverage = CaesarAutoPriceAverageint;
         robotReturn.CaesarLotExp = CaesarLotExponent;
         robotReturn.CaesarLots = CaesarLots;
         robotReturn.CaesarMaxLots = maxLotsCaesar;
         robotReturn.CaesarCambiaExpActivo = CaesarCambiaExpActivoInt;
         robotReturn.CaesarCambiaExpDesdeMartingala = CaesarCambiaExpDesdeMartingala;
         robotReturn.CaesarCambiaExpValor = CaesarCambiaExpValor;
         robotReturn.CaesarMaxTrades = (int)MaxTrades_Caesar;
         robotReturn.CaesarPipStep = (int)CaesarPipStep;
         robotReturn.CaesarModoPipStep = (int)CaesarModoPipStep;
         robotReturn.CaesarTP = (int)CaesarTakeProfit;
         robotReturn.CaesarModoTp = (int)CaesarModoTp;
         robotReturn.CaesarTimeFrame = (int)CaesarSeleccionTF;
         robotReturn.CaesarInterTimeFrame = (int)CaesarIntervaloTF;
         robotReturn.CaesarTipoOp = (int)CaesarTipoOperacion;
         robotReturn.CaesarTrailingStart = (int)CaesarTrailStart;
         robotReturn.CaesarTrailingStop = (int)CaesarTrailStop;
         robotReturn.CaesarUseTrailing = (int)CaesarUseTrailingStopint;

         robotReturn.tipoCruceLineaJ1 = (int)tipoCruceLineaJ1;
         robotReturn.direcCruceLineaJ1 = (int)direcCruceLineaJ1;
         robotReturn.colorCruceLineaJ1 = (int)colorCruceLineaJ1;
         robotReturn.stopCruceLineaJ1 = (int)stopCruceLineaJ1;
         robotReturn.colorStopCruceLineaJ1 = (int)colorStopCruceLineaJ1;
         robotReturn.tipoCruceLineaJ2 = (int)tipoCruceLineaJ2;
         robotReturn.direcCruceLineaJ2 = (int)direcCruceLineaJ2;
         robotReturn.colorCruceLineaJ2 = (int)colorCruceLineaJ2;
         robotReturn.stopCruceLineaJ2 = (int)stopCruceLineaJ2;
         robotReturn.colorStopCruceLineaJ2 = (int)colorStopCruceLineaJ2;

         robotReturn.AlexActividad = (int) AlexanderActividad;
         robotReturn.AlexAutoPriceAverage = AlexanderAutoPriceAverageint;
         robotReturn.AlexLotExp = AlexanderLotExponent;
         robotReturn.AlexLots = AlexanderLots;
         robotReturn.AlexMaxLots = maxLotsAlexander;
         robotReturn.AlexanderCambiaExpActivo = AlexanderCambiaExpActivoInt;
         robotReturn.AlexanderCambiaExpDesdeMartingala = AlexanderCambiaExpDesdeMartingala;
         robotReturn.AlexanderCambiaExpValor = AlexanderCambiaExpValor;
         robotReturn.AlexMaxTrades = (int)MaxTrades_Alexander;
         robotReturn.AlexPipStep = (int)AlexanderPipStep;
         robotReturn.AlexModoPipStep = (int)AlexanderModoPipStep;
         robotReturn.AlexTP = (int)AlexanderTakeProfit;
         robotReturn.AlexModoTp = (int)AlexanderModoTp;
         robotReturn.AlexTimeFrame = (int)AlexanderSeleccionTF;
         robotReturn.AlexInterTimeFrame = (int)AlexanderIntervaloTF;
         robotReturn.AlexTipoOp = (int)AlexanderTipoOperacion;
         robotReturn.AlexTrailingStart = (int)AlexanderTrailStart;
         robotReturn.AlexTrailingStop = (int)AlexanderTrailStop;
         robotReturn.AlexUseTrailing = (int)AlexanderUseTrailingStopint;

         robotReturn.tipoCruceLineaA1 = (int)tipoCruceLineaA1;
         robotReturn.direcCruceLineaA1 = (int)direcCruceLineaA1;
         robotReturn.colorCruceLineaA1 = (int)colorCruceLineaA1;
         robotReturn.stopCruceLineaA1 = (int)stopCruceLineaA1;
         robotReturn.colorStopCruceLineaA1 = (int)colorStopCruceLineaA1;
         robotReturn.tipoCruceLineaA2 = (int)tipoCruceLineaA2;
         robotReturn.direcCruceLineaA2 = (int)direcCruceLineaA2;
         robotReturn.colorCruceLineaA2 = (int)colorCruceLineaA2;
         robotReturn.stopCruceLineaA2 = (int)stopCruceLineaA2;
         robotReturn.colorStopCruceLineaA2 = (int)colorStopCruceLineaA2;

         robotReturn.HannibalActividad = (int) HannibalActividad;
         robotReturn.HannibalAutoPriceAverage = HannibalAutoPriceAverageint;
         robotReturn.HannibalLotExp = HannibalLotExponent;
         robotReturn.HannibalLots = HannibalLots;
         robotReturn.HannibalMaxLots = maxLotsHannibal;
         robotReturn.HannibalCambiaExpActivo = HannibalCambiaExpActivoInt;
         robotReturn.HannibalCambiaExpDesdeMartingala = HannibalCambiaExpDesdeMartingala;
         robotReturn.HannibalCambiaExpValor = HannibalCambiaExpValor;
         robotReturn.HannibalMaxTrades = (int)MaxTrades_Hannibal;
         robotReturn.HannibalPipStep = (int)HannibalPipStep;
         robotReturn.HannibalModoPipStep = (int)HannibalModoPipStep;
         robotReturn.HannibalTP = (int)HannibalTakeProfit;
         robotReturn.HannibalModoTp = (int)HannibalModoTp;
         robotReturn.HannibalTimeFrame = (int)HannibalSeleccionTF;
         robotReturn.HannibalInterTimeFrame = (int)HannibalIntervaloTF;
         robotReturn.HannibalTipoOp = (int)HannibalTipoOperacion;
         robotReturn.HannibalTrailingStart = (int)HannibalTrailStart;
         robotReturn.HannibalTrailingStop = (int)HannibalTrailStop;
         robotReturn.HannibalUseTrailing = (int)HannibalUseTrailingStopint;

         robotReturn.tipoCruceLineaH1 = (int)tipoCruceLineaH1;
         robotReturn.direcCruceLineaH1 = (int)direcCruceLineaH1;
         robotReturn.colorCruceLineaH1 = (int)colorCruceLineaH1;
         robotReturn.stopCruceLineaH1 = (int)stopCruceLineaH1;
         robotReturn.colorStopCruceLineaH1 = (int)colorStopCruceLineaH1;
         robotReturn.tipoCruceLineaH2 = (int)tipoCruceLineaH2;
         robotReturn.direcCruceLineaH2 = (int)direcCruceLineaH2;
         robotReturn.colorCruceLineaH2 = (int)colorCruceLineaH2;
         robotReturn.stopCruceLineaH2 = (int)stopCruceLineaH2;
         robotReturn.colorStopCruceLineaH2 = (int)colorStopCruceLineaH2;

         robotReturn.tipoCruceLineaM1 = (int)tipoCruceLineaM1;
         robotReturn.direcCruceLineaM1 = (int)direcCruceLineaM1;
         robotReturn.colorCruceLineaM1 = (int)colorCruceLineaM1;
         robotReturn.stopCruceLineaM1 = (int)stopCruceLineaM1;
         robotReturn.colorStopCruceLineaM1 = (int)colorStopCruceLineaM1;
         robotReturn.tipoCruceLineaM2 = (int)tipoCruceLineaM2;
         robotReturn.direcCruceLineaM2 = (int)direcCruceLineaM2;
         robotReturn.colorCruceLineaM2 = (int)colorCruceLineaM2;
         robotReturn.stopCruceLineaM2 = (int)stopCruceLineaM2;
         robotReturn.colorStopCruceLineaM2 = (int)colorStopCruceLineaM2;

         robotReturn.HoraServer = TimeCurrent();

         robotReturn.ModoOperativa = (int)modoOperativa; // era -1
         robotReturn.MinSeparacionActiva = limiteSeparacionint;
         robotReturn.MinSeparacion = (int)minSeparacionCaudillos;
         robotReturn.MaxSpreadActivo = limiteSpreadint;
         robotReturn.MaxSpread = (int)maxSpread;
         robotReturn.LacertaActiva = lacertaCaudaint;
         robotReturn.LacertaFlotante = lacertaCaudaFlotante;
         robotReturn.AceleraBTPanel = AceleraBTPanelint;
         robotReturn.Cuenta = (int)AccountInfoInteger(ACCOUNT_LOGIN);
         robotReturn.TipoCuenta = (int)tipoCuentaDouble;
         robotReturn.Palanca = (int)AccountInfoInteger(ACCOUNT_LEVERAGE);

         robotReturn.magicNumberCaesar = MagicNumber_Caesar;
         robotReturn.magicNumberAlex = MagicNumber_Alexander;
         robotReturn.magicNumberHannibal = MagicNumber_Hannibal;
         robotReturn.CaesarInterTimeFrame = (int)CaesarIntervaloTF;
         robotReturn.AlexInterTimeFrame = (int)AlexanderIntervaloTF;
         robotReturn.HannibalInterTimeFrame = (int)HannibalIntervaloTF;

         robotReturn.VolatilidadAdjJ = VolatilidadAdjJ;
         robotReturn.HolguraAdjJ = HolguraAdjJ;

         robotReturn.VolatilidadAdjA = VolatilidadAdjA;
         robotReturn.HolguraAdjA = HolguraAdjA;

         robotReturn.VolatilidadAdjH = VolatilidadAdjH;
         robotReturn.HolguraAdjH = HolguraAdjH;

         robotReturn.minLotCuenta = infoLotMin;

         robotReturn.centuApertura = centuApertura;
         robotReturn.centuCierre = centuCierre;

         if(TrailingGAActivo)
           {
            robotReturn.TrailingGAActivo = 1;
           }
         else
           {
            robotReturn.TrailingGAActivo = 0;
           }
         robotReturn.TrailingGAStart = TrailingGAStart;
         robotReturn.TrailingGAStop = TrailingGAStop;
         robotReturn.TrailingGABeneDia = TrailingGABeneDia;
         robotReturn.TrailingGATipoCierre = TrailingGATipoCierre;

         if(TrailingREActivo)
           {
            robotReturn.TrailingREActivo = 1;
           }
         else
           {
            robotReturn.TrailingREActivo = 0;
           }
         robotReturn.TrailingREStart = TrailingREStart;
         robotReturn.TrailingREStop = TrailingREStop;
         robotReturn.TrailingREMinima = TrailingREMinima;
         robotReturn.TrailingRETipoCierre = TrailingRETipoCierres;

         robotReturn.InteresCompuesto = InteresCompuesto;
         robotReturn.balanceBase = BalanceBase;
         
         robotReturn.lotExact = LotExact;

         robotReturn.BuysErroneas = BuysErroneas;
         robotReturn.SellsErroneas = SellsErroneas;

         robotReturn.Enero = Enero;
         robotReturn.Febrero = Febrero;
         robotReturn.Marzo = Marzo;
         robotReturn.Abril = Abril;
         robotReturn.Mayo = Mayo;
         robotReturn.Junio = Junio;
         robotReturn.Julio = Julio;
         robotReturn.Agosto = Agosto;
         robotReturn.Septiembre = Septiembre;
         robotReturn.Octubre = Octubre;
         robotReturn.Noviembre = Noviembre;
         robotReturn.Diciembre = Diciembre;

         robotReturn.AlsoClose = AlsoClose;
         robotReturn.LimitClose = LimitClose;

         robotReturn.LunesJ = LunesJ;
         robotReturn.MartesJ = MartesJ;
         robotReturn.MiercolesJ = MiercolesJ;
         robotReturn.JuevesJ = JuevesJ;
         robotReturn.ViernesJ = ViernesJ;
         robotReturn.SabadoJ = SabadoJ;
         robotReturn.DomingoJ = DomingoJ;
         robotReturn.desdeDiaJ = desdeDiaJ;
         robotReturn.hastaDiaJ = hastaDiaJ;
         robotReturn.desdeHoraJ = desdeHoraJ;
         robotReturn.hastaHoraJ = hastaHoraJ;
         robotReturn.desdeMinutoJ = desdeMinutoJ;
         robotReturn.hastaMinutoJ = hastaMinutoJ;
         robotReturn.InvierteTiempoJ = InvierteTiempoJ;

         robotReturn.LunesA = LunesA;
         robotReturn.MartesA = MartesA;
         robotReturn.MiercolesA = MiercolesA;
         robotReturn.JuevesA = JuevesA;
         robotReturn.ViernesA = ViernesA;
         robotReturn.SabadoA = SabadoA;
         robotReturn.DomingoA = DomingoA;
         robotReturn.desdeDiaA = desdeDiaA;
         robotReturn.hastaDiaA = hastaDiaA;
         robotReturn.desdeHoraA = desdeHoraA;
         robotReturn.hastaHoraA = hastaHoraA;
         robotReturn.desdeMinutoA = desdeMinutoA;
         robotReturn.hastaMinutoA = hastaMinutoA;
         robotReturn.InvierteTiempoA = InvierteTiempoA;

         robotReturn.LunesH = LunesH;
         robotReturn.MartesH = MartesH;
         robotReturn.MiercolesH = MiercolesH;
         robotReturn.JuevesH = JuevesH;
         robotReturn.ViernesH = ViernesH;
         robotReturn.SabadoH = SabadoH;
         robotReturn.DomingoH = DomingoH;
         robotReturn.desdeDiaH = desdeDiaH;
         robotReturn.hastaDiaH = hastaDiaH;
         robotReturn.desdeHoraH = desdeHoraH;
         robotReturn.hastaHoraH = hastaHoraH;
         robotReturn.desdeMinutoH = desdeMinutoH;
         robotReturn.hastaMinutoH = hastaMinutoH;
         robotReturn.InvierteTiempoH = InvierteTiempoH;

         robotReturn.TipoFormacion = (int)tipoFormacion;

         robotReturn.multiplicadorCierreParcial = multiplicadorCierreParcialLocal;

         robotReturn.lotajePrimeraCaesar = (lotajePrimeraCaesar);
         robotReturn.lotajePrimeraAlexander = (lotajePrimeraAlexander);
         robotReturn.lotajePrimeraHannibal = (lotajePrimeraHannibal);


         long resultH = -1;
         int hWnd1=0;
         
         conta=0;
         while(hWnd1==0 && (IsVisualMode()||!IsTesting()))
           {
            if(ChartGetInteger(ChartID(), CHART_WINDOW_HANDLE, 0, resultH))
              {
               hWnd1 = (int)resultH;
              }
            else
              {
               hWnd1 = WindowHandle(Symbol(), PERIOD_CURRENT);
              }
              conta=conta+1;
              if (conta>50)
              {
                 hWnd1=hWnd1back;
                 break;
              }
           }
           hWnd1back=hWnd1;
         robotReturn.parHwnd = hWnd1;//WindowHandle(Symbol(), Period());


         IsPined = IsTopWindow();

         if(IsPined == 0)
           {
            IsPined = 2;
           }

         robotSend.IsPined = IsPined;








         if(!IsOptimization() && ((IsVisualMode() && IsTesting()) || !IsTesting()))
           {

            int numAccount = AccountNumber();
            string par = Symbol();
            int testing = (int)IsTesting() + (int)IsOptimization();
            //int loadSet = TerminalInfoInteger(TERMINAL_KEYSTATE_CONTROL);
            Comment("Inicializando...");
            string mgList = SacaMNdePar();
            string dataPath="";
                   StringInit(dataPath,256," "); 
                   dataPath = TerminalInfoString(TERMINAL_DATA_PATH);
            robotSend.exit = 0;
            robotAcMan.ManuDigits=Digits();
            conta=0;
            do
            {
               numRobot=0;
               numRobot = InitPanel(robotReturn, numRobot, numAccount, testing, loadSet, par, mgList,dataPath);
               Sleep(1);
               conta=conta+1;
               if (conta>50)
               {
                  Print("******************* Error loop");
                  Alert("Par bloqueado, reinicie Metatrader.");
                  ExpertRemove();
                  break;
               }
            }
            while(numRobot<0);
            
            
            
            
            
            
            
            
           
            
            
            
               tipoCuenta=robotReturn.TipoCuenta;
            
               if (tipoCuenta==1)
                  tipoCuen="0";
               if (tipoCuenta==100)
                  tipoCuen="1";
                  
               tipoCuentaDouble=tipoCuenta;            
            
            
            
              
              
            
            
            if (!GlobalVariableCheck("inicio"))
            {
               GlobalVariableTemp("inicio");
               bloqueoTemporal=TimeLocal()+50;
               GlobalVariableSet("inicio",(double)TimeLocal());
            }
            else
            {
               Sleep(10);
               double doubleTime=(double)TimeLocal();
               GlobalVariableGet("inicio",doubleTime);
               bloqueoTemporal=((datetime)doubleTime)+40;
            }
            

            robotSend.exit = 0;
            exit = 0;

            MagicNumber_Caesar = robotReturn.magicNumberCaesar;
            MagicNumber_Alexander = robotReturn.magicNumberAlex;
            MagicNumber_Hannibal = robotReturn.magicNumberHannibal;

            if(ObjectFind("MNLabel")<0)
              {
               pintarEtiquetas("MNLabel", 4, 100, 10000, ""+MagicNumber_Caesar, 8, "Arial", ChartBackColorGet());
              }
           }

         //////////////////////////////////////////////////////////////////////////Sleep(100 + numRobot * 100);
         // FONDOS
         //size: 987x809
         if(ChartBackColorGet() == 12630450) // LIGHT
           {
            string filename = "\\Images\\BotvestingFondoLight.bmp";

            ObjectCreate("DucibusBackground", OBJ_BITMAP_LABEL, 0, 0, 0);
            ObjectSetInteger(0, "DucibusBackground", OBJPROP_BACK, 1);
            ObjectSetInteger(0, "DucibusBackground", OBJPROP_XDISTANCE, 0);
            ObjectSetInteger(0, "DucibusBackground", OBJPROP_YDISTANCE, 0);
            ObjectSetString(0, "DucibusBackground", OBJPROP_BMPFILE, filename);
            ObjectSetString(0, "DucibusBackground", OBJPROP_HIDDEN,true);

           }
         if(ChartBackColorGet() == 4866102) // MEDIUM
           {
            filename = "\\Images\\BotvestingFondoMedium.bmp";
            ObjectCreate("DucibusBackground", OBJ_BITMAP_LABEL, 0, 0, 0);
            ObjectSetInteger(0, "DucibusBackground", OBJPROP_BACK, 1);
            ObjectSetInteger(0, "DucibusBackground", OBJPROP_XDISTANCE, 0);
            ObjectSetInteger(0, "DucibusBackground", OBJPROP_YDISTANCE, 0);
            ObjectSetString(0, "DucibusBackground", OBJPROP_BMPFILE, filename);

           }
         if(ChartBackColorGet() == 0) // DARK
           {
            filename = "\\Images\\BotvestingFondoDark.bmp";
            ObjectCreate("DucibusBackground", OBJ_BITMAP_LABEL, 0, 0, 0);
            ObjectSetInteger(0, "DucibusBackground", OBJPROP_BACK, 1);
            ObjectSetInteger(0, "DucibusBackground", OBJPROP_XDISTANCE, 0);
            ObjectSetInteger(0, "DucibusBackground", OBJPROP_YDISTANCE, 0);
            ObjectSetString(0, "DucibusBackground", OBJPROP_BMPFILE, filename);

           }


            ChartSetInteger(0, CHART_MODE, CHART_CANDLES);
            ChartSetInteger(0, CHART_SHOW_GRID, false);
            ChartSetInteger(0, CHART_SHOW_PERIOD_SEP, false);
            ChartSetInteger(0,CHART_SHOW_OBJECT_DESCR,0,false);



         horaInicio = TimeCurrent() + 5 + numRobot;


         // INICIALIZA PARA INTERES COMPUESTO
         CaesarLotsIni = CaesarLots;
         AlexanderLotsIni = AlexanderLots;
         HannibalLotsIni = HannibalLots;
         balanceInicial = AccountInfoDouble(ACCOUNT_BALANCE);

         CaesarLotsIni2 = CaesarLots;
         AlexanderLotsIni2 = AlexanderLots;
         HannibalLotsIni2 = HannibalLots;

         // INICIALIZA PARA DECREMENTO EXPONENTE
         CaesarLotExponentIni = CaesarLotExponent;
         AlexanderLotExponentIni = AlexanderLotExponent;
         HannibalLotExponentIni = HannibalLotExponent;

         // INICIALIZA PARA OPERATIVA CENTURION
         centuAperturaIni = centuApertura;
         centuCierreIni = centuCierre;

         // INICIALIZA PARA CIERRE POR PROFIT DIARIO
         TrailingGAStartIni = TrailingGAStart;
         TrailingGAStopIni = TrailingGAStop;
         TrailingGABeneDiaIni = TrailingGABeneDia;

         LimitCloseIni = LimitClose;



         if (numRobot==0) Botlidator(numRobot, true, -1); //3,4,7,8=mal //Llama a Botlidator cuando se inserta el bot en la grafica.

         tiempoInicializacion=TimeLocal(); ////////////////////////////////////////////////
         EventKillTimer();         
         Sleep(10+(numRobot*10));

         valorTimer=75;
         while(!EventSetMillisecondTimer(valorTimer) && (!IsTesting() && !IsOptimization()) && valorTimer<666)
           {
            valorTimer =+ 25;
            Sleep(1);
           }
           if (valorTimer>=666)
           {
            Print("Error Timer");
            Alert("Error Timer");
           }
         if(numRobot==0)
           {
            Sleep(100);
            tiempoLocalElapsed=TimeLocal()+5;
           }
         else
           {
            Sleep(50);
            tiempoLocalElapsed=TimeLocal()+4;
           }



         infoLotStep = MarketInfo(Symbol(), MODE_LOTSTEP);
         infoLotMax = MarketInfo(Symbol(), MODE_MAXLOT);
         infoLotMin = MarketInfo(Symbol(), MODE_MINLOT);

         robotAcMan.ManuLotajeFijo=infoLotMin;

         


         tiempodesDeInicio=TimeLocal();


        } // FIN PRIMERA VEZ //////////////////////////////////////////////////////////////////////



      spreadActual = (Ask - Bid)/Point;
      //spreadAnt20 = spreadActual;
      //spreadAnt19 = spreadActual;
      //spreadAnt18 = spreadActual;
      //spreadAnt17 = spreadActual;
      //spreadAnt16 = spreadActual;
      //spreadAnt15 = spreadActual;
      //spreadAnt14 = spreadActual;
      //spreadAnt13 = spreadActual;
      //spreadAnt12 = spreadActual;
      //spreadAnt11 = spreadActual;
      //spreadAnt10 = spreadActual;
      //spreadAnt9 = spreadActual;
      //spreadAnt8 = spreadActual;
      //spreadAnt7 = spreadActual;
      //spreadAnt6 = spreadActual;
      //spreadAnt5 = spreadActual;
      //spreadAnt4 = spreadActual;
      //spreadAnt3 = spreadActual;
      //spreadAnt2 = spreadActual;
      spreadAnt1 = spreadActual;


      primerBotlidator = 0;

      HideTestIndicators(true);





      infoStopMin = (int)MarketInfo(Symbol(), MODE_STOPLEVEL) + MarketInfo(Symbol(), MODE_SPREAD);
      if((int)MarketInfo(Symbol(), MODE_STOPLEVEL) == 0)
        {
         infoStopMin = 40 + MarketInfo(Symbol(), MODE_SPREAD);
        }


      CaesarCambioMinuto = iTime(NULL,PERIOD_M1, 0);
      AlexanderCambioMinuto = iTime(NULL,PERIOD_M1, 0);
      HannibalCambioMinuto = iTime(NULL,PERIOD_M1, 0);



      ChartSetInteger(0, CHART_SHOW_OBJECT_DESCR, true); //Muestra texto de objectos como la linea TrailingStart (Nico)

      if(mostrarPanel == Normal || (!IsOptimization() && ((IsVisualMode() && IsTesting()) || !IsTesting())))
        {
         mostrarPanelStandard();
        }
      else
         ocultarPanel();


      prevPriceBid = Bid;
      prevPriceAsk = Ask;


      /// Ajustalotaje inicial al de la primera orden del ciclo //////////////////////
      countTradesAlexanderVar = CountTrades_AlexanderX();
      countTradesHannibalVar = CountTrades_HannibalX();
      countTradesCaesarVar = CountTrades_CaesarX();


      if(countTradesCaesarVar>0)
        {
         CaesarLots=lotajePrimeraCaesar;// = lotajePrimera;
        }
      else
        {
         lotajePrimeraCaesar = CaesarLots;
        }
      if(countTradesAlexanderVar>0)
        {
         AlexanderLots=lotajePrimeraAlexander;// = lotajePrimera;
        }
      else
        {
         lotajePrimeraAlexander = AlexanderLots;
        }
      if(countTradesHannibalVar>0)
        {
         HannibalLots=lotajePrimeraHannibal;// = lotajePrimera;
        }
      else
        {
         lotajePrimeraHannibal = HannibalLots;
        }
      ////////////////////////////////////////////////////////////////////////////////



     } // if no template

#ifdef UsaCSV

   InInit();

#endif

   if(DayOfWeek()==0 || DayOfWeek()==6)
     {
      Comment("* Mercado Cerrado *");
     }
   else
     {
      Comment("Esperando Tick...");
     }

   ObjectCreate("TRV",OBJ_LABEL,0,0,0);
   ObjectSet("TRV",OBJPROP_CORNER,3);
   ObjectSet("TRV",OBJPROP_XDISTANCE,5);
   ObjectSet("TRV",OBJPROP_YDISTANCE,3);
   ObjectSetText("TRV", "00:00:00",12,"verdana",clrDarkGray);
   //robotAcMan.ManuDigits=Digits();

   if(ObjectFind(0,"AskLine")>=0)
      ObjectDelete("AskLine");
   if(ObjectFind(0,"reg")>=0)
      ObjectDelete("reg");


   switch(Period())
     {
      case 1:
         string temporalidad="M1";
         break;
      case 5:
         temporalidad="M5";
         break;
      case 15:
         temporalidad="M15";
         break;
      case 30:
         temporalidad="M30";
         break;
      case 60:
         temporalidad="H1";
         break;
      case 240:
         temporalidad="H4";
         break;
      case 1440:
         temporalidad="D1";
         break;
      case 10080:
         temporalidad="W1";
         break;
      case 43200:
         temporalidad="MN";
         break;
      default:
         temporalidad="";
         break;
     }


   if(ObjectFind("ParTemp")>=0)
     {
      ObjectDelete("ParTemp");
     }
     
   if(ChartBackColorGet() == 12630450) // LIGHT 0xC0B9B2
     {
      pintarEtiquetas("ParTemp",3,10,10,Symbol()+" "+temporalidad,100,"impact",0xC0B9B2|0x0f0f0f,true);
     }
   if(ChartBackColorGet() == 4866102) // MEDIUM 0x4A4036
     {
      pintarEtiquetas("ParTemp",3,10,10,Symbol()+" "+temporalidad,100,"impact",0x4A4036|0x1f1f1f,true);
     }
   if(ChartBackColorGet() == 0) // DARK 0x000000
     {
      pintarEtiquetas("ParTemp",3,10,10,Symbol()+" "+temporalidad,100,"impact",0x000000|0x1f1f1f,true);
     }



   if (lastReason!=3)
   {
      Sleep(2000);
   }

   return INIT_SUCCEEDED;

  }





bool SaveFileTime(string InpFileName)
{
   ResetLastError();
   int file_handle=FileOpen(InpFileName,FILE_SHARE_WRITE|FILE_BIN);
   if(file_handle!=INVALID_HANDLE)
     {
      FileWriteInteger(file_handle,(int)TimeLocal());
      FileFlush(file_handle); 
      FileClose(file_handle);
      return True;
     }
   else
   {
      PrintFormat("Failed to open %s file, Error code = %d",InpFileName,GetLastError());
      return False;
   }
}


datetime LoadFileTime(string InpFileName)
{

   ResetLastError();
   int file_handle=FileOpen(InpFileName,FILE_SHARE_READ|FILE_BIN);
   if(file_handle!=INVALID_HANDLE)
     {
      datetime tiempo = (datetime)FileReadInteger(file_handle);
      FileClose(file_handle);
      return tiempo;
     }
   else
      PrintFormat("Failed to open %s file, Error code = %d",InpFileName,GetLastError());
      return 0;
}


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SYRInit()
  {
   set_prevBarTime(1, 0);
   set_prevBarTime(5, 0);
   set_prevBarTime(15, 0);
   set_prevBarTime(30, 0);
   set_prevBarTime(60, 0);
   set_prevBarTime(240, 0);
   set_prevBarTime(1440, 0);
   set_prevBarTime(10080, 0);
   set_prevBarTime(43200, 0);
  }


//+------------------------------------------------------------------+
//| sends broadcast event to all open charts                         |
//+------------------------------------------------------------------+
void BroadcastEvent(long lparam, double dparam, string sparam)
  {
   int eventID = 500 - CHARTEVENT_CUSTOM;
   long currChart = ChartFirst();
   int i = 0;
   while(i < CHARTS_MAX && !IsStopped())               // We have certainly no more than CHARTS_MAX open charts
     {
      EventChartCustom(currChart, eventID, lparam, dparam, sparam);
      currChart = ChartNext(currChart); // We have received a new chart from the previous
      if(currChart == -1)
         break;        // Reached the end of the charts list
      i++;// Do not forget to increase the counter
     }
  }
//+------------------------------------------------------------------+


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {



   lastReason = reason;
   Comment("");
   
   if (GlobalVariableCheck("cargCar"))return;
   

#ifdef UsaCSV
   EnDeinit(reason); // RAFA CSV
#else
#ifdef WalkForfardPro
   EnDeinit(reason); // RAFA CSV
#else
#ifdef WalkForfardPro_Modo_Null
   EnDeinit(reason); // RAFA CSV
#endif
#endif

#endif

   AceleraBTPanel=0;


   if(reason == REASON_REMOVE || reason == REASON_PROGRAM)
     {
         if(ObjectFind("DucibusBackground") >= 0)
           {
            ObjectDelete("DucibusBackground");
           }
         if(ObjectFind("MNLabel") >= 0)
           {
            ObjectDelete("MNLabel");
           }
         if(ObjectFind("ParTemp") >= 0)
           {
            ObjectDelete("ParTemp");
           }
           
            robotSend.exit=1;
            retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
     }



   if(reason == REASON_CHARTCHANGE)
     {
      SYRDeInit();
      SYRInit();
     }
   if( (reason != REASON_CHARTCHANGE) && reason != REASON_ACCOUNT  && reason != REASON_INITFAILED && reason != REASON_TEMPLATE) // && reason!=REASON_PARAMETERS   // && reason != REASON_CLOSE
     {


      estaEnUpdate = true;
      if(reason == REASON_CLOSE)
        {
         for(int es = 1; es < 10; es++)
           {
            retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
            if(retorno>=0)
              {
               break;
              }

            retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
            if(retorno>=0)
              {
               break;
              }

            Sleep(10);


           }
         Sleep(5);
        }


      Sleep(1000);
      estaEnUpdate = false;

      
      if(reason==REASON_PARAMETERS && !IsTesting())
      {
         Alert("ATENCIÓN: Espere unos segundos hasta que se cargue el preset en el panel. Cargar presets desde la carita solo si es imprescindible.");
      }
      primeraVez = true;

   if(ObjectFind("TakeProfit Manual")>=0)
     {
      ObjectDelete("TakeProfit Manual");
     }      
   if(ObjectFind("StopLoss Manual")>=0)
     {
      ObjectDelete("StopLoss Manual");
     }      
      
     }

   if(reason == REASON_TEMPLATE)
     {
   if(ObjectFind("MNLabel")>=0)
     {
      ObjectDelete("MNLabel");
     }  

      HayUpdate(-10);

      Sleep(30000);

      estaEnUpdate = false;

      return;
     }


   ocultarPanel();

   for(int i=ObjectsTotal(ChartID()); i>=0; i--)
     {
      string name = ObjectName(ChartID(), i);
      if(StringSubstr(name,0,5) == "Order")
        {
         ObjectDelete(ChartID(), name);
        }
      if(StringSubstr(name,0,9) == "textabove")
        {
         ObjectDelete(ChartID(), name);
        }
      if(StringSubstr(name,1,9) == "BreakEven")
        {
         ObjectDelete(ChartID(), name);
        }
        
      if(StringSubstr(name,1,6) == "TStart")
        {
         ObjectDelete(ChartID(), name);
        }        
      if(name == "TRV")
        {
         ObjectDelete(ChartID(), name);
        }        
      if(name == "AskLine")
        {
         ObjectDelete(ChartID(), name);
        }        
     }



   return;
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SYRDeInit()
  {
   DeleteHLineObject(StringConcatenate(getPeriodAsString(1), " Soporte"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(1), " Resistencia"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(1), " Soporte·"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(1), " Resistencia·"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(5), " Soporte"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(5), " Resistencia"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(5), " Soporte·"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(5), " Resistencia·"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(15), " Soporte"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(15), " Resistencia"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(15), " Soporte·"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(15), " Resistencia·"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(30), " Soporte"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(30), " Resistencia"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(30), " Soporte·"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(30), " Resistencia·"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(60), " Soporte"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(60), " Resistencia"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(60), " Soporte·"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(60), " Resistencia·"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(240), " Soporte"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(240), " Resistencia"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(240), " Soporte·"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(240), " Resistencia·"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(1440), " Soporte"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(1440), " Resistencia"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(1440), " Soporte·"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(1440), " Resistencia·"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(10080), " Soporte"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(10080), " Resistencia"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(10080), " Soporte·"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(10080), " Resistencia·"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(43200), " Soporte"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(43200), " Resistencia"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(43200), " Soporte·"));
   DeleteHLineObject(StringConcatenate(getPeriodAsString(43200), " Resistencia·"));
  }



//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Diap(int ai_0, bool ai_4, int ai_8, int ai_12)
  {
   double ld_ret_20;
   if(ai_4)
     {
      ld_ret_20 = get_max(ai_0, ai_12);
      for(int li_16 = 1; li_16 < ai_8; li_16++)
         if(get_max(ai_0, ai_12 - li_16) > ld_ret_20)
            ld_ret_20 = get_max(ai_0, ai_12 - li_16);
     }
   if(!ai_4)
     {
      ld_ret_20 = get_min(ai_0, ai_12);
      for(li_16 = 1; li_16 < ai_8; li_16++)
         if(get_min(ai_0, ai_12 - li_16) < ld_ret_20)
            ld_ret_20 = get_min(ai_0, ai_12 - li_16);
     }
   return (ld_ret_20);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void EmulateDoubleBuffer(double ada_0[], int ai_4)
  {
   if(ArraySize(ada_0) < ai_4)
     {
      ArraySetAsSeries(ada_0, FALSE);
      ArrayResize(ada_0, ai_4);
      ArraySetAsSeries(ada_0, TRUE);
     }
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DeleteHLineObject(string a_name_0)
  {
   ObjectDelete(a_name_0);
   ObjectDelete(a_name_0 + "_Label");
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ShowHLineObject(string a_name_0, int ai_8, int a_style_12, double ad_16, int ai_24,int gordo)
  {
   if(ObjectFind(a_name_0) != 0)
      CreateHLineObject(a_name_0, ai_8, a_style_12, ad_16, ai_24,gordo);
   ObjectSet(a_name_0 + "_Label", OBJPROP_PRICE1, ad_16);
   ObjectSet(a_name_0 + "_Label", OBJPROP_TIME1, (Time[WindowBarsPerChart()/2] + Period() * ai_24));
   ObjectSet(a_name_0 + "_Label", OBJPROP_STYLE, a_style_12);
   ObjectSet(a_name_0 + "_Label",OBJPROP_SELECTABLE, FALSE);
   ObjectSet(a_name_0, OBJPROP_PRICE1, ad_16);
   ObjectSet(a_name_0, OBJPROP_SELECTABLE, FALSE);
   ObjectSet(a_name_0, OBJPROP_WIDTH, gordo);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CreateHLineObject(string a_text_0, color a_color_8, int a_style_12, double a_price_16, int ai_24, int gordo)
  {
   ObjectCreate(a_text_0 + "_Label", OBJ_TEXT, 0, (Time[WindowBarsPerChart()/2] + Period() * ai_24), a_price_16, gordo);
   ObjectSetText(a_text_0 + "_Label", a_text_0, 8, "Verdana",White);//a_color_8 & 0x888888);
   ObjectCreate(a_text_0, OBJ_HLINE, 0, Time[0], a_price_16);
   ObjectSet(a_text_0, OBJPROP_BACK, TRUE);
   ObjectSet(a_text_0, OBJPROP_SELECTABLE, FALSE);
   ObjectSet(a_text_0, OBJPROP_STYLE, a_style_12);
   ObjectSet(a_text_0, OBJPROP_COLOR, a_color_8);
   ObjectSet(a_text_0, OBJPROP_WIDTH, gordo);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string getPeriodAsString(int ai_0)
  {
   string ls_ret_4 = 0;
   switch(ai_0)
     {
      case 1:
         ls_ret_4 = "M1";
         break;
      case 5:
         ls_ret_4 = "M5";
         break;
      case 15:
         ls_ret_4 = "M15";
         break;
      case 30:
         ls_ret_4 = "M30";
         break;
      case 60:
         ls_ret_4 = "H1";
         break;
      case 240:
         ls_ret_4 = "H4";
         break;
      case 1440:
         ls_ret_4 = "D1";
         break;
      case 10080:
         ls_ret_4 = "W1";
         break;
      case 43200:
         ls_ret_4 = "MN1";
     }
   return (ls_ret_4);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void set_prevBarTime(int ai_0, int ai_4)
  {
   switch(ai_0)
     {
      case 1:
         lastBarTime_M1 = ai_4;
         return;
      case 5:
         lastBarTime_M5 = ai_4;
         return;
      case 15:
         lastBarTime_M15 = ai_4;
         return;
      case 30:
         lastBarTime_M30 = ai_4;
         return;
      case 60:
         lastBarTime_H1 = ai_4;
         return;
      case 240:
         lastBarTime_H4 = ai_4;
         return;
      case 1440:
         lastBarTime_D1 = ai_4;
         return;
      case 10080:
         lastBarTime_W1 = ai_4;
         return;
      case 43200:
         lastBarTime_MN1 = ai_4;
         return;
         return;
     }
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int get_prevBarTime(int ai_0)
  {
   switch(ai_0)
     {
      case 1:
         return (lastBarTime_M1);
         break;
      case 5:
         return (lastBarTime_M5);
         break;
      case 15:
         return (lastBarTime_M15);
         break;
      case 30:
         return (lastBarTime_M30);
         break;
      case 60:
         return (lastBarTime_H1);
         break;
      case 240:
         return (lastBarTime_H4);
         break;
      case 1440:
         return (lastBarTime_D1);
         break;
      case 10080:
         return (lastBarTime_W1);
         break;
      case 43200:
         return (lastBarTime_MN1);
     }
   return (0);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void set_prevBarCount(int ai_0, int ai_4)
  {
   switch(ai_0)
     {
      case 1:
         indicatorBuffer_M1 = ai_4;
         return;
      case 5:
         indicatorBuffer_M5 = ai_4;
         return;
      case 15:
         indicatorBuffer_M15 = ai_4;
         return;
      case 30:
         indicatorBuffer_M30 = ai_4;
         return;
      case 60:
         indicatorBuffer_H1 = ai_4;
         return;
      case 240:
         indicatorBuffer_H4 = ai_4;
         return;
      case 1440:
         indicatorBuffer_D1 = ai_4;
         return;
      case 10080:
         indicatorBuffer_W1 = ai_4;
         return;
      case 43200:
         indicatorBuffer_MN1 = ai_4;
         return;
         return;
     }
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int get_prevBarCount(int ai_0)
  {
   switch(ai_0)
     {
      case 1:
         return (indicatorBuffer_M1);
         break;
      case 5:
         return (indicatorBuffer_M5);
         break;
      case 15:
         return (indicatorBuffer_M15);
         break;
      case 30:
         return (indicatorBuffer_M30);
         break;
      case 60:
         return (indicatorBuffer_H1);
         break;
      case 240:
         return (indicatorBuffer_H4);
         break;
      case 1440:
         return (indicatorBuffer_D1);
         break;
      case 10080:
         return (indicatorBuffer_W1);
         break;
      case 43200:
         return (indicatorBuffer_MN1);
     }
   return (0);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void set_max(int ai_0, int ai_4, double ad_8)
  {
   if(ai_4<0)ai_4=0;
   switch(ai_0)
     {
      case 1:
         maxBuffer_M1[ai_4] = ad_8;
         return;
      case 5:
         maxBuffer_M5[ai_4] = ad_8;
         return;
      case 15:
         maxBuffer_M15[ai_4] = ad_8;
         return;
      case 30:
         maxBuffer_M30[ai_4] = ad_8;
         return;
      case 60:
         maxBuffer_H1[ai_4] = ad_8;
         return;
      case 240:
         maxBuffer_H4[ai_4] = ad_8;
         return;
      case 1440:
         maxBuffer_D1[ai_4] = ad_8;
         return;
      case 10080:
         maxBuffer_W1[ai_4] = ad_8;
         return;
      case 43200:
         maxBuffer_MN1[ai_4] = ad_8;
         return;
         return;
     }
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double get_max(int ai_0, int ai_4)
  {
  if(ai_4<0)ai_4=0;
   switch(ai_0)
     {
      case 1:
         return (maxBuffer_M1[ai_4]);
         break;
      case 5:
         return (maxBuffer_M5[ai_4]);
         break;
      case 15:
         return (maxBuffer_M15[ai_4]);
         break;
      case 30:
         return (maxBuffer_M30[ai_4]);
         break;
      case 60:
         return (maxBuffer_H1[ai_4]);
         break;
      case 240:
         return (maxBuffer_H4[ai_4]);
         break;
      case 1440:
         return (maxBuffer_D1[ai_4]);
         break;
      case 10080:
         return (maxBuffer_W1[ai_4]);
         break;
      case 43200:
         return (maxBuffer_MN1[ai_4]);
     }
   return (0.0);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void set_min(int ai_0, int ai_4, double ad_8)
  {
   if(ai_4<0)ai_4=0;
   switch(ai_0)
     {
      case 1:
         minBuffer_M1[ai_4] = ad_8;
         return;
      case 5:
         minBuffer_M5[ai_4] = ad_8;
         return;
      case 15:
         minBuffer_M15[ai_4] = ad_8;
         return;
      case 30:
         minBuffer_M30[ai_4] = ad_8;
         return;
      case 60:
         minBuffer_H1[ai_4] = ad_8;
         return;
      case 240:
         minBuffer_H4[ai_4] = ad_8;
         return;
      case 1440:
         minBuffer_D1[ai_4] = ad_8;
         return;
      case 10080:
         minBuffer_W1[ai_4] = ad_8;
         return;
      case 43200:
         minBuffer_MN1[ai_4] = ad_8;
         return;
         return;
     }
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double get_min(int ai_0, int ai_4)
  {
   if(ai_4<0)ai_4=0;
   switch(ai_0)
     {
      case 1:
         return (minBuffer_M1[ai_4]);
         break;
      case 5:
         return (minBuffer_M5[ai_4]);
         break;
      case 15:
         return (minBuffer_M15[ai_4]);
         break;
      case 30:
         return (minBuffer_M30[ai_4]);
         break;
      case 60:
         return (minBuffer_H1[ai_4]);
         break;
      case 240:
         return (minBuffer_H4[ai_4]);
         break;
      case 1440:
         return (minBuffer_D1[ai_4]);
         break;
      case 10080:
         return (minBuffer_W1[ai_4]);
         break;
      case 43200:
         return (minBuffer_MN1[ai_4]);
     }
   return (0.0);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void emulate_tlbmaxmin(int ai_0, int ai_4)
  {
   switch(ai_0)
     {
      case 1:
         EmulateDoubleBuffer(maxBuffer_M1, ai_4);
         EmulateDoubleBuffer(minBuffer_M1, ai_4);
         return;
      case 5:
         EmulateDoubleBuffer(maxBuffer_M5, ai_4);
         EmulateDoubleBuffer(minBuffer_M5, ai_4);
         return;
      case 15:
         EmulateDoubleBuffer(maxBuffer_M15, ai_4);
         EmulateDoubleBuffer(minBuffer_M15, ai_4);
         return;
      case 30:
         EmulateDoubleBuffer(maxBuffer_M30, ai_4);
         EmulateDoubleBuffer(minBuffer_M30, ai_4);
         return;
      case 60:
         EmulateDoubleBuffer(maxBuffer_H1, ai_4);
         EmulateDoubleBuffer(minBuffer_H1, ai_4);
         return;
      case 240:
         EmulateDoubleBuffer(maxBuffer_H4, ai_4);
         EmulateDoubleBuffer(minBuffer_H4, ai_4);
         return;
      case 1440:
         EmulateDoubleBuffer(maxBuffer_D1, ai_4);
         EmulateDoubleBuffer(minBuffer_D1, ai_4);
         return;
      case 10080:
         EmulateDoubleBuffer(maxBuffer_W1, ai_4);
         EmulateDoubleBuffer(minBuffer_W1, ai_4);
         return;
      case 43200:
         EmulateDoubleBuffer(maxBuffer_MN1, ai_4);
         EmulateDoubleBuffer(minBuffer_MN1, ai_4);
         return;
         return;
     }
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void displayPeriod(int a_timeframe_0, int offsetSYR, int numTempo, bool onlyPaint)
  {
   int li_4;
   int l_count_8;
   int li_12;
   int li_20;
   double ld_24;
   double ld_32;
   double ld_40;
   double ld_48;
   int l_count_56;
   int l_count_60;
   int li_unused_64;
   if(get_prevBarTime(a_timeframe_0) == 0 || get_prevBarTime(a_timeframe_0) != iTime(Symbol(), a_timeframe_0, 0) || get_prevBarCount(a_timeframe_0) == 0 || get_prevBarCount(a_timeframe_0) != iBars(Symbol(), a_timeframe_0))
     {
      set_prevBarTime(a_timeframe_0, iTime(NULL, a_timeframe_0, 0));
      set_prevBarCount(a_timeframe_0, iBars(NULL, a_timeframe_0));
      li_4 = iBars(NULL, a_timeframe_0);
      if(maxBarsForPeriod > 0 && li_4 > maxBarsForPeriod)
         li_4 = maxBarsForPeriod;
      l_count_8 = 0;
      li_12 = li_4;
      emulate_tlbmaxmin(a_timeframe_0, li_4);
      li_20 = 1;
      while(iClose(Symbol(), a_timeframe_0, li_12 - 1) == iClose(Symbol(), a_timeframe_0, li_12 - 1 - li_20))
        {
         li_20++;
         if(li_20 > li_12 - 1)
            break;
        }
      if(iClose(Symbol(), a_timeframe_0, li_12 - 1) > iClose(Symbol(), a_timeframe_0, li_12 - 1 - li_20))
        {
         set_max(a_timeframe_0, 0, iHigh(Symbol(), a_timeframe_0, li_12 - 1));
         set_min(a_timeframe_0, 0, iLow(Symbol(), a_timeframe_0, li_12 - 1 - li_20));
        }
      if(iClose(Symbol(), a_timeframe_0, li_12 - 1) < iClose(Symbol(), a_timeframe_0, li_12 - 1 - li_20))
        {
         set_max(a_timeframe_0, 0, iHigh(Symbol(), a_timeframe_0, li_12 - 1 - li_20));
         set_min(a_timeframe_0, 0, iLow(Symbol(), a_timeframe_0, li_12 - 1));
        }
      for(int li_16 = 1; li_16 < LB; li_16++)
        {
         while(iClose(Symbol(), a_timeframe_0, li_12 - li_20) <= Diap(a_timeframe_0, 1, li_16, l_count_8) && iClose(Symbol(), a_timeframe_0, li_12 - li_20) >= Diap(a_timeframe_0, 0, li_16, l_count_8))
           {
            li_20++;
            if(li_20 > li_12 - 1)
               break;
           }
         if(li_20 > li_12 - 1)
            break;
         if(iClose(Symbol(), a_timeframe_0, li_12 - li_20) > get_max(a_timeframe_0,                                                                                                                    - 1))
           {
            set_max(a_timeframe_0, li_16, iHigh(Symbol(), a_timeframe_0, li_12 - li_20));
            set_min(a_timeframe_0, li_16, get_max(a_timeframe_0, li_16 - 1));
            l_count_8++;
           }
         if(iClose(Symbol(), a_timeframe_0, li_12 - li_20) < get_min(a_timeframe_0, li_16 - 1))
           {
            set_min(a_timeframe_0, li_16, iLow(Symbol(), a_timeframe_0, li_12 - li_20));
            set_max(a_timeframe_0, li_16, get_min(a_timeframe_0, li_16 - 1));
            l_count_8++;
           }
        }
      for(li_16 = LB; li_16 < li_12; li_16++)
        {
         while(iClose(Symbol(), a_timeframe_0, li_12 - li_20) <= Diap(a_timeframe_0, 1, LB, l_count_8) && iClose(Symbol(), a_timeframe_0, li_12 - li_20) >= Diap(a_timeframe_0, 0, LB, l_count_8))
           {
            li_20++;
            if(li_20 > li_12 - 1)
               break;
           }
         if(li_20 > li_12 - 1)
            break;
         if(iClose(Symbol(), a_timeframe_0, li_12 - li_20) > get_max(a_timeframe_0, li_16 - 1))
           {
            set_max(a_timeframe_0, li_16, iHigh(Symbol(), a_timeframe_0, li_12 - li_20));
            set_min(a_timeframe_0, li_16, get_max(a_timeframe_0, li_16 - 1));
            l_count_8++;
           }
         if(iClose(Symbol(), a_timeframe_0, li_12 - li_20) < get_min(a_timeframe_0, li_16 - 1))
           {
            set_min(a_timeframe_0, li_16, iLow(Symbol(), a_timeframe_0, li_12 - li_20));
            set_max(a_timeframe_0, li_16, get_min(a_timeframe_0, li_16 - 1));
            l_count_8++;
           }
        }
      ld_24 = 0;
      ld_32 = 0;
      ld_40 = 0;
      ld_48 = 0;
      l_count_56 = 0;
      l_count_60 = 0;
      li_unused_64 = 0;
      for(li_16 = 1; li_16 <= l_count_8; li_16++)
        {
         if(get_max(a_timeframe_0, li_16) > get_max(a_timeframe_0, li_16 - 1))
           {
            if(l_count_60 >= LB)
               ld_24 = get_max(a_timeframe_0, li_16 - LB);
            else
               ld_24 = get_min(a_timeframe_0, li_16 - l_count_60 - 1);
            ld_48 = get_max(a_timeframe_0, li_16);
            ld_40 = 0;
            ld_32 = 0;
            l_count_60++;
            l_count_56 = 0;
           }
         if(get_max(a_timeframe_0, li_16) < get_max(a_timeframe_0, li_16 - 1))
           {
            if(l_count_56 >= LB)
               ld_32 = get_min(a_timeframe_0, li_16 - LB);
            else
               ld_32 = get_max(a_timeframe_0, li_16 - l_count_56 - 1);
            ld_40 = get_min(a_timeframe_0, li_16);
            ld_24 = 0;
            ld_48 = 0;
            l_count_60 = 0;
            l_count_56++;
           }
        }


      if(ld_24 > 0.0 || ld_40 > 0.0)
        {

         if(ld_24 > 0.0)
           {
            soportes[numTempo]=ld_24;
            if(!onlyPaint)
              {
               //  sopFuerte=1;
              }
            soportes[numTempo]=ld_24;
            soportesNames[numTempo]=StringConcatenate(getPeriodAsString(a_timeframe_0), " Soporte");
            ShowHLineObject(soportesNames[numTempo], clrLightCoral, STYLE_SOLID, soportes[numTempo], offsetSYR, sopFuerza[numTempo]);
           }
         if(ld_40 > 0.0)
           {
            soportes[numTempo]=ld_40;
            if(!onlyPaint)
              {
               //  sopFuerte=1;
              }
            soportes[numTempo]=ld_40;
            soportesNames[numTempo]=StringConcatenate(getPeriodAsString(a_timeframe_0), " Soporte");
            ShowHLineObject(soportesNames[numTempo], clrLightCoral, STYLE_SOLID, soportes[numTempo], offsetSYR, sopFuerza[numTempo]);
           }
        }
      else
        {
         DeleteHLineObject(StringConcatenate(getPeriodAsString(a_timeframe_0), " Soporte"));
         soportes[numTempo]=0;
         soportesNames[numTempo]="";
        }

      if(ld_32 > 0.0 || ld_48 > 0.0)
        {

         if(ld_32 > 0.0)
           {
            resistencias[numTempo]=ld_32;
            if(!onlyPaint)
              {
               // resFuerte=1;
              }
            resistencias[numTempo]=ld_32;
            resistenciasNames[numTempo]=StringConcatenate(getPeriodAsString(a_timeframe_0), " Resistencia");
            ShowHLineObject(resistenciasNames[numTempo], clrLightGreen, STYLE_SOLID, resistencias[numTempo], offsetSYR, resFuerza[numTempo]);
           }
         if(ld_48 > 0.0)
           {
            resistencias[numTempo]=ld_48;
            if(!onlyPaint)
              {
               // resFuerte=1;
              }
            resistencias[numTempo]=ld_48;
            resistenciasNames[numTempo]=StringConcatenate(getPeriodAsString(a_timeframe_0), " Resistencia");

            ShowHLineObject(resistenciasNames[numTempo], clrLightGreen, STYLE_SOLID, resistencias[numTempo], offsetSYR, resFuerza[numTempo]);

           }
        }
      else
        {
         DeleteHLineObject(StringConcatenate(getPeriodAsString(a_timeframe_0), " Resistencia"));
         resistencias[numTempo]=0;
         resistenciasNames[numTempo]="";
        }
     }
  }



//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SYROnTick()
  {
  
  
   static bool primeraVezSYR=True;
   if((altKey) || primeraVezSYR)  // 18 = Alt key // & 0x8000
     {

      if(pulsadaAltKey)
      {
         SYRInit();

         primeraVezSYR=False;
         pulsadaAltKey=False;
      }

      if(Period() <= PERIOD_H1 && showM01)
         displayPeriod(PERIOD_M1,0,1,false);
      if(Period() <= PERIOD_H1 && showM05)
         displayPeriod(PERIOD_M5,0,2,false);
      if(Period() <= PERIOD_H4 && showM15)
         displayPeriod(PERIOD_M15,0,3,false);
      if(Period() <= PERIOD_H4 && showM30)
         displayPeriod(PERIOD_M30,0,4,false);
      if(Period() <= PERIOD_W1 && showH01)
         displayPeriod(PERIOD_H1,0,5,false);
      if(Period() <= PERIOD_W1 && showH04)
         displayPeriod(PERIOD_H4,0,6,false);
      if(Period() <= PERIOD_MN1 && showD01)
         displayPeriod(PERIOD_D1,0,7,false);
      if(Period() <= PERIOD_MN1 && showW01)
         displayPeriod(PERIOD_W1,0,8,false);
      if(Period() <= PERIOD_MN1 && showMN1)
         displayPeriod(PERIOD_MN1,0,9,false);

      for(int te=1; te<=9; te++)
        {
         sopFuerte=1;
         resFuerte=1;
         for(int te2=1; te2<=9; te2++)
           {
            if(te==te2)
               continue;
            if(soportes[te]!=0 && NormalizeDouble(soportes[te],Digits()-2)==NormalizeDouble(soportes[te2],Digits()-2))
              {
               sopFuerte+=te;
              }
            if(resistencias[te]!=0 && NormalizeDouble(resistencias[te],Digits()-2)==NormalizeDouble(resistencias[te2],Digits()-2))
              {
               resFuerte+=te;
              }
           }
            int visBars=ChartGetInteger(0, CHART_VISIBLE_BARS);
            int despBar = WindowFirstVisibleBar()-visBars;
         int offset=(visBars*0.2)+(visBars/15)*(9-te);//-(WindowFirstVisibleBar()/(te/3.0)));//(((5-ChartGetInteger(0,CHART_SCALE))));
         sopFuerza[te]=sopFuerte;
         resFuerza[te]=resFuerte;
         ObjectSet(resistenciasNames[te], OBJPROP_WIDTH, resFuerte);
         ObjectSet(soportesNames[te], OBJPROP_WIDTH, sopFuerte);
         ObjectSet(resistenciasNames[te] + "_Label", OBJPROP_TIME1, Time[(visBars-offset)+despBar]);// + ((Period()*te*60));
         ObjectSet(soportesNames[te] + "_Label", OBJPROP_TIME1, Time[(visBars-offset)+despBar]);// + ((Period()*te*60));

        }
      Sleep(1);




     }
   else
     {

      if(!pulsadaAltKey)
      {
         SYRDeInit();
         pulsadaAltKey=True;
      }

     }
  }




//================================================================================================================================================================================
//================================================================================================================================================================================
//================================================================================================================================================================================
//================================================================================================================================================================================
//================================================================================================================================================================================
//================================================================================================================================================================================
//================================================================================================================================================================================
//================================================================================================================================================================================
void OnTick() 
  {

   if(primeraVez)
      return;

   static double miBid = Bid;
   static double miAsk = Ask;





#ifdef UsaCSV

   if(refrescar || (((Bid < miBid - Point * saltoDist) || (Bid > miBid + Point * saltoDist)) || ((Ask < miAsk - Point * saltoDist) || (Ask > miAsk + Point * saltoDist))))
     {
      miBid = Bid;
      miAsk = Ask;
     }
   else
     {
      enTick=false;
      return;
     }

#endif

   enTick=true;


//## Walk Forward Pro OnTick() code start (MQL4) ##
#ifdef WalkForfardPro
   if(MQLInfoInteger(MQL_TESTER))
      WFA_UpdateValues();
#endif
//## Walk Forward Pro OnTick() code end (MQL4) ##

   if(IsOptimization())
     {
      if(
         ((CaesarTipoOperacion == Tipo_Manual || CaesarIntervaloTF == 0) && AlexanderActividad == Apagado && HannibalActividad == Apagado) ||
         ((AlexanderTipoOperacion == Tipo_Manual || AlexanderIntervaloTF == 0) && CaesarActividad == Apagado && HannibalActividad == Apagado) ||
         ((HannibalTipoOperacion == Tipo_Manual || HannibalIntervaloTF == 0) && CaesarActividad == Apagado && AlexanderActividad == Apagado)
      )
        {
         enTick=false;
         return;
        }
     }


   countTradesAlexanderVar = CountTrades_AlexanderX();
   countTradesHannibalVar = CountTrades_HannibalX();
   countTradesCaptainVar = CountTrades_CapitanX();
   countTradesTotalParVar = CountTrades_TotalParX();
   countTradesCaesarVar = CountTrades_CaesarX();

   comprobarActividadCaudillos(); // TEST


// COMPROBACIONES BOTLIDATOR ///////////////////////////////////////////////////////////////////////////////////

// Comment("error: "+errorBotlidator+"    conex: "+limiteConexError);

//  if (errorBotlidator!=1 || limiteConexError!=0) return;
   if((!IsOptimization() && !IsTesting()))
     {

      // PINTA MINI LINEA ASK
      if(Ask != lastAsk)
        {
         if(ObjectFind("AskLine")>=0)
           {
            if(debeMostrarLineaAsk)
              {
               TrendPointChange(0,"AskLine",0,iTime(NULL, PERIOD_CURRENT, 1),Ask);
               TrendPointChange(0,"AskLine",1,iTime(NULL, PERIOD_CURRENT, 0),Ask);
              }
            else
              {
               ObjectDelete("AskLine");
              }
           }
         else
           {
            if(debeMostrarLineaAsk)
              {
               TrendCreate(0, "AskLine", 0, iTime(NULL, PERIOD_CURRENT, 1), Ask, iTime(NULL, PERIOD_CURRENT, 0), Ask, clrRed,0,1,false,false);
              }
           }
         lastAsk=Ask;
        }

      // ----------------------------------------------
      //if ((tiempoInicializacion>TimeLocal()-6))
      if(!botlidatorChequeado)
        {
         static int puntos=0;
         string puntoChar="#";

         for(int p = 0; p < puntos; p++)
           {
            puntoChar=puntoChar+"#";
           }
         Comment("Conectando "+puntoChar);

         enTick=false;
         puntos+=1;
         if(puntos>10)
           {
            puntos=1;
            if (IsPined==1)
            {
               Botlidator(numRobot, True, -13); 
            }
            else
            {
               Botlidator(numRobot, False, -13); 
            }
           }
        }



      if((errorBotlidator == 1))// && (countTradesCaesarVar + countTradesAlexanderVar + countTradesHannibalVar == 0))
        {
         Botlidator(numRobot, True, -12); //3,4,7,8=mal.
        }
      else
        {
         errorFijo = false;
        }

      if(((errorBotlidator == 1) && (countTradesCaesarVar + countTradesAlexanderVar + countTradesHannibalVar == 0)) || errorFijo)
        {
         if(errorFijo)
           {
            Botlidator(numRobot, True, -12); //3,4,7,8=mal.
           }
         UpdatePanelExterno();
         errorFijo = true;
         enTick=false;
         return;
        }





      // Si no hay conexión pero hay órdenes, pone TERMINANDO y cuando acabe, deja de operar.-----------------------------------------
      if(limiteConexError == 0)
        {
         errorFijo2 = false;
        }
      else
        {
         Botlidator(numRobot, false, -31); //3,4,7,8=mal
        }

      if(((limiteConexError == 1) && (countTradesCaesarVar + countTradesAlexanderVar + countTradesHannibalVar == 0)) || errorFijo2)
        {

         if(errorFijo2)
           {
            Botlidator(numRobot, false, -12); //3,4,7,8=mal.
           }
         UpdatePanelExterno();
         errorFijo2 = true;
         enTick=false;
         return;
        }
      //------------------------------------------------------------------------------------------------------------------------

     }
   else
     {
      errorBotlidator = 0;
      botlidatorChequeado=True;
     }

   if(!AccountInfoInteger(ACCOUNT_TRADE_ALLOWED) || !IsTradeAllowed() || !IsExpertEnabled())
     {
      if(allowTA)
        {
            if (IsPined==1)
            {
               Botlidator(numRobot, True, -12); 
            }
            else
            {
               Botlidator(numRobot, False, -12); 
            }
            int pausa=100;
         while(debeRepetirbotlidator && botlidatorChequeado && pausa>10)
           {
            if(pausa>10)
               pausa-=10;
            Sleep(pausa);
            if (IsPined==1)
            {
               Botlidator(numRobot, True, -12); 
            }
            else
            {
               Botlidator(numRobot, False, -12); 
            }

           }
         allowTA=false;
        }
      //        Alert(Seconds());
     }
   else
     {
      if(!allowTA)
        {
         Botlidator(numRobot, true, -12); //3,4,7,8=mal.
         pausa=100;
         while(debeRepetirbotlidator && botlidatorChequeado && pausa>10)
           {
            if(pausa>10)
               pausa-=10;
            Sleep(pausa);
            if (IsPined==1)
            {
               Botlidator(numRobot, True, -12); 
            }
            else
            {
               Botlidator(numRobot, False, -12); 
            }        
           }
         allowTA=true;
        }
     }


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////


   if(mostrarPanel == Normal)
     {

      if(IsPined!=2 && IsTesting() && IsVisualMode())      
        {

         enTick=false;
         OnTimer();
         enTick=True;

        }

     }






   if((botlidatorChequeado==false) && (!IsOptimization() && !IsTesting()))
     {
      enTick=false;
      return;
     }









   if(reEntradasCaesar || reEntradasAlexander || reEntradasHannibal)
     {

      GranH=0.00;
      GranL=0.00;
      HH=iHighest(NULL,PERIOD_H1,MODE_HIGH,24,0);
      LL=iLowest(NULL,PERIOD_H1,MODE_LOW,24,0);
      if((HH>=0 && LL>=0))
        {
         GranH=iHigh(NULL,PERIOD_H1,HH);
         GranL=iLow(NULL,PERIOD_H1,LL);
        }
      else
        {
         HH=0;
         LL=0;
        }


      GranH2=0.00;
      GranL2=0.00;
      HH2=iHighest(NULL,PERIOD_M5,MODE_HIGH,12,0);
      LL2=iLowest(NULL,PERIOD_M5,MODE_LOW,12,0);
      if((HH2>=0 && LL2>=0))
        {
         GranH2=iHigh(NULL,PERIOD_M5,HH2);
         GranL2=iLow(NULL,PERIOD_M5,LL2);
        }
      else
        {
         HH2=0;
         LL2=0;
        }

      SMA=iMA(NULL,PERIOD_M5,12,0,MODE_SMMA,PRICE_CLOSE,0);
      SMA2=iMA(NULL,PERIOD_M5,6,0,MODE_SMA,PRICE_CLOSE,0);
     }




////////////////////////////////////////////////////////////////////
   if(CaesarIntervaloTF >= 0)
     {
      if(CaesarNewCandleTime != iTime(NULL, CaesarIntervaloTF, 0))
        {
         CaesarOrdenEnEstaVela = false;
         CaesarNewCandleTime = iTime(NULL, CaesarIntervaloTF, 0);
        }
     }
   else
      CaesarOrdenEnEstaVela = false;


   if(AlexanderIntervaloTF >= 0)
     {
      if(AlexanderNewCandleTime != iTime(NULL, AlexanderIntervaloTF, 0))
        {
         AlexanderOrdenEnEstaVela = false;
         AlexanderNewCandleTime = iTime(NULL, AlexanderIntervaloTF, 0);
        }
     }
   else
      AlexanderOrdenEnEstaVela = false;


   if(HannibalIntervaloTF >= 0)
     {
      if(HannibalNewCandleTime != iTime(NULL, HannibalIntervaloTF, 0))
        {
         HannibalOrdenEnEstaVela = false;
         HannibalNewCandleTime = iTime(NULL, HannibalIntervaloTF, 0);
        }
     }
   else
      HannibalOrdenEnEstaVela = false;
////////////////////////////////////////////////////////////////////




   HayLimiteHorario(1);
   HayLimiteHorario(2);
   HayLimiteHorario(3);


   static int lastHolguraAdjJ=0;
   static int lastHolguraAdjA=0;
   static int lastHolguraAdjH=0;

   static int lastVolatilidadAdjJ=0;
   static int lastVolatilidadAdjA=0;
   static int lastVolatilidadAdjH=0;

   static double atrb0 = 0.0;
   static double atrb1 = 0.0;
   static double atrb2 = 0.0;

   static double atr0 = 0.0;
   static double atr1 = 0.0;
   static double atr2 = 0.0;

   static int tendenciaX = 0;


   static datetime cambioMinuto=-1;
   tiempoMinuto = iTime(NULL,PERIOD_M1, 0);
   if(cambioMinuto != tiempoMinuto || HolguraAdjJ!=lastHolguraAdjJ || HolguraAdjA!=lastHolguraAdjA || HolguraAdjH!=lastHolguraAdjH || VolatilidadAdjJ!=lastVolatilidadAdjJ || VolatilidadAdjA!=lastVolatilidadAdjA || VolatilidadAdjH!=lastVolatilidadAdjH)
     {
      cambioMinuto = tiempoMinuto;
      atr0 = iATR(NULL, PERIOD_M1, 8, 0);
      atr1 = iATR(NULL, PERIOD_M1, 16, 8);
      atr2 = iATR(NULL, PERIOD_M1, 32, 16);

      atrTP = (iATR(NULL, PERIOD_M1, 10, 0));



      atrb0 = iATR(NULL, PERIOD_H1, 8, 0);

      static datetime cambioHora=-1;
      tiempoHora = iTime(NULL, PERIOD_H1, 0);
      //if(1==1 || iBars(NULL,PERIOD_H1)<32 || cambioHora != tiempoHora)
      if(cambioHora != tiempoHora)
        {
         cambioHora = tiempoHora;
         //         atrb0 = iATR(NULL, PERIOD_H1, 8, 0);
         atrb1 = iATR(NULL, PERIOD_H1, 16, 8);
         atrb2 = iATR(NULL, PERIOD_H1, 32, 16);
        }


      tendenciaX = SacaTendencia();

      atrValue = ((((((atr0) + (atr1 * 2) + (atr2 * 3)) + ((atrb0 * 4) + (atrb1 * 5) + atrb2 * 6)) / 18)) * MathPow(10, Digits));
      atrParcialValueJ = round(((atrValue / 8) / 10) * (HolguraAdjJ));
      atrValueJ = round((atrValue / 5) * (VolatilidadAdjJ));
   
      atrParcialValueA = round(((atrValue / 8) / 10) * (HolguraAdjA));
      atrValueA = round((atrValue / 5) * (VolatilidadAdjA));
   
      atrParcialValueH = round(((atrValue / 8) / 10) * (HolguraAdjH));
      atrValueH = round((atrValue / 5) * (VolatilidadAdjH));
   
   
      if(tendenciaX != tendencia)
         lastTendencia = tendencia;
   
      tendencia = tendenciaX;
   
     }









   double balanceActualTGA=AccountBalance();

   if(TrailingGATipoCierre == Solo_Par)
   {
   
      double profitActualTGA=flotantePar;
      double porcenProfitTGA=porcentajeFlotantePar;
   
   // 0 TARA -------------------------------------------------------------------------------
      if(countTradesTotalParVar == 0)
        {
        profitX=porcenProfitTGA;
        conseguido = false;
        double TrailingGAStopCALC=100.00;
        }
   // ------------------------------------------------------------------------------------
   }
   else
   {
   
      profitActualTGA=AccountProfit();
      porcenProfitTGA=(profitActualTGA*100.0)/balanceActualTGA;   
         
   // 0 TARA -------------------------------------------------------------------------------
      if(count_orders_account == 0)
        {
        profitX=porcenProfitTGA;
        conseguido = false;
        TrailingGAStopCALC=100.00;
        }
   // ------------------------------------------------------------------------------------
   }






   if(TrailingGAActivo)  // ======================================================================================================================
     {

      if(TrailingGABeneDia > 0)
      {
         if(countTradesTotalParVar != lastCountTradesTotalParVar)
           {
            lastCountTradesTotalParVar = countTradesTotalParVar;
            beneHoy =  (ProfitActualTotal()*100.0)/balanceActualTGA;
           } 
         else
         {
            beneHoy = 0;
         }
     }


        
        
      if((conseguido && profitX > porcenProfitTGA + (TrailingGAStopCALC)) || (conseguido && beneHoy > (TrailingGABeneDia) && beneHoy > 0))
        {
         conseguido = false;
         
         if(TrailingGATipoCierre == Solo_Par)
           {
            cerrarTodasOperacionesPar();
           }
         else
           {
            cerrarTodasOperacionesCuenta();
           }

         enTick=false;
         return;
        }

      if( porcenProfitTGA>=TrailingGAStart && porcenProfitTGA>profitX )
        {
         profitX = porcenProfitTGA;
         conseguido = true;
         //return;
        TrailingGAStopCALC=(profitX*TrailingGAStop)/100.0;
        
        }

     }




//================================================================================================================================================
   if((modoOperativa == Modo_Minerva))
     {


      AlexanderTipoOperacion = Tipo_Manual;
      HannibalTipoOperacion = Tipo_Manual;
      limiteSeparacion = false;


      robotReturn.MinSeparacionActiva = limiteSeparacion;

      robotReturn.AlexTipoOp = AlexanderTipoOperacion;
      robotReturn.HannibalTipoOp = HannibalTipoOperacion;

      AlexanderOrdenManual = 0;
      HannibalOrdenManual = 0;

      double flotanteUltimaAlexander=flotanteOrdenMasBajaAlexander;
      double flotanteUltimaHannibal=flotanteOrdenMasAltaHannibal;



      // INICIA Cosechadora BUY & SELL -----------------------------------------------
      if(countTradesTotalParVar == 0 && (!AlexanderOrdenEnEstaVela || AlexanderIntervaloTF < 0) && (!HannibalOrdenEnEstaVela || HannibalIntervaloTF < 0))
        {
         if(AlexanderActivo)
           {
            AlexanderOrdenManual = 2;
           }
         if(HannibalActivo)
           {
            HannibalOrdenManual = 1;
           }
        }
      else
         // CONTINUA ---------------------------
        {


         if(flotanteUltimaAlexander == 0)
            centurionFlotUltA = 0;
         else
            centurionFlotUltA = flotanteUltimaAlexander / tipoCuentaDouble;

         if(flotanteUltimaHannibal == 0)
            centurionFlotUltH = 0;
         else
            centurionFlotUltH = flotanteUltimaHannibal / tipoCuentaDouble;




         if(countTradesAlexanderVar == 0 && (!AlexanderOrdenEnEstaVela || AlexanderIntervaloTF < 0))
           {
            if(AlexanderActivo)
              {
               AlexanderOrdenManual = 2;
              }
           }
         else
           {

            if(countTradesAlexanderVar >= 2)
              {


               if((preLastOpenPriceAlexander - lastOpenPriceAlexander) / Point > 0)
                 {
                  int distanciaTotalEntreOrdenesAlex = (preLastOpenPriceAlexander - lastOpenPriceAlexander) / Point;
                  int distanciaParcialEntreOrdenesAlex = ((preLastOpenPriceAlexander - Ask) / Point) * 100 / distanciaTotalEntreOrdenesAlex;
                  if(distanciaParcialEntreOrdenesAlex > 100)
                    {
                     distanciaParcialEntreOrdenesAlex = 100;
                    }

                 }
               else
                 {
                  distanciaTotalEntreOrdenesAlex = 0;
                  distanciaParcialEntreOrdenesAlex = 0;
                 }


               if(((100) - distanciaParcialEntreOrdenesAlex) > 100)//MathMax(0, 100 - MathMin(100,countTradesAlexanderVar * centuMulti)))
                 {
                  cerrarUltimaOperacionSymbolCaudillo(MagicNumber_Alexander);
                 }


              }

           }

         if(countTradesHannibalVar == 0 && (!HannibalOrdenEnEstaVela || HannibalIntervaloTF < 0))
           {
            if(HannibalActivo)
              {
               HannibalOrdenManual = 1;
              }
           }
         else
           {

            if(countTradesHannibalVar >= 2)
              {

               if((lastOpenPriceHannibal - preLastOpenPriceHannibal) / Point > 0)
                 {
                  int distanciaTotalEntreOrdenesHannibal = (lastOpenPriceHannibal - preLastOpenPriceHannibal) / Point;
                  int distanciaParcialEntreOrdenesHannibal = ((Ask - preLastOpenPriceHannibal) / Point) * 100 / distanciaTotalEntreOrdenesHannibal;
                  if(distanciaParcialEntreOrdenesHannibal > 100)
                    {
                     distanciaParcialEntreOrdenesHannibal = 100;
                    }
                 }
               else
                 {
                  distanciaTotalEntreOrdenesHannibal = 0;
                  distanciaParcialEntreOrdenesHannibal = 0;
                 }


               if(((100) - distanciaParcialEntreOrdenesHannibal) > 100)//MathMax(0, 100 - MathMin(100,countTradesHannibalVar * centuMulti)))
                 {
                  cerrarUltimaOperacionSymbolCaudillo(MagicNumber_Hannibal);
                 }
              }
           }


        }



     }//Minerva




//================================================================================================================================================
   if((modoOperativa == Modo_Centurion))
     {

      AlexanderModoPipStep = PS_Fijo;
      AlexanderModoTp = TP_Fijo;
      AlexanderPipStep = 10000;
      AlexanderTakeProfit = 10000;
      AlexanderUseTrailingStop = false;
      AlexanderTipoOperacion = Tipo_Manual;

      HannibalModoPipStep = PS_Fijo;
      HannibalModoTp = TP_Fijo;
      HannibalPipStep = 10000;
      HannibalTakeProfit = 10000;
      HannibalUseTrailingStop = false;
      HannibalTipoOperacion = Tipo_Manual;

      limiteSeparacion = false;

      robotReturn.MinSeparacionActiva = limiteSeparacion;

      robotReturn.AlexModoPipStep = AlexanderModoPipStep;
      robotReturn.AlexModoTp = AlexanderModoTp;
      robotReturn.AlexPipStep = AlexanderPipStep;
      robotReturn.AlexTP = AlexanderTakeProfit;
      robotReturn.AlexUseTrailing = AlexanderUseTrailingStop;
      robotReturn.AlexTipoOp = AlexanderTipoOperacion;

      robotReturn.HannibalModoPipStep = HannibalModoPipStep;
      robotReturn.HannibalModoTp = HannibalModoTp;
      robotReturn.HannibalPipStep = HannibalPipStep;
      robotReturn.HannibalTP = HannibalTakeProfit;
      robotReturn.HannibalUseTrailing = HannibalUseTrailingStop;
      robotReturn.HannibalTipoOp = HannibalTipoOperacion;

      AlexanderOrdenManual = 0;
      HannibalOrdenManual = 0;
      otraMartinAlexander = 0;
      otraMartinHannibal = 0;



      // INICIA centurion -----------------------------------------------
      if(countTradesTotalParVar == 0 && (!AlexanderOrdenEnEstaVela || AlexanderIntervaloTF < 0) && (!HannibalOrdenEnEstaVela || HannibalIntervaloTF < 0))
        {
         if(AlexanderActivo)
           {
            AlexanderOrdenManual = 2;
           }
         if(HannibalActivo)
           {
            HannibalOrdenManual = 1;
           }
        }
      else
         // VUELVE A ABRIR ORDEN CERRADA O ABRE NUEVA SI NEGATIVA ---------------------------
        {

         if(flotanteUltimaAlexander == 0)
            centurionFlotUltA = 0;
         else
            centurionFlotUltA = flotanteUltimaAlexander / tipoCuentaDouble;

         if(flotanteUltimaHannibal == 0)
            centurionFlotUltH = 0;
         else
            centurionFlotUltH = flotanteUltimaHannibal / tipoCuentaDouble;





         if(countTradesAlexanderVar == 0 && (!AlexanderOrdenEnEstaVela || AlexanderIntervaloTF < 0))
           {
            if(AlexanderActivo)
              {
               AlexanderOrdenManual = 2;
              }
            autoNuevoModo = true;
           }

         else
           {
            if(centurionFlotUltA+((spreadActual*valorCadaTick)/ MathPow(AlexanderLotExponent, countTradesAlexanderVar)) < centuApertura * MathPow(AlexanderLotExponent, countTradesAlexanderVar) && (!AlexanderOrdenEnEstaVela || AlexanderIntervaloTF < 0))
              {
               if(AlexanderActivo)
                 {
                  otraMartinAlexander = 1;
                 }
              }
           }

         if(countTradesHannibalVar == 0 && (!HannibalOrdenEnEstaVela || HannibalIntervaloTF < 0))
           {
            if(HannibalActivo)
              {
               HannibalOrdenManual = 1;
              }
           }
         else
           {
            if(centurionFlotUltH < centuApertura * MathPow(HannibalLotExponent, countTradesHannibalVar) && (!HannibalOrdenEnEstaVela || HannibalIntervaloTF < 0))
              {
               if(HannibalActivo)
                 {
                  otraMartinHannibal = 1;
                 }
              }
           }



        }


      if(centurionFlotUltA > centuCierre * MathPow(AlexanderLotExponent, countTradesAlexanderVar))
        {
         cerrarUltimaOperacionSymbolCaudillo(MagicNumber_Alexander);
        }
      if(centurionFlotUltH > centuCierre * MathPow(HannibalLotExponent, countTradesHannibalVar))
        {
         cerrarUltimaOperacionSymbolCaudillo(MagicNumber_Hannibal);
        }





      //          }//Vela


     }// FIN MODO CENTURION












   int contraSell = 0, contraBuy = 0;



   if(CaesarCambiaExpActivo && CaesarCambiaExpDesdeMartingala <= countTradesCaesarVar)
     {
      CaesarLotExponent = MathMax(0.01, CaesarLotExponentIni + (double)((countTradesCaesarVar - CaesarCambiaExpDesdeMartingala) * CaesarCambiaExpValor));
     }
   else
     {
      CaesarLotExponent = CaesarLotExponentIni;
     }

   if(AlexanderCambiaExpActivo && AlexanderCambiaExpDesdeMartingala <= countTradesAlexanderVar)
     {
      AlexanderLotExponent = MathMax(0.01, AlexanderLotExponentIni + (double)((countTradesAlexanderVar - AlexanderCambiaExpDesdeMartingala) * AlexanderCambiaExpValor));
     }
   else
     {
      AlexanderLotExponent = AlexanderLotExponentIni;
     }

   if(HannibalCambiaExpActivo && HannibalCambiaExpDesdeMartingala <= countTradesHannibalVar)
     {
      HannibalLotExponent = MathMax(0.01, HannibalLotExponentIni + (double)((countTradesHannibalVar - HannibalCambiaExpDesdeMartingala) * HannibalCambiaExpValor));
     }
   else
     {
      HannibalLotExponent = HannibalLotExponentIni;
     }



   parte = 0;

   double balanceActualT=AccountBalance();

   if(TrailingRETipoCierres == Solo_Par)
   {
      double profitActualT=flotantePar;
      double porcenProfitT=porcentajeFlotantePar;
   // 0 TARA -------------------------------------------------------------------------------
      if(countTradesTotalParVar == 0)  // || MathAbs(AccountProfit()) < 0.01
        {
         haBajadoProfit = false;
         haSubidoProfit = false;
         maxProfit = -100.0;
         minProfit = porcenProfitT;
         TrailingREStopCALC=0;
         TrailingREStartCALC=0;
        }
   // ------------------------------------------------------------------------------------
   }
   else
   {
      profitActualT=AccountProfit();
      porcenProfitT=(profitActualT*100.0)/balanceActualT;   
   // 0 TARA -------------------------------------------------------------------------------
      if(count_orders_account == 0)  // || MathAbs(AccountProfit()) < 0.01
        {
         haBajadoProfit = false;
         haSubidoProfit = false;
         maxProfit = -100.0;
         minProfit = porcenProfitT;
         TrailingREStopCALC=0;
         TrailingREStartCALC=0;
        }
   // ------------------------------------------------------------------------------------
   }


   if(TrailingREActivo)
     {

      // 3 TrailingREStop -------------------------------------------------------------------
      if(haSubidoProfit && (porcenProfitT < maxProfit - TrailingREStopCALC) )
        {
         if(TrailingRETipoCierres == Solo_Par)
           {
            cerrarTodasOperacionesPar();
           }
         else
           {
            cerrarTodasOperacionesCuenta();
           }
           
           
         haBajadoProfit = false;
         haSubidoProfit = false;
         porcenProfitT=(profitActualT*100.0)/balanceActualT; // *****************    **********    *********
         maxProfit = -100.0;
         minProfit = porcenProfitT;
         TrailingREStopCALC=0;
         TrailingREStartCALC=0;

          // return; //===========  *************** =============== *************
        }
      // ------------------------------------------------------------------------------------

      // 2 SUBIDA
      if(haBajadoProfit && (porcenProfitT) > minProfit + (TrailingREStartCALC) && (maxProfit < porcenProfitT))
        {
         haSubidoProfit = true;
         maxProfit = porcenProfitT;
         TrailingREStopCALC=((maxProfit-minProfit)*TrailingREStop)/100.0;
        }
      // ------------------------------------------------------------------------------------

      // 1 BAJADA --------------------------------------------------------------------------
      if((porcenProfitT) < -(TrailingREMinima) && (minProfit > porcenProfitT)) //-10    15
        {
         haBajadoProfit = true;
         haSubidoProfit = false;
         maxProfit = -100.0;
         minProfit = porcenProfitT;
         TrailingREStartCALC=(minProfit*-TrailingREStart)/100.0;
        }
      // ------------------------------------------------------------------------------------
     }










//+------------------------------------------------------------------+
//| REENTRADAS CAESAR                                                |
//+------------------------------------------------------------------+

   static double backLastOpenPriceCaesar=0;
   hazReentradaCaesar = false;
   if(CaesarOrdenManual==0 && CaesarActivo)
     {
      if(reEntradasCaesar)   //&& (countTradesCaesarVar<=countTradesCaesarRe))
        {
         if(CountTrades_CaesarX() < 2)
           {
            countTradesCaesarRe = 0;
            puedeReEntrarCaesar=true;
           }
         else
           {
            if(countTradesCaesarRe==0)
              {
               puedeReEntrarCaesar=true;
               //puedeReEntrarCaesar=false;
              }
            else
              {
              }
           }
        }
      if(countTradesCaesarRe>1)
        {

         if((countTradesCaesarRe>=reStopCaesar)&&(CaesarOperacionAbiertasBuy > 0 && Ask<precioOrdenMasAltaCaesar-atrTP) && CaesarFlotante>0)
           {
            cerrarTicket(ticketOrdenMasBajaCaesar);
            return;
           }

         if((countTradesCaesarRe>=reStopCaesar)&&(CaesarOperacionAbiertasSell > 0 && Bid>precioOrdenMasBajaCaesar+atrTP) && CaesarFlotante>0)
           {
            cerrarTicket(ticketOrdenMasAltaCaesar);
            return;
           }



         if(CaesarOperacionAbiertasBuy > 0 && Ask<precioOrdenMasAltaCaesar && CaesarFlotante<0)
           {
            cerrarTodasOperacionesCiclo(MagicNumber_Caesar);
            return;
           }

         if(CaesarOperacionAbiertasSell > 0 && Bid>precioOrdenMasBajaCaesar && CaesarFlotante<0)
           {
            cerrarTodasOperacionesCiclo(MagicNumber_Caesar);
            return;
           }

        }


      if(reEntradasCaesar && ((CaesarOperacionAbiertasSell > 0 && (Bid < precioPrimeraCaesar)) || (CaesarOperacionAbiertasBuy > 0 && (Ask > precioPrimeraCaesar))))
        {

         puedeCoberturearCaesar = false;


         if((Bid < precioOrdenMasBajaCaesar - (CaesarPipStep * reDistanciaCaesar)*Point) && (Ask < precioPrimeraCaesar) && (CaesarOperacionAbiertasSell > 0)) // && (backLastOpenPriceCaesar != precioOrdenMasBajaCaesar))
           {
            if(puedeReEntrarCaesar)
              {
               backLastOpenPriceCaesar = precioOrdenMasBajaCaesar;
               hazReentradaCaesar = true;
               CaesarOrdenManual = 1;
               lotajePrimeraCaesar = reLotajeCaesar;
               CaesarLots=lotajePrimeraCaesar;
               countTradesCaesarRe += 1;
               puedeReEntrarCaesar=false;
              }
           }
         else
           {
            if((Ask > precioOrdenMasAltaCaesar + ((CaesarPipStep * reDistanciaCaesar))*Point) && (Bid > precioPrimeraCaesar) && (CaesarOperacionAbiertasBuy > 0))  // && (backLastOpenPriceCaesar != precioOrdenMasAltaCaesar))
              {
               if(puedeReEntrarCaesar)
                 {
                  backLastOpenPriceCaesar = precioOrdenMasAltaCaesar;
                  hazReentradaCaesar = true;
                  CaesarOrdenManual = 2;
                  lotajePrimeraCaesar = reLotajeCaesar;
                  CaesarLots=lotajePrimeraCaesar;
                  countTradesCaesarRe += 1;
                  puedeReEntrarCaesar=false;
                 }
              }
            else
              {
               puedeReEntrarCaesar=true;

               if(countTradesCaesarRe==0)
                 {
                  backLastOpenPriceCaesar = -rand();
                 }
              }
           }


        }
      else
        {
         puedeCoberturearCaesar = true;
        }
     }
//+------------------------------------------------------------------+



//+------------------------------------------------------------------+
//| REENTRADAS Alexander                                                |
//+------------------------------------------------------------------+

   static double backLastOpenPriceAlexander=0;
   hazReentradaAlexander = false;
   if(AlexanderOrdenManual==0 && AlexanderActivo)
     {
      if(reEntradasAlexander)   //&& (countTradesAlexanderVar<=countTradesAlexanderRe))
        {
         if(CountTrades_AlexanderX() < 2)
           {
            countTradesAlexanderRe = 0;
            puedeReEntrarAlexander=true;
           }
         else
           {
            if(countTradesAlexanderRe==0)
              {
               //puedeReEntrarAlexander=false;
               puedeReEntrarAlexander=true;
              }
            else
              {
              }
           }
        }
      if(countTradesAlexanderRe>1)
        {

         if((countTradesAlexanderRe>=reStopAlexander)&&(AlexanderOperacionAbiertasBuy > 0 && Ask<precioOrdenMasAltaAlexander-atrTP) && AlexanderFlotante>0)
           {
            cerrarTicket(ticketOrdenMasBajaAlexander);
            return;
           }

         if((countTradesAlexanderRe>=reStopAlexander)&&(AlexanderOperacionAbiertasSell > 0 && Bid>precioOrdenMasBajaAlexander+atrTP) && AlexanderFlotante>0)
           {
            cerrarTicket(ticketOrdenMasAltaAlexander);
            return;
           }



         if(AlexanderOperacionAbiertasBuy > 0 && Ask<precioOrdenMasAltaAlexander && AlexanderFlotante<0)
           {
            cerrarTodasOperacionesCiclo(MagicNumber_Alexander);
            return;
           }

         if(AlexanderOperacionAbiertasSell > 0 && Bid>precioOrdenMasBajaAlexander && AlexanderFlotante<0)
           {
            cerrarTodasOperacionesCiclo(MagicNumber_Alexander);
            return;
           }

        }


      if(reEntradasAlexander && ((AlexanderOperacionAbiertasSell > 0 && (Bid < precioPrimeraAlexander)) || (AlexanderOperacionAbiertasBuy > 0 && (Ask > precioPrimeraAlexander))))
        {
         puedeCoberturearAlexander = false;

         if((Bid < precioOrdenMasBajaAlexander - (AlexanderPipStep * reDistanciaAlexander)*Point) && (Ask < precioPrimeraAlexander) && (AlexanderOperacionAbiertasSell > 0)) // && (backLastOpenPriceAlexander != precioOrdenMasBajaAlexander))
           {
            if(puedeReEntrarAlexander)
              {
               backLastOpenPriceAlexander = precioOrdenMasBajaAlexander;
               hazReentradaAlexander = true;
               AlexanderOrdenManual = 1;
               lotajePrimeraAlexander = reLotajeAlexander;
               AlexanderLots=lotajePrimeraAlexander;
               countTradesAlexanderRe += 1;
               puedeReEntrarAlexander=false;
              }
           }
         else
           {
            if((Ask > precioOrdenMasAltaAlexander + ((AlexanderPipStep * reDistanciaAlexander))*Point) && (Bid > precioPrimeraAlexander) && (AlexanderOperacionAbiertasBuy > 0))  // && (backLastOpenPriceAlexander != precioOrdenMasAltaAlexander))
              {
               if(puedeReEntrarAlexander)
                 {
                  backLastOpenPriceAlexander = precioOrdenMasAltaAlexander;
                  hazReentradaAlexander = true;
                  AlexanderOrdenManual = 2;
                  lotajePrimeraAlexander = reLotajeAlexander;
                  AlexanderLots=lotajePrimeraAlexander;
                  countTradesAlexanderRe += 1;
                  puedeReEntrarAlexander=false;
                 }
              }
            else
              {
               puedeReEntrarAlexander=true;

               if(countTradesAlexanderRe==0)
                 {
                  backLastOpenPriceAlexander = -rand();
                 }
              }
           }


        }
      else
        {
         puedeCoberturearAlexander = true;
        }
     }
//+------------------------------------------------------------------+




//+------------------------------------------------------------------+
//| REENTRADAS Hannibal                                                |
//+------------------------------------------------------------------+

   static double backLastOpenPriceHannibal=0;
   hazReentradaHannibal = false;
   if(HannibalOrdenManual==0 && HannibalActivo)
     {
      if(reEntradasHannibal)   //&& (countTradesHannibalVar<=countTradesHannibalRe))
        {
         if(CountTrades_HannibalX() < 2)
           {
            countTradesHannibalRe = 0;
            puedeReEntrarHannibal=true;
           }
         else
           {
            if(countTradesHannibalRe==0)
              {
               puedeReEntrarHannibal=true;
              }
            else
              {
              }
           }
        }
      if(countTradesHannibalRe>1)
        {

         if((countTradesHannibalRe>=reStopHannibal)&&(HannibalOperacionAbiertasBuy > 0 && Ask<precioOrdenMasAltaHannibal-atrTP) && HannibalFlotante>0)
           {
            cerrarTicket(ticketOrdenMasBajaHannibal);
            return;
           }

         if((countTradesHannibalRe>=reStopHannibal)&&(HannibalOperacionAbiertasSell > 0 && Bid>precioOrdenMasBajaHannibal+atrTP) && HannibalFlotante>0)
           {
            cerrarTicket(ticketOrdenMasAltaHannibal);
            return;
           }



         if(HannibalOperacionAbiertasBuy > 0 && Ask<precioOrdenMasAltaHannibal && HannibalFlotante<0)
           {
            cerrarTodasOperacionesCiclo(MagicNumber_Hannibal);
            return;
           }

         if(HannibalOperacionAbiertasSell > 0 && Bid>precioOrdenMasBajaHannibal && HannibalFlotante<0)
           {
            cerrarTodasOperacionesCiclo(MagicNumber_Hannibal);
            return;
           }

        }

      if(reEntradasHannibal && ((HannibalOperacionAbiertasSell > 0 && (Bid < precioPrimeraHannibal)) || (HannibalOperacionAbiertasBuy > 0 && (Ask > precioPrimeraHannibal))))
        {

         puedeCoberturearHannibal = false;


         if((Bid < precioOrdenMasBajaHannibal - (HannibalPipStep * reDistanciaHannibal)*Point) && (Ask < precioPrimeraHannibal) && (HannibalOperacionAbiertasSell > 0)) // && (backLastOpenPriceHannibal != precioOrdenMasBajaHannibal))
           {
            if(puedeReEntrarHannibal)
              {
               backLastOpenPriceHannibal = precioOrdenMasBajaHannibal;
               hazReentradaHannibal = true;
               HannibalOrdenManual = 1;
               lotajePrimeraHannibal = reLotajeHannibal;
               HannibalLots=lotajePrimeraHannibal;
               countTradesHannibalRe += 1;
               puedeReEntrarHannibal=false;
              }
           }
         else
           {
            if((Ask > precioOrdenMasAltaHannibal + ((HannibalPipStep * reDistanciaHannibal))*Point) && (Bid > precioPrimeraHannibal) && (HannibalOperacionAbiertasBuy > 0))  // && (backLastOpenPriceHannibal != precioOrdenMasAltaHannibal))
              {
               if(puedeReEntrarHannibal)
                 {
                  backLastOpenPriceHannibal = precioOrdenMasAltaHannibal;
                  hazReentradaHannibal = true;
                  HannibalOrdenManual = 2;
                  lotajePrimeraHannibal = reLotajeHannibal;
                  HannibalLots=lotajePrimeraHannibal;
                  countTradesHannibalRe += 1;
                  puedeReEntrarHannibal=false;
                 }
              }
            else
              {
               puedeReEntrarHannibal=true;

               if(countTradesHannibalRe==0)
                 {
                  backLastOpenPriceHannibal = -rand();
                 }
              }
           }


        }
      else
        {
         puedeCoberturearHannibal = true;
        }
     }
//+------------------------------------------------------------------+

//=============================================================================================================


   if(lacertaCauda)
     {
      gestionarLacertaCauda();
     }

   if(countTradesCaesarVar == 0)
     {
      maxPriceCaesar = Bid;
     }
   if(countTradesAlexanderVar == 0)
     {
      maxPriceAlexander = Bid;
     }
   if(countTradesHannibalVar == 0)
     {
      maxPriceHannibal = Bid;
     }

   if(maxPriceCaesar < Bid)
     {
      maxPriceCaesar = Bid;
     }
   if(minPriceCaesar > Bid)
     {
      minPriceCaesar = Bid;
     }

   if(maxPriceAlexander < Bid)
     {
      maxPriceAlexander = Bid;
     }
   if(minPriceAlexander > Bid)
     {
      minPriceAlexander = Bid;
     }

   if(maxPriceHannibal < Bid)
     {
      maxPriceHannibal = Bid;
     }
   if(minPriceHannibal > Bid)
     {
      minPriceHannibal = Bid;
     }




   if(CaesarLots > maxLotsCaesar)
      CaesarLots = maxLotsCaesar;
   if(AlexanderLots > maxLotsAlexander)
      AlexanderLots = maxLotsAlexander;
   if(HannibalLots > maxLotsHannibal)
      HannibalLots = maxLotsHannibal;









// INTERES COMPUESTO
   if(InteresCompuesto && BalanceBase>0.00)
     {
      if(AlexanderLots < maxLotsAlexander && CaesarLots < maxLotsCaesar && HannibalLots < maxLotsHannibal)
        {
         
         balanceInicial=BalanceBase;
         
         if(countTradesCaesarVar==0)
         {
            CaesarLots = NormalizeLots(MathMax(infoLotMin,(AccountBalance() * CaesarLotsIni2) / (balanceInicial)));
            lotajePrimeraCaesar=CaesarLots;
         }
         if(countTradesAlexanderVar==0)
         {
            AlexanderLots = NormalizeLots(MathMax(infoLotMin,(AccountBalance() * AlexanderLotsIni2) / (balanceInicial)));
            lotajePrimeraAlexander=AlexanderLots;
         }
         if(countTradesHannibalVar==0)
         {
            HannibalLots = NormalizeLots(MathMax(infoLotMin,(AccountBalance() * HannibalLotsIni2) / (balanceInicial)));
            lotajePrimeraHannibal=HannibalLots;   
         }
         

                  
         if(CaesarLots > maxLotsCaesar)
            CaesarLots = maxLotsCaesar;
         if(AlexanderLots > maxLotsAlexander)
            AlexanderLots = maxLotsAlexander;
         if(HannibalLots > maxLotsHannibal)
            HannibalLots = maxLotsHannibal;

        }

     }
   else
     {
      CaesarLots = (CaesarLotsIni);
      AlexanderLots = (AlexanderLotsIni);
      HannibalLots = (HannibalLotsIni);

      if(CaesarLots > maxLotsCaesar)
         CaesarLots = maxLotsCaesar;
      if(AlexanderLots > maxLotsAlexander)
         AlexanderLots = maxLotsAlexander;
      if(HannibalLots > maxLotsHannibal)
         HannibalLots = maxLotsHannibal;


      TrailingGAStart = TrailingGAStartIni;
      TrailingGAStop = TrailingGAStopIni;
      TrailingGABeneDia = TrailingGABeneDiaIni;

      TrailingREStart = TrailingREStartIni;
      TrailingREStop = TrailingREStopIni;
      TrailingREMinima = TrailingREMinimaIni;

      LimitClose = LimitCloseIni;
     }











////////  comprobarActividadCaudillos(); // -------------------------



//Operativa en función del modo seleccionado por el Capitán

//Modo Clásico Hard
   if(modoOperativa == Modo_Gladiador)
     {
      if(CaesarActivo || otraMartinCaesar==1 || CaesarOrdenManual!=0)
         operativaCaesarClasicaHardLight();
      if(AlexanderActivo || otraMartinAlexander==1 || AlexanderOrdenManual!=0)
         operativaAlexanderClasicaHardLight();
      if(HannibalActivo || otraMartinHannibal==1 || HannibalOrdenManual!=0)
         operativaHannibalClasicaHardLight();

     }

   if(modoOperativa == Modo_Elite || modoOperativa == Modo_Centurion  || modoOperativa == Modo_Minerva) //
     {
      if(CaesarActivo || otraMartinCaesar==1 || CaesarOrdenManual>0)
         operativaCaesarCoordinadoManual();
      if(AlexanderActivo || otraMartinAlexander==1 || AlexanderOrdenManual>0)
         operativaAlexanderCoordinadoManual();
      if(HannibalActivo || otraMartinHannibal==1 || HannibalOrdenManual>0)
         operativaHannibalCoordinadoManual();
     }




// -------------------------------------------------------





if(IsOptimization() || IsTesting())
{
   CalculayPintaPanel(); // estaba comentado
}


#ifdef UsaCSV
   InTick();
#else
#ifdef WalkForfardPro
   InTick();
#else
#ifdef WalkForfardPro_Modo_Null
   InTick();
#endif
#endif

#endif


   static bool resultAsk=false;
   ChartShowAskLineGet(resultAsk);
   debeMostrarLineaAsk = !resultAsk;

   enTick=false;
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  }; // FIN DE ONTICK


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void pintarEtiquetas(string nombre, int esquina, int xDistance, int yDistance, string text, int textSize, string fuente, color textColor, bool back=false, bool selectable=false)
  {

//Si el objeto no existe, se crea
   if(ObjectFind(nombre) < 0)  //si devuelve un valor negativo, no existe
     {
      ObjectCreate(nombre, OBJ_LABEL, 0, 0, 1.0);
      ObjectSet(nombre, OBJPROP_CORNER, esquina);
      ObjectSet(nombre, OBJPROP_XDISTANCE, xDistance);
      ObjectSet(nombre, OBJPROP_YDISTANCE, yDistance);
      ObjectSet(nombre, OBJPROP_BACK, back);
      ObjectSet(nombre, OBJPROP_SELECTABLE, selectable);
      if(nombre=="MNLabel")
        {
         ObjectSet(nombre, OBJPROP_HIDDEN, True);
        }
     }

   ObjectSetText(nombre, text, textSize, fuente, textColor);

  }



/* Pinta en pantalla la información de los caudillos en resolución Standard */
void CalculayPintaPanel()
  {


   if(IsTesting())
     {

      mercadoAbierto = 2;
      robotSend.mercadoAbierto=mercadoAbierto;

     }

   RefreshRates();

   valorCadaTick = calcularValorTick();

   spreadActual = ((Ask - Bid) / Point);

   if(spreadActual<spreadAnt1)
     {
      spreadActual=MathMax(spreadActual,spreadAnt1*0.99);
     }
   spreadAnt1=spreadActual;




   valorSpread = DoubleToStr(spreadActual, 1);


   spread = "Spread: " + valorSpread + " ticks";
   capitanLotaje = NormalizeDouble(lotajeSymbolCaudillo(0), 2);
   dineroPorTickLotCapitan = valorCadaTick * capitanLotaje;
   capitanFlotante = NormalizeDouble(ProfitSymbolCaudillo(0), 2);
   capitanPorcentajeFlotante = NormalizeDouble(porcentajeFlotanteCuenta(capitanFlotante), 2);
   capitanBenefObjTP = NormalizeDouble(beneficioTPObjetivoSymbolCaudillo(0), 2);
   capitanBenefObjSL = NormalizeDouble(beneficioSLObjetivoSymbolCaudillo(0), 2);
   swapCapitan = NormalizeDouble(swapSymbolCaudillo(0), 2);
   comisionCapitan = NormalizeDouble(comisionSymbolCaudillo(0), 2);
   caesarLotaje = NormalizeDouble(lotajeSymbolCaudillo(MagicNumber_Caesar), 2);
   dineroPorTickLotCaesar = valorCadaTick * caesarLotaje;
   CaesarFlotante = NormalizeDouble(ProfitSymbolCaudillo(MagicNumber_Caesar), 2);
   caesarPorcentajeFlotante = NormalizeDouble(porcentajeFlotanteCuenta(CaesarFlotante), 2);
   caesarBenefObjTP = NormalizeDouble(beneficioTPObjetivoSymbolCaudillo(MagicNumber_Caesar), 2);
   caesarBenefObjSL = NormalizeDouble(beneficioSLObjetivoSymbolCaudillo(MagicNumber_Caesar), 2);
   swapCaesar = NormalizeDouble(swapSymbolCaudillo(MagicNumber_Caesar), 2);
   comisionCaesar = NormalizeDouble(comisionSymbolCaudillo(MagicNumber_Caesar), 2);
   alexanderLotaje = NormalizeDouble(lotajeSymbolCaudillo(MagicNumber_Alexander), 2);
   dineroPorTickLotAlexander = valorCadaTick * alexanderLotaje;
   AlexanderFlotante = NormalizeDouble(ProfitSymbolCaudillo(MagicNumber_Alexander), 2);
   alexanderPorcentajeFlotante = NormalizeDouble(porcentajeFlotanteCuenta(AlexanderFlotante), 2);
   alexanderBenefObjTP = NormalizeDouble(beneficioTPObjetivoSymbolCaudillo(MagicNumber_Alexander), 2);
   alexanderBenefObjSL = NormalizeDouble(beneficioSLObjetivoSymbolCaudillo(MagicNumber_Alexander), 2);
   swapAlexander = NormalizeDouble(swapSymbolCaudillo(MagicNumber_Alexander), 2);
   comisionAlexander = NormalizeDouble(comisionSymbolCaudillo(MagicNumber_Alexander), 2);
   hannibalLotaje = NormalizeDouble(lotajeSymbolCaudillo(MagicNumber_Hannibal), 2);
   dineroPorTickLotHannibal = valorCadaTick * hannibalLotaje;
   HannibalFlotante = NormalizeDouble(ProfitSymbolCaudillo(MagicNumber_Hannibal), 2);
   hannibalPorcentajeFlotante = NormalizeDouble(porcentajeFlotanteCuenta(HannibalFlotante), 2);
   hannibalBenefObjTP = NormalizeDouble(beneficioTPObjetivoSymbolCaudillo(MagicNumber_Hannibal), 2);
   hannibalBenefObjSL = NormalizeDouble(beneficioSLObjetivoSymbolCaudillo(MagicNumber_Hannibal), 2);
   swapHannibal = NormalizeDouble(swapSymbolCaudillo(MagicNumber_Hannibal), 2);
   comisionHannibal = NormalizeDouble(comisionSymbolCaudillo(MagicNumber_Hannibal), 2);
   operacionesContPar = countTradesCaptainVar + countTradesCaesarVar + countTradesAlexanderVar + countTradesHannibalVar;
   lotajeTotalPar =  capitanLotaje + caesarLotaje + alexanderLotaje + hannibalLotaje;
   dineroPorTickLotPar = valorCadaTick * lotajeTotalPar;
   flotantePar = ProfitSymbol();
   porcentajeFlotantePar = NormalizeDouble(porcentajeFlotanteCuenta(flotantePar), 2);
   benefObjParTP = capitanBenefObjTP + caesarBenefObjTP + alexanderBenefObjTP + hannibalBenefObjTP;
   benefObjParSL = capitanBenefObjSL + caesarBenefObjSL + alexanderBenefObjSL + hannibalBenefObjSL;
   swapPar = swapCapitan + swapCaesar + swapAlexander + swapHannibal;
   comisionPar = comisionCapitan + comisionCaesar + comisionAlexander + comisionHannibal;
   operacionesCont = OrdersTotal();
   lotajeTotalCuenta = lotajeTotalCuenta();
   flotanteCuenta = NormalizeDouble(AccountProfit(), 2);
   flotanteCuentaPorcentaje = NormalizeDouble(porcentajeFlotanteCuenta(flotanteCuenta), 2);
   profitActualTotal = ProfitActualTotal();

   if(AccountMargin() != 0)
     {
      saludMax = (AccountLeverage() * 1000) / 500; //   1000;//NormalizeDouble((AccountBalance()/AccountMargin())*100,2);
      saludMin = AccountStopoutLevel();
      saludPorcen = NormalizeDouble((AccountEquity() / AccountMargin()) * 100, 2);
     }
   else
     {
      saludPorcen = saludMax;
     }

   if((count_orders_account) == 0)
     {
      saludMax=1000000.0;
      saludPorcen = saludMax;
     }

   pMargenLibre = saludPorcen;


if (!IsOptimization() && (!IsTesting() || !IsVisualMode()))
{
   UpdatePanelExterno();
   mostrarPanelStandard();
}
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void MuestraSoloLineasDe(int sCaudillo=0)
  {

   static int lastSelectedCaudillo=-10;

   int colorFondo=ChartBackColorGet();

   static char strobe=0;

   if(strobe==1)
      strobe = 0;
   else
      strobe = 1;

   switch(sCaudillo)
     {
      case 1:
         if(strobe)
           {
            ObjectSetInteger(0, "DProLineIniJ1", OBJPROP_COLOR, colorLines[colorCruceLineaJ1]);
            ObjectSetInteger(0, "DProLineIniJ2", OBJPROP_COLOR, colorLines[colorCruceLineaJ2]);
            ObjectSetInteger(0, "DProStopLineJ1", OBJPROP_COLOR, colorLines[colorStopCruceLineaJ1]);
            ObjectSetInteger(0, "DProStopLineJ2", OBJPROP_COLOR, colorLines[colorStopCruceLineaJ2]);
            ChartRedraw();
            Sleep(30);

           }
         else
           {
            Sleep(20);
            ObjectSetInteger(0, "DProLineIniJ1", OBJPROP_COLOR, colorLines[colorCruceLineaJ1]&0xAAAAAA);
            ObjectSetInteger(0, "DProLineIniJ2", OBJPROP_COLOR, colorLines[colorCruceLineaJ2]&0xAAAAAA);
            ObjectSetInteger(0, "DProStopLineJ1", OBJPROP_COLOR, colorLines[colorStopCruceLineaJ1]&0xAAAAAA);
            ObjectSetInteger(0, "DProStopLineJ2", OBJPROP_COLOR, colorLines[colorStopCruceLineaJ2]&0xAAAAAA);
           }

         if(lastSelectedCaudillo != sCaudillo)
           {
            ObjectSetInteger(0, "DProLineIniA1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProLineIniA2", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineA1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineA2", OBJPROP_COLOR, colorFondo);

            ObjectSetInteger(0, "DProLineIniH1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProLineIniH2", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineH1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineH2", OBJPROP_COLOR, colorFondo);

            ObjectSetInteger(0, "DProLineIniM1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProLineIniM2", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineM1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineM2", OBJPROP_COLOR, colorFondo);
           }
         break;

      case 2:
         if(lastSelectedCaudillo != sCaudillo)
           {
            ObjectSetInteger(0, "DProLineIniJ1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProLineIniJ2", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineJ1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineJ2", OBJPROP_COLOR, colorFondo);
           }

         if(strobe)
           {
            ObjectSetInteger(0, "DProLineIniA1", OBJPROP_COLOR, colorLines[colorCruceLineaA1]);
            ObjectSetInteger(0, "DProLineIniA2", OBJPROP_COLOR, colorLines[colorCruceLineaA2]);
            ObjectSetInteger(0, "DProStopLineA1", OBJPROP_COLOR, colorLines[colorStopCruceLineaA1]);
            ObjectSetInteger(0, "DProStopLineA2", OBJPROP_COLOR, colorLines[colorStopCruceLineaA2]);
            ChartRedraw();
            Sleep(30);

           }
         else
           {
            Sleep(20);
            ObjectSetInteger(0, "DProLineIniA1", OBJPROP_COLOR, colorLines[colorCruceLineaA1]&0xAAAAAA);
            ObjectSetInteger(0, "DProLineIniA2", OBJPROP_COLOR, colorLines[colorCruceLineaA2]&0xAAAAAA);
            ObjectSetInteger(0, "DProStopLineA1", OBJPROP_COLOR, colorLines[colorStopCruceLineaA1]&0xAAAAAA);
            ObjectSetInteger(0, "DProStopLineA2", OBJPROP_COLOR, colorLines[colorStopCruceLineaA2]&0xAAAAAA);
           }


         if(lastSelectedCaudillo != sCaudillo)
           {
            ObjectSetInteger(0, "DProLineIniH1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProLineIniH2", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineH1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineH2", OBJPROP_COLOR, colorFondo);

            ObjectSetInteger(0, "DProLineIniM1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProLineIniM2", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineM1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineM2", OBJPROP_COLOR, colorFondo);
           }
         break;

      case 3:
         if(lastSelectedCaudillo != sCaudillo)
           {
            ObjectSetInteger(0, "DProLineIniJ1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProLineIniJ2", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineJ1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineJ2", OBJPROP_COLOR, colorFondo);

            ObjectSetInteger(0, "DProLineIniA1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProLineIniA2", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineA1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineA2", OBJPROP_COLOR, colorFondo);
           }

         if(strobe)
           {
            ObjectSetInteger(0, "DProLineIniH1", OBJPROP_COLOR, colorLines[colorCruceLineaH1]);
            ObjectSetInteger(0, "DProLineIniH2", OBJPROP_COLOR, colorLines[colorCruceLineaH2]);
            ObjectSetInteger(0, "DProStopLineH1", OBJPROP_COLOR, colorLines[colorStopCruceLineaH1]);
            ObjectSetInteger(0, "DProStopLineH2", OBJPROP_COLOR, colorLines[colorStopCruceLineaH2]);
            ChartRedraw();
            Sleep(30);

           }
         else
           {
            Sleep(20);
            ObjectSetInteger(0, "DProLineIniH1", OBJPROP_COLOR, colorLines[colorCruceLineaH1]&0xAAAAAA);
            ObjectSetInteger(0, "DProLineIniH2", OBJPROP_COLOR, colorLines[colorCruceLineaH2]&0xAAAAAA);
            ObjectSetInteger(0, "DProStopLineH1", OBJPROP_COLOR, colorLines[colorStopCruceLineaH1]&0xAAAAAA);
            ObjectSetInteger(0, "DProStopLineH2", OBJPROP_COLOR, colorLines[colorStopCruceLineaH2]&0xAAAAAA);
           }

         if(lastSelectedCaudillo != sCaudillo)
           {
            ObjectSetInteger(0, "DProLineIniM1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProLineIniM2", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineM1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineM2", OBJPROP_COLOR, colorFondo);
           }
         break;

      case 10:
         if(lastSelectedCaudillo != sCaudillo)
           {
            ObjectSetInteger(0, "DProLineIniJ1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProLineIniJ2", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineJ1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineJ2", OBJPROP_COLOR, colorFondo);

            ObjectSetInteger(0, "DProLineIniA1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProLineIniA2", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineA1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineA2", OBJPROP_COLOR, colorFondo);

            ObjectSetInteger(0, "DProLineIniH1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProLineIniH2", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineH1", OBJPROP_COLOR, colorFondo);
            ObjectSetInteger(0, "DProStopLineH2", OBJPROP_COLOR, colorFondo);
           }


         if(strobe)
           {
            ObjectSetInteger(0, "DProLineIniM1", OBJPROP_COLOR, colorLines[colorCruceLineaM1]);
            ObjectSetInteger(0, "DProLineIniM2", OBJPROP_COLOR, colorLines[colorCruceLineaM2]);
            ObjectSetInteger(0, "DProStopLineM1", OBJPROP_COLOR, colorLines[colorStopCruceLineaM1]);
            ObjectSetInteger(0, "DProStopLineM2", OBJPROP_COLOR, colorLines[colorStopCruceLineaM2]);
            ChartRedraw();
            Sleep(30);

           }
         else
           {
            Sleep(20);
            ObjectSetInteger(0, "DProLineIniM1", OBJPROP_COLOR, colorLines[colorCruceLineaM1]&0xAAAAAA);
            ObjectSetInteger(0, "DProLineIniM2", OBJPROP_COLOR, colorLines[colorCruceLineaM2]&0xAAAAAA);
            ObjectSetInteger(0, "DProStopLineM1", OBJPROP_COLOR, colorLines[colorStopCruceLineaM1]&0xAAAAAA);
            ObjectSetInteger(0, "DProStopLineM2", OBJPROP_COLOR, colorLines[colorStopCruceLineaM2]&0xAAAAAA);
           }
         break;

      default:
         ObjectSetInteger(0, "DProLineIniJ1", OBJPROP_COLOR, colorLines[colorCruceLineaJ1]);
         ObjectSetInteger(0, "DProLineIniJ2", OBJPROP_COLOR, colorLines[colorCruceLineaJ2]);
         ObjectSetInteger(0, "DProStopLineJ1", OBJPROP_COLOR, colorLines[colorStopCruceLineaJ1]);
         ObjectSetInteger(0, "DProStopLineJ2", OBJPROP_COLOR, colorLines[colorStopCruceLineaJ2]);

         ObjectSetInteger(0, "DProLineIniA1", OBJPROP_COLOR, colorLines[colorCruceLineaA1]);
         ObjectSetInteger(0, "DProLineIniA2", OBJPROP_COLOR, colorLines[colorCruceLineaA2]);
         ObjectSetInteger(0, "DProStopLineA1", OBJPROP_COLOR, colorLines[colorStopCruceLineaA1]);
         ObjectSetInteger(0, "DProStopLineA2", OBJPROP_COLOR, colorLines[colorStopCruceLineaA2]);

         ObjectSetInteger(0, "DProLineIniH1", OBJPROP_COLOR, colorLines[colorCruceLineaH1]);
         ObjectSetInteger(0, "DProLineIniH2", OBJPROP_COLOR, colorLines[colorCruceLineaH2]);
         ObjectSetInteger(0, "DProStopLineH1", OBJPROP_COLOR, colorLines[colorStopCruceLineaH1]);
         ObjectSetInteger(0, "DProStopLineH2", OBJPROP_COLOR, colorLines[colorStopCruceLineaH2]);

         ObjectSetInteger(0, "DProLineIniM1", OBJPROP_COLOR, colorLines[colorCruceLineaM1]);
         ObjectSetInteger(0, "DProLineIniM2", OBJPROP_COLOR, colorLines[colorCruceLineaM2]);
         ObjectSetInteger(0, "DProStopLineM1", OBJPROP_COLOR, colorLines[colorStopCruceLineaM1]);
         ObjectSetInteger(0, "DProStopLineM2", OBJPROP_COLOR, colorLines[colorStopCruceLineaM2]);

         break;
     }
   lastSelectedCaudillo=sCaudillo;
  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double calculaLotaje(double _lotIni, double _lotExp, int _numOper)
  {
   if(LotExact==0)
     {
      double newLot=(_lotIni*100.00);
      return NormalizeLotsSendOrder((0.01 * MathPow(_lotExp, _numOper)))*newLot;
     }
   else
     {
      return NormalizeLotsSendOrder((_lotIni * MathPow(_lotExp, _numOper)));
     }
  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void GestionaPintadoFiltroLineas()
  {

   if(!IsOptimization() && ((IsVisualMode() && IsTesting()) || !IsTesting()))
   {


   switch(selectedCaudillo)
     {
      case 1:
         PintaOperacionesAbiertasCaesar();
         if(ChartGetInteger(0,CHART_SHOW_TRADE_LEVELS,0))
           {
            ChartShiftGet(desplazado);
           }
         ChartSetInteger(0,CHART_SHOW_TRADE_LEVELS,false);
         ChartShiftSet(false);
         noMargenParaBuy=false;
         noMargenParaSell=false;
         if(AccountFreeMarginCheck(Symbol(),OP_BUY,calculaLotaje(CaesarLots,CaesarLotExponent,numeroOperacionesCaesar))<=0)
            noMargenParaBuy=true;
         if(AccountFreeMarginCheck(Symbol(),OP_SELL,calculaLotaje(CaesarLots,CaesarLotExponent,numeroOperacionesCaesar))<=0)
            noMargenParaSell=true;
         break;
      case 2:
         PintaOperacionesAbiertasAlexander();
         if(ChartGetInteger(0,CHART_SHOW_TRADE_LEVELS,0))
           {
            ChartShiftGet(desplazado);
           }
         ChartSetInteger(0,CHART_SHOW_TRADE_LEVELS,false);
         ChartShiftSet(false);
         noMargenParaBuy=false;
         noMargenParaSell=false;
         if(AccountFreeMarginCheck(Symbol(),OP_BUY,calculaLotaje(AlexanderLots,AlexanderLotExponent,numeroOperacionesAlexander))<=0)
            noMargenParaBuy=true;
         if(AccountFreeMarginCheck(Symbol(),OP_SELL,calculaLotaje(AlexanderLots,AlexanderLotExponent,numeroOperacionesAlexander))<=0)
            noMargenParaSell=true;
         break;
      case 3:
         PintaOperacionesAbiertasHannibal();
         if(ChartGetInteger(0,CHART_SHOW_TRADE_LEVELS,0))
           {
            ChartShiftGet(desplazado);
           }
         ChartSetInteger(0,CHART_SHOW_TRADE_LEVELS,false);
         ChartShiftSet(false);
         noMargenParaBuy=false;
         noMargenParaSell=false;
         if(AccountFreeMarginCheck(Symbol(),OP_BUY,calculaLotaje(HannibalLots,HannibalLotExponent,numeroOperacionesHannibal))<=0)
            noMargenParaBuy=true;
         if(AccountFreeMarginCheck(Symbol(),OP_SELL,calculaLotaje(HannibalLots,HannibalLotExponent,numeroOperacionesHannibal))<=0)
            noMargenParaSell=true;
         break;
      case 10:
         PintaOperacionesAbiertasManuales();
         if(ChartGetInteger(0,CHART_SHOW_TRADE_LEVELS,0))
           {
            ChartShiftGet(desplazado);
           }
         ChartSetInteger(0,CHART_SHOW_TRADE_LEVELS,false);
         ChartShiftSet(false);
         break;
      default:
         for(int i=ObjectsTotal(ChartID()); i>=0; i--)
           {
            string name = ObjectName(ChartID(), i);
            if(StringSubstr(name,0,5) == "Order")
              {
               ObjectDelete(ChartID(), name);
              }
            if(StringSubstr(name,0,9) == "textabove")
              {
               ObjectDelete(ChartID(), name);
              }
            if(StringSubstr(name,1,9) == "BreakEven")
              {
               ObjectDelete(ChartID(), name);
              }
           }
         ChartSetInteger(0,CHART_SHOW_TRADE_LEVELS,true);

         actualizadoTPCaesar=true;
         actualizadoTPAlexander=true;
         actualizadoTPHannibal=true;
         if(desplazado)
           {
            desplazado=false;
            ChartShiftSet(true);
           }
         break;
     }
   MuestraSoloLineasDe(selectedCaudillo);

  }
  }
//-------------------------------------------------------






//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int SacaTendencia()
  {
   double Y0, Y1, Y2, Y3, Y4, Y5, Y6, Y7, Y8, Y9;

   const int  periodo1 = PERIOD_M5;
   const int periodo2 = PERIOD_H1;

   Y8 = ((iHigh(NULL, periodo1, 0) - iLow(NULL, periodo1, 0)) / 2) + iLow(NULL, periodo1, 0);
   Y9 = ((iHigh(NULL, periodo2, 0) - iLow(NULL, periodo2, 0)) / 2) + iLow(NULL, periodo2, 0);

   if(iBars(NULL, periodo1) >= 100)
      Y0 = ((iHigh(NULL, periodo1, 100) - iLow(NULL, periodo1, 100)) / 2) + iLow(NULL, periodo1, 100);
   else
      Y0 = Y8;
   if(iBars(NULL, periodo1) >= 50)
      Y1 = ((iHigh(NULL, periodo1, 50) - iLow(NULL, periodo1, 50)) / 2) + iLow(NULL, periodo1, 50);
   else
      Y1 = Y8;
   if(iBars(NULL, periodo1) >= 25)
      Y2 = ((iHigh(NULL, periodo1, 25) - iLow(NULL, periodo1, 25)) / 2) + iLow(NULL, periodo1, 25);
   else
      Y2 = Y8;
   if(iBars(NULL, periodo1) >= 12)
      Y3 = ((iHigh(NULL, periodo1, 12) - iLow(NULL, periodo1, 12)) / 2) + iLow(NULL, periodo1, 12);
   else
      Y3 = Y8;

   if(iBars(NULL, periodo2) >= 100)
      Y4 = ((iHigh(NULL, periodo2, 100) - iLow(NULL, periodo2, 100)) / 2) + iLow(NULL, periodo2, 100);
   else
      Y4 = Y9;
   if(iBars(NULL, periodo2) >= 50)
      Y5 = ((iHigh(NULL, periodo2, 50) - iLow(NULL, periodo2, 50)) / 2) + iLow(NULL, periodo2, 50);
   else
      Y5 = Y9;
   if(iBars(NULL, periodo2) >= 25)
      Y6 = ((iHigh(NULL, periodo2, 25) - iLow(NULL, periodo2, 25)) / 2) + iLow(NULL, periodo2, 25);
   else
      Y6 = Y9;
   if(iBars(NULL, periodo2) >= 12)
      Y7 = ((iHigh(NULL, periodo2, 12) - iLow(NULL, periodo2, 12)) / 2) + iLow(NULL, periodo2, 12);
   else
      Y7 = Y9;

   return (int)(((((((((Y0 - Y8)) * 8) + (((Y1 - Y8)) * 4) + (((Y2 - Y8)) * 2) + ((Y3 - Y8))) / 15) * -(MathPow(Digits() + 1, Digits()))) + (((((((Y4 - Y9)) * 8) + (((Y5 - Y9)) * 4) + (((Y6 - Y9)) * 2) + ((Y7 - Y9))) / 15) * -(MathPow(Digits() + 1, Digits()))))) / 2)) / 4;
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void UpdatePanelExterno()
  {

   tiempoUpdatePanel = TimeCurrent();
   string parNomb = "";
   parNomb = Symbol();
   if(flotanteCuenta == 0)
      double flotCuen = 0;
   else
      flotCuen = flotanteCuenta / tipoCuentaDouble;

   if(profitActualTotal == 0)
      double beneDia = 0;
   else
      beneDia = profitActualTotal / tipoCuentaDouble;

   if(AccountBalance() == 0)
      double balanceCuen = 0;
   else
      balanceCuen = AccountBalance() / tipoCuentaDouble;
   int spreadYa = (int)spreadActual;
   if(valorCadaTick == 0)
      double pTick = 0;
   else
      pTick = valorCadaTick / tipoCuentaDouble;
   double swapBuy = SymbolInfoDouble(Symbol(), SYMBOL_SWAP_LONG);
   double swapSell = SymbolInfoDouble(Symbol(), SYMBOL_SWAP_SHORT);

   double MLots = capitanLotaje;
   if(dineroPorTickLotCapitan == 0)
      double MTick = 0;
   else
      MTick = dineroPorTickLotCapitan / tipoCuentaDouble;;
   int MTrad = countTradesCaptainVar;
   if(capitanFlotante == 0)
      double MFlot = 0;
   else
      MFlot = capitanFlotante / tipoCuentaDouble;
   if(capitanBenefObjTP == 0)
      double MObjTP = 0;
   else
      MObjTP = capitanBenefObjTP / tipoCuentaDouble;
   if(capitanBenefObjSL == 0)
      double MObjSL = 0;
   else
      MObjSL = capitanBenefObjSL / tipoCuentaDouble;
   if(swapCapitan == 0)
      double MSwap = 0;
   else
      MSwap = swapCapitan / tipoCuentaDouble;
   if(comisionCapitan == 0)
      double MComi = 0;
   else
      MComi = comisionCapitan / tipoCuentaDouble;

   double JLots = caesarLotaje;
   if(dineroPorTickLotCaesar == 0)
      double JTick = 0;
   else
      JTick = dineroPorTickLotCaesar / tipoCuentaDouble;;
   int JTrad = countTradesCaesarVar;
   if(CaesarFlotante == 0)
      double JFlot = 0;
   else
      JFlot = CaesarFlotante / tipoCuentaDouble;
   if(caesarBenefObjTP == 0)
      double JObjTP = 0;
   else
      JObjTP = caesarBenefObjTP / tipoCuentaDouble;
   if(caesarBenefObjSL == 0)
      double JObjSL = 0;
   else
      JObjSL = caesarBenefObjSL / tipoCuentaDouble;
   if(swapCaesar == 0)
      double JSwap = 0;
   else
      JSwap = swapCaesar / tipoCuentaDouble;
   if(comisionCaesar == 0)
      double JComi = 0;
   else
      JComi = comisionCaesar / tipoCuentaDouble;

   double ALots = alexanderLotaje;
   if(dineroPorTickLotAlexander == 0)
      double ATick = 0;
   else
      ATick = dineroPorTickLotAlexander / tipoCuentaDouble;;
   int ATrad = countTradesAlexanderVar;
   if(AlexanderFlotante == 0)
      double AFlot = 0;
   else
      AFlot = AlexanderFlotante / tipoCuentaDouble;
   if(alexanderBenefObjTP == 0)
      double AObjTP = 0;
   else
      AObjTP = alexanderBenefObjTP / tipoCuentaDouble;
   if(alexanderBenefObjSL == 0)
      double AObjSL = 0;
   else
      AObjSL = alexanderBenefObjSL / tipoCuentaDouble;
   if(swapAlexander == 0)
      double ASwap = 0;
   else
      ASwap = swapAlexander / tipoCuentaDouble;
   if(comisionAlexander == 0)
      double AComi = 0;
   else
      AComi = comisionAlexander / tipoCuentaDouble;

   double HLots = hannibalLotaje;
   if(dineroPorTickLotHannibal == 0)
      double HTick = 0;
   else
      HTick = dineroPorTickLotHannibal / tipoCuentaDouble;;
   int HTrad = countTradesHannibalVar;
   if(HannibalFlotante == 0)
      double HFlot = 0;
   else
      HFlot = HannibalFlotante / tipoCuentaDouble;
   if(hannibalBenefObjTP == 0)
      double HObjTP = 0;
   else
      HObjTP = hannibalBenefObjTP / tipoCuentaDouble;
   if(hannibalBenefObjSL == 0)
      double HObjSL = 0;
   else
      HObjSL = hannibalBenefObjSL / tipoCuentaDouble;
   if(swapHannibal == 0)
      double HSwap = 0;
   else
      HSwap = swapHannibal / tipoCuentaDouble;
   if(comisionHannibal == 0)
      double HComi = 0;
   else
      HComi = comisionHannibal / tipoCuentaDouble;

   double TLots = lotajeTotalPar;
   if(dineroPorTickLotPar == 0)
      double TTick = 0;
   else
      TTick = dineroPorTickLotPar / tipoCuentaDouble;;
   int TTrad = operacionesContPar;
   if(flotantePar == 0)
      double TFlot = 0;
   else
      TFlot = flotantePar / tipoCuentaDouble;
   if(benefObjParTP == 0)
      double TObjTP = 0;
   else
      TObjTP = benefObjParTP / tipoCuentaDouble;
   if(benefObjParSL == 0)
      double TObjSL = 0;
   else
      TObjSL = benefObjParSL / tipoCuentaDouble;
   if(swapPar == 0)
      double TSwap = 0;
   else
      TSwap = swapPar / tipoCuentaDouble;
   if(comisionPar == 0)
      double TComi = 0;
   else
      TComi = comisionPar / tipoCuentaDouble;

   double lacertaMax = flotanteLacertaCauda;
   double lacertaPorcen = flotanteParActual;

   IsPined = IsTopWindow();
   if(!IsStopped())
     {
      if(IsPined == 0)
        {
         IsPined = 2;
        }
     }

   if(lacertaCauda == false)
     {
      lacertaPorcen = 0.00;
     }

   sacaTipoOpsCaudillos();

   robotReturn.HoraServer = TimeCurrent();

   robotReturn.multiplicadorCierreParcial = multiplicadorCierreParcialLocal;

   robotReturn.lotajePrimeraCaesar = (lotajePrimeraCaesar);
   robotReturn.lotajePrimeraAlexander = (lotajePrimeraAlexander);
   robotReturn.lotajePrimeraHannibal = (lotajePrimeraHannibal);

   robotSend.exit = exit;
   robotSend.mercadoAbierto = mercadoAbierto;
   robotSend.flotCuen = flotCuen;
   robotSend.beneDia = beneDia;
   robotSend.balanceCuen = balanceCuen;
   robotSend.spreadYa = spreadYa;
   robotSend.pTick = pTick;
   robotSend.swapBuy = swapBuy;
   robotSend.swapSell = swapSell;
   robotReturn.Palanca = (int)AccountInfoInteger(ACCOUNT_LEVERAGE);

   robotSend.saludPorcen = saludPorcen;
   pMargenLibre = saludPorcen;
   robotSend.saludMin = saludMin;
   robotSend.saludMax = saludMax;
   robotSend.lacertaPorcen = lacertaPorcen;
   robotSend.lacertaMax = lacertaMax;

   robotSend.tendencia = tendencia;
   robotSend.volatilidadJ = (int)atrValueJ;
   robotSend.holguraJ = (int)atrParcialValueJ;

   robotSend.volatilidadA = (int)atrValueA;
   robotSend.holguraA = (int)atrParcialValueA;

   robotSend.volatilidadH = (int)atrValueH;
   robotSend.holguraH = (int)atrParcialValueH;

   robotSend.noOpHorarioJ = noOpHorarioJ;
   robotSend.noOpHorarioA = noOpHorarioA;
   robotSend.noOpHorarioH = noOpHorarioH;


   robotSend.JFlot = JFlot;
   robotSend.JObjTP = JObjTP;
   robotSend.JObjSL = JObjSL;
   robotSend.JSwap = JSwap;
   robotSend.JComi = JComi;
   robotSend.JTick = JTick;
   robotSend.JLots = JLots;
   robotSend.JTrad = JTrad;
   robotSend.opCaesar = opCaesar;

   robotSend.AFlot = AFlot;
   robotSend.AObjTP = AObjTP;
   robotSend.AObjSL = AObjSL;
   robotSend.ASwap = ASwap;
   robotSend.AComi = AComi;
   robotSend.ATick = ATick;
   robotSend.ALots = ALots;
   robotSend.ATrad = ATrad;
   robotSend.opAlex = opAlexander;

   robotSend.HFlot = HFlot;
   robotSend.HObjTP = HObjTP;
   robotSend.HObjSL = HObjSL;
   robotSend.HSwap = HSwap;
   robotSend.HComi = HComi;
   robotSend.HTick = HTick;
   robotSend.HLots = HLots;
   robotSend.HTrad = HTrad;
   robotSend.opHannibal = opHannibal;

   robotSend.MFlot = MFlot;
   robotSend.MObjTP = MObjTP;
   robotSend.MObjSL = MObjSL;
   robotSend.MSwap = MSwap;
   robotSend.MComi = MComi;
   robotSend.MTick = MTick;
   robotSend.MLots = MLots;
   robotSend.MTrad = MTrad;

   robotSend.TFlot = TFlot;
   robotSend.TObjTP = TObjTP;
   robotSend.TObjSL = TObjSL;
   robotSend.TSwap = TSwap;
   robotSend.TComi = TComi;
   robotSend.TTick = TTick;
   robotSend.TLots = TLots;
   robotSend.TTrad = TTrad;

   if(acumCicloJ == 0)
      robotSend.acumCicloJ = 0;
   else
      robotSend.acumCicloJ = acumCicloJ / tipoCuentaDouble;

   if(acumCicloA == 0)
      robotSend.acumCicloA = 0;
   else
      robotSend.acumCicloA = acumCicloA / tipoCuentaDouble;

   if(acumCicloH == 0)
      robotSend.acumCicloH = 0;
   else
      robotSend.acumCicloH = acumCicloH / tipoCuentaDouble;


   if(CaesarOperacionAbiertasBuy>=CaesarOperacionAbiertasSell)
      flotanteUltimaCaesar=flotanteOrdenMasBajaCaesar;
   else
      flotanteUltimaCaesar=flotanteOrdenMasAltaCaesar;

   if(flotanteUltimaCaesar == 0)
      robotSend.flotUltiJ = 0;
   else
      robotSend.flotUltiJ = flotanteUltimaCaesar / tipoCuentaDouble;

   if(flotanteOrdenMasAltaCaesar == 0)
      robotSend.flotAltaJ = 0;
   else
      robotSend.flotAltaJ = flotanteOrdenMasAltaCaesar / tipoCuentaDouble;

   if(flotanteOrdenMasBajaCaesar == 0)
      robotSend.flotBajaJ = 0;
   else
      robotSend.flotBajaJ = flotanteOrdenMasBajaCaesar / tipoCuentaDouble;


   if(AlexanderOperacionAbiertasBuy>=AlexanderOperacionAbiertasSell)
      flotanteUltimaAlexander=flotanteOrdenMasBajaAlexander;
   else
      flotanteUltimaAlexander=flotanteOrdenMasAltaAlexander;

   if(flotanteUltimaAlexander == 0)
      robotSend.flotUltiA = 0;
   else
      robotSend.flotUltiA = flotanteUltimaAlexander / tipoCuentaDouble;

   if(flotanteOrdenMasAltaAlexander == 0)
      robotSend.flotAltaA = 0;
   else
      robotSend.flotAltaA = flotanteOrdenMasAltaAlexander / tipoCuentaDouble;

   if(flotanteOrdenMasBajaAlexander == 0)
      robotSend.flotBajaA = 0;
   else
      robotSend.flotBajaA = flotanteOrdenMasBajaAlexander / tipoCuentaDouble;


   if(HannibalOperacionAbiertasBuy>=HannibalOperacionAbiertasSell)
      flotanteUltimaHannibal=flotanteOrdenMasBajaHannibal;
   else
      flotanteUltimaHannibal=flotanteOrdenMasAltaHannibal;

   if(flotanteUltimaHannibal == 0)
      robotSend.flotUltiH = 0;
   else
      robotSend.flotUltiH = flotanteUltimaHannibal / tipoCuentaDouble;

   if(flotanteOrdenMasAltaHannibal == 0)
      robotSend.flotAltaH = 0;
   else
      robotSend.flotAltaH = flotanteOrdenMasAltaHannibal / tipoCuentaDouble;

   if(flotanteOrdenMasBajaHannibal == 0)
      robotSend.flotBajaH = 0;
   else
      robotSend.flotBajaH = flotanteOrdenMasBajaHannibal / tipoCuentaDouble;

   if(CaesarOperacionAbiertasBuy>=CaesarOperacionAbiertasSell)
      lastOpenPriceCaesar=precioOrdenMasBajaCaesar;
   else
      lastOpenPriceCaesar=precioOrdenMasAltaCaesar;
   robotSend.lastOrderOpenPriceJ = lastOpenPriceCaesar;
   if(AlexanderOperacionAbiertasBuy>=AlexanderOperacionAbiertasSell)
      lastOpenPriceAlexander=precioOrdenMasBajaAlexander;
   else
      lastOpenPriceAlexander=precioOrdenMasAltaAlexander;
   robotSend.lastOrderOpenPriceA = lastOpenPriceAlexander;
   if(HannibalOperacionAbiertasBuy>=HannibalOperacionAbiertasSell)
      lastOpenPriceHannibal=precioOrdenMasBajaHannibal;
   else
      lastOpenPriceHannibal=precioOrdenMasAltaHannibal;
   robotSend.lastOrderOpenPriceH = lastOpenPriceHannibal;

   robotSend.bid = Bid;
   robotSend.ask = Ask;

   robotSend.lotajeTotalParBuys = lotajeTotalParBuys;
   robotSend.lotajeTotalParSells = lotajeTotalParSells;
   robotSend.lotajeTotalCuentaBuys = lotajeTotalCuentaBuys;
   robotSend.lotajeTotalCuentaSells = lotajeTotalCuentaSells;

   uchar arrayMoneda[8];
   StringToShortArray(AccountInfoString(ACCOUNT_CURRENCY), robotSend.moneda);
   ArrayFree(arrayMoneda);


   robotSend.noMargenParaBuy = (int)noMargenParaBuy;
   robotSend.noMargenParaSell = (int)noMargenParaSell;


   int hWnd1=0;
   long resultH = -1;

   conta=0;
   while(hWnd1==0 && (IsVisualMode()||!IsTesting()))
     {
      if(ChartGetInteger(ChartID(), CHART_WINDOW_HANDLE, 0, resultH))
        {
         hWnd1 = (int)resultH;
        }
      else
        {
         hWnd1 = WindowHandle(Symbol(), PERIOD_CURRENT);
        }
        conta=conta+1;
        if (conta>50)
        {
           hWnd1=hWnd1back;
           break;
        }
     }
     hWnd1back=hWnd1;

   robotReturn.parHwnd = hWnd1;//WindowHandle(Symbol(), Period());

   IsPined = IsTopWindow();
   if(!IsStopped())
     {
      if(IsPined == 0)
        {
         IsPined = 2;
        }
      else
        {
         cuentaParo=0;
        }
     }
   robotSend.IsPined = IsPined;




   if(!estaEnBotlidator && !IsStopped())
     {
      estaEnUpdate = true;
      for(int es = 1; es < 3; es++)
        {
         retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, parNomb);
         if(retorno>=0)
           {
            break;
           }
         Sleep(1);
        }
      Sleep(5);
      estaEnUpdate = false;

      if(CaesarLots != CaesarLotsLast)
        {
         lotajePrimeraCaesar=CaesarLots;
        }
      if(AlexanderLots != AlexanderLotsLast)
        {
         lotajePrimeraAlexander=AlexanderLots;
        }
      if(HannibalLots != HannibalLotsLast)
        {
         lotajePrimeraHannibal=HannibalLots;
        }

      CaesarLotsLast = CaesarLots;
      AlexanderLotsLast = AlexanderLots;
      HannibalLotsLast = HannibalLots;

     }


      EjecutaAcciones();

// ALERTAS ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
   if(!IsTesting())
     {
      //DE MENSAJES
      if(MensajeFrecuenciaAlerta > 0 && (tiempoAlertaMensaje < TimeCurrent() || mensajeBot != lastMensajeBot) && ((StringSubstr(mensajeBot, 0, 5) == "ERROR" && MensajeGatilloAlerta == 0) || (MensajeGatilloAlerta == 1 && StringLen(mensajeBot) > 3)))
        {
         lastMensajeBot = mensajeBot;

         bool response1 = false;
         bool response2 = false;
         if(AlertaMovilActiva == 1)
           {
            response1 = SendNotification(mensajeBot);
           }
         if(AlertaMailActiva == 1)
           {
            response2 = SendMail("Mensaje de Ducibus Pro", mensajeBot);
           }
         if(AlertaSoundActiva == 1)
           {
            Alert(mensajeBot);
           }
         if(response1 || response2 || AlertaSoundActiva == 1)
           {
            switch(MensajeFrecuenciaAlerta)
              {
               case 1:
                  tiempoAlertaMensaje = TimeCurrent() + 60;    //   2 Minutos (el calculo es de 1 pero son 2)
                  break;
               case 2:
                  tiempoAlertaMensaje = TimeCurrent() + 60 * 10; //   10 Minutos
                  break;
               case 3:
                  tiempoAlertaMensaje = TimeCurrent() + 60 * 60; //   1 Hora
                  break;
               case 4:
                  tiempoAlertaMensaje = TimeCurrent() + 60 * 60 * 4; //   4 Horas
                  break;
               default:
                  tiempoAlertaMensaje = TimeCurrent() + 60 * 60 * 24; //  24 Horas
                  break;
              }
           }
        }
      //DE MARGEN LIBRE
      if(MargenFrecuenciaAlerta > 0 && tiempoAlertaMargen < TimeCurrent() && ((MargenGatilloAlerta == 0 && saludPorcen < MargenValorAlerta) || (MargenGatilloAlerta == 1 && saludPorcen > MargenValorAlerta)))
        {
         if(MargenGatilloAlerta == 0 && saludPorcen < MargenValorAlerta)
           {
            mensajeBot = "¡ALERTA! El nivel de margen libre de la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está por debajo de " + MargenValorAlerta;
           }
         if(MargenGatilloAlerta == 1 && saludPorcen > MargenValorAlerta)
           {
            mensajeBot = "¡ALERTA! El nivel de margen libre de la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está por encima de " + MargenValorAlerta;
           }
         response1 = false;
         response2 = false;
         if(AlertaMovilActiva == 1)
           {
            response1 = SendNotification(mensajeBot);
           }
         if(AlertaMailActiva == 1)
           {
            response2 = SendMail("Mensaje de Ducibus Pro", mensajeBot);
           }
         if(AlertaSoundActiva == 1)
           {
            Alert(mensajeBot);
           }
         if(response1 || response2 || AlertaSoundActiva == 1)
           {
            switch(MargenFrecuenciaAlerta)
              {
               case 1:
                  tiempoAlertaMargen = TimeCurrent() + 60;    //   2 Minutos (el calculo es de 1 pero son 2)
                  break;
               case 2:
                  tiempoAlertaMargen = TimeCurrent() + 60 * 10; //   10 Minutos
                  break;
               case 3:
                  tiempoAlertaMargen = TimeCurrent() + 60 * 60; //   1 Hora
                  break;
               case 4:
                  tiempoAlertaMargen = TimeCurrent() + 60 * 60 * 4; //   4 Horas
                  break;
               default:
                  tiempoAlertaMargen = TimeCurrent() + 60 * 60 * 24; //  24 Horas
                  break;
              }
           }
        }

      //DE LACERTA CAUDA
      if(LacertaFrecuenciaAlerta > 0 && tiempoAlertaLacerta < TimeCurrent() && ((LacertaGatilloAlerta == 0 && lacertaPorcen < LacertaValorAlerta) || (LacertaGatilloAlerta == 1 && lacertaPorcen > LacertaValorAlerta)))
        {
         if(LacertaGatilloAlerta == 0 && lacertaPorcen < LacertaValorAlerta)
           {
            mensajeBot = "¡ALERTA! El nivel de Lacerta Cauda de la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está por debajo de " + LacertaValorAlerta;
           }
         if(LacertaGatilloAlerta == 1 && lacertaPorcen > LacertaValorAlerta)
           {
            mensajeBot = "¡ALERTA! El nivel de Lacerta Cauda de la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está por encima de " + LacertaValorAlerta;
           }
         response1 = false;
         response2 = false;
         if(AlertaMovilActiva == 1)
           {
            response1 = SendNotification(mensajeBot);
           }
         if(AlertaMailActiva == 1)
           {
            response2 = SendMail("Mensaje de Ducibus Pro", mensajeBot);
           }
         if(AlertaSoundActiva == 1)
           {
            Alert(mensajeBot);
           }
         if(response1 || response2 || AlertaSoundActiva == 1)
           {
            switch(LacertaFrecuenciaAlerta)
              {
               case 1:
                  tiempoAlertaLacerta = TimeCurrent() + 60;    //   2 Minutos (el calculo es de 1 pero son 2)
                  break;
               case 2:
                  tiempoAlertaLacerta = TimeCurrent() + 60 * 10; //   10 Minutos
                  break;
               case 3:
                  tiempoAlertaLacerta = TimeCurrent() + 60 * 60; //   1 Hora
                  break;
               case 4:
                  tiempoAlertaLacerta = TimeCurrent() + 60 * 60 * 4; //   4 Horas
                  break;
               default:
                  tiempoAlertaLacerta = TimeCurrent() + 60 * 60 * 24; //  24 Horas
                  break;
              }
           }
        }

      //DE CAESAR
      if(CaesarFrecuenciaAlerta > 0 && tiempoAlertaCaesar < TimeCurrent() &&
         (
            (CaesarTipoAlerta == 0 && ((CaesarGatilloAlerta == 0 && JFlot < CaesarValorAlerta) || (CaesarGatilloAlerta == 1 && JFlot > CaesarValorAlerta))) ||
            (CaesarTipoAlerta == 1 && ((CaesarGatilloAlerta == 0 && JTrad < CaesarValorAlerta) || (CaesarGatilloAlerta == 1 && JTrad > CaesarValorAlerta))) ||
            (CaesarTipoAlerta == 2 && ((CaesarGatilloAlerta == 0 && JLots < CaesarValorAlerta) || (CaesarGatilloAlerta == 1 && JLots > CaesarValorAlerta)))
         )
        )
        {
         if(CaesarTipoAlerta == 0 && ((CaesarGatilloAlerta == 0 && JFlot < CaesarValorAlerta)))
           {mensajeBot = "¡ALERTA! El flotante de Julius Caesar en la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está POR DEBAJO de: " + CaesarValorAlerta;}
         if(CaesarTipoAlerta == 0 && ((CaesarGatilloAlerta == 1 && JFlot > CaesarValorAlerta)))
           {mensajeBot = "¡ALERTA! El flotante de Julius Caesar en la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está POR ENCIMA de: " + CaesarValorAlerta;}

         if(CaesarTipoAlerta == 1 && ((CaesarGatilloAlerta == 0 && JTrad < (int)CaesarValorAlerta)))
           {mensajeBot = "¡ALERTA! El Nº de Martingalas de Julius Caesar en la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está POR DEBAJO de: " + (int)CaesarValorAlerta;}
         if(CaesarTipoAlerta == 1 && ((CaesarGatilloAlerta == 1 && JTrad > (int)CaesarValorAlerta)))
           {mensajeBot = "¡ALERTA! El Nº de Martingalas de Julius Caesar en la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está POR ENCIMA de: " + (int)CaesarValorAlerta;}

         if(CaesarTipoAlerta == 2 && ((CaesarGatilloAlerta == 0 && JLots < CaesarValorAlerta)))
           {mensajeBot = "¡ALERTA! El lotaje de Julius Caesar en la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está POR DEBAJO de: " + CaesarValorAlerta;}
         if(CaesarTipoAlerta == 2 && ((CaesarGatilloAlerta == 1 && JLots > CaesarValorAlerta)))
           {mensajeBot = "¡ALERTA! El lotaje de Julius Caesar en la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está POR ENCIMA de: " + CaesarValorAlerta;}

         response1 = false;
         response2 = false;
         if(AlertaMovilActiva == 1)
           {
            response1 = SendNotification(mensajeBot);
           }
         if(AlertaMailActiva == 1)
           {
            response2 = SendMail("Alerta de Ducibus Pro", mensajeBot);
           }
         if(AlertaSoundActiva == 1)
           {
            Alert(mensajeBot);
           }
         if(response1 || response2 || AlertaSoundActiva == 1)
           {
            switch(CaesarFrecuenciaAlerta)
              {
               case 1:
                  tiempoAlertaCaesar = TimeCurrent() + 60;    //   2 Minutos (el calculo es de 1 pero son 2)
                  break;
               case 2:
                  tiempoAlertaCaesar = TimeCurrent() + 60 * 10; //   10 Minutos
                  break;
               case 3:
                  tiempoAlertaCaesar = TimeCurrent() + 60 * 60; //   1 Hora
                  break;
               case 4:
                  tiempoAlertaCaesar = TimeCurrent() + 60 * 60 * 4; //   4 Horas
                  break;
               default:
                  tiempoAlertaCaesar = TimeCurrent() + 60 * 60 * 24; //  24 Horas
                  break;
              }
           }
        }

      //DE ALEXANDER
      if(AlexFrecuenciaAlerta > 0 && tiempoAlertaAlexander < TimeCurrent() &&
         (
            (AlexTipoAlerta == 0 && ((AlexGatilloAlerta == 0 && AFlot < AlexValorAlerta) || (AlexGatilloAlerta == 1 && AFlot > AlexValorAlerta))) ||
            (AlexTipoAlerta == 1 && ((AlexGatilloAlerta == 0 && ATrad < AlexValorAlerta) || (AlexGatilloAlerta == 1 && ATrad > AlexValorAlerta))) ||
            (AlexTipoAlerta == 2 && ((AlexGatilloAlerta == 0 && ALots < AlexValorAlerta) || (AlexGatilloAlerta == 1 && ALots > AlexValorAlerta)))
         )
        )
        {
         if(AlexTipoAlerta == 0 && ((AlexGatilloAlerta == 0 && AFlot < AlexValorAlerta)))
           {mensajeBot = "¡ALERTA! El flotante de Alexander Magnus en la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está POR DEBAJO de: " + AlexValorAlerta;}
         if(AlexTipoAlerta == 0 && ((AlexGatilloAlerta == 1 && AFlot > AlexValorAlerta)))
           {mensajeBot = "¡ALERTA! El flotante de Alexander Magnus en la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está POR ENCIMA de: " + AlexValorAlerta;}

         if(AlexTipoAlerta == 1 && ((AlexGatilloAlerta == 0 && ATrad < (int)AlexValorAlerta)))
           {mensajeBot = "¡ALERTA! El Nº de Martingalas de Alexander Magnus en la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está POR DEBAJO de: " + (int)AlexValorAlerta;}
         if(AlexTipoAlerta == 1 && ((AlexGatilloAlerta == 1 && ATrad > (int)AlexValorAlerta)))
           {mensajeBot = "¡ALERTA! El Nº de Martingalas de Alexander Magnus en la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está POR ENCIMA de: " + (int)AlexValorAlerta;}

         if(AlexTipoAlerta == 2 && ((AlexGatilloAlerta == 0 && ALots < AlexValorAlerta)))
           {mensajeBot = "¡ALERTA! El lotaje de Alexander Magnus en la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está POR DEBAJO de: " + AlexValorAlerta;}
         if(AlexTipoAlerta == 2 && ((AlexGatilloAlerta == 1 && ALots > AlexValorAlerta)))
           {mensajeBot = "¡ALERTA! El lotaje de Alexander Magnus en la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está POR ENCIMA de: " + AlexValorAlerta;}
         response1 = false;
         response2 = false;
         if(AlertaMovilActiva == 1)
           {
            response1 = SendNotification(mensajeBot);
           }
         if(AlertaMailActiva == 1)
           {
            response2 = SendMail("Alerta de Ducibus Pro", mensajeBot);
           }
         if(AlertaSoundActiva == 1)
           {
            Alert(mensajeBot);
           }
         if(response1 || response2 || AlertaSoundActiva == 1)
           {
            switch(AlexFrecuenciaAlerta)
              {
               case 1:
                  tiempoAlertaAlexander = TimeCurrent() + 60;    //   2 Minutos (el calculo es de 1 pero son 2)
                  break;
               case 2:
                  tiempoAlertaAlexander = TimeCurrent() + 60 * 10; //   10 Minutos
                  break;
               case 3:
                  tiempoAlertaAlexander = TimeCurrent() + 60 * 60; //   1 Hora
                  break;
               case 4:
                  tiempoAlertaAlexander = TimeCurrent() + 60 * 60 * 4; //   4 Horas
                  break;
               default:
                  tiempoAlertaAlexander = TimeCurrent() + 60 * 60 * 24; //  24 Horas
                  break;
              }
           }
        }

      //DE HANNIBAL
      if(HannibalFrecuenciaAlerta > 0 && tiempoAlertaHannibal < TimeCurrent() &&
         (
            (HannibalTipoAlerta == 0 && ((HannibalGatilloAlerta == 0 && HFlot < HannibalValorAlerta) || (HannibalGatilloAlerta == 1 && HFlot > HannibalValorAlerta))) ||
            (HannibalTipoAlerta == 1 && ((HannibalGatilloAlerta == 0 && HTrad < HannibalValorAlerta) || (HannibalGatilloAlerta == 1 && HTrad > HannibalValorAlerta))) ||
            (HannibalTipoAlerta == 2 && ((HannibalGatilloAlerta == 0 && HLots < HannibalValorAlerta) || (HannibalGatilloAlerta == 1 && HLots > HannibalValorAlerta)))
         )
        )
        {
         if(HannibalTipoAlerta == 0 && ((HannibalGatilloAlerta == 0 && HFlot < HannibalValorAlerta)))
           {mensajeBot = "¡ALERTA! El flotante de Hannibal Barca en la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está POR DEBAJO de: " + HannibalValorAlerta;}
         if(HannibalTipoAlerta == 0 && ((HannibalGatilloAlerta == 1 && HFlot > HannibalValorAlerta)))
           {mensajeBot = "¡ALERTA! El flotante de Hannibal Barca en la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está POR ENCIMA de: " + HannibalValorAlerta;}

         if(HannibalTipoAlerta == 1 && ((HannibalGatilloAlerta == 0 && HTrad < (int)HannibalValorAlerta)))
           {mensajeBot = "¡ALERTA! El Nº de Martingalas de Hannibal Barca en la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está POR DEBAJO de: " + (int)HannibalValorAlerta;}
         if(HannibalTipoAlerta == 1 && ((HannibalGatilloAlerta == 1 && HTrad > (int)HannibalValorAlerta)))
           {mensajeBot = "¡ALERTA! El Nº de Martingalas de Hannibal Barca en la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está POR ENCIMA de: " + (int)HannibalValorAlerta;}

         if(HannibalTipoAlerta == 2 && ((HannibalGatilloAlerta == 0 && HLots < HannibalValorAlerta)))
           {mensajeBot = "¡ALERTA! El lotaje de Hannibal Barca en la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está POR DEBAJO de: " + HannibalValorAlerta;}
         if(HannibalTipoAlerta == 2 && ((HannibalGatilloAlerta == 1 && HLots > HannibalValorAlerta)))
           {mensajeBot = "¡ALERTA! El lotaje de Hannibal Barca en la cuenta: " + (int)AccountInfoInteger(ACCOUNT_LOGIN) + " está POR ENCIMA de: " + HannibalValorAlerta;}

         response1 = false;
         response2 = false;
         if(AlertaMovilActiva == 1)
           {
            response1 = SendNotification(mensajeBot);
           }
         if(AlertaMailActiva == 1)
           {
            response2 = SendMail("Alerta de Ducibus Pro", mensajeBot);
           }
         if(AlertaSoundActiva == 1)
           {
            Alert(mensajeBot);
           }
         if(response1 || response2 || AlertaSoundActiva == 1)
           {
            switch(HannibalFrecuenciaAlerta)
              {
               case 1:
                  tiempoAlertaHannibal = TimeCurrent() + 60;    //   2 Minutos (el calculo es de 1 pero son 2)
                  break;
               case 2:
                  tiempoAlertaHannibal = TimeCurrent() + 60 * 10; //   10 Minutos
                  break;
               case 3:
                  tiempoAlertaHannibal = TimeCurrent() + 60 * 60; //   1 Hora
                  break;
               case 4:
                  tiempoAlertaHannibal = TimeCurrent() + 60 * 60 * 4; //   4 Horas
                  break;
               default:
                  tiempoAlertaHannibal = TimeCurrent() + 60 * 60 * 24; //  24 Horas
                  break;
              }
           }
        }
     }

// FIN ALERTAS ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


   if((retorno > 0 || robotSend.exit > 0) && !IsStopped())
     {
      exit = 1;
      robotSend.exit = 1;

      estaEnUpdate = true;
      for(es = 1; es < 3; es++)
        {
         retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, parNomb);
         if(retorno>=0)
           {
            break;
           }
         Sleep(1);
        }
      Sleep(5);
      estaEnUpdate = false;

      Sleep(1500);
      ExpertRemove();
      return;
     }



  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void EjecutaAcciones()
  {

   bool hayAccion=false;

   if(robotReturn.cierraUltimaCaesar == 1)
     {
      multiplicadorCierreParcialLocal = NormalizeDouble(robotReturn.multiplicadorCierreParcial, 2);
      cerrarTicket(ticketOrdenMasAltaCaesar);
      robotReturn.cierraUltimaCaesar = 0;
     }
   if(robotReturn.cierraUltimaCaesar == 2)
     {
      multiplicadorCierreParcialLocal = NormalizeDouble(robotReturn.multiplicadorCierreParcial, 2);
      cerrarTicket(ticketOrdenMasBajaCaesar);
      robotReturn.cierraUltimaCaesar = 0;
     }
   if(robotReturn.cierraCicloCaesar == 1)
     {
      multiplicadorCierreParcialLocal = NormalizeDouble(robotReturn.multiplicadorCierreParcial, 2);
      //cerrarTodasOperacionesSymbolCaudillo(MagicNumber_Caesar);
      cerrarTodasOperacionesCiclo(MagicNumber_Caesar);
      robotReturn.cierraCicloCaesar = 0;
     }

   if(robotReturn.cierraUltimaAlexander == 1)
     {
      multiplicadorCierreParcialLocal = NormalizeDouble(robotReturn.multiplicadorCierreParcial, 2);
      cerrarTicket(ticketOrdenMasAltaAlexander);
      robotReturn.cierraUltimaAlexander = 0;
     }
   if(robotReturn.cierraUltimaAlexander == 2)
     {
      multiplicadorCierreParcialLocal = NormalizeDouble(robotReturn.multiplicadorCierreParcial, 2);
      cerrarTicket(ticketOrdenMasBajaAlexander);
      robotReturn.cierraUltimaAlexander = 0;
     }
   if(robotReturn.cierraCicloAlexander == 1)
     {
      multiplicadorCierreParcialLocal = NormalizeDouble(robotReturn.multiplicadorCierreParcial, 2);
      //cerrarTodasOperacionesSymbolCaudillo(MagicNumber_Alexander);
      cerrarTodasOperacionesCiclo(MagicNumber_Alexander);
      robotReturn.cierraCicloAlexander = 0;
     }

   if(robotReturn.cierraUltimaHannibal == 1)
     {
      multiplicadorCierreParcialLocal = NormalizeDouble(robotReturn.multiplicadorCierreParcial, 2);
      cerrarTicket(ticketOrdenMasAltaHannibal);
      robotReturn.cierraUltimaHannibal = 0;
     }
   if(robotReturn.cierraUltimaHannibal == 2)
     {
      multiplicadorCierreParcialLocal = NormalizeDouble(robotReturn.multiplicadorCierreParcial, 2);
      cerrarTicket(ticketOrdenMasBajaHannibal);
      robotReturn.cierraUltimaHannibal = 0;
     }
   if(robotReturn.cierraCicloHannibal == 1)
     {
      multiplicadorCierreParcialLocal = NormalizeDouble(robotReturn.multiplicadorCierreParcial, 2);
      //cerrarTodasOperacionesSymbolCaudillo(MagicNumber_Hannibal);
      cerrarTodasOperacionesCiclo(MagicNumber_Hannibal);
      robotReturn.cierraCicloHannibal = 0;
     }



   if(robotReturn.cierraPar == 1)
     {
      cerrarTodasOperacionesPar();
      robotReturn.cierraPar = 0;
     }

   if(robotReturn.cierraTodo == 1)
     {
      cerrarTodasOperacionesCuenta();
      robotReturn.cierraTodo = 0;
     }

   if(robotReturn.otraMartinCaesar == 1)
     {
      otraMartinCaesar = 1;
      robotReturn.otraMartinCaesar = 0;
      hayAccion=true;
     }
   if(robotReturn.otraMartinAlexander == 1)
     {
      otraMartinAlexander = 1;
      robotReturn.otraMartinAlexander = 0;
      hayAccion=true;
     }
   if(robotReturn.otraMartinHannibal == 1)
     {
      otraMartinHannibal = 1;
      robotReturn.otraMartinHannibal = 0;
      hayAccion=true;
     }

   if(CaesarOrdenManual == 0)
     {
      CaesarOrdenManual = robotReturn.tipoManualCaesar;
      if(CaesarOrdenManual > 0)
      {
         hayAccion=true;
      }
     }
   if(AlexanderOrdenManual == 0)
     {
      AlexanderOrdenManual = robotReturn.tipoManualAlexander;
      if(AlexanderOrdenManual > 0)
         hayAccion=true;
     }
   if(HannibalOrdenManual == 0)
     {
      HannibalOrdenManual = robotReturn.tipoManualHannibal;
      if(HannibalOrdenManual > 0)
         hayAccion=true;
     }

   robotReturn.tipoManualCaesar = 0;
   robotReturn.tipoManualAlexander = 0;
   robotReturn.tipoManualHannibal = 0;

   selectedCaudillo = robotReturn.selectedCaudillo;

   if(IsPined==1)
      GestionaPintadoFiltroLineas();

   CaesarLots = robotReturn.CaesarLots;
   CaesarLotsIni = CaesarLots;
   CaesarLotsIni2 = CaesarLots;
   CaesarLotExponentIni = robotReturn.CaesarLotExp;
   CaesarCambiaExpActivo = (bool)robotReturn.CaesarCambiaExpActivo;
   CaesarCambiaExpDesdeMartingala = robotReturn.CaesarCambiaExpDesdeMartingala;
   CaesarCambiaExpValor = robotReturn.CaesarCambiaExpValor;
   maxLotsCaesar = robotReturn.CaesarMaxLots;
   MaxTrades_Caesar = robotReturn.CaesarMaxTrades;
   CaesarPipStep = robotReturn.CaesarPipStep;
   CaesarModoPipStep = (intModoPipStep)robotReturn.CaesarModoPipStep;
   CaesarTakeProfit = robotReturn.CaesarTP;
   CaesarModoTp = (intModoTp)robotReturn.CaesarModoTp;
   CaesarTrailStart = robotReturn.CaesarTrailingStart;
   CaesarTrailStop = robotReturn.CaesarTrailingStop;

   tipoCruceLineaJ1 = robotReturn.tipoCruceLineaJ1;
   direcCruceLineaJ1 = robotReturn.direcCruceLineaJ1;
   colorCruceLineaJ1 = robotReturn.colorCruceLineaJ1;
   stopCruceLineaJ1 = robotReturn.stopCruceLineaJ1;
   colorStopCruceLineaJ1 = robotReturn.colorStopCruceLineaJ1;
   tipoCruceLineaJ2 = robotReturn.tipoCruceLineaJ2;
   direcCruceLineaJ2 = robotReturn.direcCruceLineaJ2;
   colorCruceLineaJ2 = robotReturn.colorCruceLineaJ2;
   stopCruceLineaJ2 = robotReturn.stopCruceLineaJ2;
   colorStopCruceLineaJ2 = robotReturn.colorStopCruceLineaJ2;

   AlexanderLots = robotReturn.AlexLots;
   AlexanderLotsIni = AlexanderLots;
   AlexanderLotsIni2 = AlexanderLots;
   AlexanderLotExponentIni = robotReturn.AlexLotExp;
   AlexanderCambiaExpActivo = (bool)robotReturn.AlexanderCambiaExpActivo;
   AlexanderCambiaExpDesdeMartingala = robotReturn.AlexanderCambiaExpDesdeMartingala;
   AlexanderCambiaExpValor = robotReturn.AlexanderCambiaExpValor;
   maxLotsAlexander = robotReturn.AlexMaxLots;
   MaxTrades_Alexander = robotReturn.AlexMaxTrades;
   AlexanderPipStep = robotReturn.AlexPipStep;
   AlexanderModoPipStep = (intModoPipStep)robotReturn.AlexModoPipStep;
   AlexanderModoTp = (intModoTp)robotReturn.AlexModoTp;
   AlexanderTakeProfit = robotReturn.AlexTP;
   AlexanderTrailStart = robotReturn.AlexTrailingStart;
   AlexanderTrailStop = robotReturn.AlexTrailingStop;

   tipoCruceLineaA1 = robotReturn.tipoCruceLineaA1;
   direcCruceLineaA1 = robotReturn.direcCruceLineaA1;
   colorCruceLineaA1 = robotReturn.colorCruceLineaA1;
   stopCruceLineaA1 = robotReturn.stopCruceLineaA1;
   colorStopCruceLineaA1 = robotReturn.colorStopCruceLineaA1;
   tipoCruceLineaA2 = robotReturn.tipoCruceLineaA2;
   direcCruceLineaA2 = robotReturn.direcCruceLineaA2;
   colorCruceLineaA2 = robotReturn.colorCruceLineaA2;
   stopCruceLineaA2 = robotReturn.stopCruceLineaA2;
   colorStopCruceLineaA2 = robotReturn.colorStopCruceLineaA2;

   HannibalLots = robotReturn.HannibalLots;
   HannibalLotsIni = HannibalLots;
   HannibalLotsIni2 = HannibalLots;
   HannibalLotExponentIni = robotReturn.HannibalLotExp;
   HannibalCambiaExpActivo = (bool)robotReturn.HannibalCambiaExpActivo;
   HannibalCambiaExpDesdeMartingala = robotReturn.HannibalCambiaExpDesdeMartingala;
   HannibalCambiaExpValor = robotReturn.HannibalCambiaExpValor;
   maxLotsHannibal = robotReturn.HannibalMaxLots;
   MaxTrades_Hannibal = robotReturn.HannibalMaxTrades;
   HannibalPipStep = robotReturn.HannibalPipStep;
   HannibalModoPipStep = (intModoPipStep)robotReturn.HannibalModoPipStep;
   HannibalTakeProfit = robotReturn.HannibalTP;
   HannibalModoTp = (intModoTp)robotReturn.HannibalModoTp;
   HannibalTrailStart = robotReturn.HannibalTrailingStart;
   HannibalTrailStop = robotReturn.HannibalTrailingStop;

   tipoCruceLineaH1 = robotReturn.tipoCruceLineaH1;
   direcCruceLineaH1 = robotReturn.direcCruceLineaH1;
   colorCruceLineaH1 = robotReturn.colorCruceLineaH1;
   stopCruceLineaH1 = robotReturn.stopCruceLineaH1;
   colorStopCruceLineaH1 = robotReturn.colorStopCruceLineaH1;
   tipoCruceLineaH2 = robotReturn.tipoCruceLineaH2;
   direcCruceLineaH2 = robotReturn.direcCruceLineaH2;
   colorCruceLineaH2 = robotReturn.colorCruceLineaH2;
   stopCruceLineaH2 = robotReturn.stopCruceLineaH2;
   colorStopCruceLineaH2 = robotReturn.colorStopCruceLineaH2;

   tipoCruceLineaM1 = robotReturn.tipoCruceLineaM1;
   direcCruceLineaM1 = robotReturn.direcCruceLineaM1;
   colorCruceLineaM1 = robotReturn.colorCruceLineaM1;
   stopCruceLineaM1 = robotReturn.stopCruceLineaM1;
   colorStopCruceLineaM1 = robotReturn.colorStopCruceLineaM1;
   tipoCruceLineaM2 = robotReturn.tipoCruceLineaM2;
   direcCruceLineaM2 = robotReturn.direcCruceLineaM2;
   colorCruceLineaM2 = robotReturn.colorCruceLineaM2;
   stopCruceLineaM2 = robotReturn.stopCruceLineaM2;
   colorStopCruceLineaM2 = robotReturn.colorStopCruceLineaM2;


   modoOperativa = (intOpcionesModo)(robotReturn.ModoOperativa);  //era +1
   limiteSeparacion = robotReturn.MinSeparacionActiva;
   minSeparacionCaudillos = robotReturn.MinSeparacion;
   limiteSpread = robotReturn.MaxSpreadActivo;
   maxSpread = robotReturn.MaxSpread;
   lacertaCauda = robotReturn.LacertaActiva;
   lacertaCaudaFlotante = robotReturn.LacertaFlotante;
   AceleraBTPanel = robotReturn.AceleraBTPanel;

   CaesarActividad = (intOpcionesActividad)robotReturn.CaesarActividad;
   AlexanderActividad = (intOpcionesActividad)robotReturn.AlexActividad;
   HannibalActividad = (intOpcionesActividad)robotReturn.HannibalActividad;

   CaesarTipoOperacion = (intOpcionesTipoOperacion)robotReturn.CaesarTipoOp;
   AlexanderTipoOperacion = (intOpcionesTipoOperacion)robotReturn.AlexTipoOp;
   HannibalTipoOperacion = (intOpcionesTipoOperacion)robotReturn.HannibalTipoOp;

   CaesarAutoPriceAverage = robotReturn.CaesarAutoPriceAverage;
   AlexanderAutoPriceAverage = robotReturn.AlexAutoPriceAverage;
   HannibalAutoPriceAverage = robotReturn.HannibalAutoPriceAverage;

   CaesarUseTrailingStop = robotReturn.CaesarUseTrailing;
   AlexanderUseTrailingStop = robotReturn.AlexUseTrailing;
   HannibalUseTrailingStop = robotReturn.HannibalUseTrailing;

   CaesarSeleccionTF = (intOpcionesTF)robotReturn.CaesarTimeFrame;
   AlexanderSeleccionTF = (intOpcionesTF)robotReturn.AlexTimeFrame;
   HannibalSeleccionTF = (intOpcionesTF)robotReturn.HannibalTimeFrame;

   CaesarIntervaloTF = (intOpcionesInterTF)robotReturn.CaesarInterTimeFrame;
   AlexanderIntervaloTF = (intOpcionesInterTF)robotReturn.AlexInterTimeFrame;
   HannibalIntervaloTF = (intOpcionesInterTF)robotReturn.HannibalInterTimeFrame;

   CaesarTipoOperacion = (intOpcionesTipoOperacion)robotReturn.CaesarTipoOp;
   AlexanderTipoOperacion = (intOpcionesTipoOperacion)robotReturn.AlexTipoOp;
   HannibalTipoOperacion = (intOpcionesTipoOperacion)robotReturn.HannibalTipoOp;

   VolatilidadAdjJ = robotReturn.VolatilidadAdjJ;
   HolguraAdjJ = robotReturn.HolguraAdjJ;

   VolatilidadAdjA = robotReturn.VolatilidadAdjA;
   HolguraAdjA = robotReturn.HolguraAdjA;

   VolatilidadAdjH = robotReturn.VolatilidadAdjH;
   HolguraAdjH = robotReturn.HolguraAdjH;

   centuApertura = robotReturn.centuApertura;
   centuAperturaIni = centuApertura;
   centuCierre = robotReturn.centuCierre;
   centuCierreIni = centuCierre;
   
            if (robotReturn.TipoCuenta > 0)
            {
               if (robotReturn.TipoCuenta == 100)
                  tipoCuenta = Cent;
               else
                  tipoCuenta = Standard;                
            }
   

   if(robotReturn.TrailingGAActivo != 0)
     {
      TrailingGAActivo = true;
     }
   else
     {
      TrailingGAActivo = false;
     }
   TrailingGAStart = robotReturn.TrailingGAStart;
   TrailingGAStartIni = TrailingGAStart;
   TrailingGAStop = robotReturn.TrailingGAStop;
   TrailingGAStopIni = TrailingGAStop;
   TrailingGABeneDia = robotReturn.TrailingGABeneDia;
   TrailingGABeneDiaIni = TrailingGABeneDia;
   TrailingGATipoCierre = TrailingGATipoCierre;

   if(robotReturn.TrailingREActivo != 0)
     {
      TrailingREActivo = true;
     }
   else
     {
      TrailingREActivo = false;
     }
   TrailingREStart = robotReturn.TrailingREStart;
   TrailingREStartIni = TrailingREStart;
   TrailingREStop = robotReturn.TrailingREStop;
   TrailingREStopIni = TrailingREStop;
   TrailingREMinima = robotReturn.TrailingREMinima;
   TrailingREMinimaIni = TrailingREMinima;
   TrailingRETipoCierres = TrailingRETipoCierres;

   InteresCompuesto = robotReturn.InteresCompuesto;
   BalanceBase =  robotReturn.balanceBase;
   
   LotExact = robotReturn.lotExact;

   BuysErroneas = robotReturn.BuysErroneas;
   SellsErroneas = robotReturn.SellsErroneas;

   Enero = robotReturn.Enero;
   Febrero = robotReturn.Febrero;
   Marzo = robotReturn.Marzo;
   Abril = robotReturn.Abril;
   Mayo = robotReturn.Mayo;
   Junio = robotReturn.Junio;
   Julio = robotReturn.Julio;
   Agosto = robotReturn.Agosto;
   Septiembre = robotReturn.Septiembre;
   Octubre = robotReturn.Octubre;
   Noviembre = robotReturn.Noviembre;
   Diciembre = robotReturn.Diciembre;

   AlsoClose = robotReturn.AlsoClose;
   LimitClose = robotReturn.LimitClose;

   LunesJ = robotReturn.LunesJ;
   MartesJ = robotReturn.MartesJ;
   MiercolesJ = robotReturn.MiercolesJ;
   JuevesJ = robotReturn.JuevesJ;
   ViernesJ = robotReturn.ViernesJ;
   SabadoJ = robotReturn.SabadoJ;
   DomingoJ = robotReturn.DomingoJ;
   desdeDiaJ = robotReturn.desdeDiaJ;
   hastaDiaJ = robotReturn.hastaDiaJ;
   desdeHoraJ = robotReturn.desdeHoraJ;
   hastaHoraJ = robotReturn.hastaHoraJ;
   desdeMinutoJ = robotReturn.desdeMinutoJ;
   hastaMinutoJ = robotReturn.hastaMinutoJ;
   InvierteTiempoJ = robotReturn.InvierteTiempoJ;

   LunesA = robotReturn.LunesA;
   MartesA = robotReturn.MartesA;
   MiercolesA = robotReturn.MiercolesA;
   JuevesA = robotReturn.JuevesA;
   ViernesA = robotReturn.ViernesA;
   SabadoA = robotReturn.SabadoA;
   DomingoA = robotReturn.DomingoA;
   desdeDiaA = robotReturn.desdeDiaA;
   hastaDiaA = robotReturn.hastaDiaA;
   desdeHoraA = robotReturn.desdeHoraA;
   hastaHoraA = robotReturn.hastaHoraA;
   desdeMinutoA = robotReturn.desdeMinutoA;
   hastaMinutoA = robotReturn.hastaMinutoA;
   InvierteTiempoA = robotReturn.InvierteTiempoA;

   LunesH = robotReturn.LunesH;
   MartesH = robotReturn.MartesH;
   MiercolesH = robotReturn.MiercolesH;
   JuevesH = robotReturn.JuevesH;
   ViernesH = robotReturn.ViernesH;
   SabadoH = robotReturn.SabadoH;
   DomingoH = robotReturn.DomingoH;
   desdeDiaH = robotReturn.desdeDiaH;
   hastaDiaH = robotReturn.hastaDiaH;
   desdeHoraH = robotReturn.desdeHoraH;
   hastaHoraH = robotReturn.hastaHoraH;
   desdeMinutoH = robotReturn.desdeMinutoH;
   hastaMinutoH = robotReturn.hastaMinutoH;
   InvierteTiempoH = robotReturn.InvierteTiempoH;

   reEntradasCaesar = robotReturn.reEntradasCaesar;
   reEntradasAlexander = robotReturn.reEntradasAlexander;
   reEntradasHannibal = robotReturn.reEntradasHannibal;

   reStopCaesar = robotReturn.reStopCaesar;
   reStopAlexander = robotReturn.reStopAlexander;
   reStopHannibal = robotReturn.reStopHannibal;

   reDistanciaCaesar = robotReturn.reDistanciaCaesar;
   reDistanciaAlexander = robotReturn.reDistanciaAlexander;
   reDistanciaHannibal = robotReturn.reDistanciaHannibal;

   reLotajeCaesar = robotReturn.reLotajeCaesar;
   reLotajeAlexander = robotReturn.reLotajeAlexander;
   reLotajeHannibal = robotReturn.reLotajeHannibal;

   tipoFormacion = (intTipoFormacion)robotReturn.TipoFormacion;

   CaesarTipoAlerta = robotReturn.CaesarTipoAlerta;
   AlexTipoAlerta = robotReturn.AlexTipoAlerta;
   HannibalTipoAlerta = robotReturn.HannibalTipoAlerta;
   CaesarGatilloAlerta = robotReturn.CaesarGatilloAlerta;
   AlexGatilloAlerta = robotReturn.AlexGatilloAlerta;
   HannibalGatilloAlerta = robotReturn.HannibalGatilloAlerta;
   CaesarFrecuenciaAlerta = robotReturn.CaesarFrecuenciaAlerta;
   AlexFrecuenciaAlerta = robotReturn.AlexFrecuenciaAlerta;
   HannibalFrecuenciaAlerta = robotReturn.HannibalFrecuenciaAlerta;
   CaesarValorAlerta = robotReturn.CaesarValorAlerta;
   AlexValorAlerta = robotReturn.AlexValorAlerta;
   HannibalValorAlerta = robotReturn.HannibalValorAlerta;
   MargenValorAlerta = robotReturn.MargenValorAlerta;
   LacertaValorAlerta = robotReturn.LacertaValorAlerta;
   MargenGatilloAlerta = robotReturn.MargenGatilloAlerta;
   MargenFrecuenciaAlerta = robotReturn.MargenFrecuenciaAlerta;
   LacertaGatilloAlerta = robotReturn.LacertaGatilloAlerta;
   LacertaFrecuenciaAlerta = robotReturn.LacertaFrecuenciaAlerta;
   MensajeGatilloAlerta = robotReturn.MensajeGatilloAlerta;
   MensajeFrecuenciaAlerta = robotReturn.MensajeFrecuenciaAlerta;

   AlertaMovilActiva = robotReturn.AlertaMovilActiva;
   AlertaMailActiva = robotReturn.AlertaMailActiva;
   AlertaSoundActiva = robotReturn.AlertaSoundActiva;


   multiplicadorCierreParcialLocal = NormalizeDouble(robotReturn.multiplicadorCierreParcial, 2);

   if((hayAccion && refrescar))
     {
      hayAccion=false;
     }

  }




//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void mostrarPanelStandard()
  {


   if(nombreCapitan!=ShortArrayToString(robotReturn.tituloGrafica) || lemaCapitan!=ShortArrayToString(robotReturn.lemaGrafica))
     {
      nombreCapitan=ShortArrayToString(robotReturn.tituloGrafica);
      lemaCapitan=ShortArrayToString(robotReturn.lemaGrafica);
      
      if(ObjectFind ("capitanTitLabel")>=0)
        {
         ObjectDelete("capitanTitLabel");
        }        
      if(ObjectFind("capitanLabel")>=0)
        {
         ObjectDelete("capitanLabel");
        }        
      if(ObjectFind("lemaTitLabel")>=0)
        {
         ObjectDelete("lemaTitLabel");
        }        
      if(ObjectFind("lema")>=0)
        {
         ObjectDelete("lema");
        }        
      if(ObjectFind("webTitLabel")>=0)
        {
         ObjectDelete("webTitLabel");
        }        
      if(ObjectFind("Lable")>=0)
        {
         ObjectDelete("Lable");
        }        
      




      if(ChartBackColorGet() == 12630450) // LIGHT
        {

         TextSetFont("Arial", 14, 0, 0);
         botvestingUrl = "https://botvesting.com";
         uint anchoText, altoText, anchoTextSum = 0;

         pintarEtiquetas("capitanTitLabel", 2, 100, 2, "Titulo:   ", 8, "Arial", clrBlue);
         TextGetSize("Titulo:   ", anchoText, altoText);
         anchoTextSum += 80 + ((20 + anchoText) * scale_factor) / 100;



         pintarEtiquetas("capitanLabel", 2, anchoTextSum, 2, nombreCapitan + "   ", 8, "Arial", clrDarkGreen);
         TextGetSize(nombreCapitan + "   ", anchoText, altoText);
         anchoTextSum += ((20 + anchoText) * scale_factor) / 100;

         pintarEtiquetas("lemaTitLabel", 2, anchoTextSum, 2, "Lema:   ", 8, "Arial", clrBlue);
         TextGetSize("Lema:   ", anchoText, altoText);
         anchoTextSum += ((anchoText) * scale_factor) / 100;


         pintarEtiquetas("lema", 2, anchoTextSum, 2, lemaCapitan + "   ", 8, "Arial", clrDarkGreen);
         TextGetSize(lemaCapitan + "   ", anchoText, altoText);
         anchoTextSum += ((20 + anchoText) * scale_factor) / 100;

         pintarEtiquetas("webTitLabel", 2, anchoTextSum, 2, "Web:   ", 8, "Arial", clrBlue);
         TextGetSize("Web:   ", anchoText, altoText);
         anchoTextSum += ((anchoText) * scale_factor) / 100;

         pintarEtiquetas("Lable", 2, anchoTextSum, 2, botvestingUrl, 8, "Arial", clrBlack);
        }
      else
        {
         TextSetFont("Arial", 14, 0, 0);
         botvestingUrl = "https://botvesting.com";
         anchoTextSum = 0;

         pintarEtiquetas("capitanTitLabel", 2, 100, 2, "Título:   ", 8, "Arial", clrYellow);
         TextGetSize("Título:   ", anchoText, altoText);
         anchoTextSum += 80 + ((20 + anchoText) * scale_factor) / 100;

         pintarEtiquetas("capitanLabel", 2, anchoTextSum, 2, nombreCapitan + "   ", 8, "Arial", clrGoldenrod);
         TextGetSize(nombreCapitan + "   ", anchoText, altoText);
         anchoTextSum += ((20 + anchoText) * scale_factor) / 100;

         pintarEtiquetas("lemaTitLabel", 2, anchoTextSum, 2, "Lema:   ", 8, "Arial", clrYellow);
         TextGetSize("Lema:   ", anchoText, altoText);
         anchoTextSum += ((anchoText) * scale_factor) / 100;

         pintarEtiquetas("lema", 2, anchoTextSum, 2, lemaCapitan + "   ", 8, "Arial", clrGoldenrod);
         TextGetSize(lemaCapitan + "   ", anchoText, altoText);
         anchoTextSum += ((20 + anchoText) * scale_factor) / 100;

         pintarEtiquetas("webTitLabel", 2, anchoTextSum, 2, "Web:   ", 8, "Arial", clrYellow);
         TextGetSize("Web:   ", anchoText, altoText);
         anchoTextSum += ((anchoText) * scale_factor) / 100;

         pintarEtiquetas("Lable", 2, anchoTextSum, 2, botvestingUrl, 8, "Arial", DeepSkyBlue);
        }


     }

  }



//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ocultarPanel()
  {

   ObjectDelete("fondo");
   ObjectDelete("separadorComandanteJefe");
   ObjectDelete("cabeceroCapitan");
   ObjectDelete("separadorCaudillos");
   ObjectDelete("cabeceroCaesar");
   ObjectDelete("cabeceroAlexander");
   ObjectDelete("cabeceroHannibal");
   ObjectDelete("tituloInfoPar");
   ObjectDelete("cabeceroTotalPar");
   ObjectDelete("tituloInfoCuenta");
   ObjectDelete("CerrarTodasCapitan");
   ObjectDelete("CerrarTodasCaesar");
   ObjectDelete("CerrarTodasAlexander");
   ObjectDelete("CerrarTodasHannibal");
   ObjectDelete("Lable");
   ObjectDelete("lema");
   ObjectDelete("lemaTitLabel");
   ObjectDelete("capitanTitLabel");
   ObjectDelete("capitanLabel");
   ObjectDelete("webTitLabel");
   ObjectDelete("ducibusLabel");

   ObjectDelete("valorTick");
   ObjectDelete("spread");
   ObjectDelete("nombreCapitan");
   ObjectDelete("flotanteCapitan");
   ObjectDelete("benTPCapitan");
   ObjectDelete("benSLCapitan");
   ObjectDelete("swapCapitan");
   ObjectDelete("comisionCapitan");
   ObjectDelete("nombreCaesar");
   ObjectDelete("flotanteCaesar");
   ObjectDelete("benTPCaesar");
   ObjectDelete("benSLCaesar");
   ObjectDelete("swapCaesar");
   ObjectDelete("comisionCaesar");
   ObjectDelete("nombreAlexander");
   ObjectDelete("flotanteAlexander");
   ObjectDelete("benTPAlexander");
   ObjectDelete("benSLAlexander");
   ObjectDelete("swapAlexander");
   ObjectDelete("comisionAlexander");
   ObjectDelete("nombreHannibal");
   ObjectDelete("flotanteHannibal");
   ObjectDelete("benTPHannibal");
   ObjectDelete("benSLHannibal");
   ObjectDelete("swapHannibal");
   ObjectDelete("comisionHannibal");
   ObjectDelete("fregaosPar");
   ObjectDelete("flotantePar");
   ObjectDelete("benParTP");
   ObjectDelete("benParSL");
   ObjectDelete("swapPar");
   ObjectDelete("comisionPar");
   ObjectDelete("cabeceroTotalCuenta");
   ObjectDelete("profitActualTotal");
   ObjectDelete("salud");


   ObjectDelete("CerrarUltimaCaesar");
   ObjectDelete("CerrarUltimaAlexander");
   ObjectDelete("CerrarUltimaHannibal");


  }


/***************************************** Modo Clásico Hard y Light    **************************************************/

// ------------------------------------- Julius Caesar -----------------------------------------------------------
void operativaCaesarClasicaHardLight()
  {
   if(otraMartinCaesar == 1 && numeroOperacionesCaesar < MaxTrades_Caesar) //No es la primera operación de la martingala(no es la 0), y no supera el número máximo de operaciones permitidas.
     {
      hacerCoberturaCaesar();
     }

//Se ejecuta el código sólo cuando hay un cambio de vela
   if(CaesarCambioMinuto != tiempoMinuto || lastCaesarTakeProfit != (int)CaesarTakeProfit || otraMartinCaesar == 1 || CaesarOrdenManual>0)
     {
      lastCaesarTakeProfit = (int)CaesarTakeProfit;

      numeroOperacionesCaesar = countTradesCaesarVar;
      if(CaesarActivo == True || otraMartinCaesar == 1 || CaesarOrdenManual>0)//yy   // Controla la activación/desactivación del caudillo Caesar
        {

         if(CaesarActividad != Terminando || CaesarOrdenManual>0)
           {
            //Si no hay operaciones abiertas, se comprueba si se puede abrir una nueva operación, cada hora
            if(numeroOperacionesCaesar == 0)
              {
               gestionarNuevaOperacionCaesar();
              }
           }


         if(numeroOperacionesCaesar > 0)
           {

            if(numeroOperacionesCaesar < MaxTrades_Caesar)   //No es la primera operación de la martingala(no es la 0), y no supera el número máximo de operaciones permitidas.
              {
               hacerCoberturaCaesar();
              }

            //actualiza el precio promedio de toda la martingla
            //actualizarPrecioPromedioCaesar();
            //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
            //actualizarTPCaesar();
           }// fin if (numeroOperacionesCaesar > 0

        }// Fin de CaesarActivo

      if(CaesarAutoPriceAverage)
        {
         //actualiza el precio promedio de toda la martingla
         actualizarPrecioPromedioCaesar();
         //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
         actualizarTPCaesar();
        }
      CaesarCambioMinuto = tiempoMinuto;
     } //end if(CaesarCambioMinuto

   if(CaesarUseTrailingStop)
      TrailingAlls_Caesar();


  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void gestionarNuevaOperacionCaesar()
  {
   if(HayLimiteHorario(1))
      return;

   if(CaesarOrdenEnEstaVela)
      return;

   double precioMaximoVelaAnterior;
   double precioMinimoDosVelasAnteriores;

   precioMaximoVelaAnterior = iHigh(Symbol(), CaesarSeleccionTF, 1); // Precio Máximo alcanzado en la vela anterior
   precioMinimoDosVelasAnteriores = iLow(Symbol(), CaesarSeleccionTF, 2); // Precio Mínimo alcanzado en 2 velas antes de la actual

   bool permisoAbrirCaesar = true;
   bool permisoAbrirCaesarMaxIguales = false;
//lotajeActualizadoCaesar = NormalizeDouble(CaesarLots * MathPow(CaesarLotExponent, numeroOperacionesCaesar), lotdecimal);
   if(precioMaximoVelaAnterior > precioMinimoDosVelasAnteriores)    //Si el précio ha aumentado, se realiza una operación SELL, si se cumple la condición del RSI
     {
      if(CaesarOrdenManual==1 || (iRSI(NULL, CaesarSeleccionTF, PeriodRsiCaesar, PRICE_CLOSE, 1) > LevelRsiLowCaesar && ((tendencia < -atrParcialValueJ && atrParcialValueJ >= 0) || (tendencia < -atrParcialValueJ && tendencia > atrParcialValueJ && atrParcialValueJ <= 0))))   // SELL   //Si el précio ha aumentado, se realiza una operación SELL, en caso de que se cumpla que el RSI esté por encima de 30
        {
         if(modoOperativa == Modo_Gladiador)
           {
            permisoAbrirCaesarMaxIguales = true;
           }
         //Print("permisoAbrirCaesarMaxIguales: " + permisoAbrirCaesarMaxIguales);
         if(permisoAbrirCaesarMaxIguales == true)
           {
            /////////////////////  SELL ////////////////////////
            permisoAbrirCaesar = permisoAbrirSymbolCaudillo(1);
            //Print("permisoAbrirCaesar: " + permisoAbrirCaesar);
            if(permisoAbrirCaesar)
              {
               //                    resultadoAbirOrdenCaesar = OpenPendingOrder_Caesar(1, CaesarLots, slipPage, Bid, 0, 0, comentarioCaesar + "-" + 0, MagicNumber_Caesar, 0, HotPink);
               resultadoAbirOrdenCaesar = OpenPendingOrder_Caesar(1, lotajePrimeraCaesar, slipPage, Bid, 0, 0, comentarioCaesar + "-" + 0, MagicNumber_Caesar, 0, HotPink);
               if(resultadoAbirOrdenCaesar <= 0)
                 {
                  int LE = GetLastError();
                  if(LE != 0 && LE != 4051)
                     Print("Error: ", LE);
                 }
               else
                 {
                  numSeries++; // TEST
                  lotajeSumTotal += lotajePrimeraCaesar;
                  //guarda precio primera orden Caesar
                  if(reEntradasCaesar && countTradesCaesarVar == 0)
                    {
                     int ti = OrderSelect(resultadoAbirOrdenCaesar, SELECT_BY_TICKET, MODE_TRADES);
                     if(ti)
                       {
                        precioPrimeraCaesar = OrderOpenPrice();
                       }
                     else
                       {
                        precioPrimeraCaesar = Bid;
                       }
                    }
                 }
               //actualiza el precio promedio de toda la martingla
               actualizarPrecioPromedioCaesar();
               //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
               actualizarTPCaesar();
               //}

              }//fin if(permisoAbrirCaesar
           }//fin if(permisoAbrirCaesarMax2Iguales
        }//fin cálculo RSI
     }
   else     //Si el précio ha aumentado, se realiza una operación BUY, si se cumple la condición del RSI
     {
      if(CaesarOrdenManual==2 || (iRSI(NULL, CaesarSeleccionTF, PeriodRsiCaesar, PRICE_CLOSE, 1) < LevelRsiHighCaesar && ((tendencia > atrParcialValueJ && atrParcialValueJ >= 0) || (tendencia > atrParcialValueJ && tendencia < -atrParcialValueJ && atrParcialValueJ <= 0)))) // BUY  //Si el précio ha disminuido, se realiza una Compra, en caso de que se cumpla que el RSI esté por debajo de 70
        {

         if(modoOperativa == Modo_Gladiador)
           {
            permisoAbrirCaesarMaxIguales = true;
           }

         //Print("permisoAbrirCaesarMax2Iguales: " + permisoAbrirCaesarMax2Iguales);
         if(permisoAbrirCaesarMaxIguales == true)
           {
            permisoAbrirCaesar = permisoAbrirSymbolCaudillo(0);
            //Print("permisoAbrirCaesar: " + permisoAbrirCaesar);
            ////////////////////////////// COMPRA ////////////////////////
            if(permisoAbrirCaesar)
              {
               //                    resultadoAbirOrdenCaesar = OpenPendingOrder_Caesar(0, CaesarLots, slipPage, Ask, 0, 0, comentarioCaesar + "-" + 0, MagicNumber_Caesar, 0, Lime);
               resultadoAbirOrdenCaesar = OpenPendingOrder_Caesar(0, lotajePrimeraCaesar, slipPage, Ask, 0, 0, comentarioCaesar + "-" + 0, MagicNumber_Caesar, 0, Lime);
               if(resultadoAbirOrdenCaesar <= 0)
                 {
                  LE = GetLastError();
                  if(LE != 0 && LE != 4051)
                     Print("Error: ", LE);

                 }
               else
                 {
                  numSeries++; // TEST
                  lotajeSumTotal += lotajePrimeraCaesar;

                  //guarda precio primera orden Caesar
                  if(reEntradasCaesar && countTradesCaesarVar == 0)
                    {
                     ti = OrderSelect(resultadoAbirOrdenCaesar, SELECT_BY_TICKET, MODE_TRADES);
                     if(ti)
                       {
                        precioPrimeraCaesar = OrderOpenPrice();
                       }
                     else
                       {
                        precioPrimeraCaesar = Ask;
                       }
                    }
                 }
               //actualiza el precio promedio de toda la martingla
               actualizarPrecioPromedioCaesar();
               //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
               actualizarTPCaesar();
               //}

              }//fin if(permisoAbrirCaesar
           }//fin if(permisoAbrirCaesarMaxIguales
        }//fin cálculo RSI
     } //cálculo para abrir Buy o Sell



  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void hacerCoberturaCaesar()
  {

   if(CaesarOrdenEnEstaVela && otraMartinCaesar == 0)
      return;


   double atrPS = 0;
   if(CaesarModoPipStep == PS_Incrementa && countTradesCaesarVar > 0)
     {
      double multiPipStep = (CaesarPipStep / 2) * (countTradesCaesarVar - 1);
      atrPS = 0;
     }
   else
     {
      multiPipStep = 0;
      atrPS = 0;
     }
   if(CaesarModoPipStep == PS_Fijo)
     {
      multiPipStep = 0;
      atrPS = 0;
     }
   if(CaesarModoPipStep == PS_Variable)
     {
      multiPipStep = 0;
      atrPS = atrValueJ;
     }


   caesarUltimoPrecioOperacionBuy = FindLastBuyPrice_Caesar();
   caesarUltimoPrecioOperacionSell = FindLastSellPrice_Caesar();
   int tipoOperacionCaesar = tipoOperacionCaudillo(MagicNumber_Caesar);

//Buy
   if(tipoOperacionCaesar == 0)// && iClose(NULL, 60, 1) < iHigh(NULL, 5, 0) )// && (tendencia > parte))
     {
      //Se va a hacer una cobertura, cuando iguale o supere el precio más el pipstep establecido en CaesarPipStep
      if((((caesarUltimoPrecioOperacionBuy - Ask) >= (multiPipStep + (CaesarPipStep + atrPS)) * Point)) || ((caesarUltimoPrecioOperacionBuy - Ask) > 0 && otraMartinCaesar == 1))
        {
         RefreshRates();
         //            lotajeActualizadoCaesar = NormalizeDouble(CaesarLots * MathPow(CaesarLotExponent, numeroOperacionesCaesar), lotdecimal);
         lotajeActualizadoCaesar = calculaLotaje(lotajePrimeraCaesar,CaesarLotExponent,numeroOperacionesCaesar);
         resultadoAbirOrdenCaesar = OpenPendingOrder_Caesar(0, lotajeActualizadoCaesar, slipPage, Bid, 0, 0, comentarioCaesar + "-" + numeroOperacionesCaesar, MagicNumber_Caesar, 0, Lime);
         if(resultadoAbirOrdenCaesar <= 0)
           {
            int LE = GetLastError();
            if(LE != 0 && LE != 4051)
               Print("Error: ", LE);

           }
         else
           {
            lotajeSumTotal += lotajeActualizadoCaesar;

            //actualiza el precio promedio de toda la martingla
            actualizarPrecioPromedioCaesar();
            //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
            actualizarTPCaesar();
            otraMartinCaesar = 0;
           }

        }//fin if

      //Sell
     }
   else
      if(tipoOperacionCaesar == 1)// && iClose(NULL, 60, 1) > iLow(NULL, 5, 0) )// && (tendencia < -parte))
        {
         //Se va a hacer una cobertura, cuando iguale o supere el precio más el pipstep establecido en CaesarPipStep
         if((((Bid - caesarUltimoPrecioOperacionSell) >= (multiPipStep + (CaesarPipStep + atrPS)) * Point)) || ((Bid - caesarUltimoPrecioOperacionSell) > 0 && otraMartinCaesar == 1))
           {
            RefreshRates();
            lotajeActualizadoCaesar = calculaLotaje(lotajePrimeraCaesar,CaesarLotExponent,numeroOperacionesCaesar);
            RefreshRates();
            resultadoAbirOrdenCaesar = OpenPendingOrder_Caesar(1, lotajeActualizadoCaesar, slipPage, Ask, 0, 0, comentarioCaesar + "-" + numeroOperacionesCaesar, MagicNumber_Caesar, 0, HotPink);
            if(resultadoAbirOrdenCaesar <= 0)
              {
               LE = GetLastError();
               if(LE != 0 && LE != 4051)
                  Print("Error: ", LE);

              }
            else
              {
               lotajeSumTotal += lotajeActualizadoCaesar;
               //actualiza el precio promedio de toda la martingla
               actualizarPrecioPromedioCaesar();
               //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
               actualizarTPCaesar();
               otraMartinCaesar = 0;
              }

           }//fin if

        }// fin if(tipoOperacionCaesar

  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void actualizarPrecioPromedioCaesar()
  {

//   if(countTradesCaesarVar==0)return;


   promedioPrecioCaesar = 0.00;
   double lotajeMartingalaTotal = 0.00;

   int ot=OrdersTotal() - 1;

   for(posicionOrden = ot; posicionOrden >= 0; posicionOrden--)    //recorremos todas las ordenes, descendentemente
     {
      if(OrderSelect(posicionOrden, SELECT_BY_POS, MODE_TRADES))
        {
         if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Caesar)
           {
            if(OrderType() == OP_BUY || OrderType() == OP_SELL)
              {
               promedioPrecioCaesar += OrderOpenPrice() * OrderLots(); //Se suma, para todas la operaciones de la martingala, el precio de apertura de cada operación multiplicado por el Lotaje de la misma.
               lotajeMartingalaTotal += OrderLots(); //sumamos todo el lotaje de todas las órdenes de la martingala
              }
           }
        }
     }

   if(promedioPrecioCaesar != 0.00)
     {
      promedioPrecioCaesar = NormalizeDouble((promedioPrecioCaesar / lotajeMartingalaTotal), Digits); //promedio del precio para calcular el nuevo TakeProfit de la martingala
     }

  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void actualizarTPCaesar()
  {

   static double decreTPCaesarBack=0.00;

   actualizadoTPCaesar=true;
   double takeProfitActualCaesar = 0.0;


   if((CaesarModoTp == TP_Decrementa) && (countTradesCaesarVar > 0))
     {
      double decreTP = CaesarTakeProfit / ((countTradesCaesarVar+0.333)-(countTradesCaesarVar/3.0));

     }
   else
     {
      decreTP = CaesarTakeProfit;
     }

   if(reEntradasCaesar && CaesarActivo && !puedeCoberturearCaesar)
     {
      decreTP = CaesarTakeProfit*2;//(CaesarTakeProfit)+((((GranH-GranL))/Point))*(reDistanciaCaesar*1.0);//*Point;  //       ((atrTP / Point) * 25.0);
     }


////------------------------------------------------------------------------------------------------

//Se Calcula y actualiza el TP de cada operación de la martingala

   int ot=OrdersTotal() - 1;

   for(posicionOrden = ot; posicionOrden >= 0; posicionOrden--)
     {
      if(OrderSelect(posicionOrden, SELECT_BY_POS, MODE_TRADES))
        {

         if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Caesar)
           {
            takeProfitActualCaesar = OrderTakeProfit();
            if(OrderType() == OP_BUY)
              {
               caesarTakeProfitPromediado = promedioPrecioCaesar + decreTP * Point; //al promedio del precio necesario para cubir los beneficios de la martingala, le sumanos el TakeProfit establecido por parámetros
               caesarTakeProfitPromediado = NormalizeDouble(caesarTakeProfitPromediado, Digits);
               double modifyTP = NormalizeTPBuy(caesarTakeProfitPromediado, caesarTakeProfitPromediado);
               //                if (reEntradasCaesar && precioOrdenMasBajaCaesar>Ask)// && modifyTP<Ask)
               if(reEntradasCaesar && !puedeCoberturearCaesar && countTradesCaesarRe>0 && precioOrdenMasBajaCaesar>Ask) // && modifyTP<Ask)
                 {
                  modifyTP=0;
                  //continue;
                 }
              }
            if(OrderType() == OP_SELL)
              {
               caesarTakeProfitPromediado = promedioPrecioCaesar - decreTP * Point;
               caesarTakeProfitPromediado = NormalizeDouble(caesarTakeProfitPromediado, Digits);
               modifyTP = NormalizeTPSell(caesarTakeProfitPromediado, caesarTakeProfitPromediado);
               //                if (reEntradasCaesar && precioOrdenMasAltaCaesar<Bid)// && modifyTP>Bid)
               if(reEntradasCaesar && !puedeCoberturearCaesar && countTradesCaesarRe>0 && precioOrdenMasAltaCaesar<Bid) // && modifyTP>Bid)
                 {
                  modifyTP=0;
                  //continue;
                 }
              }


            if(modifyTP<0.00)
              {
               Print("Posiblemente esté usando un TP demasiado alto, procedo a eliminarlo (TP: "+modifyTP+")");
               modifyTP=0.00;
              }
            if(takeProfitActualCaesar != modifyTP)
              {
               int countLimit = 0;
               while(!OrderModify(OrderTicket(), promedioPrecioCaesar, OrderStopLoss(), modifyTP, 0, Yellow) && countLimit < 5  && !IsStopped())
                 {
                  int LE = GetLastError();
                  if(LE != 4/* SERVER_BUSY */ && LE != 136/* OFF_QUOTES */)
                     break;
                  Sleep(500);
                  RefreshRates();
                  countLimit += 1;
                 }

              }//end if(takeProfitActualCaesar

           }// edn if
        }// orderselect
     }//fin for

  }
//-----------------------------------------------------------------------------------------------------------


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CaesarBEPos()
  {
   double modifyTP = -1.00;
   double decreTP = MathMax(spreadActual,20);
   actualizarPrecioPromedioCaesar();

   int ot=OrdersTotal() - 1;

   for(posicionOrden = ot; posicionOrden >= 0; posicionOrden--)
     {
      cg = OrderSelect(posicionOrden, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Caesar && cg)
        {
         if(OrderType() == OP_BUY)
           {
            caesarTakeProfitPromediado = promedioPrecioCaesar + decreTP * Point;
            caesarTakeProfitPromediado = NormalizeDouble(caesarTakeProfitPromediado, Digits);
            modifyTP = NormalizeTPBuy(caesarTakeProfitPromediado, caesarTakeProfitPromediado);
            break;
           }
         if(OrderType() == OP_SELL)
           {
            caesarTakeProfitPromediado = promedioPrecioCaesar - decreTP * Point;
            caesarTakeProfitPromediado = NormalizeDouble(caesarTakeProfitPromediado, Digits);
            modifyTP = NormalizeTPSell(caesarTakeProfitPromediado, caesarTakeProfitPromediado);
            break;
           }
        }
     }
   return modifyTP;
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double AlexanderBEPos()
  {
   double modifyTP = -1.00;
   double decreTP = MathMax(spreadActual,20);
   actualizarPrecioPromedioAlexander();

   int ot=OrdersTotal() - 1;
   for(posicionOrden = ot; posicionOrden >= 0; posicionOrden--)
     {
      cg = OrderSelect(posicionOrden, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Alexander && cg)
        {
         if(OrderType() == OP_BUY)
           {
            alexanderTakeProfitPromediado = promedioPrecioAlexander + decreTP * Point;
            alexanderTakeProfitPromediado = NormalizeDouble(alexanderTakeProfitPromediado, Digits);
            modifyTP = NormalizeTPBuy(alexanderTakeProfitPromediado, alexanderTakeProfitPromediado);
            break;
           }
         if(OrderType() == OP_SELL)
           {
            alexanderTakeProfitPromediado = promedioPrecioAlexander - decreTP * Point;
            alexanderTakeProfitPromediado = NormalizeDouble(alexanderTakeProfitPromediado, Digits);
            modifyTP = NormalizeTPSell(alexanderTakeProfitPromediado, alexanderTakeProfitPromediado);
            break;
           }
        }
     }
   return modifyTP;
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double HannibalBEPos()
  {
   double modifyTP = -1.00;
   double decreTP = MathMax(spreadActual,20);
   actualizarPrecioPromedioHannibal();

   int ot=OrdersTotal() - 1;

   for(posicionOrden = ot; posicionOrden >= 0; posicionOrden--)
     {
      cg = OrderSelect(posicionOrden, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Hannibal && cg)
        {
         if(OrderType() == OP_BUY)
           {
            hannibalTakeProfitPromediado = promedioPrecioHannibal + decreTP * Point;
            hannibalTakeProfitPromediado = NormalizeDouble(hannibalTakeProfitPromediado, Digits);
            modifyTP = NormalizeTPBuy(hannibalTakeProfitPromediado, hannibalTakeProfitPromediado);
            break;
           }
         if(OrderType() == OP_SELL)
           {
            hannibalTakeProfitPromediado = promedioPrecioHannibal - decreTP * Point;
            hannibalTakeProfitPromediado = NormalizeDouble(hannibalTakeProfitPromediado, Digits);
            modifyTP = NormalizeTPSell(hannibalTakeProfitPromediado, hannibalTakeProfitPromediado);
            break;
           }
        }
     }
   return modifyTP;
  }

// ------------------------------------- Alexander Magnus -----------------------------------------------------------
void operativaAlexanderClasicaHardLight()
  {
   if(numeroOperacionesAlexander < MaxTrades_Alexander && otraMartinAlexander == 1) //No es la primera operación de la martingala(no es la 0), y no supera el número máximo de operaciones permitidas.
     {
      hacerCoberturaAlexander();
     }

//Se ejecuta el código sólo cuando hay un cambio de vela
   if(AlexanderCambioMinuto != tiempoMinuto || lastAlexanderTakeProfit != (int)AlexanderTakeProfit || otraMartinAlexander == 1 || AlexanderOrdenManual>0)
     {
      lastAlexanderTakeProfit = (int)AlexanderTakeProfit;
      numeroOperacionesAlexander = countTradesAlexanderVar;
      if(AlexanderActivo == True || AlexanderOrdenManual>0 || otraMartinAlexander == 1)   // Controla la activación/desactivación del caudillo Alexander
        {

         if(AlexanderActividad != Terminando || AlexanderOrdenManual>0)
           {
            //                numeroOperacionesAlexander = countTradesAlexanderVar;
            if(numeroOperacionesAlexander == 0)
              {
               //Si no hay operaciones abiertas, se comprueba si se puede abrir una nueva operación, cada hora
               gestionarNuevaOperacionAlexander();
              }
           }

         if(numeroOperacionesAlexander > 0)
           {
            if(numeroOperacionesAlexander < MaxTrades_Alexander)   //No es la primera operación de la martingala(no es la 0), y no supera el número máximo de operaciones permitidas.
              {
               hacerCoberturaAlexander();
              }

            //actualiza el precio promedio de toda la martingla
            //actualizarPrecioPromedioAlexander();
            //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
            //actualizarTPAlexander();
           }// fin if (numeroOperacionesAlexander > 0

        }// Fin de AlexanderActivo

      if(AlexanderAutoPriceAverage)
        {
         //actualiza el precio promedio de toda la martingla
         actualizarPrecioPromedioAlexander();
         //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
         actualizarTPAlexander();
        }

      AlexanderCambioMinuto = tiempoMinuto;
     } //end if(AlexanderCambioMinuto

   if(AlexanderUseTrailingStop)
      TrailingAlls_Alexander();


  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void gestionarNuevaOperacionAlexander()
  {

   if(HayLimiteHorario(2))
      return;
   if(AlexanderOrdenEnEstaVela)
      return;

   double precioCierre2VelasAtras;
   double precioCierreVelaAnterior;

//Se comprueba si se puede abrir una nueva operación, cada hora
   if(alexanderDateTime != iTime(NULL, AlexanderSeleccionTF, 0))//esto era H1
     {

      precioCierre2VelasAtras = iClose(Symbol(), AlexanderSeleccionTF, 2); //Obtiene el precio de cierre de 2 velas atrás
      precioCierreVelaAnterior = iClose(Symbol(), AlexanderSeleccionTF, 1); //Obtiene el precio de cierre de 1 vela atrás

      bool permisoAbrirAlexander = true;
      bool permisoAbrirAlexanderMaxIguales = false;

      if(precioCierre2VelasAtras > precioCierreVelaAnterior && ((tendencia < -atrParcialValueA && atrParcialValueA >= 0) || (tendencia < -atrParcialValueA && tendencia > atrParcialValueA && atrParcialValueA <= 0))) //Si el precio de cierre de la ultima vela es menor que el de la anterior(ha disminuido el precio), abrirá una operación Sell
        {

         if(modoOperativa == Modo_Gladiador)
           {
            permisoAbrirAlexanderMaxIguales = true;
           }

         if(permisoAbrirAlexanderMaxIguales == true)
           {
            /////////////////////  SELL ////////////////////////
            permisoAbrirAlexander = permisoAbrirSymbolCaudillo(1);
            //Print("permisoAbrirAlexander: " + permisoAbrirAlexander);
            if(permisoAbrirAlexander)
              {

               resultadoAbirOrdenAlexander = OpenPendingOrder_Alexander(1, lotajePrimeraAlexander, slipPage, Bid, 0, 0, comentarioAlexander + "-" + 0, MagicNumber_Alexander, 0, HotPink);
               if(resultadoAbirOrdenAlexander <= 0)
                 {
                  int LE = GetLastError();
                  if(LE != 0 && LE != 4051)
                     Print("Error: ", LE);

                 }
               else
                 {
                  numSeries++; // TEST
                  lotajeSumTotal += lotajePrimeraAlexander;
                  //guarda precio primera orden Alexander
                  if(reEntradasAlexander && countTradesAlexanderVar == 0)
                    {
                     int ti = OrderSelect(resultadoAbirOrdenAlexander, SELECT_BY_TICKET, MODE_TRADES);
                     if(ti)
                       {
                        precioPrimeraAlexander = OrderOpenPrice();
                       }
                     else
                       {
                        precioPrimeraAlexander = Bid;
                       }
                    }
                  //actualiza el precio promedio de toda la martingla
                  actualizarPrecioPromedioAlexander();
                  //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
                  actualizarTPAlexander();
                 }

              }//fin if(permisoAbrirAlexander
           }//fin if(permisoAbrirAlexanderMaxIguales

        }
      if(precioCierre2VelasAtras <= precioCierreVelaAnterior && ((tendencia > atrParcialValueA && atrParcialValueA >= 0) || (tendencia > atrParcialValueA && tendencia < -atrParcialValueA && atrParcialValueA <= 0))) //Si el precio de cierre de la ultima vela es menor que el de la anterior(ha disminuido el precio), abrirá una operación Sell
         //else     //Si el precio de cierre de la ultima vela es mayor que el de la anterior(ha aumentado el precio), abrirá una operación BUY
        {

         if(modoOperativa == Modo_Gladiador)
           {
            permisoAbrirAlexanderMaxIguales = true;
           }

         if(permisoAbrirAlexanderMaxIguales == true)
           {
            permisoAbrirAlexander = permisoAbrirSymbolCaudillo(0);
            //Print("permisoAbrirAlexander: " + permisoAbrirAlexander);
            ////////////////////////////// COMPRA ////////////////////////
            if(permisoAbrirAlexander)
              {
               resultadoAbirOrdenAlexander = OpenPendingOrder_Alexander(0, lotajePrimeraAlexander, slipPage, Ask, 0, 0, comentarioAlexander + "-" + 0, MagicNumber_Alexander, 0, Lime);
               if(resultadoAbirOrdenAlexander <= 0)
                 {
                  LE = GetLastError();
                  if(LE != 0 && LE != 4051)
                     Print("Error: ", LE);

                 }
               else
                 {
                  numSeries++; // TEST
                  lotajeSumTotal += lotajePrimeraAlexander;
                  //guarda precio primera orden Alexander
                  if(reEntradasAlexander && countTradesAlexanderVar == 0)
                    {
                     ti = OrderSelect(resultadoAbirOrdenAlexander, SELECT_BY_TICKET, MODE_TRADES);
                     if(ti)
                       {
                        precioPrimeraAlexander = OrderOpenPrice();
                       }
                     else
                       {
                        precioPrimeraAlexander = Ask;
                       }
                    }
                  //actualiza el precio promedio de toda la martingla
                  actualizarPrecioPromedioAlexander();
                  //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
                  actualizarTPAlexander();
                 }

              }//fin if(permisoAbrirAlexander
           }//fin if(permisoAbrirAlexanderMaxIguales

        } //cálculo para abrir Buy o Sell

      alexanderDateTime = iTime(NULL, PERIOD_H1, 0); // TF H1

     } //fin if (alexanderDateTime

  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void hacerCoberturaAlexander()
  {

   if(AlexanderOrdenEnEstaVela && otraMartinAlexander == 0)
      return;

   double atrPS = 0;
   if(AlexanderModoPipStep == PS_Incrementa && countTradesAlexanderVar > 0)
     {
      double multiPipStep = (AlexanderPipStep / 2) * (countTradesAlexanderVar - 1);
      atrPS = 0;
     }
   else
     {
      multiPipStep = 0;
      atrPS = 0;
     }
   if(AlexanderModoPipStep == PS_Fijo)
     {
      multiPipStep = 0;
      atrPS = 0;
     }
   if(AlexanderModoPipStep == PS_Variable)
     {
      multiPipStep = 0;
      atrPS = atrValueA;
     }


   alexanderUltimoPrecioOperacionBuy = FindLastBuyPrice_Alexander();
   alexanderUltimoPrecioOperacionSell = FindLastSellPrice_Alexander();
   int tipoOperacionAlexander = tipoOperacionCaudillo(MagicNumber_Alexander);

//Buy
   if(tipoOperacionAlexander == 0)// && iClose(NULL, 60, 1) < iHigh(NULL, 5, 0) ) //&& (tendencia > parte))
     {
      //Se va a hacer una cobertura, cuando iguale o supere el precio más el pipstep establecido en AlexanderPipStep
      if((alexanderUltimoPrecioOperacionBuy - Ask) >= (multiPipStep + (AlexanderPipStep + atrPS)) * Point || ((alexanderUltimoPrecioOperacionBuy - Ask) > 0 && otraMartinAlexander == 1))
        {
         RefreshRates();
         lotajeActualizadoAlexander = calculaLotaje(lotajePrimeraAlexander,AlexanderLotExponent,numeroOperacionesAlexander);
         resultadoAbirOrdenAlexander = OpenPendingOrder_Alexander(0, lotajeActualizadoAlexander, slipPage, Bid, 0, 0, comentarioAlexander + "-" + numeroOperacionesAlexander, MagicNumber_Alexander, 0, Lime);
         if(resultadoAbirOrdenAlexander <= 0)
           {
            int LE = GetLastError();
            if(LE != 0 && LE != 4051)
               Print("Error: ", LE);

           }
         else
           {
            lotajeSumTotal += lotajeActualizadoAlexander;
            //actualiza el precio promedio de toda la martingla
            actualizarPrecioPromedioAlexander();
            //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
            actualizarTPAlexander();
            otraMartinAlexander = 0;
           }

        }//fin if
      //Sell
     }
   else
      if(tipoOperacionAlexander == 1)// && iClose(NULL, 60, 1) > iLow(NULL, 5, 0) ) //&& (tendencia < -parte))
        {
         //Se va a hacer una cobertura, cuando iguale o supere el precio más el pipstep establecido en AlexanderPipStep
         if((Bid - alexanderUltimoPrecioOperacionSell) >= (multiPipStep + (AlexanderPipStep + atrPS)) * Point || ((Bid - alexanderUltimoPrecioOperacionSell) > 0 && otraMartinAlexander == 1))
           {
            RefreshRates();
            //                lotajeActualizadoAlexander = NormalizeDouble(AlexanderLots * MathPow(AlexanderLotExponent, numeroOperacionesAlexander), lotdecimal);
            lotajeActualizadoAlexander = calculaLotaje(lotajePrimeraAlexander,AlexanderLotExponent,numeroOperacionesAlexander);
            resultadoAbirOrdenAlexander = OpenPendingOrder_Alexander(1, lotajeActualizadoAlexander, slipPage, Ask, 0, 0, comentarioAlexander + "-" + numeroOperacionesAlexander, MagicNumber_Alexander, 0, HotPink);
            if(resultadoAbirOrdenAlexander <= 0)
              {
               LE = GetLastError();
               if(LE != 0 && LE != 4051)
                  Print("Error: ", LE);

              }
            else
              {
               lotajeSumTotal += lotajeActualizadoAlexander;
               //actualiza el precio promedio de toda la martingla
               actualizarPrecioPromedioAlexander();
               //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
               actualizarTPAlexander();
               otraMartinAlexander = 0;
              }

           }//fin if

        }// fin if(tipoOperacionAlexander

  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void actualizarPrecioPromedioAlexander()
  {

   promedioPrecioAlexander = 0.00;
   double lotajeMartingalaTotal = 0.00;

   int ot=OrdersTotal() - 1;

   for(posicionOrden = ot; posicionOrden >= 0; posicionOrden--)    //recorremos todas las ordenes, descendentemente
     {
      if(OrderSelect(posicionOrden, SELECT_BY_POS, MODE_TRADES))
        {
         if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Alexander)
           {
            if(OrderType() == OP_BUY || OrderType() == OP_SELL)
              {
               promedioPrecioAlexander += OrderOpenPrice() * OrderLots(); //Se suma, para todas la operaciones de la martingala, el precio de apertura de cada operación multiplicado por el Lotaje de la misma.
               lotajeMartingalaTotal += OrderLots(); //sumamos todo el lotaje de todas las órdenes de la martingala
              }
           }
        }
     }

   if(promedioPrecioAlexander != 0.00)
     {
      promedioPrecioAlexander = NormalizeDouble((promedioPrecioAlexander / lotajeMartingalaTotal), Digits); //promedio del precio para calcular el nuevo TakeProfit de la martingala
     }

  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void actualizarTPAlexander()
  {

   static double decreTPAlexanderBack=0.00;

   actualizadoTPAlexander=true;
   double takeProfitActualAlexander = 0.0;


   if((AlexanderModoTp == TP_Decrementa) && (countTradesAlexanderVar > 0))
     {
      double decreTP = AlexanderTakeProfit / ((countTradesAlexanderVar+0.333)-(countTradesAlexanderVar/3.0));

     }
   else
     {
      decreTP = AlexanderTakeProfit;
     }

   if(reEntradasAlexander && AlexanderActivo && !puedeCoberturearAlexander)
     {
      decreTP = AlexanderTakeProfit*2;//(AlexanderTakeProfit)+((((GranH-GranL))/Point))*(reDistanciaAlexander*1.0);//*Point;  //       ((atrTP / Point) * 25.0);
     }



//Se Calcula y actualiza el TP de cada operación de la martingala

   int ot=OrdersTotal() - 1;

   for(posicionOrden = ot; posicionOrden >= 0; posicionOrden--)
     {
      if(OrderSelect(posicionOrden, SELECT_BY_POS, MODE_TRADES))
        {
         if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Alexander)
           {
            takeProfitActualAlexander = OrderTakeProfit();
            if(OrderType() == OP_BUY)
              {
               alexanderTakeProfitPromediado = promedioPrecioAlexander + decreTP * Point; //al promedio del precio necesario para cubir los beneficios de la martingala, le sumanos el TakeProfit establecido por parámetros
               alexanderTakeProfitPromediado = NormalizeDouble(alexanderTakeProfitPromediado, Digits);
               double modifyTP = NormalizeTPBuy(alexanderTakeProfitPromediado, alexanderTakeProfitPromediado);
               if(reEntradasAlexander && modifyTP<Ask)
                 {
                  continue;
                 }
              }
            if(OrderType() == OP_SELL)
              {
               alexanderTakeProfitPromediado = promedioPrecioAlexander - decreTP * Point;
               alexanderTakeProfitPromediado = NormalizeDouble(alexanderTakeProfitPromediado, Digits);
               modifyTP = NormalizeTPSell(alexanderTakeProfitPromediado, alexanderTakeProfitPromediado);
               if(reEntradasAlexander && modifyTP>Bid)
                 {
                  continue;
                 }
              }


            if(modifyTP<0.00)
              {
               Print("Posiblemente esté usando un TP demasiado alto, procedo a eliminarlo (TP: "+modifyTP+")");
               modifyTP=0.00;
              }
            if(takeProfitActualAlexander != modifyTP)
              {
               int countLimit = 0;
               while(!OrderModify(OrderTicket(), promedioPrecioAlexander, OrderStopLoss(), modifyTP, 0, Yellow) && countLimit < 5 && !IsStopped())
                 {
                  int LE = GetLastError();
                  if(LE != 4/* SERVER_BUSY */ && LE != 136/* OFF_QUOTES */)
                     break;
                  Sleep(500);
                  RefreshRates();
                  countLimit += 1;
                 }
              }//end if(takeProfitActualAlexander

           }// edn if
        }//orderselect
     }//fin for


  }
//-----------------------------------------------------------------------------------------------------------



// ------------------------------------- Hannibal Barca -----------------------------------------------------------
void operativaHannibalClasicaHardLight()
  {
   if(numeroOperacionesHannibal < MaxTrades_Hannibal && otraMartinHannibal == 1) //No es la primera operación de la martingala(no es la 0), y no supera el número máximo de operaciones permitidas.
     {
      hacerCoberturaHannibal();
     }

//Se ejecuta el código sólo cuando hay un cambio de vela
   if(HannibalCambioMinuto != tiempoMinuto || lastHannibalTakeProfit != (int)HannibalTakeProfit || otraMartinHannibal == 1 || HannibalOrdenManual>0)
     {
      lastHannibalTakeProfit = (int)HannibalTakeProfit;

      numeroOperacionesHannibal = countTradesHannibalVar;
      if(HannibalActivo == True || HannibalOrdenManual>0 || otraMartinHannibal == 1)    // Controla la activación/desactivación del caudillo Hannibal
        {

         if(HannibalActividad != Terminando || HannibalOrdenManual>0)
           {
            if(numeroOperacionesHannibal == 0)
              {
               //Si no hay operaciones abiertas, se comprueba si se puede abrir una nueva operación, cada hora
               gestionarNuevaOperacionHannibal();
              }
           }


         if(numeroOperacionesHannibal > 0)
           {
            if(numeroOperacionesHannibal < MaxTrades_Hannibal)   //No es la primera operación de la martingala(no es la 0), y no supera el número máximo de operaciones permitidas.
              {
               hacerCoberturaHannibal();
              }

           }// fin if (numeroOperacionesHannibal > 0



        }// Fin de HannibalActivo

      if(HannibalAutoPriceAverage)
        {
         //actualiza el precio promedio de toda la martingla
         actualizarPrecioPromedioHannibal();
         //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
         actualizarTPHannibal();
        }

      HannibalCambioMinuto = tiempoMinuto;
     } //end if(HannibalCambioMinuto

   if(HannibalUseTrailingStop)
      TrailingAlls_Hannibal();


  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void gestionarNuevaOperacionHannibal()
  {

   if(HayLimiteHorario(3))
      return;
   if(HannibalOrdenEnEstaVela)
      return;

   double precioCierre2VelasAtras;
   double precioCierreVelaAnterior;

//Se comprueba si se puede abrir una nueva operación, cada hora
   if(hannibalDateTime != tiempoMinuto)
     {

      precioCierre2VelasAtras = iClose(Symbol(), HannibalSeleccionTF, 2); //Obtiene el precio de cierre de 2 velas atrás
      precioCierreVelaAnterior = iClose(Symbol(), HannibalSeleccionTF, 1); //Obtiene el precio de cierre de 1 vela atrás

      bool permisoAbrirHannibal = true;
      bool permisoAbrirHannibalMaxIguales = false;

      if(precioCierre2VelasAtras > precioCierreVelaAnterior)    //Si el precio de cierre de la ultima vela es menor que el de la anterior(ha disminuido el precio), abrirá una operación Sell
        {
         if(HannibalOrdenManual==1 || (iRSI(NULL, HannibalSeleccionTF, PeriodRsiHannibal, PRICE_CLOSE, 1) > LevelRsiLowHannibal && ((tendencia < -atrParcialValueH && atrParcialValueH >= 0) || (tendencia < -atrParcialValueH && tendencia > atrParcialValueH && atrParcialValueH <= 0)))) //Si el RSI está por encima de 30, se realiza una operación SELL
           {

            if(modoOperativa == Modo_Gladiador)
              {
               permisoAbrirHannibalMaxIguales = true;
              }

            //Print("permisoAbrirHannibalMax2Iguales: " + permisoAbrirHannibalMax2Iguales);
            if(permisoAbrirHannibalMaxIguales == true)
              {
               /////////////////////  SELL ////////////////////////
               permisoAbrirHannibal = permisoAbrirSymbolCaudillo(1);
               //Print("permisoAbrirHannibal: " + permisoAbrirHannibal);
               if(permisoAbrirHannibal)
                 {
                  //                        resultadoAbirOrdenHannibal = OpenPendingOrder_Hannibal(1, HannibalLots, slipPage, Bid, 0, 0, comentarioHannibal + "-" + 0, MagicNumber_Hannibal, 0, HotPink);
                  resultadoAbirOrdenHannibal = OpenPendingOrder_Hannibal(1, lotajePrimeraHannibal, slipPage, Bid, 0, 0, comentarioHannibal + "-" + 0, MagicNumber_Hannibal, 0, HotPink);
                  if(resultadoAbirOrdenHannibal <= 0)
                    {
                     int LE = GetLastError();
                     if(LE != 0 && LE != 4051)
                        Print("Error: ", LE);

                    }
                  else
                    {
                     numSeries++; // TEST
                     lotajeSumTotal += lotajePrimeraHannibal;
                     //guarda precio primera orden Alexander
                     if(reEntradasHannibal && countTradesHannibalVar == 0)
                       {
                        int ti = OrderSelect(resultadoAbirOrdenHannibal, SELECT_BY_TICKET, MODE_TRADES);
                        if(ti)
                          {
                           precioPrimeraHannibal = OrderOpenPrice();
                          }
                        else
                          {
                           precioPrimeraHannibal = Bid;
                          }
                       }

                     //actualiza el precio promedio de toda la martingla
                     actualizarPrecioPromedioHannibal();
                     //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
                     actualizarTPHannibal();
                    }

                 }//fin if(permisoAbrirHannibal
              }//fin if(permisoAbrirHannibalMax2Iguales

           }//end if RSI
        }
      else     //Si el precio de cierre de la ultima vela es mayor que el de la anterior(ha aumentado el precio), abrirá una operación BUY
        {
         if(HannibalOrdenManual==2 || (iRSI(NULL, HannibalSeleccionTF, PeriodRsiHannibal, PRICE_CLOSE, 1) < LevelRsiHighHannibal && ((tendencia > atrParcialValueH && atrParcialValueH >= 0) || (tendencia > atrParcialValueH && tendencia < -atrParcialValueH && atrParcialValueH <= 0)))) //Si el RSI está por debajo de 70, se realiza una operación BUY
           {

            if(modoOperativa == Modo_Gladiador)
              {
               permisoAbrirHannibalMaxIguales = true;
              }

            if(permisoAbrirHannibalMaxIguales == true)
              {
               permisoAbrirHannibal = permisoAbrirSymbolCaudillo(0);
               //Print("permisoAbrirHannibal: " + permisoAbrirHannibal);
               ////////////////////////////// COMPRA ////////////////////////
               if(permisoAbrirHannibal)
                 {
                  //                        resultadoAbirOrdenHannibal = OpenPendingOrder_Hannibal(0, HannibalLots, slipPage, Ask, 0, 0, comentarioHannibal + "-" + 0, MagicNumber_Hannibal, 0, Lime);
                  resultadoAbirOrdenHannibal = OpenPendingOrder_Hannibal(0, lotajePrimeraHannibal, slipPage, Ask, 0, 0, comentarioHannibal + "-" + 0, MagicNumber_Hannibal, 0, Lime);
                  if(resultadoAbirOrdenHannibal <= 0)
                    {
                     LE = GetLastError();
                     if(LE != 0 && LE != 4051)
                        Print("Error: ", LE);

                    }
                  else
                    {
                     numSeries++; // TEST
                     lotajeSumTotal += lotajePrimeraHannibal;
                     //guarda precio primera orden Hannibal
                     if(reEntradasHannibal && countTradesHannibalVar == 0)
                       {
                        ti = OrderSelect(resultadoAbirOrdenHannibal, SELECT_BY_TICKET, MODE_TRADES);
                        if(ti)
                          {
                           precioPrimeraHannibal = OrderOpenPrice();
                          }
                        else
                          {
                           precioPrimeraHannibal = Ask;
                          }
                       }

                     //actualiza el precio promedio de toda la martingla
                     actualizarPrecioPromedioHannibal();
                     //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
                     actualizarTPHannibal();
                    }

                 }//fin if(permisoAbrirHannibal
              }//fin if(permisoAbrirHannibalMaxIguales

           }//end if RSI
        } //cálculo para abrir Buy o Sell

      hannibalDateTime = tiempoMinuto;
     } //fin if (hannibalDateTime


  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void hacerCoberturaHannibal()
  {
   if(HannibalOrdenEnEstaVela && otraMartinHannibal == 0)
      return;

   double atrPS = 0;
   if(HannibalModoPipStep == PS_Incrementa && countTradesHannibalVar > 0)
     {
      double multiPipStep = (HannibalPipStep / 2) * (countTradesHannibalVar - 1);
      atrPS = 0;
     }
   else
     {
      multiPipStep = 0;
      atrPS = 0;
     }
   if(HannibalModoPipStep == PS_Fijo)
     {
      multiPipStep = 0;
      atrPS = 0;
     }
   if(HannibalModoPipStep == PS_Variable)
     {
      multiPipStep = 0;
      atrPS = atrValueH;
     }

   hannibalUltimoPrecioOperacionBuy = FindLastBuyPrice_Hannibal();
   hannibalUltimoPrecioOperacionSell = FindLastSellPrice_Hannibal();
   int tipoOperacionHannibal = tipoOperacionCaudillo(MagicNumber_Hannibal);

//Buy
   if(tipoOperacionHannibal == 0)// && iClose(NULL, 60, 1) < iHigh(NULL, 5, 0) ) //&& (tendencia > parte))
     {
      //Se va a hacer una cobertura, cuando iguale o supere el precio más el pipstep establecido en HannibalPipStep
      if((hannibalUltimoPrecioOperacionBuy - Ask) >= (multiPipStep + (HannibalPipStep + atrPS)) * Point || ((hannibalUltimoPrecioOperacionBuy - Ask) > 0 && otraMartinHannibal == 1))
        {
         RefreshRates();
         lotajeActualizadoHannibal = calculaLotaje(lotajePrimeraHannibal,HannibalLotExponent,numeroOperacionesHannibal);
         resultadoAbirOrdenHannibal = OpenPendingOrder_Hannibal(0, lotajeActualizadoHannibal, slipPage, Bid, 0, 0, comentarioHannibal + "-" + numeroOperacionesHannibal, MagicNumber_Hannibal, 0, Lime);
         if(resultadoAbirOrdenHannibal <= 0)
           {
            int LE = GetLastError();
            if(LE != 0 && LE != 4051)
               Print("Error: ", LE);

           }
         else
           {
            lotajeSumTotal += lotajeActualizadoHannibal;
            //actualiza el precio promedio de toda la martingla
            actualizarPrecioPromedioHannibal();
            //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
            actualizarTPHannibal();
            otraMartinHannibal = 0;
           }

        }//fin if
      //Sell
     }
   else
      if(tipoOperacionHannibal == 1)// && iClose(NULL, 60, 1) > iLow(NULL, 5, 0) ) //&& (tendencia < -parte))
        {
         //Se va a hacer una cobertura, cuando iguale o supere el precio más el pipstep establecido en HannibalPipStep
         if((Bid - hannibalUltimoPrecioOperacionSell) >= (multiPipStep + (HannibalPipStep + atrPS)) * Point || ((Bid - hannibalUltimoPrecioOperacionSell) > 0 && otraMartinHannibal == 1))
           {
            RefreshRates();
            lotajeActualizadoHannibal = calculaLotaje(lotajePrimeraHannibal,HannibalLotExponent,numeroOperacionesHannibal);
            resultadoAbirOrdenHannibal = OpenPendingOrder_Hannibal(1, lotajeActualizadoHannibal, slipPage, Ask, 0, 0, comentarioHannibal + "-" + numeroOperacionesHannibal, MagicNumber_Hannibal, 0, HotPink);
            if(resultadoAbirOrdenHannibal <= 0)
              {
               LE = GetLastError();
               if(LE != 0 && LE != 4051)
                  Print("Error: ", LE);

              }
            else
              {
               lotajeSumTotal += lotajeActualizadoHannibal;
               //actualiza el precio promedio de toda la martingla
               actualizarPrecioPromedioHannibal();
               //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
               actualizarTPHannibal();
               otraMartinHannibal = 0;
              }

           }//fin if
        }// fin if(tipoOperacionHannibal


  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void actualizarPrecioPromedioHannibal()
  {


   promedioPrecioHannibal = 0.00;
   double lotajeMartingalaTotal = 0.00;
   int ot=OrdersTotal() - 1;
   for(posicionOrden = ot; posicionOrden >= 0; posicionOrden--)    //recorremos todas las ordenes, descendentemente
     {
      if(OrderSelect(posicionOrden, SELECT_BY_POS, MODE_TRADES))
        {

         if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Hannibal)
           {
            if(OrderType() == OP_BUY || OrderType() == OP_SELL)
              {
               promedioPrecioHannibal += OrderOpenPrice() * OrderLots(); //Se suma, para todas la operaciones de la martingala, el precio de apertura de cada operación multiplicado por el Lotaje de la misma.
               lotajeMartingalaTotal += OrderLots(); //sumamos todo el lotaje de todas las órdenes de la martingala
              }
           }
        }
     }

   if(promedioPrecioHannibal != 0.00)
     {
      promedioPrecioHannibal = NormalizeDouble((promedioPrecioHannibal / lotajeMartingalaTotal), Digits); //promedio del precio para calcular el nuevo TakeProfit de la martingala
     }

  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void actualizarTPHannibal()
  {

   static double decreTPHannibalBack=0.00;

   actualizadoTPHannibal=true;
   double takeProfitActualHannibal = 0.0;


   if((HannibalModoTp == TP_Decrementa) && (countTradesHannibalVar > 0))
     {
      double decreTP = HannibalTakeProfit / ((countTradesHannibalVar+0.333)-(countTradesHannibalVar/3.0));

     }
   else
     {
      decreTP = HannibalTakeProfit;
     }

   if(reEntradasHannibal && HannibalActivo && !puedeCoberturearHannibal)
     {
      decreTP = HannibalTakeProfit*2;//(HannibalTakeProfit)+((((GranH-GranL))/Point))*(reDistanciaHannibal*1.0);//*Point;  //       ((atrTP / Point) * 25.0);
     }

////------------------------------------------------------------------------------------------------

//Se Calcula y actualiza el TP de cada operación de la martingala
   int ot=OrdersTotal() - 1;

   for(posicionOrden = ot; posicionOrden >= 0; posicionOrden--)
     {
      if(OrderSelect(posicionOrden, SELECT_BY_POS, MODE_TRADES))
        {
         if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Hannibal)
           {
            takeProfitActualHannibal = OrderTakeProfit();
            if(OrderType() == OP_BUY)
              {
               hannibalTakeProfitPromediado = promedioPrecioHannibal + decreTP * Point; //al promedio del precio necesario para cubir los beneficios de la martingala, le sumanos el TakeProfit establecido por parámetros
               hannibalTakeProfitPromediado = NormalizeDouble(hannibalTakeProfitPromediado, Digits);
               double modifyTP = NormalizeTPBuy(hannibalTakeProfitPromediado, hannibalTakeProfitPromediado);
               if(reEntradasHannibal && modifyTP<Ask)
                 {
                  continue;
                 }
              }
            if(OrderType() == OP_SELL)
              {
               hannibalTakeProfitPromediado = promedioPrecioHannibal - decreTP * Point;
               hannibalTakeProfitPromediado = NormalizeDouble(hannibalTakeProfitPromediado, Digits);
               modifyTP = NormalizeTPSell(hannibalTakeProfitPromediado, hannibalTakeProfitPromediado);
               if(reEntradasHannibal && modifyTP>Bid)
                 {
                  continue;
                 }
              }


            if(modifyTP<0.00)
              {
               Print("Posiblemente esté usando un TP demasiado alto, procedo a eliminarlo (TP: "+modifyTP+")");
               modifyTP=0.00;
              }
            if(takeProfitActualHannibal != modifyTP)
              {
               int countLimit = 0;
               while(!OrderModify(OrderTicket(), promedioPrecioHannibal, OrderStopLoss(), modifyTP, 0, Yellow) && countLimit < 5 && !IsStopped())
                 {
                  int LE = GetLastError();
                  if(LE != 4/* SERVER_BUSY */ && LE != 136/* OFF_QUOTES */)
                     break;
                  Sleep(500);
                  RefreshRates();
                  countLimit += 1;
                 }
              }//end if(takeProfitActual

           }// edn if
        }// orderselet
     }//fin for

  }
//-----------------------------------------------------------------------------------------------------------




/************************************ Fin del Modo Clásico Hard y Light *********************************************/


/************************************************************************************************************/


/***************************************** Modo Coordinado   **************************************************/



// ------------------------------------- Julius Caesar -----------------------------------------------------------
void operativaCaesarCoordinadoManual()
  {

//Se ejecuta el código sólo cuando hay un cambio de vela
   if((CaesarCambioMinuto != tiempoMinuto) || (lastCaesarTakeProfit != (int)CaesarTakeProfit) || (otraMartinCaesar == 1) || (CaesarOrdenManual > 0))
     {
      lastCaesarTakeProfit = (int)CaesarTakeProfit;
      numeroOperacionesCaesar = countTradesCaesarVar; //Obtiene el número de operaciones de la martingala del caudillo Caesar
      if((CaesarActivo == True) || (otraMartinCaesar == 1) || (CaesarOrdenManual >0))//yy   // Controla la activación/desactivación del caudillo Caesar
        {
         if((CaesarActividad != Terminando) || (CaesarOrdenManual >0))
           {
            //numeroOperacionesCaesar = countTradesCaesarVar;
            //Si no hay operaciones abiertas, se comprueba si se puede abrir una nueva operación, cada hora
            if(numeroOperacionesCaesar == 0 || (reEntradasCaesar && hazReentradaCaesar)) //nueva rutina
              {
               gestionarNuevaOperacionCoordinadoManualCaesar();
              }
           }

         //numeroOperacionesCaesar = countTradesCaesarVar;

         if(numeroOperacionesCaesar > 0)
           {

            if(numeroOperacionesCaesar < MaxTrades_Caesar && (puedeCoberturearCaesar || otraMartinCaesar==1))   //No es la primera operación de la martingala(no es la 0), y no supera el número máximo de operaciones permitidas.
              {
               hacerCoberturaCaesar();
              }

           }// fin if (numeroOperacionesCaesar > 0

        }// Fin de CaesarActivo

      if(CaesarAutoPriceAverage)
        {
         //actualiza el precio promedio de toda la martingla
         actualizarPrecioPromedioCaesar();
         //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
         actualizarTPCaesar();
        }

      CaesarCambioMinuto = tiempoMinuto;
     } //end if(CaesarCambioMinuto

   if(CaesarUseTrailingStop)
      TrailingAlls_Caesar();


  }




//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void gestionarNuevaOperacionCoordinadoManualCaesar()
  {
  
  

   if(HayLimiteHorario(1))
      return;
   if((CaesarTipoOperacion == Tipo_Sell) && ((CaesarOrdenManual==1) || ((tendencia < -atrParcialValueJ) && (atrParcialValueJ >= 0)) || ((tendencia < -atrParcialValueJ) && (tendencia > atrParcialValueJ) && (atrParcialValueJ <= 0))))  //SELL
     {
      if((tendencia < lastTendencia) || (CaesarTipoOperacion == Tipo_Manual) || (CaesarOrdenManual==1))//xx
        {
         resultadoAbirOrdenCaesar = OpenPendingOrder_Caesar(1, lotajePrimeraCaesar, slipPage, Bid, 0, 0, comentarioCaesar + "-" + 0, MagicNumber_Caesar, 0, HotPink);
         if(resultadoAbirOrdenCaesar <= 0)
           {
            int LE = GetLastError();
            if(LE != 0 && LE != 4051)
               Print("Error: ", LE);

           }
         else
           {
            lotajeSumTotal += lotajePrimeraCaesar;
            numSeries++;

            //guarda precio primera orden Caesar
            if(reEntradasCaesar && countTradesCaesarVar == 0)
              {
               int ti = OrderSelect(resultadoAbirOrdenCaesar, SELECT_BY_TICKET, MODE_TRADES);
               if(ti)
                 {
                  precioPrimeraCaesar = OrderOpenPrice();
                 }
               else
                 {
                  precioPrimeraCaesar = Bid;
                 }
              }
            //actualiza el precio promedio de toda la martingla
            actualizarPrecioPromedioCaesar();
            //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
            actualizarTPCaesar();
           }
        }
     }
   if((CaesarTipoOperacion == Tipo_Buy) && ((CaesarOrdenManual==2) || ((tendencia > atrParcialValueJ) && (atrParcialValueJ >= 0)) || ((tendencia > atrParcialValueJ) && (tendencia < -atrParcialValueJ) && (atrParcialValueJ <= 0)))) //BUY
     {
      if((tendencia > lastTendencia) || (CaesarTipoOperacion == Tipo_Manual) || (CaesarOrdenManual==2))
        {
         resultadoAbirOrdenCaesar = OpenPendingOrder_Caesar(0, lotajePrimeraCaesar, slipPage, Ask, 0, 0, comentarioCaesar + "-" + 0, MagicNumber_Caesar, 0, Lime);
         if(resultadoAbirOrdenCaesar <= 0)
           {
            LE = GetLastError();
            if(LE != 0 && LE != 4051)
               Print("Error: ", LE);

           }
         else
           {
            lotajeSumTotal += lotajePrimeraCaesar;
            numSeries++; // TEST

            //guarda precio primera orden Caesar
            if(reEntradasCaesar && countTradesCaesarVar == 0)
              {
               ti = OrderSelect(resultadoAbirOrdenCaesar, SELECT_BY_TICKET, MODE_TRADES);
               if(ti)
                 {
                  precioPrimeraCaesar = OrderOpenPrice();
                 }
               else
                 {
                  precioPrimeraCaesar = Bid;
                 }
              }
            //}
            //actualiza el precio promedio de toda la martingla
            actualizarPrecioPromedioCaesar();
            //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
            actualizarTPCaesar();
           }
        }
     } //cálculo para abrir Buy o Sell

   if(CaesarTipoOperacion == Tipo_Manual || CaesarTipoOperacion == Tipo_Tendencial || CaesarTipoOperacion == Tipo_AntiTenden || CaesarOrdenManual>0)//xx   //BUY o SELL a eleccion del trader
     {
      if(CaesarOrdenManual == 1 || (CaesarTipoOperacion == Tipo_Tendencial && ((tendencia < -atrParcialValueJ && atrParcialValueJ >= 0) || (tendencia < -atrParcialValueJ && tendencia > atrParcialValueJ && atrParcialValueJ <= 0)) && permisoAbrirSymbolCaudillo(1)) || (CaesarTipoOperacion == Tipo_AntiTenden && ((tendencia > atrParcialValueJ && atrParcialValueJ >= 0) || (tendencia > atrParcialValueJ && tendencia < -atrParcialValueJ && atrParcialValueJ <= 0)) && permisoAbrirSymbolCaudillo(1))) // Sell
        {
         if(tendencia < lastTendencia || CaesarTipoOperacion == Tipo_Manual || CaesarOrdenManual==1)//xx
           {
            resultadoAbirOrdenCaesar = OpenPendingOrder_Caesar(1, lotajePrimeraCaesar, slipPage, Bid, 0, 0, comentarioCaesar + "-" + 0, MagicNumber_Caesar, 0, HotPink);
            if(resultadoAbirOrdenCaesar <= 0)
              {
               LE = GetLastError();
               if(LE != 0 && LE != 4051)
                  Print("Error: ", LE);

              }
            else
              {
               lotajeSumTotal += lotajePrimeraCaesar;
               numSeries++; // TEST

               //guarda precio primera orden Caesar
               if(reEntradasCaesar && countTradesCaesarVar == 0)
                 {
                  ti = OrderSelect(resultadoAbirOrdenCaesar, SELECT_BY_TICKET, MODE_TRADES);
                  if(ti)
                    {
                     precioPrimeraCaesar = OrderOpenPrice();
                    }
                  else
                    {
                     precioPrimeraCaesar = Bid;
                    }
                 }
               CaesarOrdenManual = 0;
               otraMartinCaesar =0;
               //actualiza el precio promedio de toda la martingla
               actualizarPrecioPromedioCaesar();
               //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
               actualizarTPCaesar();
              }
           }
        }
      else
        {
         if((CaesarOrdenManual == 2) || ((CaesarTipoOperacion == Tipo_Tendencial) && ((tendencia > atrParcialValueJ && atrParcialValueJ >= 0) || ((tendencia > atrParcialValueJ) && (tendencia < -atrParcialValueJ) && (atrParcialValueJ <= 0))) && permisoAbrirSymbolCaudillo(0)) || ((CaesarTipoOperacion == Tipo_AntiTenden) && (((tendencia < -atrParcialValueJ) && (atrParcialValueJ >= 0)) || ((tendencia < -atrParcialValueJ) && (tendencia > atrParcialValueJ) && (atrParcialValueJ <= 0))) && permisoAbrirSymbolCaudillo(0))) // Buy
           {
            if((tendencia > lastTendencia) || (CaesarTipoOperacion == Tipo_Manual) || (CaesarOrdenManual==2))//xx
              {

               resultadoAbirOrdenCaesar = OpenPendingOrder_Caesar(0, lotajePrimeraCaesar, slipPage, Ask, 0, 0, comentarioCaesar + "-" + 0, MagicNumber_Caesar, 0, Lime);
               if(resultadoAbirOrdenCaesar <= 0)
                 {
                  LE = GetLastError();
                  if(LE != 0 && LE != 4051)
                     Print("Error: ", LE);

                 }
               else
                 {
                  numSeries++; // TEST
                  lotajeSumTotal += lotajePrimeraCaesar;
                  //guarda precio primera orden Caesar
                  if(reEntradasCaesar && countTradesCaesarVar == 0)
                    {
                     ti = OrderSelect(resultadoAbirOrdenCaesar, SELECT_BY_TICKET, MODE_TRADES);
                     if(ti)
                       {
                        precioPrimeraCaesar = OrderOpenPrice();
                       }
                     else
                       {
                        precioPrimeraCaesar = Bid;
                       }
                    }
                  CaesarOrdenManual = 0;
                  otraMartinCaesar =0;
                  actualizarPrecioPromedioCaesar();
                  actualizarTPCaesar();
                 }
              }

           }


        }

     }



  }





// ------------------------------------- Alexander Magnus -----------------------------------------------------------
void operativaAlexanderCoordinadoManual()
  {

//Se ejecuta el código sólo cuando hay un cambio de vela
   if(AlexanderCambioMinuto != tiempoMinuto || lastAlexanderTakeProfit != (int)AlexanderTakeProfit || otraMartinAlexander == 1 || (AlexanderOrdenManual > 0))
     {
      lastAlexanderTakeProfit = (int)AlexanderTakeProfit;
      numeroOperacionesAlexander = countTradesAlexanderVar; //Obtiene el número de operaciones de la martingala del caudillo Alexander
      if(AlexanderActivo == True || AlexanderOrdenManual>0 || otraMartinAlexander==1)    // Controla la activación/desactivación del caudillo Alexander
        {

         if(AlexanderActividad != Terminando || AlexanderOrdenManual>0 || otraMartinAlexander==1)
           {
            //Si no hay operaciones abiertas, se comprueba si se puede abrir una nueva operación, cada hora
            if(numeroOperacionesAlexander == 0 || (reEntradasAlexander && hazReentradaAlexander)) //nueva rutina
              {
               gestionarNuevaOperacionCoordinadoManualAlexander();
              }
           }


         if(numeroOperacionesAlexander > 0)
           {

            if(numeroOperacionesAlexander < MaxTrades_Alexander && (puedeCoberturearAlexander || otraMartinAlexander==1))   //No es la primera operación de la martingala(no es la 0), y no supera el número máximo de operaciones permitidas.
              {
               hacerCoberturaAlexander();
              }

           }// fin if (numeroOperacionesAlexander > 0

        }// Fin de AlexanderActivo

      if(AlexanderAutoPriceAverage)
        {
         //actualiza el precio promedio de toda la martingla
         actualizarPrecioPromedioAlexander();
         //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
         actualizarTPAlexander();
        }

      AlexanderCambioMinuto = tiempoMinuto;
     } //end if(AlexanderCambioMinuto

   if(AlexanderUseTrailingStop)
      TrailingAlls_Alexander();


  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void gestionarNuevaOperacionCoordinadoManualAlexander()
  {

   if(AlexanderOrdenEnEstaVela)
      return;

   if(HayLimiteHorario(2))
      return;

   if(AlexanderOrdenManual==1 || (AlexanderTipoOperacion == Tipo_Sell && ((tendencia < -atrParcialValueA && atrParcialValueA >= 0) || (tendencia < -atrParcialValueA && tendencia > atrParcialValueA && atrParcialValueA <= 0))))  //SELL
     {

      if(tendencia < lastTendencia || AlexanderTipoOperacion == Tipo_Manual || AlexanderOrdenManual==1)//xx
        {
         resultadoAbirOrdenAlexander = OpenPendingOrder_Alexander(1, lotajePrimeraAlexander, slipPage, Bid, 0, 0, comentarioAlexander + "-" + 0, MagicNumber_Alexander, 0, HotPink);
         if(resultadoAbirOrdenAlexander <= 0)
           {
            int LE = GetLastError();
            if(LE != 0 && LE != 4051)
               Print("Error: ", LE);

           }
         else
           {
            numSeries++; // TEST
            lotajeSumTotal += lotajePrimeraAlexander;
            numSeries++;

            //guarda precio primera orden Alexander
            if(reEntradasAlexander && countTradesAlexanderVar == 0)
              {
               int ti = OrderSelect(resultadoAbirOrdenAlexander, SELECT_BY_TICKET, MODE_TRADES);
               if(ti)
                 {
                  precioPrimeraAlexander = OrderOpenPrice();
                 }
               else
                 {
                  precioPrimeraAlexander = Bid;
                 }
              }
            //actualiza el precio promedio de toda la martingla
            actualizarPrecioPromedioAlexander();
            //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
            actualizarTPAlexander();
           }
        }
     }
   if(AlexanderOrdenManual==2 || (AlexanderTipoOperacion == Tipo_Buy && ((tendencia > atrParcialValueA && atrParcialValueA >= 0) || (tendencia > atrParcialValueA && tendencia < -atrParcialValueA && atrParcialValueA <= 0)))) //BUY
     {
      if(tendencia > lastTendencia || AlexanderTipoOperacion == Tipo_Manual || AlexanderOrdenManual==2)//xx
        {
         resultadoAbirOrdenAlexander = OpenPendingOrder_Alexander(0, lotajePrimeraAlexander, slipPage, Ask, 0, 0, comentarioAlexander + "-" + 0, MagicNumber_Alexander, 0, Lime);
         if(resultadoAbirOrdenAlexander <= 0)
           {
            LE = GetLastError();
            if(LE != 0 && LE != 4051)
               Print("Error: ", LE);

           }
         else
           {
            numSeries++; // TEST
            lotajeSumTotal += lotajePrimeraAlexander;
            //guarda precio primera orden Alexander
            if(reEntradasAlexander && countTradesAlexanderVar == 0)
              {
               ti = OrderSelect(resultadoAbirOrdenAlexander, SELECT_BY_TICKET, MODE_TRADES);
               if(ti)
                 {
                  precioPrimeraAlexander = OrderOpenPrice();
                 }
               else
                 {
                  precioPrimeraAlexander = Bid;
                 }
              }

            //actualiza el precio promedio de toda la martingla
            actualizarPrecioPromedioAlexander();
            //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
            actualizarTPAlexander();
           }
        }
     } //cálculo para abrir Buy o Sell

   if(AlexanderTipoOperacion == Tipo_Manual || AlexanderTipoOperacion == Tipo_Tendencial || AlexanderTipoOperacion == Tipo_AntiTenden || AlexanderOrdenManual>0)//xx    //BUY o SELL a eleccion del trader
     {
      if(AlexanderOrdenManual == 1 || (AlexanderTipoOperacion == Tipo_Tendencial && ((tendencia < -atrParcialValueA && atrParcialValueA >= 0) || (tendencia < -atrParcialValueA && tendencia > atrParcialValueA && atrParcialValueA <= 0)) && permisoAbrirSymbolCaudillo(1)) || (AlexanderTipoOperacion == Tipo_AntiTenden && ((tendencia > atrParcialValueA && atrParcialValueA >= 0) || (tendencia > atrParcialValueA && tendencia < -atrParcialValueA && atrParcialValueA <= 0)) && permisoAbrirSymbolCaudillo(1))) // Sell
        {
         if(tendencia < lastTendencia || AlexanderTipoOperacion == Tipo_Manual || AlexanderOrdenManual==1)//xx
           {
            resultadoAbirOrdenAlexander = OpenPendingOrder_Alexander(1, lotajePrimeraAlexander, slipPage, Bid, 0, 0, comentarioAlexander + "-" + 0, MagicNumber_Alexander, 0, HotPink);
            if(resultadoAbirOrdenAlexander <= 0)
              {
               LE = GetLastError();
               if(LE != 0 && LE != 4051)
                  Print("Error: ", LE);

              }
            else
              {
               numSeries++; // TEST
               lotajeSumTotal += lotajePrimeraAlexander;
               //guarda precio primera orden Alexander
               if(reEntradasAlexander && countTradesAlexanderVar == 0)
                 {
                  ti = OrderSelect(resultadoAbirOrdenAlexander, SELECT_BY_TICKET, MODE_TRADES);
                  if(ti)
                    {
                     precioPrimeraAlexander = OrderOpenPrice();
                    }
                  else
                    {
                     precioPrimeraAlexander = Bid;
                    }
                 }
               AlexanderOrdenManual = 0;
               otraMartinAlexander =0;
               //actualiza el precio promedio de toda la martingla
               actualizarPrecioPromedioAlexander();
               //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
               actualizarTPAlexander();
              }
           }
        }
      else
        {

         if(AlexanderOrdenManual == 2 || (AlexanderTipoOperacion == Tipo_Tendencial && ((tendencia > atrParcialValueA && atrParcialValueA >= 0) || (tendencia > atrParcialValueA && tendencia < -atrParcialValueA && atrParcialValueA <= 0)) && permisoAbrirSymbolCaudillo(0)) || (AlexanderTipoOperacion == Tipo_AntiTenden && ((tendencia < -atrParcialValueA && atrParcialValueA >= 0) || (tendencia < -atrParcialValueA && tendencia > atrParcialValueA && atrParcialValueA <= 0)) && permisoAbrirSymbolCaudillo(0))) // Buy
           {
            if(tendencia > lastTendencia || AlexanderTipoOperacion == Tipo_Manual || AlexanderOrdenManual==2)//xx
              {
               resultadoAbirOrdenAlexander = OpenPendingOrder_Alexander(0, lotajePrimeraAlexander, slipPage, Ask, 0, 0, comentarioAlexander + "-" + 0, MagicNumber_Alexander, 0, Lime);
               if(resultadoAbirOrdenAlexander <= 0)
                 {
                  LE = GetLastError();
                  if(LE != 0 && LE != 4051)
                     Print("Error: ", LE);
                 }
               else
                 {
                  numSeries++; // TEST
                  lotajeSumTotal += lotajePrimeraAlexander;
                  //guarda precio primera orden Alexander
                  if(reEntradasAlexander && countTradesAlexanderVar == 0)
                    {
                     ti = OrderSelect(resultadoAbirOrdenAlexander, SELECT_BY_TICKET, MODE_TRADES);
                     if(ti)
                       {
                        precioPrimeraAlexander = OrderOpenPrice();
                       }
                     else
                       {
                        precioPrimeraAlexander = Bid;
                       }
                    }
                  AlexanderOrdenManual = 0;
                  otraMartinAlexander =0;
                  actualizarPrecioPromedioAlexander();
                  actualizarTPAlexander();
                 }
              }
           }

        }


     }



  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
// ------------------------------------- Hannibal Barca -----------------------------------------------------------
void operativaHannibalCoordinadoManual()
  {

//Se ejecuta el código sólo cuando hay un cambio de vela
   if(HannibalCambioMinuto != tiempoMinuto || lastHannibalTakeProfit != (int)HannibalTakeProfit || otraMartinHannibal == 1 || (HannibalOrdenManual > 0))
     {
      lastHannibalTakeProfit = (int)HannibalTakeProfit;
      numeroOperacionesHannibal = countTradesHannibalVar; //Obtiene el número de operaciones de la martingala del caudillo Hannibal
      if(HannibalActivo == True || HannibalOrdenManual >0 || otraMartinHannibal==1)    // Controla la activación/desactivación del caudillo Hannibal
        {

         if(HannibalActividad != Terminando || HannibalOrdenManual >0 || otraMartinHannibal>0)
           {
            //Si no hay operaciones abiertas, se comprueba si se puede abrir una nueva operación, cada hora
            if(numeroOperacionesHannibal == 0 || (reEntradasHannibal && hazReentradaHannibal)) //nueva rutina
              {
               gestionarNuevaOperacionCoordinadoManualHannibal();
              }
           }

         if(numeroOperacionesHannibal > 0)
           {

            if(numeroOperacionesHannibal < MaxTrades_Hannibal && (puedeCoberturearHannibal || otraMartinHannibal==1))   //No es la primera operación de la martingala(no es la 0), y no supera el número máximo de operaciones permitidas.
              {
               hacerCoberturaHannibal();
              }

           }// fin if (numeroOperacionesHannibal > 0

        }// Fin de HannibalActivo

      if(HannibalAutoPriceAverage)
        {
         //actualiza el precio promedio de toda la martingla
         actualizarPrecioPromedioHannibal();
         //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
         actualizarTPHannibal();
        }

      HannibalCambioMinuto = tiempoMinuto;
     } //end if(HannibalCambioMinuto

   if(HannibalUseTrailingStop)
      TrailingAlls_Hannibal();


  }




//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void gestionarNuevaOperacionCoordinadoManualHannibal()
  {

   if(HannibalOrdenEnEstaVela)
      return;

   if(HayLimiteHorario(3))
      return;

   if(HannibalOrdenManual==1 || (HannibalTipoOperacion == Tipo_Sell && ((tendencia < -atrParcialValueH && atrParcialValueH >= 0) || (tendencia < -atrParcialValueH && tendencia > atrParcialValueH && atrParcialValueH <= 0))))  //SELL
     {

      if(tendencia < lastTendencia || HannibalTipoOperacion == Tipo_Manual || HannibalOrdenManual==1)//xx
        {
         resultadoAbirOrdenHannibal = OpenPendingOrder_Hannibal(1, lotajePrimeraHannibal, slipPage, Bid, 0, 0, comentarioHannibal + "-" + 0, MagicNumber_Hannibal, 0, HotPink);
         if(resultadoAbirOrdenHannibal <= 0)
           {
            int LE = GetLastError();
            if(LE != 0 && LE != 4051)
               Print("Error: ", LE);

           }
         else
           {
            lotajeSumTotal += lotajePrimeraHannibal;
            numSeries++;

            //guarda precio primera orden Hannibal
            if(reEntradasHannibal && countTradesHannibalVar == 0)
              {
               int ti = OrderSelect(resultadoAbirOrdenHannibal, SELECT_BY_TICKET, MODE_TRADES);
               if(ti)
                 {
                  precioPrimeraHannibal = OrderOpenPrice();
                 }
               else
                 {
                  precioPrimeraHannibal = Bid;
                 }
              }

            //actualiza el precio promedio de toda la martingla
            actualizarPrecioPromedioHannibal();
            //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
            actualizarTPHannibal();
           }
        }
     }
   if(HannibalOrdenManual==2 || (HannibalTipoOperacion == Tipo_Buy && ((tendencia > atrParcialValueH && atrParcialValueH >= 0) || (tendencia > atrParcialValueH && tendencia < -atrParcialValueH && atrParcialValueH <= 0)))) //BUY
     {

      if(tendencia > lastTendencia || HannibalTipoOperacion == Tipo_Manual || HannibalOrdenManual==2)//xx
        {
         //            resultadoAbirOrdenHannibal = OpenPendingOrder_Hannibal(0, HannibalLots, slipPage, Ask, 0, 0, comentarioHannibal + "-" + 0, MagicNumber_Hannibal, 0, Lime);
         resultadoAbirOrdenHannibal = OpenPendingOrder_Hannibal(0, lotajePrimeraHannibal, slipPage, Ask, 0, 0, comentarioHannibal + "-" + 0, MagicNumber_Hannibal, 0, Lime);
         if(resultadoAbirOrdenHannibal <= 0)
           {
            LE = GetLastError();
            if(LE != 0 && LE != 4051)
               Print("Error: ", LE);
           }
         else
           {
            numSeries++; // TEST
            lotajeSumTotal += lotajePrimeraHannibal;
            //guarda precio primera orden Hannibal
            if(reEntradasHannibal && countTradesHannibalVar == 0)
              {
               ti = OrderSelect(resultadoAbirOrdenHannibal, SELECT_BY_TICKET, MODE_TRADES);
               if(ti)
                 {
                  precioPrimeraHannibal = OrderOpenPrice();
                 }
               else
                 {
                  precioPrimeraHannibal = Bid;
                 }
              }
            //actualiza el precio promedio de toda la martingla
            actualizarPrecioPromedioHannibal();
            //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
            actualizarTPHannibal();
           }
        }
     } //cálculo para abrir Buy o Sell

   if(HannibalTipoOperacion == Tipo_Manual || HannibalTipoOperacion == Tipo_Tendencial || HannibalTipoOperacion == Tipo_AntiTenden || HannibalOrdenManual>0)//xx    //BUY o SELL a eleccion del trader
     {
      if(HannibalOrdenManual == 1 || (HannibalTipoOperacion == Tipo_Tendencial && ((tendencia < -atrParcialValueH && atrParcialValueH >= 0) || (tendencia < -atrParcialValueH && tendencia > atrParcialValueH && atrParcialValueH <= 0)) && permisoAbrirSymbolCaudillo(1)) || (HannibalTipoOperacion == Tipo_AntiTenden && ((tendencia > atrParcialValueH && atrParcialValueH >= 0) || (tendencia > atrParcialValueH && tendencia < -atrParcialValueH && atrParcialValueH <= 0)) && permisoAbrirSymbolCaudillo(1))) // Sell
        {
         if(tendencia < lastTendencia || HannibalTipoOperacion == Tipo_Manual || HannibalOrdenManual==1)//xx
           {
            //                resultadoAbirOrdenHannibal = OpenPendingOrder_Hannibal(1, HannibalLots, slipPage, Bid, 0, 0, comentarioHannibal + "-" + 0, MagicNumber_Hannibal, 0, HotPink);
            resultadoAbirOrdenHannibal = OpenPendingOrder_Hannibal(1, lotajePrimeraHannibal, slipPage, Bid, 0, 0, comentarioHannibal + "-" + 0, MagicNumber_Hannibal, 0, HotPink);
            if(resultadoAbirOrdenHannibal <= 0)
              {
               LE = GetLastError();
               if(LE != 0 && LE != 4051)
                  Print("Error: ", LE);
              }
            else
              {
               numSeries++; // TEST
               lotajeSumTotal += lotajePrimeraHannibal;
               //guarda precio primera orden Hannibal
               if(reEntradasHannibal && countTradesHannibalVar == 0)
                 {
                  ti = OrderSelect(resultadoAbirOrdenHannibal, SELECT_BY_TICKET, MODE_TRADES);
                  if(ti)
                    {
                     precioPrimeraHannibal = OrderOpenPrice();
                    }
                  else
                    {
                     precioPrimeraHannibal = Bid;
                    }
                 }

               HannibalOrdenManual = 0;
               otraMartinHannibal =0;
               //actualiza el precio promedio de toda la martingla
               actualizarPrecioPromedioHannibal();
               //Calcula y actualiza el TP de todas las operaciones de la martingala(Sólo lo hará si ha cambiado el TP)
               actualizarTPHannibal();
              }
           }
        }
      else
        {

         if(HannibalOrdenManual == 2 || (HannibalTipoOperacion == Tipo_Tendencial && ((tendencia > atrParcialValueH && atrParcialValueH >= 0) || (tendencia > atrParcialValueH && tendencia < -atrParcialValueH && atrParcialValueH <= 0)) && permisoAbrirSymbolCaudillo(0)) || (HannibalTipoOperacion == Tipo_AntiTenden && ((tendencia < -atrParcialValueH && atrParcialValueH >= 0) || (tendencia < -atrParcialValueH && tendencia > atrParcialValueH && atrParcialValueH <= 0)) && permisoAbrirSymbolCaudillo(0))) // Buy
           {
            if(tendencia > lastTendencia || HannibalTipoOperacion == Tipo_Manual || HannibalOrdenManual==2)//xx
              {
               resultadoAbirOrdenHannibal = OpenPendingOrder_Hannibal(0, lotajePrimeraHannibal, slipPage, Ask, 0, 0, comentarioHannibal + "-" + 0, MagicNumber_Hannibal, 0, Lime);
               if(resultadoAbirOrdenHannibal <= 0)
                 {
                  LE = GetLastError();
                  if(LE != 0 && LE != 4051)
                     Print("Error: ", LE);
                 }
               else
                 {
                  numSeries++; // TEST
                  lotajeSumTotal += lotajePrimeraHannibal;
                  //guarda precio primera orden Hannibal
                  if(reEntradasHannibal && countTradesHannibalVar == 0)
                    {
                     ti = OrderSelect(resultadoAbirOrdenHannibal, SELECT_BY_TICKET, MODE_TRADES);
                     if(ti)
                       {
                        precioPrimeraHannibal = OrderOpenPrice();
                       }
                     else
                       {
                        precioPrimeraHannibal = Bid;
                       }
                    }

                  HannibalOrdenManual = 0;
                  otraMartinHannibal =0;
                  actualizarPrecioPromedioHannibal();
                  actualizarTPHannibal();
                 }
              }
           }

        }


     }


  }



/************************************ Fin del Modo Coordinado  *********************************************/






/*Obtiene el número de operaciones abiertas del caudillo Caesar*/
int CountTrades_CaesarX()
  {
   int count_0 = 0;
   CaesarOperacionAbiertasSell = 0;
   CaesarOperacionAbiertasBuy = 0;
   flotanteOrdenMasAltaCaesar = 0.00;
   flotanteOrdenMasBajaCaesar = 0.00;
   precioOrdenMasAltaCaesar = 0.0;
   precioOrdenMasBajaCaesar = 0.00;
   prePrecioOrdenMasAltaCaesar = 0.00;
   prePrecioOrdenMasBajaCaesar = 0.00;
   int primerTicket = 999999;
   int incre3 = 10;
   int ordenes=OrdersTotal();
   bool primeraBaja=true;
   bool primeraAlta=true;

   for(int pos_4 = ordenes-1; pos_4 >= 0; pos_4--)
     {
      while(!OrderSelect(pos_4, SELECT_BY_POS, MODE_TRADES) && incre3>0)
        {
         incre3-=1;
        }

      if(!OrderSelect(pos_4, SELECT_BY_POS, MODE_TRADES) && incre3==0)
         continue;

      if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Caesar)
        {

         CaesarGlobalTP=OrderTakeProfit();

         // PRECIO PRIMERA----------------------------------
         if((OrderType()==OP_SELL || OrderType()==OP_BUY))   // (OrderTicket() <= primerTicket || primerTicket==999999)
           {
            precioPrimeraCaesar=OrderOpenPrice();
            primerTicket = OrderTicket();
            lotajePrimeraCaesar=OrderLots();
           }
         // ---------------------------------------------------

         // OPERACIÓN MÁS ALTA ----------------------------------
         if((OrderType()==OP_SELL || OrderType()==OP_BUY) && ((OrderOpenPrice() > precioOrdenMasAltaCaesar) || (primeraAlta)))
           {
            primeraAlta=false;
            ticketOrdenMasAltaCaesar=OrderTicket();
            flotanteOrdenMasAltaCaesar = OrderProfit() + OrderCommission() + OrderSwap();

            if(pos_4==0)
              {
               precioOrdenMasAltaCaesar=OrderOpenPrice();
               prePrecioOrdenMasAltaCaesar = precioOrdenMasAltaCaesar;
              }
            else
              {
               prePrecioOrdenMasAltaCaesar = precioOrdenMasAltaCaesar;
               precioOrdenMasAltaCaesar = OrderOpenPrice();
              }
           }
         // ---------------------------------------------------
         // OPERACIÓN MÁS BAJA-------------------------------
         if((OrderType()==OP_SELL || OrderType()==OP_BUY) && ((OrderOpenPrice() < precioOrdenMasBajaCaesar) || (primeraBaja)))
           {
            primeraBaja=false;
            ticketOrdenMasBajaCaesar=OrderTicket();
            flotanteOrdenMasBajaCaesar = OrderProfit() + OrderCommission() + OrderSwap();

            if(pos_4==0)
              {
               precioOrdenMasBajaCaesar=OrderOpenPrice();
               prePrecioOrdenMasBajaCaesar = precioOrdenMasBajaCaesar;
              }
            else
              {
               prePrecioOrdenMasBajaCaesar = precioOrdenMasBajaCaesar;
               precioOrdenMasBajaCaesar = OrderOpenPrice();
              }
           }
         // ---------------------------------------------------




         if(OrderType() == OP_SELL)
           {
            CaesarOperacionAbiertasSell++;
            count_0++;
           }
         if(OrderType() == OP_BUY)
           {
            CaesarOperacionAbiertasBuy++;
            count_0++;
           }
        }

     }


   if(count_0 < 1)
     {
      if(ObjectFind("TStart_Caesar") >= 0)
         ObjectDelete("TStart_Caesar");
      acumCicloJTime = TimeCurrent();
      acumCicloJ = 0;
      lotajePrimeraCaesar=CaesarLotsIni;
      caesarTPReEntVari=0;
     }
   if(count_0 < 2)
     {
      puedeCoberturearCaesar=True;
     }

   return (count_0);
  }




/*Cierra todas las operaciones de la martingala del caudillo Caesar.
  Esto lo utiliza el UseEquityStop cuando se llega al límite establecido de pérdida*/
void CloseThisSymbolAll_Caesar()
  {

   int ot=OrdersTotal() - 1;
   for(int pos_0 = ot; pos_0 >= 0; pos_0--)
     {
      cg = OrderSelect(pos_0, SELECT_BY_POS, MODE_TRADES);
      if(cg && OrderSymbol() == Symbol())
        {
         if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Caesar)
           {
            if(OrderType() == OP_BUY)
               cg = OrderClose(OrderTicket(), OrderLots(), Bid, slipPage, Blue);
            for(int z = 10; z >= 0; z--)
              {
               if(cg)
                  break;
               Sleep(100);
               cg=OrderClose(OrderTicket(), OrderLots(), Bid, slipPage, Blue);
              }
            if(OrderType() == OP_SELL)
               cg = OrderClose(OrderTicket(), OrderLots(), Ask, slipPage, Red);
            for(z = 10; z >= 0; z--)
              {
               if(cg)
                  break;
               Sleep(100);
               cg=OrderClose(OrderTicket(), OrderLots(), Ask, slipPage, Red);
              }
           }
         Sleep(30);
        }
     }

   countTradesCaesarVar = CountTrades_CaesarX();
   countTradesTotalParVar = CountTrades_TotalParX();

  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool FormacionNoValida(int caudillo, int tipoOrden, intTipoFormacion Formacion)
  {

   countTradesCaesarVar = CountTrades_CaesarX();
   countTradesAlexanderVar = CountTrades_AlexanderX();
   countTradesHannibalVar = CountTrades_HannibalX();
   countTradesCaptainVar = CountTrades_CapitanX();
   countTradesTotalParVar = CountTrades_TotalParX();

   if(countTradesTotalParVar == 0)
     {
      return false; // Permite Abrir
     }



   if(Formacion == Escuadron || Formacion == Comando) // Sólo 2 caudillos pueden ir en el mismo sentido
     {

      if(caudillo == MagicNumber_Caesar) //--------------------------------------------------------
        {
         if(tipoOrden == OP_BUY)
           {
            if(AlexanderOperacionAbiertasBuy > 0 && HannibalOperacionAbiertasBuy > 0)
              {
               return true; // Impide abrir
              }
           }
         else
           {
            if(AlexanderOperacionAbiertasSell > 0 && HannibalOperacionAbiertasSell > 0)
              {
               return true; // Impide abrir
              }
           }
        }

      if(caudillo == MagicNumber_Alexander) //--------------------------------------------------------
        {
         if(tipoOrden == OP_BUY)
           {
            if(CaesarOperacionAbiertasBuy > 0 && HannibalOperacionAbiertasBuy > 0)
              {
               return true; // Impide abrir
              }
           }
         else
           {
            if(CaesarOperacionAbiertasSell > 0 && HannibalOperacionAbiertasSell > 0)
              {
               return true; // Impide abrir
              }
           }
        }

      if(caudillo == MagicNumber_Hannibal) //--------------------------------------------------------
        {
         if(tipoOrden == OP_BUY)
           {
            if(AlexanderOperacionAbiertasBuy > 0 && CaesarOperacionAbiertasBuy > 0)
              {
               return true; // Impide abrir
              }
           }
         else
           {
            if(AlexanderOperacionAbiertasSell > 0 && CaesarOperacionAbiertasSell > 0)
              {
               return true; // Impide abrir
              }
           }
        }


     }
   if(Formacion == Comando) // Sólo 2 caudillos pueden ir en el mismo sentido y además van alternando y ademas el tercero solo abre en la misma dirección que el que menos martingalas lleve de los otros dos.
     {

      if(caudillo == MagicNumber_Caesar) //--------------------------------------------------------
        {
         int suma1 = AlexanderOperacionAbiertasBuy + HannibalOperacionAbiertasSell;
         int suma2 = AlexanderOperacionAbiertasSell + HannibalOperacionAbiertasBuy;
         if(tipoOrden == OP_BUY)
           {
            if((AlexanderOperacionAbiertasBuy >= HannibalOperacionAbiertasSell && suma1 != 0) ||
               (AlexanderOperacionAbiertasSell <= HannibalOperacionAbiertasBuy && suma2 != 0)
              )
              {
               return true; // Impide abrir
              }
           }
         else
           {
            if((AlexanderOperacionAbiertasBuy <= HannibalOperacionAbiertasSell && suma1 != 0) ||
               (AlexanderOperacionAbiertasSell >= HannibalOperacionAbiertasBuy && suma2 != 0))
              {
               return true; // Impide abrir
              }
           }
        }

      if(caudillo == MagicNumber_Alexander) //--------------------------------------------------------
        {
         suma1 = CaesarOperacionAbiertasBuy + HannibalOperacionAbiertasSell;
         suma2 = CaesarOperacionAbiertasSell + HannibalOperacionAbiertasBuy;
         if(tipoOrden == OP_BUY)
           {
            if((CaesarOperacionAbiertasBuy >= HannibalOperacionAbiertasSell && suma1 != 0) ||
               (CaesarOperacionAbiertasSell <= HannibalOperacionAbiertasBuy && suma2 != 0))
              {
               return true; // Impide abrir
              }
           }
         else
           {
            if((CaesarOperacionAbiertasBuy <= HannibalOperacionAbiertasSell && suma1 != 0) ||
               (CaesarOperacionAbiertasSell >= HannibalOperacionAbiertasBuy && suma2 != 0))
              {
               return true; // Impide abrir
              }
           }
        }

      if(caudillo == MagicNumber_Hannibal) //--------------------------------------------------------
        {
         suma1 = AlexanderOperacionAbiertasBuy + CaesarOperacionAbiertasSell;
         suma2 = AlexanderOperacionAbiertasSell + CaesarOperacionAbiertasBuy;
         if(tipoOrden == OP_BUY)
           {
            if((AlexanderOperacionAbiertasBuy >= CaesarOperacionAbiertasSell && suma1 != 0) ||
               (AlexanderOperacionAbiertasSell <= CaesarOperacionAbiertasBuy && suma2 != 0))
              {
               return true; // Impide abrir
              }
           }
         else
           {
            if((AlexanderOperacionAbiertasBuy <= CaesarOperacionAbiertasSell && suma1 != 0) ||
               (AlexanderOperacionAbiertasSell >= CaesarOperacionAbiertasBuy && suma2 != 0))
              {
               return true; // Impide abrir
              }
           }
        }
     }

   return false; // Puede abrir
  }




//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OpenManualOrder(int tipoOrdenM, double lotajeM, int slipPageM, double stopLossM, double takeProfitM, string comentM, int magicM, color colorM)
  {

   OrdenManual=0;


   int ticket = -1;
   int error = 0;
   int count = 0;
   int li = 10;

   if(lotajeM < 0.01)
      lotajeM = 0.01;
   lotajeM = NormalizeLotsSendOrder(lotajeM);

   switch(tipoOrdenM)
     {
      case 0:
         for(count = 0; count < li; count++)
           {
            RefreshRates();
            if(errorBotlidator == 1)
               return 0;

            double thisAsk=Ask;
            double thisBid=Bid;

            if(robotAcMan.ManuModo==1)
              {
               if(stopLossM>NormalizeDouble(0.00,Digits))
                 {
                  lotajeM=CalculaLotajeDesdeRiesgo(OP_BUY, thisAsk, thisBid);
                  if(robotAcMan.ManuPorcenTP>0)
                    {
                     takeProfitM=NormalizeDouble((thisAsk+(((thisAsk-stopLossM)/robotAcMan.ManuPorcenSL)*robotAcMan.ManuPorcenTP)), Digits);
                    }
                  else
                    {
                     takeProfitM=0;
                    }

                 }
               else
                 {
                  if(takeProfitM>NormalizeDouble(0.00,Digits))
                    {
                     lotajeM=CalculaLotajeDesdeBeneficio(OP_BUY, thisAsk, thisBid);
                     if(robotAcMan.ManuPorcenSL>0)
                       {
                        stopLossM=NormalizeDouble((thisAsk-(((takeProfitM-thisAsk)/robotAcMan.ManuPorcenTP)*robotAcMan.ManuPorcenSL)), Digits);
                       }
                     else
                       {
                        stopLossM=0;
                       }
                    }
                 }
              }


            ticket = OrderSend(Symbol(), OP_BUY, lotajeM, NormalizeDouble(thisAsk, Digits), slipPageM, stopLossM, takeProfitM, comentM, magicM,0, colorM);
            if(ticket >= 0)
              {
               robotAcMan.ManuValorSL=0;
               robotAcMan.ManuValorTP=0;
               break;
              }

            error = GetLastError();
            if(count_orders_account>199)
              {
               Print("AVISO: Número máximo de órdenes abiertas alcanzado.");
               break;
              }
            if(error == 134/* No Enough Money */)
               break;
            if(error == 0/* NO_ERROR */)
               break;
            if(error != 4/* SERVER_BUSY */ && error != 136/* OFF_QUOTES */)
               break;
            Sleep(50);

           }
         break;
      case 1:
         for(count = 0; count < li; count++)
           {
            if(errorBotlidator == 1)
               return 0;

            thisAsk=Ask;
            thisBid=Bid;

            if(robotAcMan.ManuModo==1)
              {
               if(stopLossM>NormalizeDouble(0.00,Digits))
                 {
                  lotajeM=CalculaLotajeDesdeRiesgo(OP_SELL, thisAsk, thisBid);

                  if(robotAcMan.ManuPorcenTP>0)
                    {
                     takeProfitM=NormalizeDouble((thisBid-(((stopLossM - thisBid)/robotAcMan.ManuPorcenSL)*robotAcMan.ManuPorcenTP)), Digits);
                    }
                  else
                    {
                     takeProfitM=0;
                    }

                 }
               else
                 {
                  if(takeProfitM>NormalizeDouble(0.00,Digits))
                    {
                     lotajeM=CalculaLotajeDesdeBeneficio(OP_SELL, thisAsk, thisBid);

                     if(robotAcMan.ManuPorcenSL>0)
                       {
                        stopLossM=NormalizeDouble((thisBid+(((thisBid - takeProfitM)/robotAcMan.ManuPorcenTP)*robotAcMan.ManuPorcenSL)), Digits);
                       }
                     else
                       {
                        stopLossM=0;
                       }


                    }
                 }

              }


            ticket = OrderSend(Symbol(), OP_SELL, lotajeM, NormalizeDouble(thisBid, Digits), slipPageM, stopLossM, takeProfitM, comentM, magicM, 0, colorM);
            if(ticket >= 0)
              {
               robotAcMan.ManuValorSL=0;
               robotAcMan.ManuValorTP=0;
               break;
              }
            error = GetLastError();
            if(count_orders_account>199)
              {
               Print("AVISO: Número máximo de órdenes abiertas alcanzado.");
               break;
              }
            if(error == 134/* No Enough Money */)
               break;
            if(error == 0/* NO_ERROR */)
               break;
            if(error != 4/* SERVER_BUSY */ && error != 136/* OFF_QUOTES */)
               break;
            Sleep(50);
           }
         break;
     }

   if(!IsOptimization() && ((IsVisualMode() && IsTesting()) || !IsTesting()))
     {
      GestionaPintadoFiltroLineas();
     }
   return (ticket);
  }






















//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OpenPendingOrder_Caesar(int tipoOrden, double a_lots_4, int CaesarSlipPage, double ad_unused_24, int ai_32, int ai_36, string a_comment_40, int a_magic_48, int a_datetime_52, color a_color_56)
  {



   if(FormacionNoValida(MagicNumber_Caesar, tipoOrden, tipoFormacion) && CaesarOrdenManual==0 && otraMartinCaesar==0)
     {
      CaesarOrdenManual=0;
      otraMartinCaesar=0;
      return -1;
     }


   string manualFlag="";
   if(CaesarOrdenManual>0 || otraMartinCaesar==1)
   {
      manualFlag="M";
   }
   else
   {
      if (bloqueoTemporal>TimeLocal())return -1;   
   }


   int ticket_60 = -1;
   int error_64 = 0;
   int count_68 = 0;
   int li_72 = 10;


   if(a_lots_4 < 0.01)
      a_lots_4 = 0.01;
   a_lots_4 = NormalizeLotsSendOrder(a_lots_4);
   if((spreadActual < maxSpread || !limiteSpread) && (a_lots_4 <= maxLotsCaesar))
     {
      switch(tipoOrden)
        {
         case 0:
            for(count_68 = 0; count_68 < li_72; count_68++)
              {
               RefreshRates();
               if(errorBotlidator == 1)
               {
                  return -1;
               }   
               ticket_60 = OrderSend(Symbol(), OP_BUY, a_lots_4, NormalizeDouble(Ask, Digits), CaesarSlipPage, StopLong(Bid, ai_32), TakeLong(Ask, ai_36), a_comment_40+manualFlag, a_magic_48, a_datetime_52,
                                     a_color_56);
               if(ticket_60 >= 0)
                 {
                  CaesarOrdenManual=0;
                  otraMartinCaesar=0;
                  lotajeSumTotal += a_lots_4;
                  CaesarOrdenEnEstaVela = true;
                  Botlidator(numRobot, false, -58); //3,4,7,8=mal //Llama a Botlidator despues de meter orden por si acaso la web esta caida y tarda en responder.
                  break;
                 }

               error_64 = GetLastError();
               if(count_orders_account>199)
                 {
                  Print("AVISO: Número máximo de órdenes abiertas alcanzado.");
                 }
               if(error_64 == 134/* No Enough Money */)
                  break;
               if(error_64 == 0/* NO_ERROR */)
                  break;
               if(error_64 != 4/* SERVER_BUSY */ && error_64 != 136/* OFF_QUOTES */)
                  break;
               Sleep(300);
              }
            break;
         case 1:
            for(count_68 = 0; count_68 < li_72; count_68++)
              {
               if(errorBotlidator == 1)
                  return -1;
               ticket_60 = OrderSend(Symbol(), OP_SELL, a_lots_4, NormalizeDouble(Bid, Digits), CaesarSlipPage, StopShort(Ask, ai_32), TakeShort(Bid, ai_36), a_comment_40+manualFlag, a_magic_48, a_datetime_52,
                                     a_color_56);
               if(ticket_60 >= 0)
                 {
                  CaesarOrdenManual=0;
                  otraMartinCaesar=0;
                  lotajeSumTotal += a_lots_4;
                  CaesarOrdenEnEstaVela = true;
                  Botlidator(numRobot, false, -6); //3,4,7,8=mal //Llama a Botlidator despues de meter orden por si acaso la web esta caida y tarda en responder.
                  break;
                 }
               error_64 = GetLastError();
               if(count_orders_account>199)
                 {
                  Print("AVISO: Número máximo de órdenes abiertas alcanzado.");
                 }
               if(error_64 == 134/* No Enough Money */)
                  break;
               if(error_64 == 0/* NO_ERROR */)
                  break;
               if(error_64 != 4/* SERVER_BUSY */ && error_64 != 136/* OFF_QUOTES */)
                  break;
               Sleep(300);
              }
              break;
        }
     }
   if(!IsOptimization() && ((IsVisualMode() && IsTesting()) || !IsTesting()))
     {
      GestionaPintadoFiltroLineas();
     }


   return (ticket_60);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double StopLong(double ad_0, int ai_8)
  {
   if(ai_8 == 0)
      return (0);
   return NormalizeDouble((ad_0 - ai_8 * Point), Digits);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double StopShort(double ad_0, int ai_8)
  {
   if(ai_8 == 0)
      return (0);
   return NormalizeDouble((ad_0 + ai_8 * Point), Digits);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double TakeLong(double ad_0, int ai_8)
  {
   if(ai_8 == 0)
      return (0);
   return NormalizeDouble((ad_0 + ai_8 * Point), Digits);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double TakeShort(double ad_0, int ai_8)
  {
   if(ai_8 == 0)
      return (0);
   return NormalizeDouble((ad_0 - ai_8 * Point), Digits);
  }

/*Obtiene el flotante de todas las operaciones abiertas del caudillo Caesar*/
double CalculateProfit_Caesar()
  {
   double flotante = 0;

   int ot=OrdersTotal() - 1;

   for(posicionOrden = ot; posicionOrden >= 0; posicionOrden--)
     {
      cg = OrderSelect(posicionOrden, SELECT_BY_POS, MODE_TRADES);
      if(cg && OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Caesar)
         if(OrderType() == OP_BUY || OrderType() == OP_SELL)
            flotante += OrderProfit();
     }
   return (flotante);
  }


/*Gestiona la operativa del Trailing Stop para el caudillo Caesar*/
void TrailingAlls_Caesar()
// Nueva función: Trailing Stop basado en Last Open Price
void TrailingAlls_ByLastOpenPrice(int caudillo) {
    // Variables locales
    int li_16;
    double price_28 = 0;
    double price_tstart = 0;
    int pos_36;
    datetime desde[3];
    datetime hasta[3];
    
    // Obtener parámetros según el caudillo (0=Caesar, 1=Alexander, 2=Hannibal)
    double trailStart, trailStop;
    int magicNumber;
    double lastOpenPrice;
    string objectNameTStart = "";
    string objectNameTStop = "";
    
    if(caudillo == 0) { // Caesar
        magicNumber = MagicNumber_Caesar;
        trailStart = CaesarTrailStart;
        trailStop = CaesarTrailStop;
        lastOpenPrice = lastOpenPriceCaesar[0];
        objectNameTStart = "TStart_Caesar";
        objectNameTStop = "TStop_Caesar";
    }
    else if(caudillo == 1) { // Alexander
        magicNumber = MagicNumber_Alexander;
        trailStart = AlexanderTrailStart;
        trailStop = AlexanderTrailStop;
        lastOpenPrice = lastOpenPriceAlexander[0];
        objectNameTStart = "TStart_Alexander";
        objectNameTStop = "TStop_Alexander";
    }
    else { // Hannibal
        magicNumber = MagicNumber_Hannibal;
        trailStart = HannibalTrailStart;
        trailStop = HannibalTrailStop;
        lastOpenPrice = lastOpenPriceHannibal[0];
        objectNameTStart = "TStart_Hannibal";
        objectNameTStop = "TStop_Hannibal";
    }
    
    // Verificar que trailStop esté habilitado
    if(trailStop == 0) return;
    
    // Calcular trailing basado en lastOpenPrice
    for(int i = 0; i < OrdersTotal(); i++) {
        if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
            if(OrderMagicNumber() == magicNumber && OrderSymbol() == Symbol()) {
                li_16 = (int)MathAbs(OrderOpenPrice() - lastOpenPrice) / Point;
                
                // Para posiciones BUY
                if(OrderType() == OP_BUY) {
                    // Precio de inicio del trailing
                    price_tstart = lastOpenPrice + trailStart * Point;
                    
                    if(li_16 < trailStart) {
                        ObjectSetString(0, objectNameTStart, OBJPROP_TEXT, 
                            "TrailingStart [" + IntegerToString(trailStart, 0) + "]");
                    }
                    
                    // Actualizar stop loss solo si el precio ha subido lo suficiente
                    if(Bid >= price_tstart) {
                        price_28 = NormalizeDouble(Bid - trailStop * Point, Digits);
                        if(OrderStopLoss() == 0 || price_28 > OrderStopLoss()) {
                            ObjectSetString(0, objectNameTStop, OBJPROP_TEXT, 
                                "TS: " + DoubleToStr(price_28, Digits));
                            OrderModify(OrderTicket(), OrderOpenPrice(), price_28, 
                                OrderTakeProfit(), 0, clrYellow);
                        }
                    }
                }
                // Para posiciones SELL
                else if(OrderType() == OP_SELL) {
                    // Precio de inicio del trailing
                    price_tstart = lastOpenPrice - trailStart * Point;
                    
                    if(li_16 < trailStart) {
                        ObjectSetString(0, objectNameTStart, OBJPROP_TEXT, 
                            "TrailingStart [" + IntegerToString(trailStart, 0) + "]");
                    }
                    
                    // Actualizar stop loss solo si el precio ha bajado lo suficiente
                    if(Ask <= price_tstart) {
                        price_28 = NormalizeDouble(Ask + trailStop * Point, Digits);
                        if(OrderStopLoss() == 0 || price_28 < OrderStopLoss() || OrderStopLoss() == 0) {
                            ObjectSetString(0, objectNameTStop, OBJPROP_TEXT, 
                                "TS: " + DoubleToStr(price_28, Digits));
                            OrderModify(OrderTicket(), OrderOpenPrice(), price_28, 
                                OrderTakeProfit(), 0, clrYellow);
                        }
                    }
                }
            }
        }
    }
}
  {

   if(numeroOperacionesCaesar < 1)
     {
      if(ObjectFind("TStart_Caesar") >= 0)
         ObjectDelete("TStart_Caesar");

      return;
     }

   if(promedioPrecioCaesar == 0.00)
     {
      //actualiza el precio promedio de toda la martingla
      actualizarPrecioPromedioCaesar();
     }

   if(promedioPrecioCaesar != 0.00)
     {
      int li_16;
      double order_stoploss_20;
      double price_28;
      if(CaesarTrailStop != 0)
        {
         int desde = OrdersTotal() - 1;
         int hasta = 0;
         for(int pos_36 = desde; pos_36 >= hasta; pos_36--)
           {
            if(OrderSelect(pos_36, SELECT_BY_POS, MODE_TRADES))
              {
               if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Caesar)
                 {
                  if(OrderType() == OP_BUY)
                    {
                     li_16 = NormalizeDouble((Bid - promedioPrecioCaesar) / Point, 0);
                     double price_tstart = promedioPrecioCaesar + CaesarTrailStart * Point;
                     if(li_16 < CaesarTrailStart)
                       {

                        if(selectedCaudillo==1 || selectedCaudillo==0 || selectedCaudillo>3)
                          {
                           if(ObjectFind("TStart_Caesar") < 0)
                             {
                              HLineCreate(0, "TStart_Caesar", 0, price_tstart, clrGray, 4, 1, true, false);
                              ObjectSetString(0, "TStart_Caesar", OBJPROP_TEXT, "TrailingStart Caesar [" + IntegerToString(CaesarTrailStart, 0) + "]");
                             }
                           else
                             {
                              HLineMove(0, "TStart_Caesar", price_tstart);
                              ObjectSetString(0, "TStart_Caesar", OBJPROP_TEXT, "TrailingStart Caesar [" + IntegerToString(CaesarTrailStart, 0) + "]");
                             }
                          }
                        continue;
                       }
                     else
                       {
                        if(ObjectFind("TStart_Caesar") >= 0)
                           ObjectDelete("TStart_Caesar");
                       }
                     price_28 = Bid - CaesarTrailStop * Point;
                     order_stoploss_20 = OrderStopLoss();
                     if(order_stoploss_20 == 0.0 || (order_stoploss_20 != 0.0 && price_28 > order_stoploss_20))
                        cg = OrderModify(OrderTicket(), promedioPrecioCaesar, price_28, OrderTakeProfit(), 0, Aqua);
                    }
                  if(OrderType() == OP_SELL)
                    {
                     li_16 = NormalizeDouble((promedioPrecioCaesar - Ask) / Point, 0);
                     price_tstart = promedioPrecioCaesar - CaesarTrailStart * Point;
                     if(li_16 < CaesarTrailStart)
                       {
                        if(selectedCaudillo==1 || selectedCaudillo==0 || selectedCaudillo>3)
                          {
                           if(ObjectFind("TStart_Caesar") < 0)
                             {
                              HLineCreate(0, "TStart_Caesar", 0, price_tstart, clrGray, 4, 1, true, false);
                              ObjectSetString(0, "TStart_Caesar", OBJPROP_TEXT, "TrailingStart Caesar [" + IntegerToString(CaesarTrailStart, 0) + "]");
                             }
                           else
                             {
                              HLineMove(0, "TStart_Caesar", price_tstart);
                              ObjectSetString(0, "TStart_Caesar", OBJPROP_TEXT, "TrailingStart Caesar [" + IntegerToString(CaesarTrailStart, 0) + "]");
                             }
                          }
                        continue;
                       }
                     else
                       {
                        if(ObjectFind("TStart_Caesar") >= 0)
                           ObjectDelete("TStart_Caesar");
                       }
                     price_28 = Ask + CaesarTrailStop * Point;
                     order_stoploss_20 = OrderStopLoss();
                     if(order_stoploss_20 == 0.0 || (order_stoploss_20 != 0.0 && price_28 < order_stoploss_20))
                        cg = OrderModify(OrderTicket(), promedioPrecioCaesar, price_28, OrderTakeProfit(), 0, Red);
                    }
                  Sleep(1000);
                  RefreshRates();
                 }
              }
           }
        }

      if(numeroOperacionesCaesar < 1)
        {
         if(ObjectFind("TStart_Caesar") >= 0)
            ObjectDelete("TStart_Caesar");

        }
     }//fin if(promedioPrecioCaesar
  }



/* Encuentra el precio de apertura de la última operación BUY del caudillo Caesar*/
double FindLastBuyPrice_Caesar()
  {
   double order_open_price_0;
   double priceActual;
   double priceAnterior = 999999.00;
   double priceAnterior2 = 0;

   int ot=OrdersTotal();

   for(int pos_24=0; pos_24<ot; pos_24++)
     {
      cg = OrderSelect(pos_24, SELECT_BY_POS, MODE_TRADES);
      if(cg && OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Caesar && OrderType() == OP_BUY)
        {
         priceActual = OrderOpenPrice();
         if(priceActual < priceAnterior)
           {
            order_open_price_0 = priceActual;
            priceAnterior = priceActual;
           }
         if(priceActual > priceAnterior2)
           {
            caesarPrimerPrecioOperacionBuy = priceActual;
            priceAnterior2 = priceActual;
           }
        }
     }
   return (order_open_price_0);
  }

/* Encuentra el precio de apertura de la última operación SELL del caudillo Caesar*/
double FindLastSellPrice_Caesar()
  {
   double order_open_price_0;
   double priceActual;
   double priceAnterior = 0;
   double priceAnterior2 = 999999;

   int ot=OrdersTotal();

   for(int pos_24=0; pos_24<ot; pos_24++)
     {
      cg = OrderSelect(pos_24, SELECT_BY_POS, MODE_TRADES);
      if(cg && OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Caesar && OrderType() == OP_SELL)
        {
         priceActual = OrderOpenPrice();
         if(priceActual > priceAnterior)
           {
            order_open_price_0 = priceActual;
            priceAnterior = priceActual;
           }
         if(priceActual < priceAnterior2)
           {
            caesarPrimerPrecioOperacionSell = priceActual;
            priceAnterior2 = priceActual;
           }
        }
     }
   return (order_open_price_0);
  }


/*Obtiene el número de operaciones abiertas del caudillo Alexander*/
int CountTrades_AlexanderX()
  {
   int count_0 = 0;
   AlexanderOperacionAbiertasSell = 0;
   AlexanderOperacionAbiertasBuy = 0;
   flotanteOrdenMasAltaAlexander = 0.00;
   flotanteOrdenMasBajaAlexander = 0.00;
   precioOrdenMasAltaAlexander = -0.001;
   precioOrdenMasBajaAlexander = -0.001;
   int primerTicket = 999999;
   double mayorProfit = -999999999.00;
   int incre3 = 10;
   int ordenes=OrdersTotal();
   bool primeraAlta=true;
   bool primeraBaja=true;


   for(int pos_4 = ordenes-1; pos_4 >= 0; pos_4--)
     {
      while(!OrderSelect(pos_4, SELECT_BY_POS, MODE_TRADES) && incre3>0)
        {
         incre3-=1;
        }

      if(!OrderSelect(pos_4, SELECT_BY_POS, MODE_TRADES) && incre3==0)
         continue;

      if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Alexander)
        {

         AlexanderGlobalTP=OrderTakeProfit();

         // PRECIO PRIMERA----------------------------------
         if((OrderType()==OP_SELL || OrderType()==OP_BUY))   // (OrderTicket() <= primerTicket || primerTicket==999999)
           {
            precioPrimeraAlexander=OrderOpenPrice();
            primerTicket = OrderTicket();
            lotajePrimeraAlexander=OrderLots();
           }


         // ---------------------------------------------------

         // OPERACIÓN MÁS ALTA ----------------------------------
         if((OrderType()==OP_SELL || OrderType()==OP_BUY) && ((OrderOpenPrice() > precioOrdenMasAltaAlexander) || (primeraAlta)))
           {
            primeraAlta=false;
            ticketOrdenMasAltaAlexander=OrderTicket();
            flotanteOrdenMasAltaAlexander = OrderProfit() + OrderCommission() + OrderSwap();

            if(precioOrdenMasAltaAlexander<=0)
               precioOrdenMasAltaAlexander=OrderOpenPrice();
            prePrecioOrdenMasAltaAlexander = precioOrdenMasAltaAlexander;
            precioOrdenMasAltaAlexander = OrderOpenPrice();
           }
         // ---------------------------------------------------
         // OPERACIÓN MÁS BAJA-------------------------------
         if((OrderType()==OP_SELL || OrderType()==OP_BUY) && ((OrderOpenPrice() < precioOrdenMasBajaAlexander) || (primeraBaja)))
           {
            primeraBaja=false;
            ticketOrdenMasBajaAlexander=OrderTicket();
            flotanteOrdenMasBajaAlexander = OrderProfit() + OrderCommission() + OrderSwap();

            if(precioOrdenMasBajaAlexander<=0)
               precioOrdenMasBajaAlexander=OrderOpenPrice();
            prePrecioOrdenMasBajaAlexander = precioOrdenMasBajaAlexander;
            precioOrdenMasBajaAlexander = OrderOpenPrice();
           }
         // ---------------------------------------------------





         if(OrderType() == OP_SELL)
           {
            AlexanderOperacionAbiertasSell++;
            count_0++;
           }
         if(OrderType() == OP_BUY)
           {
            AlexanderOperacionAbiertasBuy++;
            count_0++;
           }
        }

     }


   if(count_0 < 1)
     {
      if(ObjectFind("TStart_Alexander") >= 0)
         ObjectDelete("TStart_Alexander");
      acumCicloATime = TimeCurrent();
      acumCicloA = 0;
      lotajePrimeraAlexander=AlexanderLotsIni;
      alexanderTPReEntVari=0;
     }
   if(count_0 < 2)
      puedeCoberturearAlexander=True;

   return (count_0);
  }

/*Cierra todas las operaciones de la martingala del caudillo Alexander.
  Esto lo utiliza el UseEquityStop cuando se llega al límite establecido de pérdida*/
void CloseThisSymbolAll_Alexander()
  {

   int ot=OrdersTotal() - 1;

   for(int pos_0 = ot; pos_0 >= 0; pos_0--)
     {
      cg = OrderSelect(pos_0, SELECT_BY_POS, MODE_TRADES);
      if(cg && OrderSymbol() == Symbol())
        {
         if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Alexander)
           {
            if(OrderType() == OP_BUY)
               cg = OrderClose(OrderTicket(), OrderLots(), Bid, slipPage, Blue);
            for(int z = 10; z >= 0; z--)
              {
               if(cg)
                  break;
               Sleep(100);
               cg=OrderClose(OrderTicket(), OrderLots(), Bid, slipPage, Blue);
              }
            if(OrderType() == OP_SELL)
               cg = OrderClose(OrderTicket(), OrderLots(), Ask, slipPage, Red);
            for(z = 10; z >= 0; z--)
              {
               if(cg)
                  break;
               Sleep(100);
               cg=OrderClose(OrderTicket(), OrderLots(), Ask, slipPage, Red);
              }
           }
         Sleep(30);
        }
     }

   countTradesAlexanderVar = CountTrades_AlexanderX();
   countTradesTotalParVar = CountTrades_TotalParX();

  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OpenPendingOrder_Alexander(int tipoOrden, double a_lots_4, int AlexanderSlipPage, double ad_unused_24, int ai_32, int ai_36, string a_comment_40, int a_magic_48, int a_datetime_52, color a_color_56)
  {




   if(FormacionNoValida(MagicNumber_Alexander, tipoOrden, tipoFormacion) && AlexanderOrdenManual==0 && otraMartinAlexander==0)
     {
      AlexanderOrdenManual=0;
      otraMartinAlexander=0;
      return -1;
     }

   string manualFlag="";
   if(AlexanderOrdenManual>0 || otraMartinAlexander==1)
   {
      manualFlag="M";
   }
   else
   {
      if (bloqueoTemporal>TimeLocal())return -1;   
   }
      
      
   AlexanderOrdenManual=0;
   otraMartinAlexander=0;


   int ticket_60 = -1;
   int error_64 = 0;
   int count_68 = 0;
   int li_72 = 10;

   a_lots_4 = NormalizeLotsSendOrder(a_lots_4);
   if((spreadActual < maxSpread || !limiteSpread) && (a_lots_4 <= maxLotsAlexander))
     {
      switch(tipoOrden)
        {
         case 0:
            for(count_68 = 0; count_68 < li_72; count_68++)
              {
               RefreshRates();
               if(errorBotlidator == 1)
                  return 0;
               ticket_60 = OrderSend(Symbol(), OP_BUY, a_lots_4, NormalizeDouble(Ask, Digits), AlexanderSlipPage, StopLong(Bid, ai_32), TakeLong(Ask, ai_36), a_comment_40+manualFlag, a_magic_48, a_datetime_52, a_color_56);
               if(ticket_60 >= 0)
                 {
                  lotajeSumTotal += a_lots_4;
                  AlexanderOrdenEnEstaVela = true;
                  Botlidator(numRobot, false, -24); //3,4,7,8=mal //Llama a Botlidator despues de meter orden por si acaso la web esta caida y tarda en responder.
                  break;
                 }
               error_64 = GetLastError();
               if(count_orders_account>199)
                 {
                  Print("AVISO: Número máximo de órdenes abiertas alcanzado.");
                 }
               if(error_64 == 0/* NO_ERROR */)
                  break;
               if(error_64 == 134/* No Enough Money */)
                  break;
               if(error_64 != 4/* SERVER_BUSY */ && error_64 != 136/* OFF_QUOTES */)
                  break;
               Sleep(300);
              }
            break;
         case 1:
            for(count_68 = 0; count_68 < li_72; count_68++)
              {
               if(errorBotlidator == 1)
                  return 0;
               ticket_60 = OrderSend(Symbol(), OP_SELL, a_lots_4, NormalizeDouble(Bid, Digits), AlexanderSlipPage, StopShort(Ask, ai_32), TakeShort(Bid, ai_36), a_comment_40+manualFlag, a_magic_48, a_datetime_52,
                                     a_color_56);
               if(ticket_60 >= 0)
                 {
                  lotajeSumTotal += a_lots_4;
                  AlexanderOrdenEnEstaVela = true;
                  Botlidator(numRobot, false, -8); //3,4,7,8=mal //Llama a Botlidator despues de meter orden por si acaso la web esta caida y tarda en responder.
                  break;
                 }
               error_64 = GetLastError();
               if(count_orders_account>199)
                 {
                  Print("AVISO: Número máximo de órdenes abiertas alcanzado.");
                 }
               if(error_64 == 0/* NO_ERROR */)
                  break;
               if(error_64 == 134/* No Enough Money */)
                  break;
               if(error_64 != 4/* SERVER_BUSY */ && error_64 != 136/* OFF_QUOTES */)
                  break;
               Sleep(300);
              }
        }
     }
   if(!IsOptimization() && ((IsVisualMode() && IsTesting()) || !IsTesting()))
     {
      GestionaPintadoFiltroLineas();
     }


   return (ticket_60);
  }



/*Obtiene el flotante de las operaciones todas abiertas del caudillo Alexander*/
double CalculateProfit_Alexander()
  {
   double flotante = 0;

   int ot=OrdersTotal() - 1;

   for(alexanderPosition = ot; alexanderPosition >= 0; alexanderPosition--)
     {
      cg = OrderSelect(alexanderPosition, SELECT_BY_POS, MODE_TRADES);
      if(cg && OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Alexander)
         if(OrderType() == OP_BUY || OrderType() == OP_SELL)
            flotante += OrderProfit();
     }
   return (flotante);
  }


/*Gestiona la operativa del Trailing Stop para el caudillo Alexander*/
void TrailingAlls_Alexander()
  {

   if(numeroOperacionesAlexander < 1)
     {
      return;
     }

   if(promedioPrecioAlexander == 0.00)
     {
      //actualiza el precio promedio de toda la martingla
      actualizarPrecioPromedioAlexander();
     }


   if(promedioPrecioAlexander != 0.00)
     {
      int li_16;
      double order_stoploss_20;
      double price_28;
      if(AlexanderTrailStop != 0)
        {
         int desde = OrdersTotal() - 1;
         int hasta = 0;
         for(int pos_36 = desde; pos_36 >= hasta; pos_36--)
           {

            if(OrderSelect(pos_36, SELECT_BY_POS, MODE_TRADES))
              {
               if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Alexander)
                 {
                  if(OrderType() == OP_BUY)
                    {
                     li_16 = NormalizeDouble((Bid - promedioPrecioAlexander) / Point, 0);
                     double price_tstart = promedioPrecioAlexander + AlexanderTrailStart * Point;
                     if(li_16 < AlexanderTrailStart)
                       {
                        if(selectedCaudillo==2 || selectedCaudillo==0 || selectedCaudillo>3)
                          {
                           if(ObjectFind("TStart_Alexander") < 0)
                             {
                              HLineCreate(0, "TStart_Alexander", 0, price_tstart, clrGray, 4, 1, true, false);
                              ObjectSetString(0, "TStart_Alexander", OBJPROP_TEXT, "TrailingStart Alexander [" + IntegerToString(AlexanderTrailStart, 0) + "]");
                             }
                           else
                             {
                              HLineMove(0, "TStart_Alexander", price_tstart);
                              ObjectSetString(0, "TStart_Alexander", OBJPROP_TEXT, "TrailingStart Alexander [" + IntegerToString(AlexanderTrailStart, 0) + "]");
                             }
                          }
                        continue;
                       }
                     else
                       {
                        if(ObjectFind("TStart_Alexander") >= 0)
                           ObjectDelete("TStart_Alexander");
                       }

                     order_stoploss_20 = OrderStopLoss();
                     price_28 = Bid - AlexanderTrailStop * Point;
                     if(order_stoploss_20 == 0.0 || (order_stoploss_20 != 0.0 && price_28 > order_stoploss_20))
                        cg = OrderModify(OrderTicket(), promedioPrecioAlexander, price_28, OrderTakeProfit(), 0, Aqua);
                    }
                  if(OrderType() == OP_SELL)
                    {
                     li_16 = NormalizeDouble((promedioPrecioAlexander - Ask) / Point, 0);
                     price_tstart = promedioPrecioAlexander - AlexanderTrailStart * Point;
                     if(li_16 < AlexanderTrailStart)
                       {
                        if(selectedCaudillo==2 || selectedCaudillo==0 || selectedCaudillo>3)
                          {
                           if(ObjectFind("TStart_Alexander") < 0)
                             {
                              HLineCreate(0, "TStart_Alexander", 0, price_tstart, clrGray, 4, 1, true, false);
                              ObjectSetString(0, "TStart_Alexander", OBJPROP_TEXT, "TrailingStart Alexander [" + IntegerToString(AlexanderTrailStart, 0) + "]");
                             }
                           else
                             {
                              HLineMove(0, "TStart_Alexander", price_tstart);
                              ObjectSetString(0, "TStart_Alexander", OBJPROP_TEXT, "TrailingStart Alexander [" + IntegerToString(AlexanderTrailStart, 0) + "]");
                             }
                          }
                        continue;
                       }
                     else
                       {
                        if(ObjectFind("TStart_Alexander") >= 0)
                           ObjectDelete("TStart_Alexander");
                       }

                     order_stoploss_20 = OrderStopLoss();
                     price_28 = Ask + AlexanderTrailStop * Point;
                     if(order_stoploss_20 == 0.0 || (order_stoploss_20 != 0.0 && price_28 < order_stoploss_20))
                        cg = OrderModify(OrderTicket(), promedioPrecioAlexander, price_28, OrderTakeProfit(), 0, Red);
                    }
                  Sleep(1000);
                  RefreshRates();
                 }
              }
           }
        }

      if(numeroOperacionesAlexander < 1)
        {
         if(ObjectFind("TStart_Alexander") >= 0)
            ObjectDelete("TStart_Alexander");

        }
     } //fin if(promedioPrecioAlexander
  }



/* Encuentra el precio de apertura de la última operación BUY del caudillo Alexander*/
double FindLastBuyPrice_Alexander()
  {
   double order_open_price_0;
   double priceActual;
   double priceAnterior = 999999.00;
   double priceAnterior2 = 0;

   int ot=OrdersTotal();

   for(int pos_24=0; pos_24<ot; pos_24++)
     {
      cg = OrderSelect(pos_24, SELECT_BY_POS, MODE_TRADES);
      if(cg && OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Alexander && OrderType() == OP_BUY)
        {
         priceActual = OrderOpenPrice();
         if(priceActual < priceAnterior)
           {
            order_open_price_0 = priceActual;
            priceAnterior = priceActual;
           }
         if(priceActual > priceAnterior2)
           {
            alexanderPrimerPrecioOperacionBuy = priceActual;
            priceAnterior2 = priceActual;
           }
        }
     }
   return (order_open_price_0);
  }

/* Encuentra el precio de apertura de la última operación SELL del caudillo Alexander*/
double FindLastSellPrice_Alexander()
  {
   double order_open_price_0;
   double priceActual;
   double priceAnterior = 0;
   double priceAnterior2 = 999999;
   int ot=OrdersTotal();

   for(int pos_24=0; pos_24<ot; pos_24++)
     {
      cg = OrderSelect(pos_24, SELECT_BY_POS, MODE_TRADES);
      if(cg && OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Alexander && OrderType() == OP_SELL)
        {
         priceActual = OrderOpenPrice();
         if(priceActual > priceAnterior)
           {
            order_open_price_0 = priceActual;
            priceAnterior = priceActual;
           }
         if(priceActual < priceAnterior2)
           {
            alexanderPrimerPrecioOperacionSell = priceActual;
            priceAnterior2 = priceActual;
           }
        }
     }
   return (order_open_price_0);
  }






/*Obtiene el número de operaciones abiertas del caudillo Hannibal*/
int CountTrades_HannibalX()
  {
   int count_0 = 0;
   HannibalOperacionAbiertasSell = 0;
   HannibalOperacionAbiertasBuy = 0;
   flotanteOrdenMasAltaHannibal = 0.00;
   flotanteOrdenMasBajaHannibal = 0.00;
   precioOrdenMasAltaHannibal = -0.001;
   precioOrdenMasBajaHannibal = -0.001;
   int primerTicket = 999999;
   double mayorProfit = -999999999.00;
   int incre3 = 10;
   int ordenes=OrdersTotal();
   bool primeraAlta=true;
   bool primeraBaja=true;


   for(int pos_4 = ordenes-1; pos_4 >= 0; pos_4--)
     {
      while(!OrderSelect(pos_4, SELECT_BY_POS, MODE_TRADES) && incre3>0)
        {
         incre3-=1;
        }

      if(!OrderSelect(pos_4, SELECT_BY_POS, MODE_TRADES) && incre3==0)
         continue;

      if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Hannibal)
        {

         HannibalGlobalTP=OrderTakeProfit();

         // PRECIO PRIMERA----------------------------------
         if((OrderType()==OP_SELL || OrderType()==OP_BUY))   // (OrderTicket() <= primerTicket || primerTicket==999999)
           {
            precioPrimeraHannibal=OrderOpenPrice();
            primerTicket = OrderTicket();
            lotajePrimeraHannibal=OrderLots();
           }


         // ---------------------------------------------------

         // OPERACIÓN MÁS ALTA ----------------------------------
         if((OrderType()==OP_SELL || OrderType()==OP_BUY) && ((OrderOpenPrice() > precioOrdenMasAltaHannibal) || (primeraAlta)))
           {
            primeraAlta=false;
            ticketOrdenMasAltaHannibal=OrderTicket();
            flotanteOrdenMasAltaHannibal = OrderProfit() + OrderCommission() + OrderSwap();

            if(precioOrdenMasAltaHannibal<=0)
               precioOrdenMasAltaHannibal=OrderOpenPrice();
            prePrecioOrdenMasAltaHannibal = precioOrdenMasAltaHannibal;
            precioOrdenMasAltaHannibal = OrderOpenPrice();
           }
         // ---------------------------------------------------
         // OPERACIÓN MÁS BAJA-------------------------------
         if((OrderType()==OP_SELL || OrderType()==OP_BUY) && ((OrderOpenPrice() < precioOrdenMasBajaHannibal) || (primeraBaja)))
           {
            primeraBaja=false;
            ticketOrdenMasBajaHannibal=OrderTicket();
            flotanteOrdenMasBajaHannibal = OrderProfit() + OrderCommission() + OrderSwap();

            if(precioOrdenMasBajaHannibal<=0)
               precioOrdenMasBajaHannibal=OrderOpenPrice();
            prePrecioOrdenMasBajaHannibal = precioOrdenMasBajaHannibal;
            precioOrdenMasBajaHannibal = OrderOpenPrice();
           }
         // ---------------------------------------------------





         if(OrderType() == OP_SELL)
           {
            HannibalOperacionAbiertasSell++;
            count_0++;
           }
         if(OrderType() == OP_BUY)
           {
            HannibalOperacionAbiertasBuy++;
            count_0++;
           }
        }

     }


   if(count_0 < 1)
     {
      if(ObjectFind("TStart_Hannibal") >= 0)
         ObjectDelete("TStart_Hannibal");
      acumCicloHTime = TimeCurrent();
      acumCicloH = 0;
      lotajePrimeraHannibal=HannibalLotsIni;
      hannibalTPReEntVari=0;
     }
   if(count_0 < 2)
      puedeCoberturearHannibal=True;

   return (count_0);
  }


/*Cierra todas las operaciones de la martingala del caudillo Hannibal.
  Esto lo utiliza el UseEquityStop cuando se llega al límite establecido de pérdida*/
void CloseThisSymbolAll_Hannibal()
  {
   int ot=OrdersTotal() - 1;

   for(int pos_0 = ot; pos_0 >= 0; pos_0--)
     {
      cg = OrderSelect(pos_0, SELECT_BY_POS, MODE_TRADES);
      if(cg && OrderSymbol() == Symbol())
        {
         if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Hannibal)
           {
            if(OrderType() == OP_BUY)
               cg = OrderClose(OrderTicket(), OrderLots(), Bid, slipPage, Blue);
            for(int z = 10; z >= 0; z--)
              {
               if(cg)
                  break;
               Sleep(100);
               cg=OrderClose(OrderTicket(), OrderLots(), Bid, slipPage, Blue);
              }
            if(OrderType() == OP_SELL)
               cg = OrderClose(OrderTicket(), OrderLots(), Ask, slipPage, Red);
            for(z = 10; z >= 0; z--)
              {
               if(cg)
                  break;
               Sleep(100);
               cg=OrderClose(OrderTicket(), OrderLots(), Ask, slipPage, Red);
              }
           }
         Sleep(30);
        }
     }

   countTradesHannibalVar = CountTrades_HannibalX();
   countTradesTotalParVar = CountTrades_TotalParX();
  }




//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int RunScriptCountWindows()
  {
   int ChartNum=0;
   bool blnContinue = true;
   int intParent = GetParent(robotReturn.parHwnd);
//   int intParent = GetParent( WindowHandle( Symbol(), Period() ) );
   int intChild = GetWindow(intParent, GW_HWNDFIRST);

   if(intChild > 0)
     {
      if(intChild != intParent)
        {
        }
      ChartNum+=1;
     }
   else
      blnContinue = false;

   conta=0;
   while(blnContinue && conta<100)
     {
      intChild = GetWindow(intChild, GW_HWNDNEXT);

      if(intChild > 0)
        {
         if(intChild != intParent)
           {
           }
         ChartNum+=1;
        }
      else
         blnContinue = false;
      conta=conta+1;
     }

   return(ChartNum);
  }//end if (changeAll)

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void RunScriptCloseWindows()
  {

   bool blnContinue = true;
   int intParent = GetParent(robotReturn.parHwnd);
   int intChild = GetWindow(intParent, GW_HWNDFIRST);

   if(intChild > 0)
     {
      if(intChild != intParent)
        {
         PostMessageA(intChild, WM_CLOSE, 0, 0);
         Sleep(100);
        }
     }
   else
      blnContinue = false;

   conta=0;
   while(blnContinue && conta<100)
     {
      intChild = GetWindow(intChild, GW_HWNDNEXT);

      if(intChild > 0)
        {
         if(intChild != intParent)
           {
            PostMessageA(intChild, WM_CLOSE, 0, 0);
            Sleep(100);
           }
        }
      else
         blnContinue = false;
      conta=conta+1;   
     }
  }//end if (changeAll)





//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void RunScriptAndChangeTemplate(string templatePos)
  {

   bool blnContinue = true;
   int intParent = GetParent(WindowHandle(Symbol(), Period()));
   int intChild = GetWindow(intParent, GW_HWNDFIRST);
   int templateIndex=StrToInteger(templatePos);

   if(intChild > 0)
     {
      if(intChild != intParent)
        {
         SetActiveWindow(intChild);
         SetFocus(intChild);
         Sleep(50);
         PostMessageA(intChild, WM_COMMAND, 34800 + templateIndex, 0);
         Sleep(200);
         ChartRedraw();
        }
     }
   else
      blnContinue = false;

   conta=0;
   while(blnContinue && conta<100)
     {
      intChild = GetWindow(intChild, GW_HWNDNEXT);

      if(intChild > 0)
        {
         if(intChild != intParent)
           {
            SetActiveWindow(intChild);
            SetFocus(intChild);
            Sleep(50);
            PostMessageA(intChild, WM_COMMAND, 34800 + templateIndex, 0);
            Sleep(200);
            ChartRedraw();
           }
        }
      else
         blnContinue = false;
      conta=conta+1;
     }
  }//end if (changeAll)



//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OpenPendingOrder_Hannibal(int tipoOrden, double a_lots_4, int HannibalSlipPage, double ad_unused_24, int ai_32, int ai_36, string a_comment_40, int a_magic_48, int a_datetime_52, color a_color_56)
  {




   if(FormacionNoValida(MagicNumber_Hannibal, tipoOrden, tipoFormacion) && HannibalOrdenManual==0 && otraMartinHannibal==0)
     {
      HannibalOrdenManual=0;
      otraMartinHannibal=0;
      return -1;
     }

   string manualFlag="";
   if(HannibalOrdenManual>0 || otraMartinHannibal==1)
   {
      manualFlag="M";
   }
   else
   {
      if (bloqueoTemporal>TimeLocal())return -1;   
   }
      
   HannibalOrdenManual=0;
   otraMartinHannibal=0;


   int ticket_60 = -1;
   int error_64 = 0;
   int count_68 = 0;
   int li_72 = 10;

   a_lots_4 = NormalizeLotsSendOrder(a_lots_4);
   if((spreadActual < maxSpread || !limiteSpread) && (a_lots_4 <= maxLotsHannibal))
     {
      switch(tipoOrden)
        {
         case 0:
            for(count_68 = 0; count_68 < li_72; count_68++)
              {
               RefreshRates();
               if(errorBotlidator == 1)
                  return 0;
               ticket_60 = OrderSend(Symbol(), OP_BUY, a_lots_4, NormalizeDouble(Ask, Digits), HannibalSlipPage, StopLong(Bid, ai_32), TakeLong(Ask, ai_36), a_comment_40+manualFlag, a_magic_48, a_datetime_52, a_color_56);
               if(ticket_60 >= 0)
                 {
                  lotajeSumTotal += a_lots_4;
                  HannibalOrdenEnEstaVela = true;
                  Botlidator(numRobot, false, -99); //3,4,7,8=mal //Llama a Botlidator despues de meter orden por si acaso la web esta caida y tarda en responder.
                  break;
                 }
               error_64 = GetLastError();
               if(count_orders_account>199)
                 {
                  Print("AVISO: Número máximo de órdenes abiertas alcanzado.");
                 }
               if(error_64 == 0/* NO_ERROR */)
                  break;
               if(error_64 == 134/* No Enough Money */)
                  break;
               if(error_64 != 4/* SERVER_BUSY */ && error_64 != 136/* OFF_QUOTES */)
                  break;
               Sleep(300);
              }
            break;
         case 1:
            for(count_68 = 0; count_68 < li_72; count_68++)
              {
               if(errorBotlidator == 1)
                  return 0;
               ticket_60 = OrderSend(Symbol(), OP_SELL, a_lots_4, NormalizeDouble(Bid, Digits), HannibalSlipPage, StopShort(Ask, ai_32), TakeShort(Bid, ai_36), a_comment_40+manualFlag, a_magic_48, a_datetime_52,
                                     a_color_56);
               if(ticket_60 >= 0)
                 {
                  lotajeSumTotal += a_lots_4;
                  HannibalOrdenEnEstaVela = true;
                  Botlidator(numRobot, false, -83); //3,4,7,8=mal //Llama a Botlidator despues de meter orden por si acaso la web esta caida y tarda en responder.
                  break;
                 }
               error_64 = GetLastError();
               if(count_orders_account>199)
                 {
                  Print("AVISO: Número máximo de órdenes abiertas alcanzado.");
                 }
               if(error_64 == 0/* NO_ERROR */)
                  break;
               if(error_64 == 134/* No Enough Money */)
                  break;
               if(error_64 != 4/* SERVER_BUSY */ && error_64 != 136/* OFF_QUOTES */)
                  break;
               Sleep(300);
              }
        }
     }
   if(!IsOptimization() && ((IsVisualMode() && IsTesting()) || !IsTesting()))
     {
      GestionaPintadoFiltroLineas();
     }


   return (ticket_60);
  }



/*Obtiene el flotante de las operaciones todas abiertas del caudillo Hannibal*/
double CalculateProfit_Hannibal()
  {
   double flotante = 0;

   int ot=OrdersTotal() - 1;
   
   for(hannibalPosition = ot; hannibalPosition >= 0; hannibalPosition--)
     {
      cg = OrderSelect(hannibalPosition, SELECT_BY_POS, MODE_TRADES);

      if(cg && OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Hannibal)
         if(OrderType() == OP_BUY || OrderType() == OP_SELL)
            flotante += OrderProfit();
     }
   return (flotante);
  }

/*Gestiona la operativa del Trailing Stop para el caudillo Hannibal*/
void TrailingAlls_Hannibal()
  {

   if(numeroOperacionesHannibal < 1)
     {
      return;
     }

   if(promedioPrecioHannibal == 0.00)
     {
      //actualiza el precio promedio de toda la martingla
      actualizarPrecioPromedioHannibal();
     }


   if(promedioPrecioHannibal != 0.00)
     {
      int li_16;
      double order_stoploss_20;
      double price_28;
      if(HannibalTrailStop != 0)
        {
         int desde = OrdersTotal() - 1;
         int hasta = 0;
         for(int pos_36 = desde; pos_36 >= hasta; pos_36--)
           {
            if(OrderSelect(pos_36, SELECT_BY_POS, MODE_TRADES))
              {
               if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Hannibal)
                 {
                  if(OrderType() == OP_BUY)
                    {
                     li_16 = NormalizeDouble((Bid - promedioPrecioHannibal) / Point, 0);
                     double price_tstart = promedioPrecioHannibal + HannibalTrailStart * Point;
                     if(li_16 < HannibalTrailStart)
                       {
                        if(selectedCaudillo==3 || selectedCaudillo==0 || selectedCaudillo>3)
                          {
                           if(ObjectFind("TStart_Hannibal") < 0)
                             {
                              HLineCreate(0, "TStart_Hannibal", 0, price_tstart, clrGray, 4, 1, true, false);
                              ObjectSetString(0, "TStart_Hannibal", OBJPROP_TEXT, "TrailingStart Hannibal [" + IntegerToString(HannibalTrailStart, 0) + "]");
                             }
                           else
                             {
                              HLineMove(0, "TStart_Hannibal", price_tstart);
                              ObjectSetString(0, "TStart_Hannibal", OBJPROP_TEXT, "TrailingStart Hannibal [" + IntegerToString(HannibalTrailStart, 0) + "]");
                             }
                          }
                        continue;
                       }
                     else
                       {
                        if(ObjectFind("TStart_Hannibal") >= 0)
                           ObjectDelete("TStart_Hannibal");
                       }
                     order_stoploss_20 = OrderStopLoss();
                     price_28 = Bid - HannibalTrailStop * Point;
                     if(order_stoploss_20 == 0.0 || (order_stoploss_20 != 0.0 && price_28 > order_stoploss_20))
                        cg = OrderModify(OrderTicket(), promedioPrecioHannibal, price_28, OrderTakeProfit(), 0, Aqua);
                    }
                  if(OrderType() == OP_SELL)
                    {
                     li_16 = NormalizeDouble((promedioPrecioHannibal - Ask) / Point, 0);
                     price_tstart = promedioPrecioHannibal - HannibalTrailStart * Point;
                     if(li_16 < HannibalTrailStart)
                       {
                        if(selectedCaudillo==3 || selectedCaudillo==0 || selectedCaudillo>3)
                          {
                           if(ObjectFind("TStart_Hannibal") < 0)
                             {
                              HLineCreate(0, "TStart_Hannibal", 0, price_tstart, clrGray, 4, 1, true, false);
                              ObjectSetString(0, "TStart_Hannibal", OBJPROP_TEXT, "TrailingStart Hannibal [" + IntegerToString(HannibalTrailStart, 0) + "]");
                             }
                           else
                             {
                              HLineMove(0, "TStart_Hannibal", price_tstart);
                              ObjectSetString(0, "TStart_Hannibal", OBJPROP_TEXT, "TrailingStart Hannibal [" + IntegerToString(HannibalTrailStart, 0) + "]");
                             }
                          }
                        continue;
                       }
                     else
                       {
                        if(ObjectFind("TStart_Hannibal") >= 0)
                           ObjectDelete("TStart_Hannibal");
                       }
                     order_stoploss_20 = OrderStopLoss();
                     price_28 = Ask + HannibalTrailStop * Point;
                     if(order_stoploss_20 == 0.0 || (order_stoploss_20 != 0.0 && price_28 < order_stoploss_20))
                        cg = OrderModify(OrderTicket(), promedioPrecioHannibal, price_28, OrderTakeProfit(), 0, Red);
                    }
                  Sleep(1000);
                  RefreshRates();
                 }
              }
           }
        }

      if(numeroOperacionesHannibal < 1)
        {
         if(ObjectFind("TStart_Hannibal") >= 0)
            ObjectDelete("TStart_Hannibal");

        }
     } //fin if(promedioPrecioHannibal
  }



/* Encuentra el precio de apertura de la última operación BUY del caudillo Hannibal*/
double FindLastBuyPrice_Hannibal()
  {
   double order_open_price_0;
   double priceActual;
   double priceAnterior = 999999.00;
   double priceAnterior2 = 0;
   
   int ot=OrdersTotal();
   
   for(int pos_24=0; pos_24<ot; pos_24++)
     {
      cg = OrderSelect(pos_24, SELECT_BY_POS, MODE_TRADES);
      if(cg && OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Hannibal && OrderType() == OP_BUY)
        {
         priceActual = OrderOpenPrice();
         if(priceActual < priceAnterior)
           {
            order_open_price_0 = priceActual;
            priceAnterior = priceActual;
           }
         if(priceActual > priceAnterior2)
           {
            hannibalPrimerPrecioOperacionBuy = priceActual;
            priceAnterior2 = priceActual;
           }
        }
     }
   return (order_open_price_0);
  }

/* Encuentra el precio de apertura de la última operación SELL del caudillo Hannibal*/
double FindLastSellPrice_Hannibal()
  {
   double order_open_price_0;
   double priceActual;
   double priceAnterior = 0;
   double priceAnterior2 = 999999;
   
   int ot=OrdersTotal();
   
   for(int pos_24=0; pos_24<ot; pos_24++)
     {
      cg = OrderSelect(pos_24, SELECT_BY_POS, MODE_TRADES);
      if(cg && OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Hannibal && OrderType() == OP_SELL)
        {
         priceActual = OrderOpenPrice();
         if(priceActual > priceAnterior)
           {
            order_open_price_0 = priceActual;
            priceAnterior = priceActual;
           }
         if(priceActual < priceAnterior2)
           {
            hannibalPrimerPrecioOperacionSell = priceActual;
            priceAnterior2 = priceActual;
           }
        }
     }
   return (order_open_price_0);
  }


/*Obtiene el número de operaciones abiertas del en este par*/
int CountTrades_TotalParX()
  {

   int count_0 = 0;
   count_orders_account = 0;

   int ot=OrdersTotal() - 1;

   for(int pos_4 = ot; pos_4 >= 0; pos_4--)
     {
      if(OrderSelect(pos_4, SELECT_BY_POS, MODE_TRADES))
        {

         if(OrderType() == OP_SELL || OrderType() == OP_BUY)
            count_orders_account++;

         if(OrderSymbol() != Symbol())
            continue;
         if(OrderSymbol() == Symbol())
            if(OrderType() == OP_SELL || OrderType() == OP_BUY)
               count_0++;
        }

     }
   return (count_0);
  }


/*Obtiene el número de operaciones abiertas del caudillo Capitan(Operaciones Manuales)*/
int CountTrades_CapitanX()
  {
   ManualOperacionAbiertasSell=0;
   ManualOperacionAbiertasBuy=0;

   int count_0 = 0;
   flotanteOrdenMasAltaManual = 0.00;
   flotanteOrdenMasBajaManual = 0.00;
   precioOrdenMasAltaManual = -0.001;
   precioOrdenMasBajaManual = -0.001;
   double mayorProfit = -999999999.00;
   int incre3 = 10;
   int ordenes=OrdersTotal();


   for(int pos_4 = ordenes-1; pos_4 >= 0; pos_4--)
     {
      while(!OrderSelect(pos_4, SELECT_BY_POS, MODE_TRADES) && incre3>0)
        {
         incre3-=1;
        }

      if(OrderSymbol() == Symbol() && OrderMagicNumber() == 0)
        {

         // OPERACIÓN MÁS ALTA ----------------------------------
         if((OrderType()==OP_SELL || OrderType()==OP_BUY) && ((OrderOpenPrice() > precioOrdenMasAltaManual) || (precioOrdenMasAltaManual<=0)))
           {
            ticketOrdenMasAltaManual=OrderTicket();
            flotanteOrdenMasAltaManual = OrderProfit() + OrderCommission() + OrderSwap();

            if(precioOrdenMasAltaManual<=0)
               precioOrdenMasAltaManual=OrderOpenPrice();
            prePrecioOrdenMasAltaManual = precioOrdenMasAltaManual;
            precioOrdenMasAltaManual = OrderOpenPrice();
           }
         // ---------------------------------------------------
         // OPERACIÓN MÁS BAJA-------------------------------
         if((OrderType()==OP_SELL || OrderType()==OP_BUY) && ((OrderOpenPrice() < precioOrdenMasBajaManual) || (precioOrdenMasBajaManual<=0)))
           {
            ticketOrdenMasBajaManual=OrderTicket();
            flotanteOrdenMasBajaManual = OrderProfit() + OrderCommission() + OrderSwap();

            if(precioOrdenMasBajaManual<=0)
               precioOrdenMasBajaManual=OrderOpenPrice();
            prePrecioOrdenMasBajaManual = precioOrdenMasBajaManual;
            precioOrdenMasBajaManual = OrderOpenPrice();
           }
         // ---------------------------------------------------




         if(OrderType() == OP_SELL)
           {
            ManualOperacionAbiertasSell++;
            count_0++;
           }
         if(OrderType() == OP_BUY)
           {
            ManualOperacionAbiertasBuy++;
            count_0++;
           }
        }
     }
   if(ManualOperacionAbiertasBuy>0 && ManualOperacionAbiertasSell>0)
     {
      robotAcMan.ManuDirecOrders=3;
     }
   else
     {
      if(ManualOperacionAbiertasBuy>0)
        {
         robotAcMan.ManuDirecOrders=1;
        }
      else
        {
         if(ManualOperacionAbiertasSell>0)
           {
            robotAcMan.ManuDirecOrders=2;
           }
         else
           {
            robotAcMan.ManuDirecOrders=0; // SIN ÓRDENES
           }
        }
     }
   return (count_0);
  }


//*****************************************************************
// Obtiene el flotante de todo el par
double ProfitSymbol()
  {
   double Beneficio = 0;
   int a;

   int ot=OrdersTotal();

   for(int i = 0; i < ot; i ++)
     {
      a = OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if(a && OrderSymbol() == Symbol())
        {
         Beneficio += OrderProfit() + OrderCommission() + OrderSwap();
        }
     }
   return(Beneficio);
  }
//*****************************************************************

// Obtiene el flotante de cada caudillo en el par
double ProfitSymbolCaudillo(int caudillo)
  {

   double Beneficio = 0;
   int a;

   int ot=OrdersTotal();

   for(int i = 0; i < ot; i ++)
     {
      a = OrderSelect(i, SELECT_BY_POS, MODE_TRADES);

      if(a && OrderSymbol() == Symbol() && OrderMagicNumber() == caudillo)
        {
         Beneficio += OrderProfit() + OrderCommission() + OrderSwap();
        }
     }//for ( i
   return(Beneficio);
  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double calcularValorTick()
  {
   double valorTick = (((MarketInfo(Symbol(), MODE_TICKVALUE) * Point) / MarketInfo(Symbol(), MODE_TICKSIZE)));
   return(valorTick);
  }




//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double beneficioTPObjetivoSymbolCaudillo(int caudillo, int numOrder = -1)
  {

   double takeProfit = 0.0;
   double beneficioTPObjetivo = 0.0;
   string tipoOrden = "";

   beneficioTPObjetivoAcumulado = 0.0;
   int a;
   
   int ot=OrdersTotal();
   
   for(int i = 0; i < ot; i ++)
     {

      ticksRecorridos = 0.0;


      if(numOrder>=0)
        {
         a = OrderSelect(numOrder, SELECT_BY_TICKET, MODE_TRADES);
         if(!a)
            break;

        }
      else
        {
         a = OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
        }

      if(a && OrderSymbol() == Symbol() && OrderMagicNumber() == caudillo)
        {
         takeProfit = OrderTakeProfit();

         if(takeProfit != 0.0)
           {

            if(OrderType() == OP_BUY)
              {
               //precioActual = Ask;
               tipoOrden = "BUY";
               ticksRecorridos = takeProfit - OrderOpenPrice(); //El precio de apertura lo marca el Ask

              }
            else
              {
               //precioActual = Bid;
               tipoOrden = "SELL";
               ticksRecorridos = OrderOpenPrice() - takeProfit; //El precio de apertura lo marca el Bid

              }
           }
         ticksRecorridos = ticksRecorridos / MarketInfo(Symbol(), MODE_TICKSIZE);
         beneficioTPObjetivo = 0.0;

         beneficioTPObjetivo = ticksRecorridos * valorCadaTick * OrderLots();
         if(beneficioTPObjetivo != 0.0)
           {
            beneficioTPObjetivo += OrderCommission() + OrderSwap();
           }
         beneficioTPObjetivoAcumulado += beneficioTPObjetivo;
        }

      if(numOrder>=0)
        {
         break;
        }

     }//for ( i

   return(beneficioTPObjetivoAcumulado);
  }




//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double beneficioSLObjetivoSymbolCaudillo(int caudillo, int numOrder = -1)
  {

   double stopLoss = 0.0;
   double beneficioSLObjetivo = 0.0;
   string tipoOrden = "";

   beneficioSLObjetivoAcumulado = 0.0;

   int a;
   
   int ot=OrdersTotal();
   
   for(int i = 0; i < ot; i ++)
     {


      if(numOrder>=0)
        {
         a = OrderSelect(numOrder, SELECT_BY_TICKET, MODE_TRADES);
         if(!a)
            break;

        }
      else
        {
         a = OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
        }
      ticksRecorridos = 0.0;
      //Print(caudillo + " >> Orden " + tipoOrden +" " + i + " >> " + "beneficioSLObjetivoAcumulado Inicial: ", beneficioSLObjetivoAcumulado);


      if(a && OrderSymbol() == Symbol() && OrderMagicNumber() == caudillo)
        {
         stopLoss = OrderStopLoss();

         if(stopLoss != 0.0)
           {
            if(OrderType() == OP_BUY)
              {
               //precioActual = Ask;
               tipoOrden = "BUY";
               ticksRecorridos = stopLoss - OrderOpenPrice(); //El precio de apertura lo marca el Ask

              }
            else
              {
               //precioActual = Bid;
               tipoOrden = "SELL";
               ticksRecorridos = OrderOpenPrice() - stopLoss; //El precio de apertura lo marca el Bid

              }
           }
         ticksRecorridos = ticksRecorridos / MarketInfo(Symbol(), MODE_TICKSIZE);
         beneficioSLObjetivo = 0.0;

         beneficioSLObjetivo = ticksRecorridos * valorCadaTick * OrderLots();

         if(beneficioSLObjetivo != 0.0)
           {
            beneficioSLObjetivo += OrderCommission() + OrderSwap();
           }
         beneficioSLObjetivoAcumulado += beneficioSLObjetivo;
        }

      if(numOrder>=0)
        {
         break;
        }

     }//for ( i


   return(beneficioSLObjetivoAcumulado);
  }





//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double PrevioPorcen(double stopLoss, double lotaje, int tipoOrden)
{
double ticksRe=0.0;

         if( stopLoss != 0.0)
           {
            if(tipoOrden == OP_BUY)
              {
               ticksRe = stopLoss - Ask;

              }
            else
              {
               ticksRe = Bid - stopLoss;
              }
           }
         ticksRe = ticksRe / MarketInfo(Symbol(), MODE_TICKSIZE);


   return(NormalizeDouble(((ticksRe * valorCadaTick * lotaje)*100.00)/AccountBalance(),2));
}



//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CalculaRi(int tipoOrden, double lotaje, double priceSL, double thisAsk, double thisBid)
  {


   double cantidadTicks = 0.0;

   if(priceSL != 0.0)
     {
      if(tipoOrden == OP_BUY)
        {
         cantidadTicks = thisAsk - priceSL;
        }
      else
        {
         cantidadTicks = priceSL - thisBid;
        }
      cantidadTicks = cantidadTicks / MarketInfo(Symbol(), MODE_TICKSIZE);
     }

   return(cantidadTicks * valorCadaTick * lotaje);

  }



//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CalculaLotajeDesdeRiesgo(int tipoOP, double thisAsk, double thisBid)
  {

   double finalLot=0.01;
   double riesgoLimite;

   riesgoLimite=(robotAcMan.ManuPorcenSL*AccountBalance())/100.00;


   for(int i=1; i<1000000; i++)
     {
      double testLot=(0.01*(double)i);
      double perdida=MathAbs(CalculaRi(tipoOP,testLot,robotAcMan.ManuValorSL, thisAsk, thisBid));
      if(perdida<=riesgoLimite)
        {
         finalLot=testLot;
        }
      else
        {
         break;
        }
     }

   return finalLot;

  }






//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CalculaBe(int tipoOrden, double lotaje, double priceTP, double thisAsk, double thisBid)
  {


   double cantidadTicks = 0.0;

   if(priceTP != 0.0)
     {
      if(tipoOrden == OP_BUY)
        {
         cantidadTicks = priceTP - thisAsk;
        }
      else
        {
         cantidadTicks = thisBid - priceTP;
        }
      cantidadTicks = cantidadTicks / MarketInfo(Symbol(), MODE_TICKSIZE);
     }


   return(cantidadTicks * valorCadaTick * lotaje);

  }



//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CalculaLotajeDesdeBeneficio(int tipoOP, double thisAsk, double thisBid)
  {

   double finalLot=0.01;

   double beneficioLimite=(robotAcMan.ManuPorcenTP*AccountBalance())/100.0;

   for(int i=1; i<1000000; i++)
     {
      double testLot=(0.01*(double)i);
      double beneficio=MathAbs(CalculaBe(tipoOP,testLot,robotAcMan.ManuValorTP, thisAsk, thisBid));
      if(beneficio<=beneficioLimite)
        {
         finalLot=testLot;
        }
      else
        {
         break;
        }

     }

   return finalLot;

  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double comisionSymbolCaudillo(int caudillo)
  {

   double comision = 0.0;

   comisionAcumulado = 0.0;
   int a;
   
   int ot=OrdersTotal();
   
   for(int i = 0; i < ot; i ++)
     {
      a = OrderSelect(i, SELECT_BY_POS, MODE_TRADES);

      if(a && OrderSymbol() == Symbol() && OrderMagicNumber() == caudillo)
        {
         comision = OrderCommission();
         //Print(caudillo + " >> comision: " + comision);

         comisionAcumulado += comision;
         //Print(caudillo + " >> Orden " + tipoOrden +" " + i + " >> " + "comisionAcumulado: ", comisionAcumulado);

        }
     }//for ( i

   return(comisionAcumulado);
  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double swapSymbolCaudillo(int caudillo)
  {

   double swap = 0.0;

   swapAcumulado = 0.0;
   int a;
   
   int ot=OrdersTotal();

   for(int i = 0; i < ot; i ++)
     {
      a = OrderSelect(i, SELECT_BY_POS, MODE_TRADES);

      if(a && OrderSymbol() == Symbol() && OrderMagicNumber() == caudillo)
        {
         swap = OrderSwap();
         //Print(caudillo + " >> swap: " + swap);

         swapAcumulado += swap;
         //Print(caudillo + " >> Orden " + tipoOrden +" " + i + " >> " + "swapAcumulado: ", swapAcumulado);

        }
     }//for ( i

   return(swapAcumulado);
  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double lotajeSymbolCaudillo(int caudillo)
  {

   double lotaje = 0.0;

   lotajeAcumulado = 0.0;
   int a;
   
   int ot=OrdersTotal();
   
   for(int i = 0; i < ot; i ++)
     {
      a = OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      //No tendrá en cuenta las órdenes pendientes
      if(a && OrderSymbol() == Symbol() && OrderMagicNumber() == caudillo && OrderType() < 2)
        {
         lotaje = OrderLots();
         //Print(caudillo + " >> lotaje: " + lotaje);

         lotajeAcumulado += lotaje;
         //Print(caudillo + " >> Orden " + tipoOrden +" " + i + " >> " + "lotajeAcumulado: ", lotajeAcumulado);

        }
     }//for ( i

   return(lotajeAcumulado);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double lotajeTotalCuenta()
  {

   double lotaje = 0.0;

   lotajeTotalAcumulado = 0.0;
   lotajeTotalCuentaBuys = 0.0;
   lotajeTotalCuentaSells = 0.0;
   lotajeTotalParBuys = 0.0;
   lotajeTotalParSells = 0.0;
   bool parActual = false;
   int a;
   
   int ot=OrdersTotal();
   
   for(int i = 0; i < ot; i ++)
     {
      a = OrderSelect(i, SELECT_BY_POS, MODE_TRADES);

      // No tendrá en cuenta las ordenes pendientes
      if(a && OrderType() < 2)
        {
         if(OrderSymbol() == Symbol())
           {
            parActual = true;
           }
         else
           {
            parActual = false;
           }
         lotaje = OrderLots();
         if(OrderType() == OP_BUY)
           {
            lotajeTotalCuentaBuys += lotaje;
            if(parActual == true)
              {
               lotajeTotalParBuys += lotaje;
              }
           }
         else
           {
            lotajeTotalCuentaSells += lotaje;
            if(parActual == true)
              {
               lotajeTotalParSells += lotaje;
              }
           }
         //Print(" >> lotaje: " + lotaje);
        }

      //        lotajeTotalAcumulado += lotaje;
      //Print(" >> Orden " +  i + " >> " + "lotajeTotalAcumulado: ", lotajeTotalAcumulado);


     }//for ( i

   lotajeTotalAcumulado = lotajeTotalCuentaBuys + lotajeTotalCuentaSells;
   return(lotajeTotalAcumulado);
  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool permisoAbrirSymbolCaudillo(int tipoOrdenIntentaAbrir)
  {

// Inicialmente es true. Si el precio está separado a la distancia mínima, en todas las ordenes, se permitirá abir la nueva orden.
   bool permisoAbrir = true;
   double precioMinAperturaPorEncima = 0.0;
   double precioMinAperturaPorDebajo = 0.0;

   int a;
   
   int ot=OrdersTotal();
   
   for(int i = 0; i < ot; i ++)
     {
      a = OrderSelect(i, SELECT_BY_POS, MODE_TRADES);

      if(a && OrderSymbol() == Symbol())
        {
         // Si el tipo de orden que se intenta abrir es IGUAL que la orden a comprobar - 0 es Buy, 1 es Sell
         if(OrderType() == tipoOrdenIntentaAbrir)
           {

            //Print(tipoOrdenIntentaAbrir + " >> Orden " + OrderTicket() + " - Tipo: " + OrderType() +" " + i + " >> " + "precioAperturaOrden: ", OrderOpenPrice());

              //Tipo BUY
              if(OrderType() == OP_BUY)
              {
               precioMinAperturaPorEncima = OrderOpenPrice() + minSeparacionCaudillos * Point; //Se analiza el precio POR ENCIMA del de apertura más la separación minima
               precioMinAperturaPorDebajo = OrderOpenPrice() - minSeparacionCaudillos * Point; //Se analiza el precio POR DEBAJO del de apertura más la separación minima
               if(precioMinAperturaPorEncima > Ask && precioMinAperturaPorDebajo < Ask && limiteSeparacion)
                 {
                  permisoAbrir = false;
                  return(permisoAbrir);
                 }

              }
            else
              {
               //Tipo Sell
               precioMinAperturaPorDebajo = OrderOpenPrice() - minSeparacionCaudillos * Point; //Se analiza el precio POR DEBAJO del de apertura más la separación minima
               precioMinAperturaPorEncima = OrderOpenPrice() + minSeparacionCaudillos * Point; //Se analiza el precio POR ENCIMA del de apertura más la separación minima
               if(precioMinAperturaPorDebajo < Bid && precioMinAperturaPorEncima > Bid && limiteSeparacion)
                 {
                  permisoAbrir = false;
                  return(permisoAbrir);
                 }

              }//end if OrderType()

           } //end if tipoOrdenIntentaAbrir

        } //end if OrderSymbol()
     }//for ( i

   return(permisoAbrir);
  }




//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool permisoAbrirSymbolCaudilloMaximo2Iguales(int tipoOrdenIntentaAbrir)
  {

// Inicialmente es falso. Si hay menos de 2 operaciones del mismo tipo, se permitirá abir la nueva orden.
   bool permisoAbrir = false;
   int contBuys = 0;
   int contSells = 0;

   if(tipoOrdenIntentaAbrir == Tipo_Buy)
     {

      if(CaesarOperacionAbiertasBuy > 0)
         CaesarOperacionAbiertasBuy = 1;
      if(AlexanderOperacionAbiertasBuy > 0)
         AlexanderOperacionAbiertasBuy = 1;
      if(HannibalOperacionAbiertasBuy > 0)
         HannibalOperacionAbiertasBuy = 1;

      contBuys = CaesarOperacionAbiertasBuy + AlexanderOperacionAbiertasBuy + HannibalOperacionAbiertasBuy;

      if(contBuys < 2)
        {
         permisoAbrir = true;
        }

     }
   else
      if(tipoOrdenIntentaAbrir == Tipo_Sell)
        {

         if(CaesarOperacionAbiertasSell > 0)
            CaesarOperacionAbiertasSell = 1;
         if(AlexanderOperacionAbiertasSell > 0)
            AlexanderOperacionAbiertasSell = 1;
         if(HannibalOperacionAbiertasSell > 0)
            HannibalOperacionAbiertasSell = 1;

         contSells = CaesarOperacionAbiertasSell + AlexanderOperacionAbiertasSell + HannibalOperacionAbiertasSell;

         if(contSells < 2)
           {
            permisoAbrir = true;
           }
        }

   return(permisoAbrir);
  }




//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool permisoAbrirSymbolCaudilloMaximo1Iguales(int tipoOrdenIntentaAbrir)
  {

// Inicialmente es falso. Si no hay operaciones del mismo tipo, se permitirá abir la nueva orden.
   bool permisoAbrir = false;
   int contBuys = 0;
   int contSells = 0;


   if(tipoOrdenIntentaAbrir == Tipo_Buy)
     {

      if(CaesarOperacionAbiertasBuy > 0)
         CaesarOperacionAbiertasBuy = 1;
      if(AlexanderOperacionAbiertasBuy > 0)
         AlexanderOperacionAbiertasBuy = 1;
      if(HannibalOperacionAbiertasBuy > 0)
         HannibalOperacionAbiertasBuy = 1;

      contBuys = CaesarOperacionAbiertasBuy + AlexanderOperacionAbiertasBuy + HannibalOperacionAbiertasBuy;

      if(contBuys == 0)
        {
         permisoAbrir = true;
        }
     }
   else
      if(tipoOrdenIntentaAbrir == Tipo_Sell)
        {

         if(CaesarOperacionAbiertasSell > 0)
            CaesarOperacionAbiertasSell = 1;
         if(AlexanderOperacionAbiertasSell > 0)
            AlexanderOperacionAbiertasSell = 1;
         if(HannibalOperacionAbiertasSell > 0)
            HannibalOperacionAbiertasSell = 1;


         contSells = CaesarOperacionAbiertasSell + AlexanderOperacionAbiertasSell + HannibalOperacionAbiertasSell;

         if(contSells == 0)
           {
            permisoAbrir = true;
           }
        }

   return(permisoAbrir);
  }



//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int tipoOperacionCaudillo(int caudillo)
  {

   int varTipoOperacionCaudillo = -1; //BUY --> 0
//SELL --> 1

   int ot=OrdersTotal() - 1;

   for(int vPosicionOrden = ot; vPosicionOrden >= 0; vPosicionOrden--)
     {
      cg = OrderSelect(vPosicionOrden, SELECT_BY_POS, MODE_TRADES);

      if(cg && OrderSymbol() == Symbol() && OrderMagicNumber() == caudillo)
        {
         if(OrderType() == OP_BUY)
           {
            varTipoOperacionCaudillo = 0;
            return varTipoOperacionCaudillo;
            break;
           }
        }
      if(OrderSymbol() == Symbol() && OrderMagicNumber() == caudillo)
        {
         if(OrderType() == OP_SELL)
           {
            varTipoOperacionCaudillo = 1;
            return varTipoOperacionCaudillo;
            break;
           }
        }
     }//fin For
   return varTipoOperacionCaudillo;
  }



//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int ModificaOrdenManualSeleccionada(int TPoSL=1)
  {


   bool check;
   bool resultado;

   bool os=false;
   bool os2=false;
   bool os3=false;

   double modySL;
   double modyTP;

   datetime   lastOpenTime=0;
   string     guardaTickets="";
   int        selectedTicket=-1;

   int selec=0;
   int ordTot=OrdersTotal();



   for(int pos = ordTot; pos >= 0; pos--)
     {

      lastOpenTime=0;
      selectedTicket=-1;

      for(int pos2 = ordTot-1; pos2 >= 0; pos2--)
        {

         os2 = OrderSelect(pos2, SELECT_BY_POS, MODE_TRADES);
         if(OrderSymbol() != Symbol() || OrderMagicNumber() != 0 || !os2)
            continue;

         if(OrderOpenTime()>lastOpenTime && StringFind(guardaTickets,""+OrderTicket())<0)
           {
            lastOpenTime=OrderOpenTime();
            selectedTicket=OrderTicket();
           }
        }


      os3 = OrderSelect(selectedTicket, SELECT_BY_TICKET, MODE_TRADES);


      if(selectedTicket>=0 && os3)
        {

         guardaTickets+=" "+selectedTicket;


         selec+=1;
         if(robotAcMan.ManuNumOrder>0)
           {
            if((countTradesCaptainVar+1) - robotAcMan.ManuNumOrder!=selec)
               continue;
           }

         resultado = OrderSelect(selectedTicket, SELECT_BY_TICKET);

         if(TPoSL==1)  // Si debe cambiar el TP
           {
            modyTP=NormalizeDouble(mousePrice,Digits());
            modySL=OrderStopLoss();
           }
         else
           {
            modyTP=OrderTakeProfit();
            modySL=NormalizeDouble(mousePrice,Digits());
           }

         if(resultado)
           {
            if(!OrderModify(selectedTicket, OrderOpenPrice(),modySL, modyTP, 0))
              {
               for(int z = 10; z >= 0; z--)
                 {
                  Sleep(100);
                  check=!OrderModify(selectedTicket, OrderOpenPrice(),modySL, modyTP, 0);
                  if(check)
                     break;
                 }
              }
            RefreshRates();

           }

        }
     }


   return(0);
  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int cerrarOrdenManualSeleccionada()
  {

   bool check;
   bool resultado;

   bool os=false;
   bool os2=false;
   bool os3=false;

   datetime   lastOpenTime=0;
   string     guardaTickets="";
   int        selectedTicket=-1;

   int selec=0;
   int ordTot=OrdersTotal();



   for(int pos = ordTot; pos >= 0; pos--)
     {

      lastOpenTime=0;
      selectedTicket=-1;

      for(int pos2 = ordTot-1; pos2 >= 0; pos2--)
        {

         os2 = OrderSelect(pos2, SELECT_BY_POS, MODE_TRADES);
         if(OrderSymbol() != Symbol() || OrderMagicNumber() != 0 || !os2)
            continue;

         if(OrderOpenTime()>lastOpenTime && StringFind(guardaTickets,""+OrderTicket())<0)
           {
            lastOpenTime=OrderOpenTime();
            selectedTicket=OrderTicket();
           }
        }


      os3 = OrderSelect(selectedTicket, SELECT_BY_TICKET, MODE_TRADES);


      if(selectedTicket>=0 && os3)
        {

         guardaTickets+=" "+selectedTicket;


         selec+=1;
         if(robotAcMan.ManuNumOrder>0)
           {
            if((countTradesCaptainVar+1) - robotAcMan.ManuNumOrder!=selec)
               continue;
           }




         resultado = OrderSelect(selectedTicket, SELECT_BY_TICKET);
         if(resultado)
           {
            if(!OrderClose(selectedTicket, NormalizeLotsClose(robotAcMan.ManuOrderLotsPartial), OrderClosePrice(), 3, clrRed))
              {
               for(int z = 10; z >= 0; z--)
                 {
                  Sleep(100);
                  check=OrderClose(selectedTicket, NormalizeLotsClose(robotAcMan.ManuOrderLotsPartial), OrderClosePrice(), 4, clrRed);
                  if(check)
                     break;
                 }
              }
            RefreshRates();


           }



         for(int i=ObjectsTotal(ChartID()); i>=0; i--)
           {
            string name = ObjectName(ChartID(), i);
            if(StringSubstr(name,0,5) == "Order")
              {
               ObjectDelete(ChartID(), name);
              }
            if(StringSubstr(name,0,9) == "textabove")
              {
               ObjectDelete(ChartID(), name);
              }
            if(StringSubstr(name,1,9) == "BreakEven")
              {
               ObjectDelete(ChartID(), name);
              }
           }


         ChartRedraw();
        }
     }


   countTradesCaptainVar = CountTrades_CapitanX();
   countTradesTotalParVar = CountTrades_TotalParX();

   robotAcMan.ManuNumOrder=0;
   robotAcMan.ManuNumOrderEnEx4=0;

   return(0);
  }



//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int cerrarTodasOperacionesSymbolCaudillo(int caudillo)
  {

   bool resultado;

   int ot=OrdersTotal() - 1;

   for(int i = ot; i >= 0; i--)
     {
      resultado = OrderSelect(i, SELECT_BY_POS);
      if(resultado && OrderSymbol() == Symbol() && OrderMagicNumber() == caudillo)
        {
         cerrarUltimaOperacionSymbolCaudillo(caudillo);
        }
     }

   if(caudillo == MagicNumber_Caesar)
     {
      countTradesCaesarVar = CountTrades_CaesarX();
      CaesarOrdenManual = 0;
      otraMartinCaesar =0;
     }
   if(caudillo == MagicNumber_Alexander)
     {
      countTradesAlexanderVar = CountTrades_AlexanderX();
      AlexanderOrdenManual = 0;
      otraMartinAlexander =0;
     }
   if(caudillo == MagicNumber_Hannibal)
     {
      countTradesHannibalVar = CountTrades_HannibalX();
      HannibalOrdenManual = 0;
      otraMartinHannibal =0;
     }
   if(caudillo == 0)
      countTradesCaptainVar = CountTrades_CapitanX();

   countTradesTotalParVar = CountTrades_TotalParX();



   return(0);
  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int UltimoTicket(int caudillo)
  {
   int resultado=0;
   bool res=false;
   
   int ot=OrdersTotal() - 1;
   
   for(int i = ot; i >= 0; i--)
     {
      res = OrderSelect(i, SELECT_BY_POS);
      if((res==true) && (OrderSymbol() == Symbol()) && (OrderMagicNumber() == caudillo))
        {
         if(OrderTicket()>resultado)
           {
            resultado=OrderTicket();
           }
        }
     }
   return resultado;
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int cerrarTodasOperacionesCiclo(int caudillo, int typeOps=-1) // Desde últimas a primeras
  {

   bool check;
   bool resultado;
   int selectedTicket;
   double lastSelectedProfit;
   int lastSelectedOpenTime;
   int ultiTicket=UltimoTicket(caudillo);



   if(multiplicadorCierreParcialLocal>0.85) // Si no parciales, cierra primero las más perdedoras
     {

      int ot=OrdersTotal() - 1;

      for(int ii = ot; ii >= 0; ii--)
        {
         selectedTicket = -1;
         lastSelectedProfit = 9999999;

         for(int i = ot; i >= 0; i--)
           {
            resultado = OrderSelect(i, SELECT_BY_POS);
            if(resultado && OrderSymbol() == Symbol() && (OrderMagicNumber() == caudillo) && OrderTicket()<=ultiTicket && (OrderType()==typeOps || typeOps<0))
              {
               if(OrderProfit() < lastSelectedProfit)
                 {
                  lastSelectedProfit = OrderProfit();
                  selectedTicket = OrderTicket();
                 }
              }
           }
         if(selectedTicket >= 0)
           {
            resultado = OrderSelect(selectedTicket, SELECT_BY_TICKET);

            if(resultado)
              {
               if(!OrderClose(selectedTicket, OrderLots(), OrderClosePrice(), 3, clrRed))
                 {
                  for(int z = 10; z >= 0; z--)
                    {
                     Sleep(100);
                     check=OrderClose(selectedTicket, OrderLots(), OrderClosePrice(), 4, clrRed);
                     if(check)
                        break;
                    }
                 }
               Sleep(50);
               RefreshRates();


              }


           }
        }

     }
   else
     {
      bool esPrimeraOrden=true;
      
      ot=OrdersTotal() - 1;
      
      for(ii = ot; ii >= 0; ii--)
        {
         selectedTicket = -1;
         lastSelectedOpenTime = TimeCurrent();

         for(i = ot; i >= 0; i--)
           {
            resultado = OrderSelect(i, SELECT_BY_POS);
            if(resultado && OrderSymbol() == Symbol() && (OrderMagicNumber() == caudillo) && OrderTicket()<=ultiTicket)
              {
               if(OrderOpenTime() < lastSelectedOpenTime)
                 {
                  lastSelectedOpenTime = OrderOpenTime();
                  selectedTicket = OrderTicket();
                 }
              }
           }
         if(selectedTicket >= 0)
           {
            resultado = OrderSelect(selectedTicket, SELECT_BY_TICKET);

            if(resultado)
              {
               if(!OrderClose(selectedTicket, NormalizeLotsClose(OrderLots() * multiplicadorCierreParcialLocal), OrderClosePrice(), 3, clrRed))
                 {
                  for(z = 50; z >= 0; z--)
                    {
                     Sleep(100);
                     check=OrderClose(selectedTicket, NormalizeLotsClose(OrderLots() * multiplicadorCierreParcialLocal), OrderClosePrice(), 4, clrRed);
                     if(check)
                        break;
                    }
                 }

              }

            if(esPrimeraOrden)
              {
               esPrimeraOrden=false;
               double lotajePrimera=NormalizeLotsSendOrder(OrderLots()-(OrderLots() * multiplicadorCierreParcialLocal));
              }
            Sleep(50);
            RefreshRates();
           }
        }

     }





   if(caudillo == MagicNumber_Caesar)
     {
      countTradesCaesarVar = CountTrades_CaesarX();
      if(countTradesCaesarVar>0)
        {
         lotajePrimeraCaesar = lotajePrimera;;
        }
      else
        {
         lotajePrimeraCaesar = CaesarLots;
        }
      CaesarOrdenManual = 0;
      otraMartinCaesar =0;
     }
   if(caudillo == MagicNumber_Alexander)
     {
      countTradesAlexanderVar = CountTrades_AlexanderX();
      if(countTradesAlexanderVar>0)
        {
         lotajePrimeraAlexander = lotajePrimera;;
        }
      else
        {
         lotajePrimeraAlexander = AlexanderLots;
        }
      AlexanderOrdenManual = 0;
      otraMartinAlexander =0;
     }
   if(caudillo == MagicNumber_Hannibal)
     {
      countTradesHannibalVar = CountTrades_HannibalX();
      if(countTradesHannibalVar>0)
        {
         lotajePrimeraHannibal = lotajePrimera;;
        }
      else
        {
         lotajePrimeraHannibal = HannibalLots;
        }
      HannibalOrdenManual = 0;
      otraMartinHannibal =0;
     }

   if(caudillo == 0)
      countTradesCaptainVar = CountTrades_CapitanX();

   countTradesTotalParVar = CountTrades_TotalParX();
   return(0);
  }




//+------------------------------------------------------------------+
//|                          OJO NO USADA                                        |
//+------------------------------------------------------------------+
int cerrarTodasOperacionesCicloOrdenProfit(int caudillo)
  {

   bool check;
   bool resultado;
   int selectedTicket;
   int lastSelectedProfit;
   int ultiTicket=UltimoTicket(caudillo);

   int ot=OrdersTotal() - 1;

   for(int ii = ot; ii >= 0; ii--)
     {
      selectedTicket = -1;
      lastSelectedProfit = 9999999;

      for(int i = ot; i >= 0; i--)
        {
         resultado = OrderSelect(i, SELECT_BY_POS);
         if(resultado && OrderSymbol() == Symbol() && (OrderMagicNumber() == caudillo) && OrderTicket()<=ultiTicket)
           {

            if(OrderProfit() < lastSelectedProfit)
              {
               lastSelectedProfit = OrderProfit();
               selectedTicket = OrderTicket();
              }

           }
        }
      if(selectedTicket >= 0)
        {
         resultado = OrderSelect(selectedTicket, SELECT_BY_TICKET);
         if(resultado && !OrderClose(selectedTicket, NormalizeLotsClose(OrderLots() * multiplicadorCierreParcialLocal), OrderClosePrice(), 10, clrRed))
           {
            Sleep(800);
            check=OrderClose(selectedTicket, NormalizeLotsClose(OrderLots() * multiplicadorCierreParcialLocal), OrderClosePrice(), 10, clrRed);
           }
         Sleep(200);
         RefreshRates();
        }
     }




   if(caudillo == MagicNumber_Caesar)
     {
      countTradesCaesarVar = CountTrades_CaesarX();
      CaesarOrdenManual = 0;
      otraMartinCaesar =0;
     }
   if(caudillo == MagicNumber_Alexander)
     {
      countTradesAlexanderVar = CountTrades_AlexanderX();
      AlexanderOrdenManual = 0;
      otraMartinAlexander =0;
     }
   if(caudillo == MagicNumber_Hannibal)
     {
      countTradesHannibalVar = CountTrades_HannibalX();
      HannibalOrdenManual = 0;
      otraMartinHannibal =0;
     }

   if(caudillo == 0)
      countTradesCaptainVar = CountTrades_CapitanX();

   countTradesTotalParVar = CountTrades_TotalParX();
   return(0);
  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int cerrarTodasOperacionesPar()
  {


   bool check;
   bool resultado;
   int selectedTicket;
   int lastSelectedProfit = 9999999;

   int ot=OrdersTotal() - 1;

   for(int ii = ot; ii >= 0; ii--)
     {
      selectedTicket = -1;
      lastSelectedProfit = 9999999;

      for(int i = ot; i >= 0; i--)
        {
         resultado = OrderSelect(i, SELECT_BY_POS);
         if(resultado && OrderSymbol() == Symbol() && (OrderMagicNumber() == MagicNumber_Caesar || OrderMagicNumber() == MagicNumber_Alexander || OrderMagicNumber() == MagicNumber_Hannibal || OrderMagicNumber() == 0))
           {
            if(OrderProfit() < lastSelectedProfit)
              {
               lastSelectedProfit = OrderProfit();
               selectedTicket = OrderTicket();
              }
           }
        }
      if(selectedTicket >= 0)
        {
         resultado = OrderSelect(selectedTicket, SELECT_BY_TICKET);

         if(resultado)
           {
            if(!OrderClose(selectedTicket, OrderLots(), OrderClosePrice(), 3, clrRed))
              {
               for(int z = 10; z >= 0; z--)
                 {
                  Sleep(100);
                  check=OrderClose(selectedTicket, OrderLots(), OrderClosePrice(), 4, clrRed);
                  if(check)
                     break;
                 }
              }
            Sleep(50);
            RefreshRates();


           }

        }
     }

   countTradesCaesarVar = CountTrades_CaesarX();
   countTradesAlexanderVar = CountTrades_AlexanderX();
   countTradesHannibalVar = CountTrades_HannibalX();
   countTradesCaptainVar = CountTrades_CapitanX();
   countTradesTotalParVar = CountTrades_TotalParX();

   CaesarOrdenManual = 0;
   AlexanderOrdenManual = 0;
   HannibalOrdenManual = 0;
   otraMartinCaesar =0;
   otraMartinAlexander =0;
   otraMartinHannibal =0;

   return(0);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int cerrarTodasOperacionesCuenta()
  {


   bool check;
   bool resultado;
   int selectedTicket;
   int lastSelectedProfit = 9999999;

   int ot=OrdersTotal() - 1;

   for(int ii = ot; ii >= 0; ii--)
     {
      selectedTicket = -1;
      lastSelectedProfit = 9999999;

      for(int i = ot; i >= 0; i--)
        {
         resultado = OrderSelect(i, SELECT_BY_POS);
         if(resultado)
           {
            if(OrderProfit() < lastSelectedProfit)
              {
               lastSelectedProfit = OrderProfit();
               selectedTicket = OrderTicket();
              }
           }
        }
      if(selectedTicket >= 0)
        {
         resultado = OrderSelect(selectedTicket, SELECT_BY_TICKET);

         if(resultado)
           {
            if(!OrderClose(selectedTicket, OrderLots(), OrderClosePrice(), 3, clrRed))
              {
               for(int z = 10; z >= 0; z--)
                 {
                  Sleep(100);
                  check=OrderClose(selectedTicket, OrderLots(), OrderClosePrice(), 5, clrRed);
                  if(check)
                     break;
                 }
              }
            Sleep(50);
            RefreshRates();

           }

        }
     }

   countTradesCaesarVar = CountTrades_CaesarX();
   countTradesAlexanderVar = CountTrades_AlexanderX();
   countTradesHannibalVar = CountTrades_HannibalX();
   countTradesCaptainVar = CountTrades_CapitanX();
   countTradesTotalParVar = CountTrades_TotalParX();

   CaesarOrdenManual = 0;
   AlexanderOrdenManual = 0;
   HannibalOrdenManual = 0;
   otraMartinCaesar =0;
   otraMartinAlexander =0;
   otraMartinHannibal =0;
   
   bloqueoTemporal=TimeLocal()+50;
   GlobalVariableSet("inicio",(double)TimeLocal());   

   return(0);
  }




//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void sacaTipoOpsCaudillos()
  {


   opCaesar = -1; // NOOP
   opAlexander = -1; // NOOP
   opHannibal = -1; // NOOP

   int resultado;
   
   int ot=OrdersTotal() - 1;
   
   for(int i = ot; i >= 0; i--)
     {
      resultado = OrderSelect(i, SELECT_BY_POS);
      if(resultado)
        {
         // JULIUS //////////////////////////////////////////////////////////////////////
         if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Caesar)
           {
            if(OrderType() == OP_BUY)
              {
               opCaesar = OP_BUY;
              }
            else
               if(OrderType() == OP_SELL)
                 {
                  opCaesar = OP_SELL;
                 }
           }

         // ALEX //////////////////////////////////////////////////////////////////////
         if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Alexander)
           {
            if(OrderType() == OP_BUY)
              {
               opAlexander = OP_BUY;
              }
            else
               if(OrderType() == OP_SELL)
                 {
                  opAlexander = OP_SELL;
                 }
           }

         // HANNIBAL //////////////////////////////////////////////////////////////////
         if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber_Hannibal)
           {
            if(OrderType() == OP_BUY)
              {
               opHannibal = OP_BUY;
              }
            else
               if(OrderType() == OP_SELL)
                 {
                  opHannibal = OP_SELL;
                 }
           }
        }
     }
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double ProfitActualTotal()
  {


   if( ((IsTesting() && !IsVisualMode()) || IsOptimization()) && (!TrailingGAActivo) )return (0);

   double profit = 0.0;

   profitAcumulado = 0.0;
   profitAcumuladoMonth = 0.0;

   acumCicloJ = 0.0;
   acumCicloA = 0.0;
   acumCicloH = 0.0;

   ingreso = 0.0;
   retiro = 0.0;

   datetime tiempoActual=TimeCurrent();
   datetime tiempoLocalActual=TimeLocal();
   static datetime tiempoElapsed=-1;


   if(tiempoActual==tiempoElapsed)
     {
      if(tiempoLocalActual>tiempoLocalElapsed)
        {
         return(0.00);
        }
     }
   else
     {
      tiempoElapsed=TimeCurrent();
      tiempoLocalElapsed=TimeLocal()+(60*60);
     }


   int a;
   
   int oht=OrdersHistoryTotal() - 1;
   
   for(int i = oht; i >= 0; i--)
     {
      a = OrderSelect(i, SELECT_BY_POS, MODE_HISTORY);
      if(a)
        {
         if(OrderType() < 6)    //Si no es de tipo balance
           {
           
            if(TimeToStr(TimeCurrent(), TIME_DATE) == TimeToStr(OrderCloseTime(), TIME_DATE))
              {
           
               profit = OrderProfit() + OrderCommission() + OrderSwap();
   
               if(OrderMagicNumber() == MagicNumber_Caesar && OrderSymbol() == Symbol() && OrderCloseTime() > acumCicloJTime)
                 {
                  acumCicloJ += profit;
                 }
               if(OrderMagicNumber() == MagicNumber_Alexander && OrderSymbol() == Symbol() && OrderCloseTime() > acumCicloATime)
                 {
                  acumCicloA += profit;
                 }
               if(OrderMagicNumber() == MagicNumber_Hannibal && OrderSymbol() == Symbol() && OrderCloseTime() > acumCicloHTime)
                 {
                  acumCicloH += profit;
                 }

                  profitAcumulado += profit;
              }
            else
              {
               continue;
              }




           }//if(OrderType
         else
           {
            if(OrderType() == 6) // && (TimeToStr(TimeCurrent(), TIME_DATE) == TimeToStr(OrderCloseTime(), TIME_DATE)) )
              {
               profit = OrderProfit();
               if(profit > 0)
                 {
                  ingreso += profit;
                 }
               if(profit < 0)
                 {
                  retiro += profit;
                 }
              }
           }
        }// orderselect
     }//for ( i

   return(profitAcumulado);
  }



//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double porcentajeFlotanteCuenta(double flotante)
  {
   double porcentaje = 0;
   double balanceCuenta = AccountBalance();
   if(flotante != 0 && balanceCuenta != 0)
     {
      porcentaje = (flotante * 100) / balanceCuenta;
     }

   return(porcentaje);
  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void comprobarActividadCaudillos()
  {

   if(CaesarActividad == Encendido && (errorBotlidator==0 && limiteConexError==0))
     {
      CaesarActivo = true;
     }
   else
      if(CaesarActividad == Apagado)
        {
         CaesarActivo = false;
        }
      else
         if(CaesarActividad == Terminando || (errorBotlidator==1 || limiteConexError==1))
           {

            numeroOperacionesCaesar = countTradesCaesarVar;
            if(CaesarOperacionAbiertasBuy == 0 && CaesarOperacionAbiertasSell == 0)
              {
               CaesarActivo = false;
              }
            else
              {
               CaesarActivo = true;
              }

           }

   if(AlexanderActividad == Encendido && (errorBotlidator==0 && limiteConexError==0))
     {
      AlexanderActivo = true;
     }
   else
      if(AlexanderActividad == Apagado)
        {
         AlexanderActivo = false;
        }
      else
         if(AlexanderActividad == Terminando || (errorBotlidator==1 || limiteConexError==1))
           {

            numeroOperacionesAlexander = countTradesAlexanderVar;
            if(AlexanderOperacionAbiertasBuy == 0 && AlexanderOperacionAbiertasSell == 0)
              {
               AlexanderActivo = false;
              }
            else
              {
               AlexanderActivo = true;
              }

           }

   if(HannibalActividad == Encendido && (errorBotlidator==0 && limiteConexError==0))
     {
      HannibalActivo = true;
     }
   else
      if(HannibalActividad == Apagado)
        {
         HannibalActivo = false;
        }
      else
         if(HannibalActividad == Terminando || (errorBotlidator==1 || limiteConexError==1))
           {

            numeroOperacionesHannibal = countTradesHannibalVar;
            if(HannibalOperacionAbiertasBuy == 0 && HannibalOperacionAbiertasSell == 0)
              {
               HannibalActivo = false;
              }
            else
              {
               HannibalActivo = true;
              }

           }

  }





//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void gestionarLacertaCauda()
  {
   bool debeCerrar=false;     

   flotanteLacertaCauda = (NormalizeDouble(lacertaCaudaFlotante, 2));
   flotanteParActual = porcentajeFlotantePar;


   if (flotanteLacertaCauda>100 || flotanteLacertaCauda<-100){
      lacertaCaudaEsDinero = true;
   }else{
      lacertaCaudaEsDinero = false;       
      }


   if (lacertaCaudaEsDinero)
   {
      flotanteLacertaCauda=flotanteLacertaCauda*tipoCuentaDouble;
      //Cierra todo si el DINERO de flotante del par ha superado el límite de la Lacerta Cauda (+ o -)
      if(lacertaCauda &&
      ((flotanteLacertaCauda < 0 && flotantePar <= flotanteLacertaCauda) ||
      (flotanteLacertaCauda >= 0 && flotantePar >= flotanteLacertaCauda)))
        {
         debeCerrar=true;     
        }
   }
   else
   {
      //Cierra todo si el PORCENTAJE de flotante del par ha superado el límite de la Lacerta Cauda (+ o -)
      if(lacertaCauda &&
      ((flotanteLacertaCauda < 0 && flotanteParActual <= flotanteLacertaCauda) ||
      (flotanteLacertaCauda >= 0 && flotanteParActual >= flotanteLacertaCauda)))
        {
         debeCerrar=true;     
        }
  }
  
  if (debeCerrar)
  {
         //CaesarActividad = Apagado;
         //AlexanderActividad = Apagado;
         //HannibalActividad = Apagado;
   if (lacertaCaudaEsDinero)
   {
         Print(">>>>>>>>>> Lacerta Cauda cierra operaciones al alcanzase un flotante de " + flotantePar + " en este par. >>>>>>>>>>");
   }
   else
   {
         Print(">>>>>>>>>> Lacerta Cauda cierra operaciones al alcanzase un flotante del " + flotanteParActual + "% en este par. >>>>>>>>>>");
   }
   
         cerrarTodasOperacionesSymbolCaudillo(0);
         cerrarTodasOperacionesSymbolCaudillo(MagicNumber_Caesar);
         cerrarTodasOperacionesSymbolCaudillo(MagicNumber_Alexander);
         cerrarTodasOperacionesSymbolCaudillo(MagicNumber_Hannibal);
   
         bloqueoTemporal=TimeLocal()+50;
         GlobalVariableSet("inicio",(double)TimeLocal());
  }
  lastFlotanteParActual = flotanteParActual;
  
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void cerrarTicket(int ticket)
  {

   if(ticket >= 0)
     {

      bool result = OrderSelect(ticket, SELECT_BY_TICKET);
      if(result)
        {


         if(multiplicadorCierreParcialLocal>0.85) // No parciales.
           {
            multiplicadorCierreParcialLocal=1.00;
            if(!OrderClose(ticket, OrderLots(), OrderClosePrice(), 3, clrRed))
              {
               for(int z = 10; z >= 0; z--)
                 {
                  Sleep(100);
                  result=OrderClose(ticket, OrderLots(), OrderClosePrice(), 5, clrRed);
                  if(result)
                     break;
                 }
              }
           }
         else
           {
            if(!OrderClose(ticket, NormalizeLotsClose(OrderLots() * multiplicadorCierreParcialLocal), OrderClosePrice(), 3, clrRed))
              {
               for(z = 10; z >= 0; z--)
                 {
                  Sleep(100);
                  result=OrderClose(ticket, NormalizeLotsClose(OrderLots() * multiplicadorCierreParcialLocal), OrderClosePrice(), 3, clrRed);
                  if(result)
                     break;
                 }
              }
           }


         countTradesCaesarVar = CountTrades_CaesarX();
         countTradesAlexanderVar = CountTrades_AlexanderX();
         countTradesHannibalVar = CountTrades_HannibalX();
         countTradesCaptainVar = CountTrades_CapitanX();
         countTradesTotalParVar = CountTrades_TotalParX();


         if(OrderMagicNumber()==MagicNumber_Caesar)
           {
            if(CaesarAutoPriceAverage)
              {
               actualizarPrecioPromedioCaesar();
               actualizarTPCaesar();
              }
            actualizadoTPCaesar=true;
           }
         if(OrderMagicNumber()==MagicNumber_Alexander)
           {
            if(AlexanderAutoPriceAverage)
              {
               actualizarPrecioPromedioAlexander();
               actualizarTPAlexander();
              }
            actualizadoTPAlexander=true;
           }
         if(OrderMagicNumber()==MagicNumber_Hannibal)
           {
            if(HannibalAutoPriceAverage)
              {
               actualizarPrecioPromedioHannibal();
               actualizarTPHannibal();
              }
            actualizadoTPHannibal=true;
           }


         GestionaPintadoFiltroLineas();
        }
     }
  }



//+------------------------------------------------------------------+
//|       cierra la última según dirección (buy-sell) y precio                                                           |
//+------------------------------------------------------------------+
int cerrarUltimaOperacionSymbolCaudillo(int caudillo)
  {

   bool check;
   bool resultado;
   int selectedTicket;
   double lastSelectedOpenTime=-0.000001;


   selectedTicket = -1;

   lastSelectedOpenTime = 0;

   int ot=OrdersTotal() - 1;

   for(int i = ot; i >= 0; i--)
     {
      resultado = OrderSelect(i, SELECT_BY_POS);
      if(resultado && OrderSymbol() == Symbol() && (OrderMagicNumber() == caudillo))
        {


         if(((OrderOpenTime() > lastSelectedOpenTime && OrderType()==OP_SELL)||lastSelectedOpenTime<=0) || ((OrderOpenTime() < lastSelectedOpenTime && OrderType()==OP_BUY)||lastSelectedOpenTime<=0))
           {
            lastSelectedOpenTime = OrderOpenTime();
            selectedTicket = OrderTicket();
           }

        }
     }

   if(selectedTicket >= 0)
     {
      resultado = OrderSelect(selectedTicket, SELECT_BY_TICKET);
      if(resultado)
        {
         if(multiplicadorCierreParcialLocal>0.85) // No parciales.
           {
            multiplicadorCierreParcialLocal=1.00;
            if(!OrderClose(selectedTicket, OrderLots(), OrderClosePrice(), 3, clrRed))
              {
               for(int z = 10; z >= 0; z--)
                 {
                  Sleep(100);
                  check=OrderClose(selectedTicket, OrderLots(), OrderClosePrice(), 5, clrRed);
                  if(check)
                     break;
                 }
              }
            RefreshRates();
           }
         else
           {
            if(!OrderClose(selectedTicket, NormalizeLotsClose(OrderLots() * multiplicadorCierreParcialLocal), OrderClosePrice(), 3, clrRed))
              {
               for(z = 10; z >= 0; z--)
                 {
                  Sleep(100);
                  check=OrderClose(selectedTicket, NormalizeLotsClose(OrderLots() * multiplicadorCierreParcialLocal), OrderClosePrice(), 3, clrRed);
                  if(check)
                     break;
                 }
              }
            RefreshRates();
           }
        }
     }




   countTradesCaesarVar = CountTrades_CaesarX();
   countTradesAlexanderVar = CountTrades_AlexanderX();
   countTradesHannibalVar = CountTrades_HannibalX();
   countTradesCaptainVar = CountTrades_CapitanX();
   countTradesTotalParVar = CountTrades_TotalParX();


   if(caudillo==MagicNumber_Caesar)
     {
      if(CaesarAutoPriceAverage)
        {
         actualizarPrecioPromedioCaesar();
         actualizarTPCaesar();
        }
      actualizadoTPCaesar=true;
     }
   if(caudillo==MagicNumber_Alexander)
     {
      if(AlexanderAutoPriceAverage)
        {
         actualizarPrecioPromedioAlexander();
         actualizarTPAlexander();
        }
      actualizadoTPAlexander=true;
     }
   if(caudillo==MagicNumber_Hannibal)
     {
      if(HannibalAutoPriceAverage)
        {
         actualizarPrecioPromedioHannibal();
         actualizarTPHannibal();
        }
      actualizadoTPHannibal=true;
     }

   GestionaPintadoFiltroLineas();

   return(0);
  }



//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int cerrarMenosPerdidaOperacionSymbolCaudillo(int caudillo)
  {

   bool check;
   bool resultado;
   int selectedTicket;
   double lastSelectedProfit;



   selectedTicket = -1;

   lastSelectedProfit = -999999999;

   int ot=OrdersTotal() - 1;

   for(int i = ot; i >= 0; i--)
     {
      resultado = OrderSelect(i, SELECT_BY_POS);
      if(resultado && OrderSymbol() == Symbol() && (OrderMagicNumber() == caudillo))
        {


         if((OrderProfit() > lastSelectedProfit))
           {
            lastSelectedProfit = OrderProfit();
            selectedTicket = OrderTicket();
           }

        }
     }
   if(selectedTicket >= 0)
     {
      resultado = OrderSelect(selectedTicket, SELECT_BY_TICKET);
      if(resultado)
        {
         if(multiplicadorCierreParcialLocal>0.85) // No parciales.
           {
            multiplicadorCierreParcialLocal=1.00;
            if(!OrderClose(selectedTicket, OrderLots(), OrderClosePrice(), 3, clrRed))
              {
               for(int z = 10; z >= 0; z--)
                 {
                  Sleep(100);
                  check=OrderClose(selectedTicket, OrderLots(), OrderClosePrice(), 5, clrRed);
                  if(check)
                     break;
                 }
              }
            RefreshRates();
           }
         else
           {
            if(!OrderClose(selectedTicket, NormalizeLotsClose(OrderLots() * multiplicadorCierreParcialLocal), OrderClosePrice(), 3, clrRed))
              {
               for(z = 10; z >= 0; z--)
                 {
                  Sleep(100);
                  check=OrderClose(selectedTicket, NormalizeLotsClose(OrderLots() * multiplicadorCierreParcialLocal), OrderClosePrice(), 3, clrRed);
                  if(check)
                     break;
                 }
              }
            RefreshRates();
           }
        }
     }




   countTradesCaesarVar = CountTrades_CaesarX();
   countTradesAlexanderVar = CountTrades_AlexanderX();
   countTradesHannibalVar = CountTrades_HannibalX();
   countTradesCaptainVar = CountTrades_CapitanX();
   countTradesTotalParVar = CountTrades_TotalParX();


   if(caudillo==MagicNumber_Caesar)
     {
      if(CaesarAutoPriceAverage)
        {
         actualizarPrecioPromedioCaesar();
         actualizarTPCaesar();
        }
      actualizadoTPCaesar=true;
     }
   if(caudillo==MagicNumber_Alexander)
     {
      if(AlexanderAutoPriceAverage)
        {
         actualizarPrecioPromedioAlexander();
         actualizarTPAlexander();
        }
      actualizadoTPAlexander=true;
     }
   if(caudillo==MagicNumber_Hannibal)
     {
      if(HannibalAutoPriceAverage)
        {
         actualizarPrecioPromedioHannibal();
         actualizarTPHannibal();
        }
      actualizadoTPHannibal=true;
     }

   GestionaPintadoFiltroLineas();

   return(0);
  }







//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int cerrarUltimaOperacionSymbolCaudilloPorTicket(int caudillo)
  {

   bool check;
   bool resultado;
   int selectedTicket;
   double lastSelectedOpenTime;
   int ultiTicket=UltimoTicket(caudillo);



   selectedTicket = ultiTicket;

   lastSelectedOpenTime = 0;

   if(selectedTicket >= 0)
     {
      resultado = OrderSelect(selectedTicket, SELECT_BY_TICKET);
      if(resultado)
        {
         if(multiplicadorCierreParcialLocal>0.85) // No parciales.
           {
            multiplicadorCierreParcialLocal=1.00;
            if(!OrderClose(selectedTicket, OrderLots(), OrderClosePrice(), 3, clrRed))
              {
               for(int z = 10; z >= 0; z--)
                 {
                  Sleep(100);
                  check=OrderClose(selectedTicket, OrderLots(), OrderClosePrice(), 5, clrRed);
                  if(check)
                     break;
                 }
              }
            RefreshRates();
           }
         else
           {
            if(!OrderClose(selectedTicket, NormalizeLotsClose(OrderLots() * multiplicadorCierreParcialLocal), OrderClosePrice(), 3, clrRed))
              {
               for(z = 10; z >= 0; z--)
                 {
                  Sleep(100);
                  check=OrderClose(selectedTicket, NormalizeLotsClose(OrderLots() * multiplicadorCierreParcialLocal), OrderClosePrice(), 3, clrRed);
                  if(check)
                     break;
                 }
              }
            RefreshRates();
           }
        }
     }




   countTradesCaesarVar = CountTrades_CaesarX();
   countTradesAlexanderVar = CountTrades_AlexanderX();
   countTradesHannibalVar = CountTrades_HannibalX();
   countTradesCaptainVar = CountTrades_CapitanX();
   countTradesTotalParVar = CountTrades_TotalParX();


   if(caudillo==MagicNumber_Caesar)
     {
      if(CaesarAutoPriceAverage)
        {
         actualizarPrecioPromedioCaesar();
         actualizarTPCaesar();
        }
      actualizadoTPCaesar=true;
     }
   if(caudillo==MagicNumber_Alexander)
     {
      if(AlexanderAutoPriceAverage)
        {
         actualizarPrecioPromedioAlexander();
         actualizarTPAlexander();
        }
      actualizadoTPAlexander=true;
     }
   if(caudillo==MagicNumber_Hannibal)
     {
      if(HannibalAutoPriceAverage)
        {
         actualizarPrecioPromedioHannibal();
         actualizarTPHannibal();
        }
      actualizadoTPHannibal=true;
     }

   GestionaPintadoFiltroLineas();

   return(0);
  }






//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int cerrarMayorOperacionSymbolCaudillo(int caudillo)
  {

   bool check;
   bool resultado;
   int selectedTicket;
   double lastSelectedLots;
   int ultiTicket=UltimoTicket(caudillo);

   int ot=OrdersTotal() - 1;

   for(int ii = ot; ii >= 0; ii--)
     {
      selectedTicket = -1;

      lastSelectedLots = 0.00;

      for(int i = ot; i >= 0; i--)
        {
         resultado = OrderSelect(i, SELECT_BY_POS);
         if(resultado && OrderSymbol() == Symbol() && (OrderMagicNumber() == caudillo) && OrderTicket()<=ultiTicket)
           {


            if((OrderLots() > lastSelectedLots))
              {
               lastSelectedLots = OrderLots();
               selectedTicket = OrderTicket();
              }

           }
        }
      if(selectedTicket >= 0)
        {
         resultado = OrderSelect(selectedTicket, SELECT_BY_TICKET);


         if(resultado)
           {
            if(!OrderClose(selectedTicket, NormalizeLotsClose(OrderLots() * multiplicadorCierreParcialLocal), OrderClosePrice(), 3, clrRed))
              {
               for(int z = 10; z >= 0; z--)
                 {
                  Sleep(100);
                  check=OrderClose(selectedTicket, NormalizeLotsClose(OrderLots() * multiplicadorCierreParcialLocal), OrderClosePrice(), 5, clrRed);
                  if(check)
                     break;
                 }
              }
            Sleep(50);
            RefreshRates();

           }


         break;
        }
     }


   countTradesCaesarVar = CountTrades_CaesarX();
   countTradesAlexanderVar = CountTrades_AlexanderX();
   countTradesHannibalVar = CountTrades_HannibalX();
   countTradesCaptainVar = CountTrades_CapitanX();
   countTradesTotalParVar = CountTrades_TotalParX();

   if(caudillo==MagicNumber_Caesar)
     {
      if(CaesarAutoPriceAverage)
        {
         actualizarPrecioPromedioCaesar();
         actualizarTPCaesar();
        }
      actualizadoTPCaesar=true;
     }
   if(caudillo==MagicNumber_Alexander)
     {
      if(AlexanderAutoPriceAverage)
        {
         actualizarPrecioPromedioAlexander();
         actualizarTPAlexander();
        }
      actualizadoTPAlexander=true;
     }
   if(caudillo==MagicNumber_Hannibal)
     {
      if(HannibalAutoPriceAverage)
        {
         actualizarPrecioPromedioHannibal();
         actualizarTPHannibal();
        }
      actualizadoTPHannibal=true;
     }

   GestionaPintadoFiltroLineas();

   return(0);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int cerrarPrimeraOperacioneSymbolCaudillo(int caudillo)
  {
   RefreshRates();

   bool resultado;
   
   int ot=OrdersTotal() - 1;
   
   for(int i = 0; i < ot; i++)
     {
      resultado = OrderSelect(i, SELECT_BY_POS);

      if(resultado && OrderSymbol() == Symbol() && OrderMagicNumber() == caudillo)
        {

         if(multiplicadorCierreParcialLocal>0.85) // No parciales.
           {
            multiplicadorCierreParcialLocal=1.00;
            if(!OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), 3, clrRed))
              {
               for(int z = 10; z >= 0; z--)
                 {
                  Sleep(100);
                  resultado=OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), 5, clrRed);
                  if(resultado)
                     break;
                 }
              }
            RefreshRates();
           }
         else
           {
            if(!OrderClose(OrderTicket(), NormalizeLotsClose(OrderLots() * multiplicadorCierreParcialLocal), OrderClosePrice(), 3, clrRed))
              {
               for(z = 10; z >= 0; z--)
                 {
                  Sleep(100);
                  resultado=OrderClose(OrderTicket(), NormalizeLotsClose(OrderLots() * multiplicadorCierreParcialLocal), OrderClosePrice(), 3, clrRed);
                  if(resultado)
                     break;
                 }
              }
            RefreshRates();
           }

         break;
        }
     }

   countTradesCaesarVar = CountTrades_CaesarX();
   countTradesAlexanderVar = CountTrades_AlexanderX();
   countTradesHannibalVar = CountTrades_HannibalX();
   countTradesCaptainVar = CountTrades_CapitanX();
   countTradesTotalParVar = CountTrades_TotalParX();

   if(multiplicadorCierreParcialLocal<0.99)
     {
      if(caudillo==MagicNumber_Caesar)
         actualizadoTPCaesar=true;
      if(caudillo==MagicNumber_Alexander)
         actualizadoTPAlexander=true;
      if(caudillo==MagicNumber_Hannibal)
         actualizadoTPHannibal=true;
     }

   GestionaPintadoFiltroLineas();

   return(0);
  }



//+------------------------------------------------------------------+
//| Create the horizontal line                                       |
//+------------------------------------------------------------------+
bool HLineCreate(const long            chart_ID = 0,      // chart's ID
                 const string          name = "HLine",    // line name
                 const int             sub_window = 0,    // subwindow index
                 double                price = 0,         // line price
                 const color           clr = clrRed,      // line color
                 const ENUM_LINE_STYLE style = STYLE_SOLID, // line style
                 const int             width = 1,         // line width
                 const bool            back = false,      // in the background
                 const bool            selection = true,  // highlight to move
                 const bool            hidden = true,     // hidden in the object list
                 const long            z_order = 0)       // priority for mouse click
  {
//--- if the price is not set, set it at the current Bid price level
   if(!price)
      price = SymbolInfoDouble(Symbol(), SYMBOL_BID);
//--- reset the error value
   ResetLastError();
//--- create a horizontal line
   if(!ObjectCreate(chart_ID, name, OBJ_HLINE, sub_window, 0, price))
     {
      Print(__FUNCTION__,
            ": failed to create a horizontal line! Error code = ", GetLastError());
      return(false);
     }
//--- set line color
   ObjectSetInteger(chart_ID, name, OBJPROP_COLOR, clr);
//--- set line display style
   ObjectSetInteger(chart_ID, name, OBJPROP_STYLE, style);
//--- set line width
   ObjectSetInteger(chart_ID, name, OBJPROP_WIDTH, width);
//--- display in the foreground (false) or background (true)
   ObjectSetInteger(chart_ID, name, OBJPROP_BACK, back);
//--- enable (true) or disable (false) the mode of moving the line by mouse
//--- when creating a graphical object using ObjectCreate function, the object cannot be
//--- highlighted and moved by default. Inside this method, selection parameter
//--- is true by default making it possible to highlight and move the object
   ObjectSetInteger(chart_ID, name, OBJPROP_SELECTABLE, selection);
   ObjectSetInteger(chart_ID, name, OBJPROP_SELECTED, selection);
//--- hide (true) or display (false) graphical object name in the object list
   ObjectSetInteger(chart_ID, name, OBJPROP_HIDDEN, hidden);
//--- set the priority for receiving the event of a mouse click in the chart
   ObjectSetInteger(chart_ID, name, OBJPROP_ZORDER, z_order);
//--- successful execution
   return(true);
  }
//+------------------------------------------------------------------+
//| Move horizontal line                                             |
//+------------------------------------------------------------------+
bool HLineMove(const long   chart_ID = 0, // chart's ID
               const string name = "HLine", // line name
               double       price = 0)    // line price
  {
//--- if the line price is not set, move it to the current Bid price level
   if(!price)
      price = SymbolInfoDouble(Symbol(), SYMBOL_BID);
//--- reset the error value
   ResetLastError();
//--- move a horizontal line
   if(!ObjectMove(chart_ID, name, 0, 0, price))
     {
      Print(__FUNCTION__,
            ": failed to move the horizontal line! Error code = ", GetLastError());
      return(false);
     }
//--- successful execution
   return(true);
  }


//+------------------------------------------------------------------+
//| Get chart scale (from 0 to 5).                                   |
//+------------------------------------------------------------------+
int ChartScaleGet(const long chart_ID=0)
  {
//--- prepare the variable to get the property value
   long result=-1;
//--- reset the error value
   ResetLastError();
//--- receive the property value
   if(!ChartGetInteger(chart_ID,CHART_SCALE,0,result))
     {
      //--- display the error message in Experts journal
      Print(__FUNCTION__+", Error Code = ",GetLastError());
     }
//--- return the value of the chart property
   return((int)result);
  }

//+------------------------------------------------------------------+
//| MUEVE LINEAS CON TECLADO                                         |
//+------------------------------------------------------------------+
void MueveLineaCursor(string nombreL)
  {

   if(GetKeyState(17) & 0x8000)   // 17 = Ctrl key
      //   if( ctrlKey ) // 17 = Ctrl key
     {
      double paso=(WindowPriceMax()-WindowPriceMin())/500.0;
     }
   else
     {
      paso=(WindowPriceMax()-WindowPriceMin())/100.0;
     }

   if(GetKeyState(38) & 0x8000)
      //   if( upKey )
     {
      //            double zoom=ChartScaleGet();
      double p1=ObjectGetDouble(0,nombreL,OBJPROP_PRICE1,0);
      datetime t1=ObjectGetInteger(0,nombreL,OBJPROP_TIME1,0);

      double p2=ObjectGetDouble(0,nombreL,OBJPROP_PRICE2,0);
      datetime t2=ObjectGetInteger(0,nombreL,OBJPROP_TIME2,0);

      TrendPointChange(0, nombreL,0,t1,p1+paso);
      TrendPointChange(0, nombreL,1,t2,p2+paso);
     }

   if(GetKeyState(40) & 0x8000)
      //   if( downKey )
     {
      //            zoom=ChartScaleGet();
      p1=ObjectGetDouble(0,nombreL,OBJPROP_PRICE1,0);
      t1=ObjectGetInteger(0,nombreL,OBJPROP_TIME1,0);

      p2=ObjectGetDouble(0,nombreL,OBJPROP_PRICE2,0);
      t2=ObjectGetInteger(0,nombreL,OBJPROP_TIME2,0);

      TrendPointChange(0, nombreL,0,t1,p1-paso);
      TrendPointChange(0, nombreL,1,t2,p2-paso);
     }




  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void  GestionaOrdenesManuales()
  {


   robotAcMan.ManuDigits=Digits();

   if(robotAcMan.ManuPoneOrden != 0)
     {
      if(robotAcMan.ManuPoneOrden == 1)
        {

         if(OpenManualOrder(OP_SELL,NormalizeLotsSendOrder(robotAcMan.ManuLotajeFijo),1,robotAcMan.ManuValorSL,robotAcMan.ManuValorTP,"Manual Dpro",0,0)>=0)
           {
            robotAcMan.ManuPoneOrden=0;
            robotAcMan.ManuTPDel=1;
            robotAcMan.ManuSLDel=1;
            lotajeLineaM=0;
           }
        }
      if(robotAcMan.ManuPoneOrden == 2)
        {
         if(OpenManualOrder(OP_BUY,NormalizeLotsSendOrder(robotAcMan.ManuLotajeFijo),1,robotAcMan.ManuValorSL,robotAcMan.ManuValorTP,"Manual Dpro",0,0)>=0)
           {
            robotAcMan.ManuPoneOrden=0;
            robotAcMan.ManuTPDel=1;
            robotAcMan.ManuSLDel=1;
            lotajeLineaM=0;
           }
        }

      if(robotAcMan.ManuPoneOrden == 11) // SELL DESDE LINEAS DE OPERACIÓN
        {

         if(OpenManualOrder(OP_SELL,NormalizeLotsSendOrder(lotajeLineaM),1,robotAcMan.ManuValorSL,robotAcMan.ManuValorTP,"Manual Dpro",0,0)>=0)
           {
            robotAcMan.ManuPoneOrden=0;
            robotAcMan.ManuTPDel=1;
            robotAcMan.ManuSLDel=1;
            lotajeLineaM=0;
            robotAcMan.ManuModificadoPorDLL=0;
            retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
           }
        }
      if(robotAcMan.ManuPoneOrden == 12) // BUY DESDE LINEAS DE OPERACIÓN
        {
         if(OpenManualOrder(OP_BUY,NormalizeLotsSendOrder(lotajeLineaM),1,robotAcMan.ManuValorSL,robotAcMan.ManuValorTP,"Manual Dpro",0,0)>=0)
           {
            robotAcMan.ManuPoneOrden=0;
            robotAcMan.ManuTPDel=1;
            robotAcMan.ManuSLDel=1;
            lotajeLineaM=0;
            robotAcMan.ManuModificadoPorDLL=0;
            retorno=PanelUpdate(robotReturn, robotSend, robotAcMan, numRobot, Symbol());
           }
        }
      robotAcMan.ManuPoneOrden=0;
      robotAcMan.ManuModificadoPorDLL=0;
     }

   if(robotAcMan.ManuCambiaSL==1)
     {
      if(mousePrice>0)
        {
         ModificaOrdenManualSeleccionada(0);
         mousePrice=0;
        }
      robotAcMan.ManuCambiaSL=0;
     }

   if(robotAcMan.ManuPoneSL>0 || ObjectFind("StopLoss Manual")>=0)  //|| (NormalizeDouble(robotAcMan.ManuValorSL,Digits())!=NormalizeDouble(0,Digits()) && NormalizeDouble(robotAcMan.ManuValorSL,Digits())!=ObjectGetDouble(0,"STOPLOSS",OBJPROP_PRICE)))
     {
      if(robotAcMan.ManuPoneSL==2)
        {
         mousePrice=NormalizeDouble(robotAcMan.ManuValorSL,Digits());//ObjectGetDouble(0,"STOPLOSS",OBJPROP_PRICE);
        }
      if(mousePrice>0 && robotAcMan.ManuPoneSL>0)
        {
         robotAcMan.ManuValorSL=NormalizeDouble(mousePrice,Digits());
         if (ObjectFind("StopLoss Manual")>=0) ObjectDelete("StopLoss Manual");
         HLineCreate(0,"StopLoss Manual",0,robotAcMan.ManuValorSL,clrRed,STYLE_DASHDOTDOT,1,false,false);
         if (robotAcMan.ManuValorSL>Ask)
         {
            int tipoOP=OP_SELL;
         }
         else
         {
                tipoOP=OP_BUY;
         }
         if (robotAcMan.ManuModo==1)
         {
            string futuroLotaje = DoubleToString( CalculaLotajeDesdeRiesgo(tipoOP, Ask, Bid),2);
         }
         else
         {
            futuroLotaje=DoubleToString(robotAcMan.ManuLotajeFijo,2);
         }
         string futuroObjectivo=DoubleToString(PrevioPorcen(robotAcMan.ManuValorSL,futuroLotaje,tipoOP),2);
         ObjectSetString(0, "StopLoss Manual", OBJPROP_TEXT, "STOPLOSS    [LOTS~: "+futuroLotaje+"]  [RIESGO~: "+futuroObjectivo+"%]");
         mousePrice=0;
        }
        else
        {
         if (robotAcMan.ManuValorSL>Ask)
         {
                tipoOP=OP_SELL;
         }
         else
         {
                tipoOP=OP_BUY;
         }
         if (robotAcMan.ManuModo==1)
         {
            futuroLotaje = DoubleToString( CalculaLotajeDesdeRiesgo(tipoOP, Ask, Bid),2);
         }
         else
         {
            futuroLotaje=DoubleToString(robotAcMan.ManuLotajeFijo,2);
         }
         futuroObjectivo=DoubleToString(PrevioPorcen(robotAcMan.ManuValorSL,futuroLotaje,tipoOP),2);
         ObjectSetString(0, "StopLoss Manual", OBJPROP_TEXT, "STOPLOSS    [LOTS~: "+futuroLotaje+"]  [RIESGO~: "+futuroObjectivo+"%]");
        }        
      robotAcMan.ManuPoneSL=0;
     }


   if(robotAcMan.ManuCambiaTP==1)
     {
      if(mousePrice>0)
        {
         ModificaOrdenManualSeleccionada(1);
         mousePrice=0;
        }
      robotAcMan.ManuCambiaTP=0;
     }

   if(robotAcMan.ManuPoneTP>0 || ObjectFind("TakeProfit Manual")>=0) // || (NormalizeDouble(robotAcMan.ManuValorTP,Digits())!=NormalizeDouble(0,Digits()) && NormalizeDouble(robotAcMan.ManuValorTP,Digits())!=ObjectGetDouble(0,"TAKEPROFIT",OBJPROP_PRICE)))
     {
      if(robotAcMan.ManuPoneTP==2)
        {
         mousePrice=NormalizeDouble(robotAcMan.ManuValorTP,Digits());//ObjectGetDouble(0,"TAKEPROFIT",OBJPROP_PRICE);
        }
      if(mousePrice>0 && robotAcMan.ManuPoneTP>0)
        {
         robotAcMan.ManuValorTP=NormalizeDouble(mousePrice,Digits());
         if (ObjectFind("TakeProfit Manual")>=0) ObjectDelete("TakeProfit Manual");
         HLineCreate(0,"TakeProfit Manual",0,robotAcMan.ManuValorTP,clrGreen,STYLE_DASHDOTDOT,1,false,false);
         
         if (robotAcMan.ManuValorTP<Bid)
         {
                tipoOP=OP_SELL;
         }
         else
         {
                tipoOP=OP_BUY;
         }
         if (robotAcMan.ManuModo==1)
         {
            futuroLotaje = DoubleToString( CalculaLotajeDesdeBeneficio(tipoOP, Ask, Bid),2);
         }
         else
         {
            futuroLotaje=DoubleToString(robotAcMan.ManuLotajeFijo,2);
         }
         futuroObjectivo=DoubleToString(PrevioPorcen(robotAcMan.ManuValorTP,futuroLotaje,tipoOP),2);         
         ObjectSetString(0, "TakeProfit Manual", OBJPROP_TEXT, "TAKEPROFIT    [LOTS~: "+futuroLotaje+"]  [OBJETIVO~: "+futuroObjectivo+"%]");
         mousePrice=0;
        }
        else
        {
         if (robotAcMan.ManuValorTP<Bid)
         {
                tipoOP=OP_SELL;
         }
         else
         {
                tipoOP=OP_BUY;
         }
         if (robotAcMan.ManuModo==1)
         {
            futuroLotaje = DoubleToString( CalculaLotajeDesdeBeneficio(tipoOP, Ask, Bid),2);
         }
         else
         {
            futuroLotaje=DoubleToString(robotAcMan.ManuLotajeFijo,2);
         }
         futuroObjectivo=DoubleToString(PrevioPorcen(robotAcMan.ManuValorTP,futuroLotaje,tipoOP),2);         
         ObjectSetString(0, "TakeProfit Manual", OBJPROP_TEXT, "TAKEPROFIT    [LOTS~: "+futuroLotaje+"]  [OBJETIVO~: "+futuroObjectivo+"%]");
        }
        
      robotAcMan.ManuPoneTP=0;
     }

   if(robotAcMan.ManuTPDel==1)
     {
      robotAcMan.ManuValorTP=NormalizeDouble(0,Digits());
      if (ObjectFind("TakeProfit Manual")>=0) ObjectDelete("TakeProfit Manual");
      robotAcMan.ManuPoneTP=0;
      robotAcMan.ManuTPDel=0;
      mousePrice=0;
     }
   if(robotAcMan.ManuSLDel==1)
     {
      robotAcMan.ManuValorSL=NormalizeDouble(0,Digits());
      if (ObjectFind("StopLoss Manual")>=0) ObjectDelete("StopLoss Manual");
      robotAcMan.ManuPoneSL=0;
      robotAcMan.ManuSLDel=0;
      mousePrice=0;
     }

   if(robotAcMan.ManuCierre==1)
     {
      if(robotAcMan.ManuNumOrder==0)
        {
         cerrarTodasOperacionesSymbolCaudillo(0);
        }
      else
        {
         cerrarOrdenManualSeleccionada();
        }
      robotAcMan.ManuCierre=0;
     }



  }



//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TiempoRestanteVela()
  {
   static datetime lastLeftTime = 0;
   datetime leftTime = (Period()*60)-(TimeCurrent()-Time[0]);

   if(lastLeftTime == leftTime)
      return;

   lastLeftTime=leftTime;

   if(ObjectFind("TRV")<0)
     {
      ObjectCreate("TRV",OBJ_LABEL,0,0,0);
      ObjectSet("TRV",OBJPROP_CORNER,3);
      ObjectSet("TRV",OBJPROP_XDISTANCE,5);
      ObjectSet("TRV",OBJPROP_YDISTANCE,3);
      ObjectSetText("TRV", "00:00:00",12,"verdana",clrDarkGray);
     }




   if(leftTime<0)
     {
      leftTime=leftTime+(Period()*60);
     }

   if(leftTime<0)
     {
      ObjectSetText("TRV", "",12,"verdana",clrDarkRed);
     }
   else
     {

      string sTime= TimeToStr(leftTime,TIME_SECONDS);

      int days =((leftTime/60)/60)/24;
      int daysRes = days*24*60*60;
      if((Period() == PERIOD_MN1 || Period()==PERIOD_W1))
        {
         if(days>0)
           {
            leftTime=leftTime-daysRes;
            sTime= TimeToStr(leftTime,TIME_SECONDS);
            ObjectSetText("TRV", days +"D + "+sTime,12,"verdana",clrDarkGray);
           }
         else
           {
            ObjectSetText("TRV", ""+sTime,12,"verdana",clrDarkGray);
           }
        }
      else
        {
         if(leftTime<61)
           {
            if(leftTime>30)
              {
               ObjectSetText("TRV", ""+leftTime,17,"verdana",clrWhiteSmoke);
              }
            else
              {
               if(leftTime>10)
                 {
                  ObjectSetText("TRV", ""+leftTime,20,"verdana",clrWhite);
                 }
               else
                 {
                  ObjectSetText("TRV", ""+leftTime,30,"verdana",clrYellow);
                 }
              }
           }
         else
           {
            ObjectSetText("TRV", sTime,14,"verdana",clrGray);
           }
        }

     }

  }












//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer()
  {
   if(enTick || (((tiempoEnTimer>GetTickCount()- (valorTimer-3)) && IsPined==1) || ((tiempoEnTimer2>GetTickCount()- ((valorTimer*3)-1)) && IsPined==2)))
     {
      return;
     }
   estaEnTimer=True;





   if(IsPined==1)
      tiempoEnTimer=GetTickCount(); //------
   if(IsPined==2)
     {
      tiempoEnTimer2=GetTickCount(); //------
      altKey=0;
     }





      if(!botlidatorChequeado)
        {
         static int puntos=0;
         string puntoChar="#";

         for(int p = 0; p < puntos; p++)
           {
            puntoChar=puntoChar+"#";
           }
         Comment("Conectando "+puntoChar);
         Sleep(1);

         enTick=false;
         puntos+=1;
         if(puntos>10)
           {
            puntos=1;
            if (IsPined==1)
            {
               Botlidator(numRobot, True, -13); 
            }
            else
            {
               Botlidator(numRobot, False, -13); 
            }
           }
           
           
   switch(Period())
     {
      case 1:
         string temporalidad="M1";
         break;
      case 5:
         temporalidad="M5";
         break;
      case 15:
         temporalidad="M15";
         break;
      case 30:
         temporalidad="M30";
         break;
      case 60:
         temporalidad="H1";
         break;
      case 240:
         temporalidad="H4";
         break;
      case 1440:
         temporalidad="D1";
         break;
      case 10080:
         temporalidad="W1";
         break;
      case 43200:
         temporalidad="MN";
         break;
      default:
         temporalidad="";
         break;
     }           
         if(ObjectFind("ParTemp")>=0)
           {
            ObjectDelete("ParTemp");
           }
           
         if(ChartBackColorGet() == 12630450) // LIGHT 0xC0B9B2
           {
            pintarEtiquetas("ParTemp",3,10,10,Symbol()+" "+temporalidad,100,"impact",clrRed,true);
           }
         if(ChartBackColorGet() == 4866102) // MEDIUM 0x4A4036
           {
            pintarEtiquetas("ParTemp",3,10,10,Symbol()+" "+temporalidad,100,"impact",clrRed,true);
           }
         if(ChartBackColorGet() == 0) // DARK 0x000000
           {
            pintarEtiquetas("ParTemp",3,10,10,Symbol()+" "+temporalidad,100,"impact",clrRed,true);
           }
                      
        }
        else
        {
         static int primeraVezColor=1;

         if (primeraVezColor)
         {
         primeraVezColor=0;
            switch(Period())
              {
               case 1:
                  temporalidad="M1";
                  break;
               case 5:
                  temporalidad="M5";
                  break;
               case 15:
                  temporalidad="M15";
                  break;
               case 30:
                  temporalidad="M30";
                  break;
               case 60:
                  temporalidad="H1";
                  break;
               case 240:
                  temporalidad="H4";
                  break;
               case 1440:
                  temporalidad="D1";
                  break;
               case 10080:
                  temporalidad="W1";
                  break;
               case 43200:
                  temporalidad="MN";
                  break;
               default:
                  temporalidad="";
                  break;
              }
         
         
            if(ObjectFind("ParTemp")>=0)
              {
               ObjectDelete("ParTemp");
              }
              
            if(ChartBackColorGet() == 12630450) // LIGHT 0xC0B9B2
              {
               pintarEtiquetas("ParTemp",3,10,10,Symbol()+" "+temporalidad,100,"impact",0xC0B9B2|0x0f0f0f,true);
              }
            if(ChartBackColorGet() == 4866102) // MEDIUM 0x4A4036
              {
               pintarEtiquetas("ParTemp",3,10,10,Symbol()+" "+temporalidad,100,"impact",0x4A4036|0x1f1f1f,true);
              }
            if(ChartBackColorGet() == 0) // DARK 0x000000
              {
               pintarEtiquetas("ParTemp",3,10,10,Symbol()+" "+temporalidad,100,"impact",0x000000|0x1f1f1f,true);
              }
          }    

        }



   RefreshRates();

   static bool first=false;

   if(!IsOptimization() && ((IsVisualMode() && IsTesting()) || !IsTesting()))  //&& !estaEnBotlidator
     {

   SYROnTick();

      // PINTA MINI LINEA ASK
      if(Ask != lastAsk)
        {
         if(ObjectFind("AskLine")>=0)
           {
            if(debeMostrarLineaAsk)
              {
               TrendPointChange(0,"AskLine",0,iTime(NULL, PERIOD_CURRENT, 1),Ask);
               TrendPointChange(0,"AskLine",1,iTime(NULL, PERIOD_CURRENT, 0),Ask);
              }
            else
              {
               ObjectDelete("AskLine");
              }
           }
         else
           {
            if(debeMostrarLineaAsk)
              {
               TrendCreate(0, "AskLine", 0, iTime(NULL, PERIOD_CURRENT, 1), Ask, iTime(NULL, PERIOD_CURRENT, 0), Ask, clrRed,0,1,false,false);
              }
           }
         lastAsk=Ask;
        }



      TiempoRestanteVela();





      GestionaPintadoFiltroLineas();
      


      if(botlidatorChequeado)
        {
        
         if (bloqueoTemporal>TimeLocal())
         {
            string parpa;
            int ts=(int)(bloqueoTemporal)-TimeLocal();
            if (ts%2)
               parpa="> ";
               else
               parpa=">> ";
            
            string tss=StringFormat(parpa+"BLOQUEO TEMPORAL: %0.2d",ts);
            Comment(tss);
         }
         else
         {        
        
         static string running = "#";
         static int runningN = 0;
         static int direc = 0;

         if(direc==1)
           {

            if(runningN==0)
              {running=")";}
            else
              {
               if(runningN==1)
                 {running="|";}
               else
                 {
                  running="(";
                 }
              }
            if(runningN<2)
              {
               runningN+=1;
              }
            else
              {
               direc=0;
              }


           }
         else
           {
            if(runningN==0)
              {running=")";}
            else
              {
               if(runningN==1)
                 {running="|";}
               else
                 {
                  running="(";
                 }
              }
            if(runningN>0)
              {
               runningN-=1;
              }
            else
              {
               direc=1;
              }
           }
         Comment(running);
         }
        }
      else
        {
         if(!IsTesting()&&!IsOptimization())
           {
            Botlidator(numRobot, false, -13); //3,4,7,8=mal.
           }
        }
        
   





      first=true;
      int hWnd1=0;
      long resultH = -1;

         conta=0;
         while(hWnd1==0 && (IsVisualMode()||!IsTesting()))
           {
            if(ChartGetInteger(ChartID(), CHART_WINDOW_HANDLE, 0, resultH))
              {
               hWnd1 = (int)resultH;
              }
            else
              {
               hWnd1 = WindowHandle(Symbol(), PERIOD_CURRENT);
              }
              conta=conta+1;
              if (conta>50)
              {
                 hWnd1=hWnd1back;
                 break;
              }
           }
           hWnd1back=hWnd1;
       robotReturn.parHwnd = hWnd1;//WindowHandle(Symbol(), Period());


      static int lastIsPined=-1;

      // COMPRUEBA SI HAY LINEAS DE TP Y SL PINTADAS ----------------------------------
      if(lastIsPined!=IsPined)
        {
         lastIsPined=IsPined;

         if(ObjectFind("StopLoss Manual")>=0)
           {
            robotAcMan.ManuValorSL=ObjectGetDouble(0,"StopLoss Manual",OBJPROP_PRICE);
           }
         else
           {
            robotAcMan.ManuValorSL=0;
           }

         if(ObjectFind("TakeProfit Manual")>=0)
           {
            robotAcMan.ManuValorTP=ObjectGetDouble(0,"TakeProfit Manual",OBJPROP_PRICE);
           }
         else
           {
            robotAcMan.ManuValorTP=0;
           }

        }
      //------------------------------------------------------------------------------



      IsPined = IsTopWindow();
      if(IsPined == 0)
        {
         IsPined = 2;
        }
      else
        {


         int Hu=0;
         robotSend.IsPined=IsPined;
         if(IsPined==1)
           {
            Hu=HayUpdate(numRobot);
           }

         if(Hu==2)
           {
           if (!GlobalVariableCheck("cargCar"))
           {
            GlobalVariableTemp("cargCar");
           }
            cargCar=1;
            if (ObjectFind("MNLabel")>=0) ObjectDelete("MNLabel");
            string paresCartera="";
            string paresCartera2=AbrePares();
            paresCartera=paresCartera2;
            string sep="_";
            ushort u_sep;
            string result[];
            u_sep=StringGetCharacter(sep,0);
            int k=StringSplit(paresCartera,u_sep,result);
            
            //Pone coletilla par --------------
            if (coletaPar!="")
            {
            paresCartera="";
            for(int h=0; h<k; h++)
              {
               if(StringLen(result[h])<=6)
                 {
                  result[h]=result[h]+coletaPar;
                  paresCartera+=result[h]+"_";
                 }
              }
            }
            //---------------------------------


            if(k>1)
              {
               

               int numMin=1;
               if(StringFind(paresCartera,Symbol())<0)
                 {
                  numMin=2;
                 }

               while(RunScriptCountWindows()>numMin)
                 {

                  RunScriptCloseWindows();
                 }

               int veces=100;
               long chartid=ChartFirst();
               while(veces>0)
                 {
                  if(chartid != ChartID())
                    {
                     ChartClose(chartid);
                     Sleep(1);
                     chartid=ChartFirst();
                     continue;
                    }
                  Sleep(1);
                  chartid=ChartNext(chartid);
                  if(chartid<0)
                     break;
                  veces -= 1;
                 }
               Sleep(5);
               veces=100;
               chartid=ChartFirst();
               while(veces>0)
                 {
                  if(chartid != ChartID())
                    {
                     ChartClose(chartid);
                     Sleep(1);
                     chartid=ChartFirst();
                     continue;
                    }
                  Sleep(1);
                  chartid=ChartNext(chartid);
                  if(chartid<0)
                     break;
                  veces -= 1;
                 }
               Sleep(5);
               veces=100;
               chartid=ChartFirst();
               while(veces>0)
                 {
                  if(chartid != ChartID())
                    {
                     ChartClose(chartid);
                     Sleep(1);
                     chartid=ChartFirst();
                     continue;
                    }
                  Sleep(1);
                  chartid=ChartNext(chartid);
                  if(chartid<0)
                     break;
                  veces -= 1;
                 }


               Sleep(5);

               for(int i=1; i<k; i++)
                 {
                  Sleep(5);
                  if(result[i]!=Symbol())
                    {
                     ChID=ChartOpen(result[i],60);
                     Sleep(10);
                     ChartRedraw(ChID);
                    }
                 }



               conta=0;
               while(RunScriptCountWindows()<k-numMin && conta<100)  // Era k-1
                 {
                  Sleep(1);
                  conta=conta+1;
                 }




               RunScriptAndChangeTemplate(result[0]);



               Sleep(1000); // era 4000


               veces=100;
               chartid=ChartFirst();
               while(veces>0)
                 {
                  ChartRedraw(chartid);
                  chartid=ChartNext(chartid);

                  if(chartid<0)
                     break;
                  veces -= 1;
                  Sleep(1);
                 }



               if(StringFind(paresCartera,Symbol())<0)
                 {
                  ChartClose(); ////////////////////
                  ChartClose(); ////////////////////
                  ChartClose(); ////////////////////
                  ChartClose(); ////////////////////
                  ChartClose(); ////////////////////
                  ChartClose(); ////////////////////
                  ChartClose(); ////////////////////
                  ChartClose(); ////////////////////
                 }

               //       PostMessageA(GetParent(GetParent(GetParent(WindowHandle(Symbol(), Period())))), WM_CLOSE, 0, 0);

               HayUpdate(-1);
               Sleep(500);

               if(StringFind(paresCartera,Symbol())<0)
                 {
                  ChartClose(); ////////////////////
                  ChartClose(); ////////////////////
                  ChartClose(); ////////////////////
                  ChartClose(); ////////////////////
                  ChartClose(); ////////////////////
                  ChartClose(); ////////////////////
                  ChartClose(); ////////////////////
                  ChartClose(); ////////////////////
                  ChartClose(); ////////////////////
                 }



               tiempoCargadaCartera=TimeCurrent();
               cargCar=2;
              }

            estaEnTimer=false;

            return;




           }

        }


      static int contadorDelayRecount=50;

      if((IsPined==1))   // || (Hu==1)) // || cuentaParo>10) ) ///  || lastIsPined==1
        {



         cuentaParo=0;
         if((selectedCaudillo==1 || selectedCaudillo==4) && direcCruceLineaJ1 == CL_DibujaInicio)
            MueveLineaCursor("DProLineIniJ1");
         if((selectedCaudillo==1 || selectedCaudillo==4) && direcCruceLineaJ2 == CL_DibujaInicio)
            MueveLineaCursor("DProLineIniJ2");
         if((selectedCaudillo==1 || selectedCaudillo==4) && stopCruceLineaJ1 == CL_DibujaStop)
            MueveLineaCursor("DProStopLineJ1");
         if((selectedCaudillo==1 || selectedCaudillo==4) && stopCruceLineaJ2 == CL_DibujaStop)
            MueveLineaCursor("DProStopLineJ2");

         if((selectedCaudillo==2 || selectedCaudillo==5) && direcCruceLineaA1 == CL_DibujaInicio)
            MueveLineaCursor("DProLineIniA1");
         if((selectedCaudillo==2 || selectedCaudillo==5) && direcCruceLineaA2 == CL_DibujaInicio)
            MueveLineaCursor("DProLineIniA2");
         if((selectedCaudillo==2 || selectedCaudillo==5) && stopCruceLineaA1 == CL_DibujaStop)
            MueveLineaCursor("DProStopLineA1");
         if((selectedCaudillo==2 || selectedCaudillo==5) && stopCruceLineaA2 == CL_DibujaStop)
            MueveLineaCursor("DProStopLineA2");

         if((selectedCaudillo==3 || selectedCaudillo==6) && direcCruceLineaH1 == CL_DibujaInicio)
            MueveLineaCursor("DProLineIniH1");
         if((selectedCaudillo==3 || selectedCaudillo==6) && direcCruceLineaH2 == CL_DibujaInicio)
            MueveLineaCursor("DProLineIniH2");
         if((selectedCaudillo==3 || selectedCaudillo==6) && stopCruceLineaH1 == CL_DibujaStop)
            MueveLineaCursor("DProStopLineH1");
         if((selectedCaudillo==3 || selectedCaudillo==6) && stopCruceLineaH2 == CL_DibujaStop)
            MueveLineaCursor("DProStopLineH2");

         if((selectedCaudillo==10 || selectedCaudillo==13) && direcCruceLineaM1 == CL_DibujaInicio)
            MueveLineaCursor("DProLineIniM1");
         if((selectedCaudillo==10 || selectedCaudillo==13) && direcCruceLineaM2 == CL_DibujaInicio)
            MueveLineaCursor("DProLineIniM2");
         if((selectedCaudillo==10 || selectedCaudillo==13) && stopCruceLineaM1 == CL_DibujaStop)
            MueveLineaCursor("DProStopLineM1");
         if((selectedCaudillo==10 || selectedCaudillo==13) && stopCruceLineaM2 == CL_DibujaStop)
            MueveLineaCursor("DProStopLineM2");


         if(contadorDelayRecount>=50)
           {
            contadorDelayRecount=0;
            countTradesCaesarVar = CountTrades_CaesarX();
            countTradesAlexanderVar = CountTrades_AlexanderX();
            countTradesHannibalVar = CountTrades_HannibalX();
            countTradesCaptainVar = CountTrades_CapitanX();
            countTradesTotalParVar = CountTrades_TotalParX();
           }
         else
           {
            contadorDelayRecount+=1;
           }


        }
      else
        {
         cuentaParo +=1;
        }

      refrescar = true;
      CalculayPintaPanel();

      refrescar = false;
      if(robotReturn.actualizarMT4data==1)
        {
         robotReturn.actualizarMT4data=0;
         estaEnTimer=false;
         OnTick();
         CalculayPintaPanel();
         robotReturn.actualizarMT4data=0;
         estaEnTimer=true;
        }



      //= LINEAS EN GRAFICA =================== hay otra tanda igual en OnTick

      OperaLineaStopM2();
      OperaLineaM2();
      OperaLineaStopM1();
      OperaLineaM1();

      OperaLineaStopH2();
      OperaLineaH2();
      OperaLineaStopH1();
      OperaLineaH1();

      OperaLineaStopA2();
      OperaLineaA2();
      OperaLineaStopA1();
      OperaLineaA1();

      OperaLineaStopJ2();
      OperaLineaJ2();
      OperaLineaStopJ1();
      OperaLineaJ1();
      //=======================================

      GestionaOrdenesManuales(); // ================================================== ORDENES MANUALES ========================================
     }
   else
     {
      botlidatorChequeado=1;
     }


// ACTUALIZATPs ====================================================================
   if(CaesarAutoPriceAverage && lastCaesarTakeProfit != (int)CaesarTakeProfit)
     {
      actualizarPrecioPromedioCaesar();
      actualizarTPCaesar();
     }
   if(AlexanderAutoPriceAverage && lastAlexanderTakeProfit != (int)AlexanderTakeProfit)
     {
      actualizarPrecioPromedioAlexander();
      actualizarTPAlexander();
     }
   if(HannibalAutoPriceAverage && lastHannibalTakeProfit != (int)HannibalTakeProfit)
     {
      actualizarPrecioPromedioHannibal();
      actualizarTPHannibal();
     }
//===================================================================================


   if(((MathAbs(Bid - prevPriceBid) < 0.00001) && (MathAbs(Ask - prevPriceAsk) < 0.00001)))
     {

      if(!IsTesting() && segundosMercadoCerrado >= 600)
        {
         Botlidator(numRobot, false, -43); //3,4,7,8=mal //Llama aBotlidator con el mercado cerrado (a intervalos)
         segundosMercadoCerrado = 0; // Esto es nuevo (probar)
        }

      if(segundosMercadoCerrado<2400)
        {
         segundosMercadoCerrado += 1;
        }

      if(segundosMercadoCerrado >= 600 && segundosMercadoCerrado < 1200)
        {
         mercadoAbierto = -1; // MERCADO LENTO
        }

      if(segundosMercadoCerrado >= 2200 || (TimeDayOfWeek(TimeCurrent()) == 0 || TimeDayOfWeek(TimeCurrent()) == 6 || (  (TimeHour(TimeCurrent())==23 && TimeMinute(TimeCurrent())>55) || (TimeHour(TimeCurrent())==0 && TimeMinute(TimeCurrent())<5) )))
        {
         mercadoAbierto = 0; // MERCADO CERRADO O PARADO
        }
     }
   else
     {
      prevPriceBid = Bid;
      prevPriceAsk = Ask;
      segundosMercadoCerrado = 0;
      mercadoAbierto = 1; // MERCADO ABIERTO
      if(IsDemo())
        {
         mercadoAbierto = 3; // MERCADO DEMO
        }
     }



   if(IsTesting())
     {
      mercadoAbierto = 2; // MERCADO BACKTEST
     }
   else
     {
      if(mercadoAbierto == 1 || mercadoAbierto == 3)
        {

         if(countTradesCaesarVar + countTradesAlexanderVar + countTradesHannibalVar == 0 || primerBotlidator == 0)
           {
            primerBotlidator = 50;
            if(IsPined==1)
              {
               Botlidator(numRobot, false, -61); //3,4,7,8=mal //Llama aBotlidator con el mercado abierto y sin ordenes cada timer*5
              }
           }
         else
           {
            if(primerBotlidator > 0)
              {
               primerBotlidator -= 1;
              }
           }
        }
     }


   estaEnTimer=False;

  }
//=============================================================================================================








//+------------------------------------------------------------------+
//| IsTopWindow function                                             |
//+------------------------------------------------------------------+
int IsTopWindow(int handle = -1)
  {
   if(IsTesting())return 1;

   static int hWnd1backX=0;
   int state = 0;
   string symbol = Symbol();
   long resultH = -1;
   if(handle == -1)


     {



      int hWnd1=0;

         conta=0;
         while(hWnd1==0 && (IsVisualMode()||!IsTesting()))
           {
            if(ChartGetInteger(ChartID(), CHART_WINDOW_HANDLE, 0, resultH))
              {
               hWnd1 = (int)resultH;
              }
            else
              {
               hWnd1 = WindowHandle(Symbol(), PERIOD_CURRENT);
              }
              conta=conta+1;
              if (conta>50)
              {
                 hWnd1=hWnd1backX;
                 break;
              }
           }
           hWnd1backX=hWnd1;


     }
   else
     {
      hWnd1 = handle;
     }

   int hWnd2 = GetParent(hWnd1);
   int hWnd3 = GetParent(hWnd2);
   int hWnd4 = GetTopWindow(hWnd3);

   state = (hWnd2 == hWnd4) ? 1 : 0;

   return state;
  }

//+------------------------------------------------------------------+
//| NORMALIZE LOTS CLOSE                                                   |
//+------------------------------------------------------------------+
double NormalizeLotsClose(double lots, string pair = "")
  {
   if(pair == "")
      pair = Symbol();

   double  lotis = MathFloor((lots+(infoLotStep/2)) / infoLotStep) * infoLotStep;
   if(lotis > infoLotMax)
      lotis = infoLotMax;
   if(lotis < infoLotMin)
      lotis = infoLotMin;
   return(NormalizeDouble(lotis, 2));
  }

//+------------------------------------------------------------------+
//| NORMALIZE LOTS                                                   |
//+------------------------------------------------------------------+
double NormalizeLots(double lots, string pair = "")
  {
   if(pair == "")
      pair = Symbol();

   double  lotis = MathFloor((lots) / infoLotStep) * infoLotStep;
   if(lotis > infoLotMax)
      lotis = infoLotMax;
   if(lotis < infoLotMin && multiplicadorCierreParcialLocal<0.85)
      lotis = infoLotMin;
   return(NormalizeDouble(lotis, 2));
  }

//+------------------------------------------------------------------+
//| NORMALIZE LOTS SEND ORDER                                        |
//+------------------------------------------------------------------+
double NormalizeLotsSendOrder(double lots, string pair = "")
  {
   if(pair == "")
      pair = Symbol();
   double  lotis = MathRound((lots) / infoLotStep) * infoLotStep;
   if(lotis > infoLotMax)
      lotis = infoLotMax;
   if(lotis < infoLotMin && multiplicadorCierreParcialLocal<0.85)
      lotis = infoLotMin;

   double test=NormalizeDouble(lotis, 2);

   while(test>lotis)
     {
      test-=0.01;
     }


   return(NormalizeDouble(test, 2));
  }

//+------------------------------------------------------------------+
//| NORMALIZE TPs                                                   |
//+------------------------------------------------------------------+
double NormalizeTPBuy(double precio, double tp)
  {
   double buffer = (tp - precio);
   double canti = 0;
   while(buffer / Point < infoStopMin * Point  && !IsStopped())
     {
      buffer += Point;
      canti += Point;
     }
   return(NormalizeDouble(tp + canti, Digits));
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double NormalizeTPSell(double precio, double tp)
  {
   double buffer = (precio - tp);
   double canti = 0;
   while(buffer / Point < infoStopMin * Point  && !IsStopped())
     {
      buffer += Point;
      canti += Point;
     }
   return(NormalizeDouble(tp - canti, Digits));
  }


//+------------------------------------------------------------------+
//| The function receives chart background color.                    |
//+------------------------------------------------------------------+
color ChartBackColorGet(const long chart_ID = 0)
  {
//--- prepare the variable to receive the color
   long result = clrNONE;
//--- reset the error value
   ResetLastError();
//--- receive chart background color
   if(!ChartGetInteger(chart_ID, CHART_COLOR_BACKGROUND, 0, result))
     {
      //--- display the error message in Experts journal
      Print(__FUNCTION__ + ", Error Code = ", GetLastError());
     }
//--- return the value of the chart property
   return((color)result);
  }
//+------------------------------------------------------------------+
//| The function sets chart background color.                        |
//+------------------------------------------------------------------+
bool ChartBackColorSet(const color clr, const long chart_ID = 0)
  {
//--- reset the error value
   ResetLastError();
//--- set the chart background color
   if(!ChartSetInteger(chart_ID, CHART_COLOR_BACKGROUND, clr))
     {
      //--- display the error message in Experts journal
      Print(__FUNCTION__ + ", Error Code = ", GetLastError());
      return(false);
     }
//--- successful execution
   return(true);
  }


/*Obtiene los magic numbers mayores a los iniciales y sus pares*/
string SacaMNdePar()
  {
   int mn = 0;
   int increMN = 0;
   string salida = " ";


   if(ObjectFind("MNLabel")>=0)
     {

      mn = StrToInteger(ObjectGetString(0,"MNLabel",OBJPROP_TEXT));

      if((((mn == 10278))) || (((mn > 10278 && mn < 22324))))   //  && (StringFind(OrderComment(),"Caesar")>=0 || StringFind(OrderComment(),"Barca")>=0 || StringFind(OrderComment(),"Magnus")>=0)
        {
         increMN = 0;
         if(mn > 10278 && mn < 22324)
            increMN = mn - 10278;

         salida = StringConcatenate(salida, increMN, " ");
        }
      salida = StringConcatenate(StringTrimLeft(StringTrimRight(salida)), "#");
      return (salida);
     }













   int total=OrdersTotal();
   for(int pos=0; pos<total; pos++)
     {
      cg = OrderSelect(pos, SELECT_BY_POS, MODE_TRADES);
      if(cg && OrderSymbol() == Symbol())
        {
         mn = OrderMagicNumber();

         if((((mn == 10278) || (mn == 22324) || (mn == 23794))) || (((mn > 10278 && mn < 22324) || (mn > 22324 && mn < 23794) || (mn > 23794))))   //  && (StringFind(OrderComment(),"Caesar")>=0 || StringFind(OrderComment(),"Barca")>=0 || StringFind(OrderComment(),"Magnus")>=0)
           {
            increMN = 0;
            if(mn > 10278 && mn < 22324)
               increMN = mn - 10278;
            else
               if(mn > 22324 && mn < 23794)
                  increMN = mn - 22324;
               else
                  if(mn > 23794)
                     increMN = mn - 23794;

            if(StringFind(salida, IntegerToString(increMN) + " ") < 0) //  && increMN > 0
              {
               salida = StringConcatenate(salida, increMN, " ");
              }
           }
        }
     }
   return (StringTrimRight(salida));
  }


//+------------------------------------------------------------------+
//| Create a trend line by the given coordinates                     |
//+------------------------------------------------------------------+
bool TrendCreate(const long            chart_ID = 0,      // chart's ID
                 const string          name = "TrendLine", // line name
                 const int             sub_window = 0,    // subwindow index
                 datetime              time1 = 0,         // first point time
                 double                price1 = 0,        // first point price
                 datetime              time2 = 0,         // second point time
                 double                price2 = 0,        // second point price
                 const color           clr = clrRed,      // line color
                 const ENUM_LINE_STYLE style = STYLE_SOLID, // line style
                 const int             width = 1,         // line width
                 const bool            back = false,      // in the background
                 const bool            selection = true,  // highlight to move
                 const bool            ray_right = true, // line's continuation to the right
                 const bool            hidden = true,     // hidden in the object list
                 const long            z_order = 0)       // priority for mouse click
  {
//--- set anchor points' coordinates if they are not set
   ChangeTrendEmptyPoints(time1, price1, time2, price2);
//--- reset the error value
   ResetLastError();
//--- create a trend line by the given coordinates
   if(!ObjectCreate(chart_ID, name, OBJ_TREND, sub_window, time1, price1, time2, price2))
     {
      Print(__FUNCTION__,
            ": failed to create a trend line! Error code = ", GetLastError());
      return(false);
     }
//--- set line color
   ObjectSetInteger(chart_ID, name, OBJPROP_COLOR, clr);
//--- set line display style
   ObjectSetInteger(chart_ID, name, OBJPROP_STYLE, style);
//--- set line width
   ObjectSetInteger(chart_ID, name, OBJPROP_WIDTH, width);
//--- display in the foreground (false) or background (true)
   ObjectSetInteger(chart_ID, name, OBJPROP_BACK, back);
//--- enable (true) or disable (false) the mode of moving the line by mouse
//--- when creating a graphical object using ObjectCreate function, the object cannot be
//--- highlighted and moved by default. Inside this method, selection parameter
//--- is true by default making it possible to highlight and move the object
   ObjectSetInteger(chart_ID, name, OBJPROP_SELECTABLE, selection);
   ObjectSetInteger(chart_ID, name, OBJPROP_SELECTED, selection);
//--- enable (true) or disable (false) the mode of continuation of the line's display to the right
   ObjectSetInteger(chart_ID, name, OBJPROP_RAY_RIGHT, ray_right);
//--- hide (true) or display (false) graphical object name in the object list
   ObjectSetInteger(chart_ID, name, OBJPROP_HIDDEN, hidden);
//--- set the priority for receiving the event of a mouse click in the chart
   ObjectSetInteger(chart_ID, name, OBJPROP_ZORDER, z_order);
//--- successful execution
   return(true);
  }
//+------------------------------------------------------------------+
//| Move trend line anchor point                                     |
//+------------------------------------------------------------------+
bool TrendPointChange(const long   chart_ID = 0,     // chart's ID
                      const string name = "TrendLine", // line name
                      const int    point_index = 0,  // anchor point index
                      datetime     time = 0,         // anchor point time coordinate
                      double       price = 0)        // anchor point price coordinate
  {
//--- if point position is not set, move it to the current bar having Bid price
   if(!time)
      time = TimeCurrent();
   if(!price)
      price = SymbolInfoDouble(Symbol(), SYMBOL_BID);
//--- reset the error value
   ResetLastError();
//--- move trend line's anchor point
   if(!ObjectMove(chart_ID, name, point_index, time, price))
     {
      Print(__FUNCTION__,
            ": failed to move the anchor point! Error code = ", GetLastError());
      return(false);
     }
//--- successful execution
   return(true);
  }
//+------------------------------------------------------------------+
//| The function deletes the trend line from the chart.              |
//+------------------------------------------------------------------+
bool TrendDelete(const long   chart_ID = 0,     // chart's ID
                 const string name = "TrendLine") // line name
  {
//--- reset the error value
   ResetLastError();
//--- delete a trend line
   if(!ObjectDelete(chart_ID, name))
     {
      Print(__FUNCTION__,
            ": failed to delete a trend line! Error code = ", GetLastError());
      return(false);
     }
//--- successful execution
   return(true);
  }
//+------------------------------------------------------------------+
//| Check the values of trend line's anchor points and set default   |
//| values for empty ones                                            |
//+------------------------------------------------------------------+
void ChangeTrendEmptyPoints(datetime &time1, double &price1,
                            datetime &time2, double &price2)
  {
//--- if the first point's time is not set, it will be on the current bar
   if(!time1)
      time1 = TimeCurrent();
//--- if the first point's price is not set, it will have Bid value
   if(!price1)
      price1 = SymbolInfoDouble(Symbol(), SYMBOL_BID);
//--- if the second point's time is not set, it is located 9 bars left from the second one
   if(!time2)
     {
      //--- array for receiving the open time of the last 10 bars
      datetime temp[10];
      CopyTime(Symbol(), Period(), time1, 10, temp);
      //--- set the second point 9 bars left from the first one
      time2 = temp[0];
     }
//--- if the second point's price is not set, it is equal to the first point's one
   if(!price2)
      price2 = price1;
  }

//+------------------------------------------------------------------+
//| Devuelve una cantidad en base al PORCENTAGE de otra                                                       |
//+------------------------------------------------------------------+
///////////////////////////////////
double CantidadDePorcentage(double porcentage, double cantidad)
  {
   if(porcentage < 0)
     {
      return(0);
     }
   if(porcentage > 100)
     {
      porcentage = 10;
     }
   return(cantidad - (((100 - porcentage) / 100) * cantidad));
  }
///////////////////////////////////////////////////////



//+------------------------------------------------------------------+
//| Devuelve PORCENTAGE de una cantidad sobre el balance                                                      |
//+------------------------------------------------------------------+
///////////////////////////////////
double PorcentageDeCantidadSobreBalance(double cantidad)
  {
   double   aBal=AccountBalance();
   if(cantidad == 0.0 || aBal==0.0)
     {
      return(0.00);
     }
   return(cantidad * 100.0 / aBal);
  }
///////////////////////////////////////////////////////







//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {


   if(id == CHARTEVENT_CLICK)
     {
      ushort altKeyX=GetAsyncKeyState(18) & 0x8000;
      if(altKeyX)
        {
         if(altKey==1)
           {
            altKey=0;
           }
         else
           {
            altKey=1;
           }
        }

      mouseCoorX=(int)lparam;
      mouseCoorY=(int)dparam;
      mouseTime=0;
      mousePrice =0;
      int window=0;

      if(!ChartXYToTimePrice(0,mouseCoorX,mouseCoorY,window,mouseTime,mousePrice))
        {
         mouseTime=0;
         mousePrice=0;
        }


      string clipText="";
      for(int caudillo=0; caudillo<=3; caudillo++)
        {
         if(caudillo==0)
           {
            double ultimoPrecioOperacionBuy = FindLastBuyPrice_Caesar();
            double ultimoPrecioOperacionSell = FindLastSellPrice_Caesar();
            int tipoOperacion = tipoOperacionCaudillo(MagicNumber_Caesar);
            switch(tipoOperacion)
              {
               case  OP_BUY:

                  int distance=round((ultimoPrecioOperacionBuy - mousePrice) / Point);

                  if(distance>=0)
                    {
                     for(int i=1; i<2000000; i++)
                       {
                        if(CalculaPosNextOrd(caudillo,i,AlexanderPipStep,HannibalPipStep) <= mousePrice)
                          {
                           break;
                          }
                       }
                    }
                  else
                    {
                     for(i=1; i<2000000; i++)
                       {
                        double newTP=Point*(double)i;

                        if(promedioPrecioCaesar+newTP >= mousePrice)
                          {
                           break;
                          }
                       }
                     i=-i;
                    }
                  if(i!=2000000)
                     clipText+="·J"+i+" ";

                  break;
               case  OP_SELL:

                  distance=round((ultimoPrecioOperacionSell - mousePrice) / Point);

                  if(distance<0)
                    {
                     for(i=1; i<2000000; i++)
                       {
                        if(CalculaPosNextOrd(caudillo,i,AlexanderPipStep,HannibalPipStep) >= mousePrice)
                          {
                           break;
                          }
                       }
                    }
                  else
                    {
                     for(i=1; i<2000000; i++)
                       {
                        newTP=Point*(double)i;

                        if(promedioPrecioCaesar-newTP <= mousePrice)
                          {
                           break;
                          }
                       }
                     i=-i;
                    }

                  if(i!=2000000)
                     clipText+="·J"+i+" ";


                  break;
               default:
                  break;
              }
           }
         else
           {
            if(caudillo==1)
              {
               ultimoPrecioOperacionBuy = FindLastBuyPrice_Alexander();
               ultimoPrecioOperacionSell = FindLastSellPrice_Alexander();
               tipoOperacion = tipoOperacionCaudillo(MagicNumber_Alexander);
               switch(tipoOperacion)
                 {
                  case  OP_BUY:
                     distance=round((ultimoPrecioOperacionBuy - mousePrice) / Point);

                     if(distance>=0)
                       {
                        for(i=1; i<2000000; i++)
                          {
                           if(CalculaPosNextOrd(caudillo,CaesarPipStep,i,HannibalPipStep) <= mousePrice)
                             {
                              break;
                             }
                          }
                       }
                     else
                       {
                        for(i=1; i<2000000; i++)
                          {
                           newTP=Point*(double)i;

                           if(promedioPrecioAlexander+newTP >= mousePrice)
                             {
                              break;
                             }
                          }
                        i=-i;
                       }

                     if(i!=2000000)
                        clipText+="·A"+i+" ";
                     //              clipText+="·J"+distancePipStep+" ";

                     break;
                  case  OP_SELL:

                     distance=round((ultimoPrecioOperacionSell - mousePrice) / Point);

                     if(distance<0)
                       {
                        for(i=1; i<2000000; i++)
                          {
                           if(CalculaPosNextOrd(caudillo,CaesarPipStep,i,HannibalPipStep) >= mousePrice)
                             {
                              break;
                             }
                          }
                       }
                     else
                       {
                        for(i=1; i<2000000; i++)
                          {
                           newTP=Point*(double)i;

                           if(promedioPrecioAlexander-newTP <= mousePrice)
                             {
                              break;
                             }
                          }
                        i=-i;
                       }

                     if(i!=2000000)
                        clipText+="·A"+i+" ";
                     //              clipText+="·J"+distancePipStep+" ";


                     break;
                  default:
                     break;
                 }
              }
            else
              {
               if(caudillo==2)
                 {
                  ultimoPrecioOperacionBuy = FindLastBuyPrice_Hannibal();
                  ultimoPrecioOperacionSell = FindLastSellPrice_Hannibal();
                  tipoOperacion = tipoOperacionCaudillo(MagicNumber_Hannibal);
                  switch(tipoOperacion)
                    {
                     case  OP_BUY:
                        distance=round((ultimoPrecioOperacionBuy - mousePrice) / Point);

                        if(distance>=0)
                          {
                           for(i=1; i<2000000; i++)
                             {
                              if(CalculaPosNextOrd(caudillo,CaesarPipStep,AlexanderPipStep,i) <= mousePrice)
                                {
                                 break;
                                }
                             }
                          }
                        else
                          {
                           for(i=1; i<2000000; i++)
                             {
                              newTP=Point*(double)i;

                              if(promedioPrecioHannibal+newTP >= mousePrice)
                                {
                                 break;
                                }
                             }
                           i=-i;
                          }

                        if(i!=2000000)
                           clipText+="·H"+i+" ";

                        break;
                     case  OP_SELL:

                        distance=round((ultimoPrecioOperacionSell - mousePrice) / Point);

                        if(distance<0)
                          {
                           for(i=1; i<2000000; i++)
                             {
                              if(CalculaPosNextOrd(caudillo,CaesarPipStep,AlexanderPipStep,i) >= mousePrice)
                                {
                                 break;
                                }
                             }
                          }
                        else
                          {

                           for(i=1; i<2000000; i++)
                             {
                              newTP=Point*(double)i;
                              if(promedioPrecioHannibal-newTP <= mousePrice)
                                {
                                 break;
                                }
                             }
                           i=-i;
                          }

                        if(i!=2000000)
                           clipText+="·H"+i+" ";


                        break;
                     default:
                        break;
                    }
                 }
               else
                 {

                  if(caudillo==3)    // uso el 3 para aprovechar el bucle for, pero en realidad es el Manual 10
                    {
                     ultimoPrecioOperacionBuy = robotAcMan.ManuOrderPrice; // FindLastBuyPrice_Hannibal();
                     ultimoPrecioOperacionSell = ultimoPrecioOperacionBuy; // FindLastSellPrice_Hannibal();
                     tipoOperacion = robotAcMan.ManuOpType; // tipoOperacionCaudillo(MagicNumber_Hannibal);
                     switch(tipoOperacion)
                       {
                        case  OP_BUY:
                           distance=round((ultimoPrecioOperacionBuy - mousePrice) / Point);

                           if(distance>=0)
                             {
                              for(i=1; i<2000000; i++)
                                {
                                 double newSL=Point*(double)i;

                                 if(newSL <= mousePrice)
                                   {
                                    break;
                                   }
                                }
                             }
                           else
                             {
                              for(i=1; i<2000000; i++)
                                {
                                 newTP=Point*(double)i;

                                 if(newTP >= mousePrice)
                                   {
                                    break;
                                   }
                                }
                              i=-i;
                             }

                           if(i!=2000000)
                              clipText+="·M"+i+" ";

                           break;
                        case  OP_SELL:

                           distance=round((ultimoPrecioOperacionSell - mousePrice) / Point);

                           if(distance<0)
                             {
                              for(i=1; i<2000000; i++)
                                {
                                 newSL=Point*(double)i;
                                 if(newSL >= mousePrice)
                                   {
                                    break;
                                   }
                                }
                             }
                           else
                             {

                              for(i=1; i<2000000; i++)
                                {
                                 newTP=Point*(double)i;
                                 if(newTP <= mousePrice)
                                   {
                                    break;
                                   }
                                }
                              i=-i;
                             }

                           if(i!=2000000)
                              clipText+="·M"+i+" ";


                           break;
                        default:
                           break;
                       }
                    }

                 }
              }
           }

        }

      CopyTextToClipboard(clipText);

     }



//////////////////////////////

  }



//+------------------------------------------------------------------------------------+
//| The function defines if the mode of shift of the price chart from the right border |
//| is enabled.                                                                        |
//+------------------------------------------------------------------------------------+
bool ChartShiftGet(bool &result,const long chart_ID=0)
  {
//--- prepare the variable to get the property value
   long value;
//--- reset the error value
   ResetLastError();
//--- receive the property value
   if(!ChartGetInteger(chart_ID,CHART_SHIFT,0,value))
     {
      //--- display the error message in Experts journal
      Print(__FUNCTION__+", Error Code = ",GetLastError());
      return(false);
     }
//--- store the value of the chart property in memory
   result=value;
//--- successful execution
   return(true);
  }
//+--------------------------------------------------------------------------+
//| The function enables/disables the mode of displaying a price chart with  |
//| a shift from the right border.                                           |
//+--------------------------------------------------------------------------+
bool ChartShiftSet(const bool value,const long chart_ID=0)
  {
//--- reset the error value
   ResetLastError();
//--- set property value
   if(!ChartSetInteger(chart_ID,CHART_SHIFT,0,value))
     {
      //--- display the error message in Experts journal
      Print(__FUNCTION__+", Error Code = ",GetLastError());
      return(false);
     }
//--- successful execution
   return(true);
  }




//+-----------------------------------------------------------------------+
//| The function defines if the mode of displaying Ask value line on the  |
//| chart.                                                                |
//+-----------------------------------------------------------------------+
bool ChartShowAskLineGet(bool &result,const long chart_ID=0)
  {
//--- prepare the variable to get the property value
   long value;
//--- reset the error value
   ResetLastError();
//--- receive the property value
   if(!ChartGetInteger(chart_ID,CHART_SHOW_ASK_LINE,0,value))
     {
      //--- display the error message in Experts journal
      Print(__FUNCTION__+", Error Code = ",GetLastError());
      return(false);
     }
//--- store the value of the chart property in memory
   result=value;
//--- successful execution
   return(true);
  }





//+------------------------------------------------------------------+
//| Create Linear Regression Channel by the given coordinates        |
//+------------------------------------------------------------------+
bool RegressionCreate(const long            chart_ID=0,        // chart's ID
                      const string          name="Regression", // channel name
                      const int             sub_window=0,      // subwindow index
                      datetime              time1=0,           // first point time
                      datetime              time2=0,           // second point time
                      const color           clr=clrRed,        // channel color
                      const ENUM_LINE_STYLE style=STYLE_SOLID, // style of channel lines
                      const int             width=1,           // width of channel lines
                      const bool            fill=false,        // filling the channel with color
                      const bool            back=false,        // in the background
                      const bool            selection=true,    // highlight to move
                      const bool            ray_right=false,   // channel's continuation to the right
                      const bool            hidden=true,       // hidden in the object list
                      const long            z_order=0)         // priority for mouse click
  {
//--- set anchor points' coordinates if they are not set
   ChangeRegressionEmptyPoints(time1,time2);
//--- reset the error value
   ResetLastError();
//--- create a channel by the given coordinates
   if(!ObjectCreate(chart_ID,name,OBJ_REGRESSION,sub_window,time1,0,time2,0))
     {
      Print(__FUNCTION__,
            ": failed to create linear regression channel! Error code = ",GetLastError());
      return(false);
     }
//--- set channel color
   ObjectSetInteger(chart_ID,name,OBJPROP_COLOR,clr);
//--- set style of the channel lines
   ObjectSetInteger(chart_ID,name,OBJPROP_STYLE,style);
//--- set width of the channel lines
   ObjectSetInteger(chart_ID,name,OBJPROP_WIDTH,width);
//--- display in the foreground (false) or background (true)
   ObjectSetInteger(chart_ID,name,OBJPROP_BACK,back);
//--- enable (true) or disable (false) the mode of highlighting the channel for moving
//--- when creating a graphical object using ObjectCreate function, the object cannot be
//--- highlighted and moved by default. Inside this method, selection parameter
//--- is true by default making it possible to highlight and move the object
   ObjectSetInteger(chart_ID,name,OBJPROP_SELECTABLE,selection);
   ObjectSetInteger(chart_ID,name,OBJPROP_SELECTED,selection);
//--- enable (true) or disable (false) the mode of continuation of the channel's display to the right
   ObjectSetInteger(chart_ID,name,OBJPROP_RAY_RIGHT,ray_right);
//--- hide (true) or display (false) graphical object name in the object list
   ObjectSetInteger(chart_ID,name,OBJPROP_HIDDEN,hidden);
//--- set the priority for receiving the event of a mouse click in the chart
   ObjectSetInteger(chart_ID,name,OBJPROP_ZORDER,z_order);
//--- successful execution
   return(true);
  }
//+------------------------------------------------------------------+
//| Move the channel's anchor point                                  |
//+------------------------------------------------------------------+
bool RegressionPointChange(const long   chart_ID=0,     // chart's ID
                           const string name="Channel", // channel name
                           const int    point_index=0,  // anchor point index
                           datetime     time=0)         // anchor point time coordinate
  {
//--- if point time is not set, move the point to the current bar
   if(!time)
      time=TimeCurrent();
//--- reset the error value
   ResetLastError();
//--- move the anchor point
   if(!ObjectMove(chart_ID,name,point_index,time,0))
     {
      Print(__FUNCTION__,
            ": failed to move the anchor point! Error code = ",GetLastError());
      return(false);
     }
//--- successful execution
   return(true);
  }
//+------------------------------------------------------------------+
//| Delete the channel                                               |
//+------------------------------------------------------------------+
bool RegressionDelete(const long   chart_ID=0,     // chart's ID
                      const string name="Channel") // channel name
  {
//--- reset the error value
   ResetLastError();
//--- delete the channel
   if(!ObjectDelete(chart_ID,name))
     {
      Print(__FUNCTION__,
            ": failed to delete the channel! Error code = ",GetLastError());
      return(false);
     }
//--- successful execution
   return(true);
  }
//+-------------------------------------------------------------------------+
//| Check the values of the channel's anchor points and set default values  |
//| for empty ones                                                          |
//+-------------------------------------------------------------------------+
void ChangeRegressionEmptyPoints(datetime &time1,datetime &time2)
  {
//--- if the second point's time is not set, it will be on the current bar
   if(!time2)
      time2=TimeCurrent();
//--- if the first point's time is not set, it is located 9 bars left from the second one
   if(!time1)
     {
      //--- array for receiving the open time of the last 10 bars
      datetime temp[10];
      CopyTime(Symbol(),Period(),time2,10,temp);
      //--- set the first point 9 bars left from the second one
      time1=temp[0];
     }
  }





//+------------------------------------------------------------------+

//CaptainFX.
//+------------------------------------------------------------------+
