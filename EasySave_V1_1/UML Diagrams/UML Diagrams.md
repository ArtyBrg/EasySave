# UML Diagrams

📘 [Français](#documentation-des-diagrammes-UML) | 📙 [English](#uml-diagram-documentation)

## 🧾 Documentation des diagrammes UML

**Projet : EasySave – Version 1.1**  
**Date :** `19 mai 2025`  
**Groupe :** `Groupe 2`
**Chef de projet :** `Bruno RIECKENBERG`

---

### Sommaire

1. [Diagramme de cas d’utilisation (Use Case)](#1-diagramme-de-cas-dutilisation-use-case)
    
2. [Diagramme de séquence](#2-diagramme-de-séquence)
    
3. [Diagramme de classes](#3-diagramme-de-classes)
    
4. [Diagramme de composants](#4-diagramme-de-composants)
    

---

### 1. Diagramme de cas d’utilisation (Use Case)

Le diagramme de cas d’utilisation présente les interactions principales entre l’utilisateur et l’application EasySave. Il permet de visualiser les différentes fonctionnalités accessibles à l’utilisateur via l’interface console.

#### Objectifs :

- Définir les fonctionnalités offertes à l’utilisateur
    
- Mettre en évidence les dépendances internes (logger, état)
    

> _Voir l'image dans `pictures/usecase.png`._
> _Diagramme réalisé avec PlantUML. Voir le code dans `diagrams/usecase.puml`._

---

### 2. Diagramme de séquence

Le diagramme de séquence modélise l’enchaînement des opérations lors du lancement et de l’exécution de sauvegardes, incluant la gestion de la langue, la création de travaux et l’exécution conditionnelle des sauvegardes complètes ou différentielles.

#### Points clés :

- Langue sélectionnée au démarrage
    
- Gestion par `BackupManager` des processus
    
- Journalisation et mise à jour de l'état en temps réel
    

> _Voir l'image dans `pictures/sequence.png`._
> _Voir le code dans `diagrams/sequence.puml`._

---

### 3. Diagramme de classes

Ce diagramme structure l’architecture orientée objet de la solution. Il présente les classes principales, leurs attributs, méthodes et relations.

#### Classes principales :

- `BackupJob` : décrit une tâche de sauvegarde
    
- `BackupManager` : orchestre l’exécution
    
- `Logger`, `StateWriter` : responsables des sorties JSON
    
- `LanguageManager` : gestion multilingue
    

> _Voir l'image dans `pictures/classes.png`._
> _Voir le code dans `diagrams/classes.puml`._

---

### 4. Diagramme de composants

Ce diagramme met en lumière la structure modulaire de l’application, notamment l’usage de composants réutilisables comme la DLL de journalisation (`Logger.dll`).

#### Objectif :

- Identifier les modules logiques du projet
    
- Préparer une architecture modulaire pour les futures versions (GUI)
    

> _Voir l'image dans `pictures/components.png`._
> _Voir le code dans `diagrams/components.puml`._

---

### Conclusion

Ces diagrammes ont permis de :

- Clarifier le comportement de l’application en version console
    
- Structurer son architecture logicielle
    
- Préparer son extensibilité vers une version 2.0 avec interface graphique (MVVM)

- Les diagrammes seront modifiés pour chaque nouvelle version majeure (ou modification mineure dans le cadre de la V1)
    

👉 Tous les diagrammes sont générés avec **PlantUML** et stockés dans le dossier `/diagrams`.

---
---

## UML Diagram Documentation

**Project:** EasySave – Version 1.1 
**Date:** `May 19, 2025`  
**Team:** `Group 2`
**Project manager:** `Bruno RIECKENBERG`

---

### Table of Contents

1. [Use Case Diagram](#1-use-case-diagram)
    
2. [Sequence Diagram](#2-sequence-diagram)
    
3. [Class Diagram](#3-class-diagram)
    
4. [Component Diagram](#4-component-diagram)
    

---

### 1. Use Case Diagram

The use case diagram outlines the primary interactions between the user and the EasySave console application. It highlights the core features available through the command-line interface.

#### Objectives:

- Define the user-accessible features
    
- Show internal component dependencies (logger, state management)
    

> _Picture in `pictures/usecase.png`._
> _Diagram created with PlantUML. Source code in `diagrams/usecase.puml`._

---

### 2. Sequence Diagram

The sequence diagram models the flow of operations during **command input and backup execution**, including **language selection**, **job creation**, and conditional handling of **full or differential backups**.

#### Highlights:

- Language selection at startup
    
- BackupManager handles the core logic
    
- Real-time logging and state tracking
    

> _Picture in `pictures/sequence.png`._
> _Source file: `diagrams/sequence.puml`_

---

### 3. Class Diagram

This diagram shows the object-oriented architecture of the solution. It defines the main classes, their attributes, methods, and relationships.

#### Key classes:

- `BackupJob`: defines a backup task
    
- `BackupManager`: orchestrates the execution
    
- `Logger`, `StateWriter`: handle JSON output
    
- `LanguageManager`: manages multilingual support
    

> _Picture in `pictures/classes.png`._
> _Source file: `diagrams/classes.puml`_

---

### 4. Component Diagram

The component diagram provides a modular view of the application, especially the reusable logging component (`Logger.dll`) and planned extensibility for GUI versioning.

#### Purpose:

- Identify the main logical modules
    
- Support modularity and scalability for version 2.0 (MVVM GUI)
    

> _Picture in `pictures/components.png`._
> _Source file: `diagrams/components.puml`_

---

### Conclusion

These diagrams helped to:

- Clarify the expected behavior of the console version
    
- Structure the overall software design
    
- Prepare for a GUI-based version (2.0) using MVVM architecture

- The diagrams will be modified for each new major version (or minor modification for V1).
    

👉 All diagrams are created with **PlantUML** and stored in the `/diagrams` folder.
