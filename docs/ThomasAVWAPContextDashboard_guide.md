# Guide d’intégration et de tests — ThomasAVWAPContextDashboard

Ce guide explique comment installer, compiler et tester l’indicateur dans NinjaTrader 8.

## Installation
1. Ouvrir NinjaTrader 8 → menu **New > NinjaScript Editor**.
2. Dans l’explorateur de fichiers NinjaScript, clic droit sur le dossier **Indicators** → **New Indicator**.
3. Copier le contenu de `src/ThomasAVWAPContextDashboard.cs` dans l’éditeur, remplacer le squelette, puis **Compile**.
4. Vérifier qu’aucune erreur n’apparaît dans l’Output (sinon relancer la compilation après correction).

## Ajout sur un graphique
1. Ouvrir un graphique (ex. NQ 5 min, session RTH).
2. Clic droit → **Indicators** → chercher **ThomasAVWAPContextDashboard**.
3. Paramètres principaux :
   - **FastEmaPeriod / SlowEmaPeriod** : 20 / 50 (défaut).
   - **SessionTemplate** : RTH par défaut (changer en ETH si besoin).
   - **AvwapProximityTicks** : 8 ou cocher **UseAtrProximity** avec **AvwapProximityAtr = 0.15**.
   - **AtrPeriod** : 14.
   - **EnableTrendColoring / EnablePullbackDetection / ShowInfoPanel** : activés par défaut.
   - **EnableAlerts** : désactivé par défaut (activer si souhaité, son "Alert1").
   - **UseVolumeFilter** : désactivé par défaut (volume < 50 % SMA20 ignoré si activé).
4. Cliquer **Apply** puis **OK**.

## Lecture rapide du panel
- Trend + Regime Score : état actuel (StrongBull/Bull/Neutral/Bear) et score 2/1/0/-1.
- AVWAP : Active si l’indicateur custom renvoie une valeur valide.
- Dist AVWAP : distance en ticks et en ATR.
- Setup AVWAP lite : Ready quand régime haussier + proximité AVWAP + conditions pullback valides.
- Last Signal : nombre de barres depuis le dernier triangle bleu (ou "Aucun signal").

## Plan de tests
1. **Test de base NQ RTH** : charger quelques jours de données RTH 5 min, vérifier coloration de fond selon régime EMA/VWAP.
2. **Affichage AVWAP** : ajouter aussi l’indicateur custom ThomasAnchoredVWAP_Test pour vérifier que le dashboard détecte l’état "Active" et affiche la distance.
3. **Proximité AVWAP** : modifier AvwapProximityTicks (ex. 2 vs 10) et observer la ligne "Ready" qui bascule quand le prix touche l’AVWAP.
4. **Signal pullback** : simuler/repérer une séquence de 2 bougies baissières touchant l’AVWAP puis une bougie de rejet haussier (mèche basse > 1,5x corps, close en haut). Vérifier apparition du triangle cyan et du plot LongSignal.
5. **Filtre volume** : activer UseVolumeFilter, vérifier qu’aucun signal ne se déclenche sur les barres dont le volume < 50 % de SMA20.
6. **Alertes** : activer EnableAlerts, confirmer la réception du message/son à la détection du signal.

## Conseils de debug
- Si compilation échoue : vérifier que `ThomasAnchoredVWAP_Test` est bien présent dans vos indicateurs, et que les using/namespace restent `NinjaTrader.NinjaScript.Indicators`.
- Si aucun AVWAP détecté : ouvrir l’indicateur custom sur le même graphique pour confirmer qu’il produit un plot (index 0).
- Si trop de triangles : augmenter `MinBarsBetweenSignals` ou diminuer `MaxSignals`.
- Performances : l’indicateur calcule uniquement **OnBarClose** en v1 ; passer éventuellement sur un timeframe plus large si besoin.
