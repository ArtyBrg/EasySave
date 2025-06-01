# EasySave 3.0

## 🌐 Language / Langue

* [🇫🇷 Français](#-français)
* [🇬🇧 English](#-english)

---

## 🇫🇷 Français

EasySave est une application de sauvegarde multiplateforme, dotée d'une interface graphique moderne et d'une console distante. Elle permet de créer, gérer et exécuter des travaux de sauvegarde de manière efficace, parallèle et sécurisée.

## Nouveautés de la version 3.0

* **Exécution en parallèle** des travaux de sauvegarde
* **Priorité des fichiers** : les fichiers avec extensions prioritaires sont traités en premier
* **Limitation de bande passante** : interdiction de transférer simultanément plusieurs fichiers > *n* Ko (paramétrable)
* **Contrôle en temps réel** : Pause / Reprise / Arrêt par travail ou global
* **Pause automatique** en cas de détection d'un logiciel métier
* **Console distante** (Client graphique) connectée via **Sockets**
* **CryptoSoft mono-instance** : chiffrage séquentiel pour garantir l'exclusivité

---

## Fonctionnalités principales

### Travaux de sauvegarde

* Nom unique
* Répertoires source et cible
* Type : sauvegarde **complète** ou **différentielle**
* Extensions à chiffrer (cryptage conditionnel)
* Priorisation de certaines extensions

### Journalisation (Log)

Chaque action est enregistrée (format JSON ou XML) avec :

* Date/heure
* Nom du job
* Chemins complets
* Taille, temps de transfert, temps de cryptage
* Erreurs éventuelles

### Fichier d'état en temps réel

Pour chaque travail :

* Nom, état (actif/inactif)
* Fichiers restants/transférés
* Taille restante
* Pourcentage d'avancement
* Fichier en cours

### Interaction temps réel

L'utilisateur peut pour chaque travail (ou globalement) :

* ⏸ **Mettre en pause** (appliqué après le fichier en cours)
* ▶️ **Relancer/Reprendre**
* ⏹ **Arrêter immédiatement**

### Console distante (EasySave Client)

* Connexion via IP/Port (Sockets)
* Suivi en temps réel des jobs
* Contrôle des jobs à distance

### Gestion du logiciel métier

* Détection d’un logiciel métier
* Mise en pause automatique des sauvegardes
* Reprise automatique à la fermeture du logiciel

---

## Installation

### Téléchargement

* `EASYSAVE_V3_0.zip` → Application principale
* `EasySaveClient.zip` → Console distante (Client)

### Installation

1. Décompressez les archives.
2. Placez `EasySave.exe` et `EasySaveClient.exe` dans les dossiers souhaités.
3. Au premier lancement de l'application, les dossiers suivants seront créés automatiquement :

   * `Logs`, `Settings`, `BackupsJobs`, `States`
4. Lancez simplement l'exécutable pour profiter de l'application.

---

## Utilisation

### Application principale (EasySave)

Bandeau de navigation avec les principales fonctions :

* Création de jobs
* Liste des jobs
* Décryptage
* Paramètres
* Lancement du serveur distant
* Accès aux logs

### Console distante (Client)

1. Lancer le **serveur distant** dans l'application principale (IP + port affiché).
2. Démarrer le **client distant**.
3. Entrer **IP** et **port** pour se connecter.
4. Suivre et contrôler les jobs en temps réel (Play / Pause / Stop).

---

## Technologies utilisées

* **.NET 8**, **WPF** 
* **Sockets TCP** pour la communication client/serveur
* **JSON / XML** (`System.Text.Json`, `System.Xml`)
* **.resx** pour la gestion multilingue (Fr / En)
* **CryptoSoft** mono-instance pour le chiffrement

---

## Historique des versions

| Version | Description                                                            |
| ------- | ---------------------------------------------------------------------- |
| 1.0     | Version console avec 5 jobs max, logs JSON                             |
| 1.1     | Choix du format de log (JSON/XML)                                      |
| 2.0     | Interface graphique, jobs illimités, CryptoSoft, gestion métier        |
| 3.0     | Sauvegardes parallèles, priorités, client distant, contrôle temps réel |

---

## 📝 Licence

Projet pédagogique élaboré dans le cadre d’un cursus étudiant à CESI. Non destiné à un usage commercial.

---

## Contact

**Équipe EasySave – CESI**

* Chloé ARMAND : [chloe.armand@viacesi.fr](mailto:chloe.armand@viacesi.fr)
* Arthur BERGBAUER : [arthur.bergbauer@viacesi.fr](mailto:arthur.bergbauer@viacesi.fr)
* Yvan Mounir MBOPUWO LINJOUOM NJOYA : [yvanmounir.mbopuwolinjouomnjoya@viacesi.fr](mailto:yvanmounir.mbopuwolinjouomnjoya@viacesi.fr)
* Bruno RIECKENBERG : [bruno.rieckenberg@viacesi.fr](mailto:bruno.rieckenberg@viacesi.fr)

---

## 🇬🇧 English

**EasySave** is a cross-platform backup application with a modern GUI and remote monitoring client. It lets you create, manage, and run parallel backup jobs efficiently and securely.

### What's new in version 3.0

* **Parallel job execution**
* **File priority management**: prioritized extensions processed first
* **Bandwidth limiter**: large files (> n KB, configurable) can't be transferred in parallel
* **Live interaction**: Pause / Play / Stop per job or globally
* **Auto-pause when business software is detected**
* **Remote GUI client** with live control via **Sockets**
* **CryptoSoft single-instance**: only one encryption process at a time

---

### Main Features

* Create unlimited backup jobs
* Full or differential backup
* Conditional encryption for specified extensions
* Logging in JSON or XML with transfer & encryption time
* Real-time state tracking with progress percentage
* Remote control via a client app (start/pause/stop jobs)

---

### Installation

1. Download the following archives:

   * `EASYSAVE_V3_0.zip` (Main app)
   * `EasySaveClient.zip` (Remote client)
2. Unzip them and place the `.exe` files wherever you want.
3. On first launch, folders will be auto-created: `Logs`, `Settings`, `BackupsJobs`, `States`

---

### Usage

#### Main Application

* Job creation & listing
* File decryption
* Configuration (priorities, thresholds, business software)
* Launch remote server
* View logs

#### Remote Client

1. Start the server from the main app (note the IP & port).
2. Launch the client and connect with provided IP/port.
3. Monitor all jobs in real time and control them remotely.

---

### Technologies

* **.NET 8**, **WPF
* **TCP Sockets** for client/server communication
* **JSON / XML** logging
* **Multilingual** support via `.resx`
* **Mono-instance CryptoSoft** for secure encryption

---

### Version History

| Version | Description                                                    |
| ------- | -------------------------------------------------------------- |
| 1.0     | Console version, 5 jobs max, JSON logs                         |
| 1.1     | JSON or XML log formats                                        |
| 2.0     | GUI, unlimited jobs, CryptoSoft, business software management  |
| 3.0     | Parallel jobs, file priority, real-time control, remote client |

---

### 📝 License

Developed as part of an academic project – Not for commercial use.

---

### Contact

**EasySave Team – CESI**

See French section for full team list and emails.
