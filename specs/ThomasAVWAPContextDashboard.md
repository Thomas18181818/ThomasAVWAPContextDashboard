# ThomasAVWAPContextDashboard — Spécifications techniques

## 1. Description fonctionnelle
Indicateur NinjaTrader 8 qui combine, dans un seul dashboard, quatre blocs :
- **Contexte de tendance (Trend Regime)** via deux EMA et le VWAP de session.
- **Surveillance AVWAP ancré** (ThomasAnchoredVWAP_Test) avec distance prix–AVWAP et test de proximité.
- **Détection de pullback long “setup AVWAP lite”** (price action simple, sans imbalances).
- **Info Panel** consolidant les informations clés en temps réel.

## 2. Paramètres (inputs)
| Paramètre | Type | Défaut | Rôle |
| --- | --- | --- | --- |
| FastEmaPeriod | int | 20 | Période EMA rapide. |
| SlowEmaPeriod | int | 50 | Période EMA lente. |
| SessionTemplate | Session trading hours | RTH US | Choix RTH/ETH pour le VWAP de session. |
| Calculate | NinjaTrader Calculate | OnBarClose | Mode de calcul (v1). |
| AvwapProximityTicks | int | 8 | Tolérance en ticks pour dire “proche AVWAP”. |
| UseAtrProximity | bool | false | Si vrai, on remplace la tolérance en ticks par un multiple d’ATR. |
| AvwapProximityAtr | double | 0.15 | Multiple d’ATR utilisé si UseAtrProximity = true. |
| AtrPeriod | int | 14 | Période de l’ATR (ATR standard). |
| EnableTrendColoring | bool | true | Active la coloration de fond/bougies selon le régime. |
| EnablePullbackDetection | bool | true | Active la détection du setup AVWAP lite. |
| ShowInfoPanel | bool | true | Affiche ou non le panneau d’information. |
| PanelFontSize | int | 12 | Taille du texte du panel. |
| PanelPosition | TextPosition | TopRight | Position du panel (TopLeft/TopRight/BottomLeft/BottomRight). |
| PanelOffsetX | int | 20 | Décalage X du panel en pixels. |
| PanelOffsetY | int | 20 | Décalage Y du panel en pixels. |
| EnableAlerts | bool | false | Active l’alerte sonore/texte pour le signal. |
| AlertMessage | string | "ThomasAVWAPContextDashboard : Setup AVWAP lite long détecté." | Message d’alerte. |
| AlertSound | string | "Alert1" | Son NinjaTrader utilisé si EnableAlerts. |
| MinBarsBetweenSignals | int | 5 | Espacement minimal entre deux signaux. |
| MaxSignals | int | 200 | Limite de dessins de signaux (les plus anciens sont supprimés). |
| UseVolumeFilter | bool | false | Filtre de volume optionnel. |
| VolumeSmaPeriod | int | 20 | Période de SMA volume pour le filtre. |

## 3. Bloc A – Contexte de tendance (Trend Regime)
- Indicateurs : EMA rapide (FastEmaPeriod), EMA lente (SlowEmaPeriod), VWAP de session (basé sur SessionTemplate).
- Règles :
  - **Bullish** : Close > EMA rapide > EMA lente ET Close > VWAP session.
  - **Bearish** : Close < EMA rapide < EMA lente ET Close < VWAP session.
  - **Neutral** : tout le reste.
- Ajout d’un sous-niveau “StrongBull” : Bullish + pente EMA rapide positive et écart EMA rapide – EMA lente > 0 (option simple, sans seuil). “StrongBear” non utilisé mais état Bear reste affiché.
- Sorties :
  - Variable d’état `RegimeState` (enum : StrongBull, Bull, Neutral, Bear).
  - `RegimeScore` numérique : StrongBull = 2, Bull = 1, Neutral = 0, Bear = -1.
  - Coloration conditionnelle (fond ou bougies) si EnableTrendColoring = true.

## 4. Bloc B – Surveillant AVWAP ancré
- Réutilise l’indicateur custom **ThomasAnchoredVWAP_Test** (plot principal index 0, nommé "AVWAP").
- Distance prix–AVWAP : `distanceTicks = Abs(Close - AVWAP) / TickSize`.
- Proximité AVWAP :
  - Si `UseAtrProximity = false` : proche si `distanceTicks <= AvwapProximityTicks`.
  - Si `UseAtrProximity = true` : proche si `Abs(Close - AVWAP) <= AvwapProximityAtr * ATR`.
- Expose la distance en ticks et en ATR pour l’Info Panel.

## 5. Bloc C – Détection pullback long “setup AVWAP lite”
Conditions cumulatives (vérifier uniquement si régime = Bull ou StrongBull) :
1. **Correction préalable** :
   - Au moins 2 bougies baissières consécutives (Close < Close[1] et Close[1] < Close[2])
   **OU**
   - `Low[0] < Min(Low[1], Low[2])` (nouveau plus bas relatif sur 2 barres).
2. **Bougie de signal (rejet haussier)** :
   - Bougie verte : Close > Open.
   - Mèche basse significative : `LowerWick >= 1.5 * Body`, où `LowerWick = Close - Low` (bougie verte) et `Body = Abs(Close - Open)`.
   - Close dans la partie haute : `(Close - Low) / (High - Low) >= 0.70`.
   - Filtre taille : rejeter si `High - Low < 0.3 * ATR` **ou** `Body < 0.1 * ATR`.
3. **Proximité AVWAP** : condition du Bloc B valide sur la bougie de signal.
4. **Filtre volume optionnel** : si UseVolumeFilter = true, ignorer le signal si `Volume[0] < 0.5 * SMA(Volume, 20)`.
5. **Anti-spam** : `CurrentBar - lastSignalBar >= MinBarsBetweenSignals`.

Sorties :
- Plot booléen `LongSignal` (1 quand signal). 
- Dessin d’un triangle Up (cyan) sous la barre signal (Draw.TriangleUp) avec gestion MaxSignals.
- Alerte optionnelle (EnableAlerts) utilisant AlertMessage + AlertSound.

## 6. Bloc D – Info Panel (Dashboard)
- Affichage texte (Draw.TextFixed) si ShowInfoPanel = true.
- Position par défaut : TopRight, offset (PanelOffsetX = 20, PanelOffsetY = 20), taille police PanelFontSize = 12.
- Fond semi-transparent (pinceau semi-opaque) pour lisibilité.
- Contenu (ordre des lignes) :
  1. `Trend : [StrongBull / Bull / Neutral / Bear]`
  2. `Regime Score : [valeur]`
  3. `AVWAP : [Active/Inactive]`
  4. `Dist AVWAP : [X ticks] / [Y ATR]`
  5. `Setup AVWAP lite : [Ready / No]`
  6. `Last Signal : [n bars ago]` (ou "Aucun signal").

## 7. Pseudo-code OnBarUpdate
```
if CurrentBar < max(FastEmaPeriod, SlowEmaPeriod, AtrPeriod) + 5 : return

// Bloc A : Régime
regime = CalcRegime(EMAfast, EMAslow, SessionVWAP)
regimeScore = map(regime)
color chart if EnableTrendColoring

// Bloc B : AVWAP
avwap = ThomasAnchoredVWAP_Test()[0]
distanceTicks = Abs(Close - avwap) / TickSize
distanceAtr = Abs(Close - avwap) / ATR
procheAvwap = UseAtrProximity ? Abs(Close - avwap) <= AvwapProximityAtr * ATR : distanceTicks <= AvwapProximityTicks

// Bloc C : Pullback lite
ready = regime in {Bull, StrongBull} && procheAvwap
if ready && EnablePullbackDetection && passesPullbackPattern && passesVolume && antiSpam
    LongSignal[0] = 1
    Draw triangle + alert + mémorise lastSignalBar
else
    LongSignal[0] = 0

// Bloc D : Panel
if ShowInfoPanel : UpdatePanel(regime, regimeScore, distanceTicks, distanceAtr, lastSignalBar)
```

## 8. Cas particuliers
- Manque de données : ne rien calculer tant que les périodes nécessaires ne sont pas disponibles.
- Indicateur AVWAP absent/null : afficher "AVWAP : Inactive" et neutraliser la condition de proximité (donc pas de signal).
- SessionTemplate : utiliser la template choisie dans les propriétés de l’indicateur.
- MaxSignals : suppression du dessin le plus ancien quand la limite est dépassée.

## 9. Limites connues et extensions
- Version v1 : calcul uniquement OnBarClose (pas d’intrabar). Extension possible vers OnPriceChange.
- Setup uniquement côté long ; logique short à ajouter ultérieurement si besoin.
- Filtre volume simple (SMA 20) ; pourrait être affiné (ex. percentile session).
- Régime StrongBear non implémenté (Bear unique). Extension possible avec pente EMA lente.


## 10. Architecture proposée (classe Indicator)
- **Namespace** : `NinjaTrader.NinjaScript.Indicators`.
- **Champs privés** :
  - EMA `emaFast`, `emaSlow`; VWAP `sessionVwap`; ATR `atr`; SMA `volumeSma` (optionnel) ; ThomasAnchoredVWAP_Test `anchoredAvwap`.
  - États : `RegimeState currentRegime`; `int regimeScore`; `int lastSignalBar` ; `double lastDistanceTicks/lastDistanceAtr` ; `List<string> signalTags` pour gérer MaxSignals.
- **Propriétés [NinjaScriptProperty]** pour tous les paramètres listés en section 2 (inputs publics + descripteurs).
- **OnStateChange** :
  - `State.SetDefaults` : nom, description, paramètres par défaut, `Calculate = OnBarClose`, `IsOverlay = true`, `AddPlot` pour le plot booléen LongSignal.
  - `State.Configure` : rien de spécial (plots déjà ajoutés en defaults).
  - `State.DataLoaded` : instancier EMA, VWAP session, ATR, SMA(volume) si UseVolumeFilter, et l’indicateur custom ThomasAnchoredVWAP_Test.
- **OnBarUpdate** (ordre) :
  1. `if (CurrentBar < barsRequired) return;` où `barsRequired = Math.Max(Math.Max(FastEmaPeriod, SlowEmaPeriod), AtrPeriod) + 2`.
  2. `UpdateTrendRegime()` : déduire regime + score, appliquer coloration conditionnelle.
  3. `UpdateAvwapMetrics()` : lire valeur AVWAP, calculer distance ticks/ATR + proximité.
  4. `DetectPullbackSignal()` : vérifier pattern price action, volume, anti-spam, proximité, régime bullish ; poser plot + dessin + alerte ; mémoriser `lastSignalBar`.
  5. `UpdateInfoPanel()` : composer texte multi-lignes, Draw.TextFixed avec fond semi-transparent et offset.
- **Méthodes utilitaires privées** : `UpdateTrendRegime`, `UpdateAvwapMetrics`, `IsPullbackStructureValid`, `PassesVolumeFilter`, `DrawSignal`, `UpdateInfoPanel`, `GetRegimeBrush`.
- **Performances** : calcul OnBarClose uniquement, allocations minimisées (StringBuilder réutilisé, liste de tags limitée), pas de try/catch inutile.
