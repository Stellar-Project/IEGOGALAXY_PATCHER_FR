# IEGOGALAXY_PATCHER_FR

Installateur du patch de traduction française pour **Inazuma Eleven GO Galaxy** (Bigbang & Supernova), destiné à la communauté française du jeu.

## Présentation

IEGOGALAXY_PATCHER_FR est une application Windows qui automatise l'installation du patch de traduction française, que ce soit pour une utilisation sur émulateur (Citra, Azahar) ou sur une console 3DS modifiée.

L'application détecte automatiquement le chemin d'installation approprié selon la plateforme sélectionnée, vérifie l'intégrité du patch téléchargé, sauvegarde l'installation existante avant chaque mise à jour, et permet de revenir en arrière en cas de problème.

## Fonctionnalités

- Sélection de la version du jeu (Bigbang ou Supernova)
- Détection automatique du dossier d'installation pour Citra, Azahar ou une carte SD 3DS
- Sélection manuelle du dossier de destination
- Détection de la version du patch déjà installée, avec indication d'une mise à jour disponible
- Téléchargement du patch avec suivi de progression détaillé (taille téléchargée, vitesse implicite via mises à jour régulières)
- Vérification d'intégrité du fichier téléchargé (checksum SHA256)
- Sauvegarde automatique de l'installation existante avant chaque nouveau patch
- Restauration en un clic de la sauvegarde précédente
- Annulation possible en cours de téléchargement
- Nouvelle tentative automatique en cas d'échec réseau, avec délai d'expiration configuré
- Journal d'application détaillé pour le support et le diagnostic
- Mise à jour automatique de l'application elle-même

## Prérequis

Cette application nécessite le **.NET 10 Desktop Runtime** (x64) pour fonctionner.

[Télécharger le .NET Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

Sur la page de téléchargement, choisir **.NET Desktop Runtime** (et non .NET Runtime) dans la section consacrée aux applications de bureau, en version **x64**.

## Installation

1. Installer le .NET Desktop Runtime si ce n'est pas déjà fait (voir Prérequis)
2. Télécharger la dernière version de `IEGOGALAXY_PATCHER_FR.exe` depuis la page [Releases](https://github.com/Stellar-Project/IEGOGALAXY_PATCHER_FR/releases)
3. Lancer l'exécutable
4. Sélectionner la version du jeu et la plateforme cible
5. Vérifier ou ajuster le chemin d'installation proposé
6. Cliquer sur "Télécharger et Installer le patch"

## Utilisation

### Sur émulateur (Citra / Azahar)

L'application détecte automatiquement le dossier de mods de l'émulateur sélectionné dans le répertoire `AppData` de l'utilisateur. Aucune configuration supplémentaire n'est nécessaire dans la plupart des cas.

### Sur console 3DS modifiée

L'application recherche une carte SD amovible contenant un dossier `Nintendo 3DS` ou `luma`, et propose automatiquement le chemin d'installation correspondant sous `luma/titles/`. Si plusieurs cartes SD sont détectées, la sélection du dossier doit être faite manuellement.

Il est possible de définir manuellement un chemin différent via le bouton de parcours si la détection automatique échoue.

### Annulation et restauration

Un téléchargement en cours peut être annulé à tout moment via le bouton "Annuler". En cas de problème après une installation, le bouton "Restaurer" permet de revenir à l'état précédent le dernier patch appliqué.

### Diagnostic

En cas d'erreur, un code d'erreur est affiché ainsi que le chemin du fichier journal de l'application, utile pour obtenir de l'aide sur le serveur discord.

## Développement

### Prérequis techniques

- .NET 10 SDK
- Visual Studio 2022 (ou tout IDE compatible C#/WPF)

### Compilation locale

```bash
dotnet restore
dotnet build -c Release
```

### Publication

```bash
dotnet publish IEGOGALAXY_PATCHER_FR/IEGOGALAXY_PATCHER_FR.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

### Releases automatisées

Chaque tag suivant le format `vX.Y.Z` poussé sur le dépôt déclenche automatiquement, via GitHub Actions :

- la compilation de l'application
- le calcul du checksum SHA256 du binaire
- la génération du fichier `update.xml` utilisé par le système de mise à jour automatique
- la publication d'une nouvelle Release GitHub avec l'exécutable en pièce jointe

## Architecture technique

- **Interface** : WPF avec MahApps.Metro
- **Mise à jour automatique** : AutoUpdater.NET
- **Sélection de dossier** : Ookii.Dialogs.Wpf

Le code est organisé autour de deux gestionnaires principaux :

- `PatchManager` : téléchargement (avec retry et timeout), vérification d'intégrité, extraction, sauvegarde/restauration et installation du patch
- `PathManager` : détection des chemins d'installation selon la plateforme et vérification sommaire de la présence du jeu
- `LogManager` : journalisation des opérations dans `%AppData%/IEGOGALAXY_PATCHER_FR/log.txt`

### Gestion des erreurs

Les erreurs liées au processus de patch sont catégorisées via `PatchErrorCode` (réseau, délai d'expiration, intégrité du fichier, extraction, installation, annulation), ce qui permet d'afficher un message précis à l'utilisateur et de faciliter le diagnostic.

## Licence

Ce projet est distribué sous licence GPL-3.0. Voir le fichier [LICENSE.txt](./LICENSE.txt) pour plus de détails.

## Communauté

Développé par la team Stellar-Project pour la communauté française d'Inazuma Eleven.