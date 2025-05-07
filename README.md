# EasySave - Version 1.0

<<<<<<< HEAD
📘 [Français](#📘-lire-ce-readme-en-français) | 📙 [English](#📙-read-this-readme-in-english)

---

## 📘 Lire ce README en Français

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
- 1-3   # Exécute les sauvegardes 1 à 3
- 1;3   # Exécute les sauvegardes 1 et 3

### Fichiers générés
- og_YYYY-MM-DD.json : journal des actions
- state.json : état temps réel des sauvegardes

### Structure technique
- Architecture préparée pour MVVM (version GUI future)
- Composant de log intégré sous forme de DLL réutilisable
- Fichiers de configuration et journaux : JSON, avec retour à la ligne pour la lisibilité
