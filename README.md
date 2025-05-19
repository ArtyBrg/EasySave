# EasySave 2.0

## 🌐 Language / Langue

- [🇫🇷 Français](#-français)
- [🇬🇧 English](#-english)

---

## 🇫🇷 Français
...contenu français ici...

EasySave est une application de sauvegarde multiplateforme, désormais dotée d'une interface graphique conviviale. Elle permet de créer, gérer et exécuter des travaux de sauvegarde complets ou différentiels de manière sécurisée et flexible, tout en répondant aux exigences des environnements professionnels.

## Nouveautés de la version 2.0

- Passage à une **interface graphique** (WPF ou Avalonia)
- **Nombre illimité** de travaux de sauvegarde
- **Cryptage des fichiers** via le logiciel *CryptoSoft*
- Détection et gestion d’un **logiciel métier** empêchant les sauvegardes
- Format de **Log JSON ou XML** (introduit dans la version 1.1)
- Amélioration des **fichiers log** : ajout du temps de cryptage
- Compatibilité multilingue : **Français et Anglais**

---

## Fonctionnalités principales

### Travaux de sauvegarde

Chaque travail de sauvegarde comprend :

- Un nom unique
- Un répertoire source et cible (disques locaux, externes, ou lecteurs réseau)
- Un type : sauvegarde **complète** ou **différentielle**
- Une option de **cryptage conditionnel** (extensions filtrées via paramètres)

### Journalisation (Log)

Chaque action est consignée dans un fichier **Log journalier** (JSON ou XML), avec :

- Date et heure
- Nom du travail de sauvegarde
- Chemin complet source et destination (UNC)
- Taille du fichier
- Temps de transfert
- **Temps de cryptage**
- Codes d'erreurs éventuels

### Fichier d'état en temps réel

Stocke en direct l’avancement de chaque sauvegarde :

- Nom du travail
- État : Actif / Inactif
- Dernière action horodatée
- Fichiers restants et transférés
- Taille restante
- Fichier en cours (source et destination)

### Gestion du logiciel métier

- Possibilité de définir un logiciel métier dans les paramètres généraux
- Lorsqu'il est détecté : pause ou blocage des sauvegardes
- Les arrêts sont consignés dans les logs
- Comportement adapté en cas de traitement séquentiel

---

## Technologies utilisées

- **.NET Core / .NET 6+**
- **WPF** (ou **Avalonia** pour compatibilité multiplateforme)
- **JSON** & **XML** via `System.Text.Json` et `System.Xml`
- **Multilingue** : fichiers de ressources `.resx` pour i18n
- **Bibliothèque externe** : `CryptoSoft` pour le cryptage

---

## 📦 Structure du projet

```
/EasySave
│
├── /UI            → Interface graphique (WPF/Avalonia)
├── /Core          → Logique métier (sauvegardes, état, paramètres)
├── /Logger        → DLL de log (JSON/XML - compatible v1.0)
├── /Resources     → Langues (fr-FR, en-EN)
├── /CryptoSoft    → Interface d’intégration du logiciel de cryptage
├── /Configs       → Paramètres généraux (fichiers JSON)
├── /Logs          → Fichiers journaux (JSON/XML)
├── /State         → Fichier unique d’état des travaux
```

---

## ⚙️ Installation

### Prérequis

- .NET 6.0 ou supérieur
- Windows, Linux ou macOS (si Avalonia)
- `CryptoSoft.exe` disponible dans le PATH ou défini dans les paramètres

### Exécution

```bash
dotnet run --project EasySave.exe
```

#### Ajout d'arguments possible pour lancer les jobs en mode console
```bash
dotnet run --project EasySave.exe 1-3 # Exécute les jobs 1 à 3
dotnet run --project EasySave.exe 1 ;3 # Exécute les jobs 1 et 3
dotnet run --project EasySave.exe 3 # Exécute le job 3
```

---

## Exemple d’utilisation

1. **Créer un travail de sauvegarde** via l’interface
2. **Sélectionner les répertoires source/cible**
3. Définir le type : `Complète` ou `Différentielle`
4. Spécifier les extensions à crypter
5. Lancer la sauvegarde (ou enchaînement séquentiel)
6. Suivre la progression via l’interface ou le fichier `state.json`
7. Consulter les logs en `Logs/` (format défini par utilisateur)

---

## À propos du cryptage

- Le logiciel utilise **CryptoSoft** pour chiffrer les fichiers
- Seules les extensions définies dans les paramètres sont concernées
- Le temps de cryptage est enregistré dans les logs :
  - `0` → non chiffré
  - `>0` → cryptage OK (ms)
  - `<0` → erreur lors du cryptage

---

## Emplacements recommandés

- **Logs & état** : utiliser des emplacements valides pour serveurs clients
  - Exemple : `C:\ProgramData\EasySave\Logs\`
  - À éviter : `C:	emp\`

---

## Historique des versions

| Version | Description |
|--------|-------------|
| 1.0     | Version console avec 5 travaux max, logs JSON, DLL de log |
| 1.1     | Choix du format de log (JSON ou XML) |
| 2.0     | Interface graphique, travaux illimité, CryptoSoft, XML, gestion métier |

---

## 📄 Licence

Projet développé dans le cadre d’un projet pédagogique – Non destiné à une distribution commerciale.

---

## Contact

Pour toute remarque, amélioration ou signalement de bug contacter :  
**Équipe EasySave – CESI**  
Membres de l'équipe :
- Chloé ARMAND : chloe.armand@viacesi.fr
- Arthur BERGBAUER : arthur.bergbauer@viacesi.fr
- Yvan Mounir MBOPUWO LINJOUOM NJOYA : yvanmounir.mbopuwolinjouomnjoya@viacesi.fr
- Bruno RIECKENBERG : bruno.rieckenberg@viacesi.fr


# 🇬🇧 English
...English content here...

EasySave is a cross-platform backup application, now featuring a user-friendly graphical interface. It allows you to create, manage, and execute full or differential backup tasks securely and flexibly, while meeting professional environment requirements.

## What’s New in Version 2.0

- Switched to a **graphical interface** (WPF or Avalonia)
- **Unlimited** backup jobs
- **File encryption** via *CryptoSoft*
- Detection and handling of **business software** that may block backups
- **Log format** in JSON or XML (introduced in v1.1)
- Improved **log files**: added encryption time
- Multilingual support: **French and English**

---

## Key Features

### Backup Jobs

Each backup job includes:

- A unique name
- A source and destination directory (local, external, or network drives)
- A type: **full** or **differential** backup
- A **conditional encryption** option (file extensions filtered via settings)

### Logging

Each action is recorded in a **daily log file** (JSON or XML), including:

- Date and time
- Backup job name
- Full source and destination paths (UNC)
- File size
- Transfer time
- Encryption time
- Any error codes

### Real-Time Status File

Tracks each backup’s progress in real time:

- Job name
- Status: Active / Inactive
- Last timestamped action
- Remaining and transferred files
- Remaining size
- Current file (source and destination)

### Business Software Handling

- Ability to define business software in general settings
- When detected: pause or block backups
- Stops are logged
- Behavior adapts for sequential processing

---

## Technologies Used

- **.NET Core / .NET 6+**
- **WPF** (or **Avalonia** for cross-platform compatibility)
- **JSON** & **XML** via `System.Text.Json` and `System.Xml`
- **Multilingual**: `.resx` resource files for i18n
- **External library**: `CryptoSoft` for encryption

---

## Project Structure

```
/EasySave
│
├── /UI            → Graphical interface (WPF/Avalonia)
├── /Core          → Business logic (backups, state, settings)
├── /Logger        → Logging DLL (JSON/XML - v1.0 compatible)
├── /Resources     → Languages (fr-FR, en-EN)
├── /CryptoSoft    → Encryption software integration interface
├── /Configs       → General settings (JSON files)
├── /Logs          → Log files (JSON/XML)
├── /State         → Single backup job state file
```

---

## Installation

### Requirements

- .NET 6.0 or higher
- Windows, Linux, or macOS (if using Avalonia)
- `CryptoSoft.exe` available in PATH or set in settings

### Execution

```bash
dotnet run --project EasySave.UI
```

#### Add arguments to run jobs in console mode
```bash
dotnet run --project EasySave.exe 1-3 # Runs jobs 1 to 3
dotnet run --project EasySave.exe 1 ;3 # Runs jobs 1 and 3
dotnet run --project EasySave.exe 3 # Run job 3
```

---

## Usage Example

1. **Create a backup job** via the interface
2. **Select source/destination directories**
3. Set the type: `Full` or `Differential`
4. Specify extensions to encrypt
5. Launch the backup (or sequential chaining)
6. Monitor progress via the interface or `state.json`
7. Check logs in `Logs/` (format defined by user)

---

## About Encryption

- The software uses **CryptoSoft** to encrypt files
- Only extensions defined in the settings are affected
- Encryption time is logged:
  - `0` → not encrypted
  - `>0` → encrypted OK (ms)
  - `<0` → encryption error

---

## Recommended Locations

- **Logs & state**: use valid client-server paths
  - Example: `C:\ProgramData\EasySave\Logs\`
  - Avoid: `C:	emp\`

---

## Version History

| Version | Description |
|--------|-------------|
| 1.0     | Console version with 5 job limit, JSON logs, logging DLL |
| 1.1     | Choice of log format (JSON or XML) |
| 2.0     | Graphical interface, unlimited jobs, CryptoSoft, XML, business software handling |

---

## License

Developed as part of an educational project – Not intended for commercial distribution.

---

## Contact

For feedback, suggestions, or bug reports, contact:  
**EasySave Team – CESI**  
Team Members:
- Chloé ARMAND : chloe.armand@viacesi.fr
- Arthur BERGBAUER : arthur.bergbauer@viacesi.fr
- Yvan Mounir MBOPUWO LINJOUOM NJOYA : yvanmounir.mbopuwolinjouomnjoya@viacesi.fr
- Bruno RIECKENBERG : bruno.rieckenberg@viacesi.fr
