#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using System.Windows.Media;
#endregion

// Ce code est prêt pour NinjaTrader 8 (OnBarClose), sans dépendances externes
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
        private Indicators.ThomasAnchoredVWAP_Test anchoredAvwap;

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
                    DisplayName = Name;

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

                    anchoredAvwap = Indicators.ThomasAnchoredVWAP_Test();
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
            {
                Brush regimeBrush = GetRegimeBrush();
                BackBrushes[0] = regimeBrush;
            }
        }

        private void UpdateAvwapMetrics()
        {
            hasAvwap = anchoredAvwap != null && !anchoredAvwap[0].IsNaN();
            if (!hasAvwap)
            {
                lastDistanceTicks = double.NaN;
                lastDistanceAtr = double.NaN;
                isCloseToAvwap = false;
                return;
            }

            double avwapValue = anchoredAvwap[0];
            double diff = Math.Abs(Close[0] - avwapValue);
            lastDistanceTicks = diff / TickSize;
            double atrValue = atr[0];
            lastDistanceAtr = atrValue.ApproxCompare(0) == 0 ? double.NaN : diff / atrValue;

            if (UseAtrProximity)
                isCloseToAvwap = atrValue.ApproxCompare(0) != 0 && diff <= AvwapProximityAtr * atrValue;
            else
                isCloseToAvwap = lastDistanceTicks <= AvwapProximityTicks;
        }

        private bool IsPullbackStructureValid()
        {
            double atrValue = atr[0];
            double range = High[0] - Low[0];
            double body = Math.Abs(Close[0] - Open[0]);

            if (range < 0.3 * atrValue || body < 0.1 * atrValue)
                return false;

            bool correction = (Close[0] < Close[1] && Close[1] < Close[2]) || (Low[0] < Math.Min(Low[1], Low[2]));
            if (!correction)
                return false;

            bool bullishCandle = Close[0] > Open[0];
            if (!bullishCandle)
                return false;

            double lowerWick = Close[0] - Low[0];
            if (body.ApproxCompare(0) == 0 || lowerWick < 1.5 * body)
                return false;

            double position = (Close[0] - Low[0]) / range;
            return position >= 0.70;
        }

        private bool PassesVolumeFilter()
        {
            if (!UseVolumeFilter)
                return true;

            if (volumeSma == null || CurrentBar < VolumeSmaPeriod || volumeSma[0].IsNaN())
                return false;

            return Volume[0] >= 0.5 * volumeSma[0];
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
                Alert("ThomasAVWAPLiteLong" + CurrentBar, Priority.High, AlertMessage, AlertSound, 0, Brushes.Transparent, Brushes.DeepSkyBlue);
        }

        private void DrawSignal()
        {
            string tag = $"TAVWAP_Signal_{CurrentBar}";
            Draw.TriangleUp(this, tag, false, 0, Low[0] - TickSize, Brushes.DeepSkyBlue);
            signalTags.Add(tag);

            if (signalTags.Count > MaxSignals)
            {
                string oldest = signalTags[0];
                RemoveDrawObject(oldest);
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

            bool ready = EnablePullbackDetection && (currentRegime == TrendRegime.Bull || currentRegime == TrendRegime.StrongBull) && isCloseToAvwap && hasAvwap;
            panelBuilder.AppendLine($"Setup AVWAP lite : {(ready ? "Ready" : "No")}");

            string lastSig = lastSignalBar < 0 ? "Aucun signal" : $"{CurrentBar - lastSignalBar} bars ago";
            panelBuilder.AppendLine($"Last Signal : {lastSig}");

            Draw.TextFixed(this, "TAVWAP_PANEL", panelBuilder.ToString(), PanelPosition, Brushes.White, new SimpleFont("Arial", PanelFontSize), panelBackground, Brushes.Transparent, PanelOffsetX, PanelOffsetY);
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
    }
}
