# ✈ Flight Companion

![Version](https://img.shields.io/badge/version-v0.3-brightgreen)
![Platform](https://img.shields.io/badge/MSFS-2024-blue)
![Framework](https://img.shields.io/badge/.NET_Framework_4.8-purple)
![Status](https://img.shields.io/badge/status-active-success)

Flight Companion est un assistant de vol en temps réel pour Microsoft Flight Simulator 2024 utilisant SimConnect.

Son objectif est d'aider le pilote pendant toutes les phases du vol grâce à des calculs automatiques et des informations de navigation.

---

# Fonctionnalités

## Données de vol en temps réel

- Altitude
- Vitesse sol
- Vitesse verticale
- Cap
- Nom de l'avion

---

## Profils avions

Détection automatique de nombreux appareils :

- Cessna
- Beechcraft
- Cirrus
- Diamond
- Daher
- Pilatus
- ATR
- Airbus
- Boeing
- Bell
- Airbus Helicopters
- etc.

Chaque profil possède :

- vitesse d'approche
- vitesse finale
- vitesse de montée
- taux de descente conseillé

---

## Détection automatique des phases de vol

- Parking
- Taxi
- Décollage
- Montée
- Croisière
- Descente
- Approche
- Atterrissage

---

## Navigation GPS

Lecture automatique du plan de vol SimConnect.

Affichage :

- plan actif
- waypoint suivant
- progression
- distance restante
- temps restant

---

## Calculateur de descente

Calcul automatique :

- VS conseillé
- Top Of Descent (TOD)
- temps avant TOD
- distance nécessaire
- conseils de descente

Possibilité d'utiliser automatiquement la distance GPS du plan de vol.

---

# Captures d'écran

À venir.

---

# Installation

1. Cloner le dépôt

```
git clone https://github.com/MrBrioche74/FlightCompanion.git
```

2. Ouvrir la solution Visual Studio.

3. Copier :

```
Microsoft.FlightSimulator.SimConnect.dll
```

dans le dossier du projet.

4. Compiler.

5. Lancer Microsoft Flight Simulator 2024.

6. Lancer Flight Companion.

---

# Roadmap

## Version 0.4

- Moving Map
- Checklists intelligentes
- Profils avions enrichis

## Version 0.5

- Assistant VNAV
- Calcul automatique des descentes
- Conseils de vitesse

## Version 0.6

- Copilote intelligent
- Alertes vocales
- Gestion des approches

## Version 1.0

- Assistant de vol complet
- Navigation avancée
- Support de la majorité des avions MSFS

---

# Licence

Projet Open Source sous licence MIT.