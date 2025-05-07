# EasySave - Version 1.0

📘 [Français](#lire-ce-readme-en-français) | 📙 [English](#read-this-readme-in-english)

---

## Lire ce README en Français

### Présentation

EasySave est une application console développée en .NET Core permettant de gérer jusqu'à 5 travaux de sauvegarde. Elle prend en charge les sauvegardes **complètes** et **différentielles**, tout en générant des fichiers JSON pour le suivi en temps réel et l’historique des actions.

### Fonctionnalités

- Création de 5 travaux de sauvegarde maximum
- Types de sauvegarde : complète ou différentielle
- Exécution via ligne de commande : `1-3`, `1;3`
- Gestion multilingue : Français / Anglais
- Prise en charge : disques locaux, externes, lecteurs réseau
- Fichier log journalier (au format JSON, lisible dans Notepad)
- Fichier d’état temps réel (JSON)
- Journalisation en temps réel via une DLL

### Utilisation

```bash
dotnet run
```

Puis entrez une commande comme :
```bash
1-3   # Exécute les sauvegardes 1 à 3
1;3   # Exécute les sauvegardes 1 et 3
```

### Fichiers générés
- og_YYYY-MM-DD.json : journal des actions
- state.json : état temps réel des sauvegardes

### Structure technique
- Architecture préparée pour MVVM (version GUI future)
- Composant de log intégré sous forme de DLL réutilisable
- Fichiers de configuration et journaux : JSON, avec retour à la ligne pour la lisibilité

---

## Read this README in English
Overview
EasySave is a .NET Core console application that manages up to 5 backup jobs. It supports both full and differential backups, while generating JSON files for real-time state and daily logging.

### Features
- Up to 5 configurable backup jobs
- Backup types: Full or Differential
- Command-line execution: 1-3, 1;3
- Multilingual: French / English
- Support for local disks, external drives, and network shares
- Daily log file (JSON format, human-readable)
- Real-time state file (JSON)
- Real-time logging via a reusable DLL

### Usage
```bash
dotnet run
```

Then enter a command like:

```bash
1-3   # Runs backups 1 to 3
1;3   # Runs backups 1 and 3
```

### Generated Files
- log_YYYY-MM-DD.json: log of file transfers
- state.json: live state tracking of backups

### Technical Design
- Architecture ready for MVVM (future GUI version)
- Logging implemented as a reusable DLL
- Config and log files in readable JSON format with line breaks