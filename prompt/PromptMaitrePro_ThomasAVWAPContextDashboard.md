# Prompt Maître Pro — Indicateur “ThomasAVWAPContextDashboard” (NinjaTrader 8)

Ce fichier reprend le prompt initial qui décrit le projet, les rôles attendus et les étapes de travail (clarification → specs → archi → code → guide → itérations GitHub).

Résumé rapide :
- Objectif : un indicateur NinjaTrader 8 unique combinant contexte de tendance (EMA/VWAP), suivi de l’AVWAP ancré (ThomasAnchoredVWAP_Test), détection d’un setup pullback long “AVWAP lite” et un info panel.
- Contexte utilisateur : scalping ES/NQ intraday, débutant en C#/NinjaScript.
- Structure repo : `src/` code, `specs/` specs techniques, `docs/` guide utilisateur, `prompt/` ce fichier.
- Contraintes : C# / NinjaScript 8 uniquement, code complet commenté en français, pas de dépendances externes.
- Paramètres choisis (version actuelle) : EMA 20/50, VWAP session RTH par défaut, tolérance AVWAP 8 ticks (ou 0,15 ATR si activé), ATR(14), filtres volume/alertes optionnels, panel TopRight offset 20/20.

Pour les détails complets, voir `specs/ThomasAVWAPContextDashboard.md`.
