#region Using declarations 
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
using System.Windows.Media;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class ThomasAVWAPContextDashboard : Indicator
    {
        #region enums / const
        private enum TrendRegime
        {
            StrongBull,
            Bull,
            Neutral,
            Bear
        }
        #endregion

        #region champs privés
        private EMA emaFast;
        private EMA emaSlow;
        private VWAP sessionVwap;
        private ATR atr;
        private SMA volumeSma;
        private VWAP anchoredAvwap;

        private TrendRegime currentRegime = TrendRegime.Neutral;
        private int regimeScore = 0;
        private int lastSignalBar = -1000;
        private double lastDistanceTicks = double.NaN;
        private double lastDistanceAtr = double.NaN;
        private bool isCloseToAvwap;
        private bool hasAvwap;

        private readonly List<string> signalTags = new List<string>();
        private SolidColorBrush panelBackground;
        private StringBuilder panelBuilder;
        #endregion

        #region Propriétés utilisateur
        [NinjaScriptProperty]
        [Display(Name = "Fast EMA", Order = 1, GroupName = "Paramètres tendance")]
        public int FastEmaPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Slow EMA", Order = 2, GroupName = "Paramètres tendance")]
        public int SlowEmaPeriod { get; set; }

        [XmlIgnore]
        [Display(Name = "Session template", Order = 3, GroupName = "Paramètres tendance")]
        public TradingHours SessionTemplate { get; set; }

        [Browsable(false)]
        public string SessionTemplateSerialized
        {
            get { return SessionTemplate != null ? SessionTemplate.Name : string.Empty; }
            set { SessionTemplate = TradingHours.Get(value); }
        }

        [NinjaScriptProperty]
        [Display(Name = "Tolérance AVWAP (ticks)", Order = 1, GroupName = "Proximité AVWAP")]
        public int AvwapProximityTicks { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Utiliser ATR pour proximité", Order = 2, GroupName = "Proximité AVWAP")]
        public bool UseAtrProximity { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Tolérance AVWAP (ATR)", Order = 3, GroupName = "Proximité AVWAP")]
        public double AvwapProximityAtr { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Période ATR", Order = 4, GroupName = "Proximité AVWAP")]
        public int AtrPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Activer coloration régime", Order = 1, GroupName = "Affichage")]
        public bool EnableTrendColoring { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Activer détection pullback", Order = 2, GroupName = "Affichage")]
        public bool EnablePullbackDetection { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Afficher info panel", Order = 3, GroupName = "Affichage")]
        public bool ShowInfoPanel { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Taille police panel", Order = 4, GroupName = "Affichage")]
        public int PanelFontSize { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Position panel", Order = 5, GroupName = "Affichage")]
        public TextPosition PanelPosition { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Offset X panel", Order = 6, GroupName = "Affichage")]
        public int PanelOffsetX { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Offset Y panel", Order = 7, GroupName = "Affichage")]
        public int PanelOffsetY { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Activer alertes", Order = 1, GroupName = "Alertes")]
        public bool EnableAlerts { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Message alerte", Order = 2, GroupName = "Alertes")]
        public string AlertMessage { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Son alerte", Order = 3, GroupName = "Alertes")]
        public string AlertSound { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min bars entre signaux", Order = 1, GroupName = "Signaux")]
        public int MinBarsBetweenSignals { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max dessins signaux", Order = 2, GroupName = "Signaux")]
        public int MaxSignals { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Filtre volume", Order = 1, GroupName = "Volume")]
        public bool UseVolumeFilter { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Période SMA volume", Order = 2, GroupName = "Volume")]
        public int VolumeSmaPeriod { get; set; }
        #endregion

        #region Overrides
        protected override void OnStateChange()
        {
            switch (State)
            {
                case State.SetDefaults:
                    Name = "ThomasAVWAPContextDashboard";
                    Description = "Dashboard tendance + AVWAP + pullback AVWAP lite + info panel.";
                    Calculate = Calculate.OnBarClose;
                    IsOverlay = true;

                    FastEmaPeriod = 20;
                    SlowEmaPeriod = 50;
                    SessionTemplate = TradingHours.Get("CME US Index Futures RTH");

                    AvwapProximityTicks = 8;
                    UseAtrProximity = false;
                    AvwapProximityAtr = 0.15;
                    AtrPeriod = 14;

                    EnableTrendColoring = true;
                    EnablePullbackDetection = true;
                    ShowInfoPanel = true;
                    PanelFontSize = 12;
                    PanelPosition = TextPosition.TopRight;
                    PanelOffsetX = 20;
                    PanelOffsetY = 20;

                    EnableAlerts = false;
                    AlertMessage = "ThomasAVWAPContextDashboard : Setup AVWAP lite long détecté.";
                    AlertSound = "Alert1";

                    MinBarsBetweenSignals = 5;
                    MaxSignals = 200;

                    UseVolumeFilter = false;
                    VolumeSmaPeriod = 20;

                    AddPlot(Brushes.DeepSkyBlue, "LongSignal");

                    panelBackground = new SolidColorBrush(Color.FromArgb(120, 20, 20, 20));
                    panelBackground.Freeze();
                    panelBuilder = new StringBuilder();
                    break;

                case State.Configure:
                    TradingHours = SessionTemplate;
                    break;

                case State.DataLoaded:
                    emaFast = EMA(FastEmaPeriod);
                    emaSlow = EMA(SlowEmaPeriod);
                    sessionVwap = VWAP();
                    atr = ATR(AtrPeriod);
                    if (UseVolumeFilter)
                        volumeSma = SMA(Volume, VolumeSmaPeriod);

                    anchoredAvwap = VWAP();
                    break;
            }
        }

        protected override void OnBarUpdate()
        {
            int barsRequired = Math.Max(Math.Max(FastEmaPeriod, SlowEmaPeriod), AtrPeriod) + 2;
            if (CurrentBar < barsRequired)
            {
                LongSignal[0] = 0;
                return;
            }

            UpdateTrendRegime();
            UpdateAvwapMetrics();
            DetectPullbackSignal();
            UpdateInfoPanel();
        }
        #endregion

        #region Méthodes privées
        private void UpdateTrendRegime()
        {
            double fast = emaFast[0];
            double slow = emaSlow[0];
            double vwap = sessionVwap[0];

            bool bull = Close[0] > fast && fast > slow && Close[0] > vwap;
            bool bear = Close[0] < fast && fast < slow && Close[0] < vwap;

            if (bull)
            {
                bool strong = emaFast[0] > emaFast[1] && (fast - slow) > 0;
                currentRegime = strong ? TrendRegime.StrongBull : TrendRegime.Bull;
                regimeScore = strong ? 2 : 1;
            }
            else if (bear)
            {
                currentRegime = TrendRegime.Bear;
                regimeScore = -1;
            }
            else
            {
                currentRegime = TrendRegime.Neutral;
                regimeScore = 0;
            }

            if (EnableTrendColoring)
                BackBrushes[0] = GetRegimeBrush();
        }

        private void UpdateAvwapMetrics()
        {
            double avwapVal = anchoredAvwap != null ? anchoredAvwap[0] : double.NaN;
            hasAvwap = !double.IsNaN(avwapVal);

            if (!hasAvwap)
            {
                lastDistanceTicks = double.NaN;
                lastDistanceAtr = double.NaN;
                isCloseToAvwap = false;
                return;
            }

            double diff = Math.Abs(Close[0] - avwapVal);
            lastDistanceTicks = diff / TickSize;

            double atrValue = atr[0];
            if (double.IsNaN(atrValue) || atrValue == 0)
            {
                lastDistanceAtr = double.NaN;
            }
            else
            {
                lastDistanceAtr = diff / atrValue;
            }

            if (UseAtrProximity)
            {
                if (double.IsNaN(atrValue) || atrValue == 0)
                    isCloseToAvwap = false;
                else
                    isCloseToAvwap = diff <= AvwapProximityAtr * atrValue;
            }
            else
            {
                isCloseToAvwap = lastDistanceTicks <= AvwapProximityTicks;
            }
        }

        private bool IsPullbackStructureValid()
        {
            double atrValue = atr[0];
            double range = High[0] - Low[0];
            double body = Math.Abs(Close[0] - Open[0]);

            if (double.IsNaN(atrValue) || atrValue <= 0)
                return false;

            if (range < 0.3 * atrValue || body < 0.1 * atrValue)
                return false;

            bool correction = (Close[0] < Close[1] && Close[1] < Close[2]) || (Low[0] < Math.Min(Low[1], Low[2]));
            if (!correction)
                return false;

            if (Close[0] <= Open[0])
                return false;

            double lowerWick = Close[0] - Low[0];
            if (body <= 0 || lowerWick < 1.5 * body)
                return false;

            double position = (Close[0] - Low[0]) / range;
            return position >= 0.70;
        }

        private bool PassesVolumeFilter()
        {
            if (!UseVolumeFilter)
                return true;

            if (volumeSma == null || CurrentBar < VolumeSmaPeriod)
                return false;

            double sma = volumeSma[0];
            if (double.IsNaN(sma) || sma <= 0)
                return false;

            return Volume[0] >= 0.5 * sma;
        }

        private void DetectPullbackSignal()
        {
            LongSignal[0] = 0;

            bool bullishRegime = currentRegime == TrendRegime.Bull || currentRegime == TrendRegime.StrongBull;
            if (!EnablePullbackDetection || !bullishRegime || !hasAvwap || !isCloseToAvwap)
                return;

            if (!PassesVolumeFilter())
                return;

            if (CurrentBar - lastSignalBar < MinBarsBetweenSignals)
                return;

            if (!IsPullbackStructureValid())
                return;

            LongSignal[0] = 1;
            lastSignalBar = CurrentBar;
            DrawSignal();

            if (EnableAlerts)
            {
                Alert(
                    "ThomasAVWAPLiteLong" + CurrentBar,
                    Priority.High,
                    AlertMessage,
                    AlertSound,
                    0,
                    Brushes.Transparent,
                    Brushes.DeepSkyBlue);
            }
        }

        private void DrawSignal()
        {
            string tag = $"TAVWAP_Signal_{CurrentBar}";
            Draw.TriangleUp(this, tag, false, 0, Low[0] - TickSize, Brushes.DeepSkyBlue);
            signalTags.Add(tag);

            if (signalTags.Count > MaxSignals)
            {
                RemoveDrawObject(signalTags[0]);
                signalTags.RemoveAt(0);
            }
        }

        private void UpdateInfoPanel()
        {
            if (!ShowInfoPanel)
                return;

            panelBuilder.Clear();
            panelBuilder.AppendLine($"Trend : {currentRegime}");
            panelBuilder.AppendLine($"Regime Score : {regimeScore}");
            panelBuilder.AppendLine($"AVWAP : {(hasAvwap ? "Active" : "Inactive")}");

            string distTicksText = double.IsNaN(lastDistanceTicks) ? "n/a" : lastDistanceTicks.ToString("0.0");
            string distAtrText = double.IsNaN(lastDistanceAtr) ? "n/a" : lastDistanceAtr.ToString("0.00");
            panelBuilder.AppendLine($"Dist AVWAP : {distTicksText} ticks / {distAtrText} ATR");

            bool ready = EnablePullbackDetection
                && (currentRegime == TrendRegime.Bull || currentRegime == TrendRegime.StrongBull)
                && isCloseToAvwap
                && hasAvwap;
            panelBuilder.AppendLine($"Setup AVWAP lite : {(ready ? "Ready" : "No")}");

            string lastSig = lastSignalBar < 0 ? "Aucun signal" : $"{CurrentBar - lastSignalBar} bars ago";
            panelBuilder.AppendLine($"Last Signal : {lastSig}");

            Draw.TextFixed(
                this,
                "TAVWAP_PANEL",
                panelBuilder.ToString(),
                PanelPosition,
                Brushes.White,
                new SimpleFont("Arial", PanelFontSize),
                panelBackground,
                Brushes.Transparent,
                0);
        }

        private Brush GetRegimeBrush()
        {
            switch (currentRegime)
            {
                case TrendRegime.StrongBull:
                    return new SolidColorBrush(Color.FromArgb(40, 0, 200, 0));
                case TrendRegime.Bull:
                    return new SolidColorBrush(Color.FromArgb(30, 0, 150, 0));
                case TrendRegime.Bear:
                    return new SolidColorBrush(Color.FromArgb(30, 200, 0, 0));
                default:
                    return new SolidColorBrush(Color.FromArgb(20, 100, 100, 100));
            }
        }
        #endregion

        #region Séries / Plots
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> LongSignal
        {
            get { return Values[0]; }
        }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
{
private ThomasAVWAPContextDashboard[] cacheThomasAVWAPContextDashboard;
public ThomasAVWAPContextDashboard ThomasAVWAPContextDashboard(int fastEmaPeriod, int slowEmaPeriod, int avwapProximityTicks, bool useAtrProximity, double avwapProximityAtr, int atrPeriod, bool enableTrendColoring, bool enablePullbackDetection, bool showInfoPanel, int panelFontSize, TextPosition panelPosition, int panelOffsetX, int panelOffsetY, bool enableAlerts, string alertMessage, string alertSound, int minBarsBetweenSignals, int maxSignals, bool useVolumeFilter, int volumeSmaPeriod)
{
return ThomasAVWAPContextDashboard(Input, fastEmaPeriod, slowEmaPeriod, avwapProximityTicks, useAtrProximity, avwapProximityAtr, atrPeriod, enableTrendColoring, enablePullbackDetection, showInfoPanel, panelFontSize, panelPosition, panelOffsetX, panelOffsetY, enableAlerts, alertMessage, alertSound, minBarsBetweenSignals, maxSignals, useVolumeFilter, volumeSmaPeriod);
}

public ThomasAVWAPContextDashboard ThomasAVWAPContextDashboard(ISeries<double> input, int fastEmaPeriod, int slowEmaPeriod, int avwapProximityTicks, bool useAtrProximity, double avwapProximityAtr, int atrPeriod, bool enableTrendColoring, bool enablePullbackDetection, bool showInfoPanel, int panelFontSize, TextPosition panelPosition, int panelOffsetX, int panelOffsetY, bool enableAlerts, string alertMessage, string alertSound, int minBarsBetweenSignals, int maxSignals, bool useVolumeFilter, int volumeSmaPeriod)
{
if (cacheThomasAVWAPContextDashboard != null)
for (int idx = 0; idx < cacheThomasAVWAPContextDashboard.Length; idx++)
if (cacheThomasAVWAPContextDashboard[idx] != null && cacheThomasAVWAPContextDashboard[idx].FastEmaPeriod == fastEmaPeriod && cacheThomasAVWAPContextDashboard[idx].SlowEmaPeriod == slowEmaPeriod && cacheThomasAVWAPContextDashboard[idx].AvwapProximityTicks == avwapProximityTicks && cacheThomasAVWAPContextDashboard[idx].UseAtrProximity == useAtrProximity && cacheThomasAVWAPContextDashboard[idx].AvwapProximityAtr == avwapProximityAtr && cacheThomasAVWAPContextDashboard[idx].AtrPeriod == atrPeriod && cacheThomasAVWAPContextDashboard[idx].EnableTrendColoring == enableTrendColoring && cacheThomasAVWAPContextDashboard[idx].EnablePullbackDetection == enablePullbackDetection && cacheThomasAVWAPContextDashboard[idx].ShowInfoPanel == showInfoPanel && cacheThomasAVWAPContextDashboard[idx].PanelFontSize == panelFontSize && cacheThomasAVWAPContextDashboard[idx].PanelPosition == panelPosition && cacheThomasAVWAPContextDashboard[idx].PanelOffsetX == panelOffsetX && cacheThomasAVWAPContextDashboard[idx].PanelOffsetY == panelOffsetY && cacheThomasAVWAPContextDashboard[idx].EnableAlerts == enableAlerts && cacheThomasAVWAPContextDashboard[idx].AlertMessage == alertMessage && cacheThomasAVWAPContextDashboard[idx].AlertSound == alertSound && cacheThomasAVWAPContextDashboard[idx].MinBarsBetweenSignals == minBarsBetweenSignals && cacheThomasAVWAPContextDashboard[idx].MaxSignals == maxSignals && cacheThomasAVWAPContextDashboard[idx].UseVolumeFilter == useVolumeFilter && cacheThomasAVWAPContextDashboard[idx].VolumeSmaPeriod == volumeSmaPeriod && cacheThomasAVWAPContextDashboard[idx].EqualsInput(input))
return cacheThomasAVWAPContextDashboard[idx];
return CacheIndicator<ThomasAVWAPContextDashboard>(new ThomasAVWAPContextDashboard(){ FastEmaPeriod = fastEmaPeriod, SlowEmaPeriod = slowEmaPeriod, AvwapProximityTicks = avwapProximityTicks, UseAtrProximity = useAtrProximity, AvwapProximityAtr = avwapProximityAtr, AtrPeriod = atrPeriod, EnableTrendColoring = enableTrendColoring, EnablePullbackDetection = enablePullbackDetection, ShowInfoPanel = showInfoPanel, PanelFontSize = panelFontSize, PanelPosition = panelPosition, PanelOffsetX = panelOffsetX, PanelOffsetY = panelOffsetY, EnableAlerts = enableAlerts, AlertMessage = alertMessage, AlertSound = alertSound, MinBarsBetweenSignals = minBarsBetweenSignals, MaxSignals = maxSignals, UseVolumeFilter = useVolumeFilter, VolumeSmaPeriod = volumeSmaPeriod }, input, ref cacheThomasAVWAPContextDashboard);
}
}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
{
public Indicators.ThomasAVWAPContextDashboard ThomasAVWAPContextDashboard(int fastEmaPeriod, int slowEmaPeriod, int avwapProximityTicks, bool useAtrProximity, double avwapProximityAtr, int atrPeriod, bool enableTrendColoring, bool enablePullbackDetection, bool showInfoPanel, int panelFontSize, TextPosition panelPosition, int panelOffsetX, int panelOffsetY, bool enableAlerts, string alertMessage, string alertSound, int minBarsBetweenSignals, int maxSignals, bool useVolumeFilter, int volumeSmaPeriod)
{
return indicator.ThomasAVWAPContextDashboard(Input, fastEmaPeriod, slowEmaPeriod, avwapProximityTicks, useAtrProximity, avwapProximityAtr, atrPeriod, enableTrendColoring, enablePullbackDetection, showInfoPanel, panelFontSize, panelPosition, panelOffsetX, panelOffsetY, enableAlerts, alertMessage, alertSound, minBarsBetweenSignals, maxSignals, useVolumeFilter, volumeSmaPeriod);
}

public Indicators.ThomasAVWAPContextDashboard ThomasAVWAPContextDashboard(ISeries<double> input , int fastEmaPeriod, int slowEmaPeriod, int avwapProximityTicks, bool useAtrProximity, double avwapProximityAtr, int atrPeriod, bool enableTrendColoring, bool enablePullbackDetection, bool showInfoPanel, int panelFontSize, TextPosition panelPosition, int panelOffsetX, int panelOffsetY, bool enableAlerts, string alertMessage, string alertSound, int minBarsBetweenSignals, int maxSignals, bool useVolumeFilter, int volumeSmaPeriod)
{
return indicator.ThomasAVWAPContextDashboard(input, fastEmaPeriod, slowEmaPeriod, avwapProximityTicks, useAtrProximity, avwapProximityAtr, atrPeriod, enableTrendColoring, enablePullbackDetection, showInfoPanel, panelFontSize, panelPosition, panelOffsetX, panelOffsetY, enableAlerts, alertMessage, alertSound, minBarsBetweenSignals, maxSignals, useVolumeFilter, volumeSmaPeriod);
}
}
}

namespace NinjaTrader.NinjaScript.Strategies
{
public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
{
public Indicators.ThomasAVWAPContextDashboard ThomasAVWAPContextDashboard(int fastEmaPeriod, int slowEmaPeriod, int avwapProximityTicks, bool useAtrProximity, double avwapProximityAtr, int atrPeriod, bool enableTrendColoring, bool enablePullbackDetection, bool showInfoPanel, int panelFontSize, TextPosition panelPosition, int panelOffsetX, int panelOffsetY, bool enableAlerts, string alertMessage, string alertSound, int minBarsBetweenSignals, int maxSignals, bool useVolumeFilter, int volumeSmaPeriod)
{
return indicator.ThomasAVWAPContextDashboard(Input, fastEmaPeriod, slowEmaPeriod, avwapProximityTicks, useAtrProximity, avwapProximityAtr, atrPeriod, enableTrendColoring, enablePullbackDetection, showInfoPanel, panelFontSize, panelPosition, panelOffsetX, panelOffsetY, enableAlerts, alertMessage, alertSound, minBarsBetweenSignals, maxSignals, useVolumeFilter, volumeSmaPeriod);
}

public Indicators.ThomasAVWAPContextDashboard ThomasAVWAPContextDashboard(ISeries<double> input , int fastEmaPeriod, int slowEmaPeriod, int avwapProximityTicks, bool useAtrProximity, double avwapProximityAtr, int atrPeriod, bool enableTrendColoring, bool enablePullbackDetection, bool showInfoPanel, int panelFontSize, TextPosition panelPosition, int panelOffsetX, int panelOffsetY, bool enableAlerts, string alertMessage, string alertSound, int minBarsBetweenSignals, int maxSignals, bool useVolumeFilter, int volumeSmaPeriod)
{
return indicator.ThomasAVWAPContextDashboard(input, fastEmaPeriod, slowEmaPeriod, avwapProximityTicks, useAtrProximity, avwapProximityAtr, atrPeriod, enableTrendColoring, enablePullbackDetection, showInfoPanel, panelFontSize, panelPosition, panelOffsetX, panelOffsetY, enableAlerts, alertMessage, alertSound, minBarsBetweenSignals, maxSignals, useVolumeFilter, volumeSmaPeriod);
}
}
}

#endregion
