pipeline {
    agent any

    environment {
        // Configurer le chemin de .NET (vérifiez avec 'which dotnet' sur votre serveur)
        DOTNET_CLI_HOME = "/tmp"  // Évite les problèmes de cache
        DOTNET_PATH = "/usr/bin/dotnet"
        
        // URL du dépôt GitHub (utilisez SSH si possible)
        REPO_URL = "git@github.com:Njoyayvan22/ScanLab.git"
        // OU en HTTPS avec token (décommenter si nécessaire)
        // REPO_URL = "https://<VOTRE-TOKEN>@github.com/Njoyayvan22/ScanLab.git"
        
        BRANCH = "main"
    }

    options {
        timeout(time: 30, unit: 'MINUTES')  // Timeout du pipeline
        disableConcurrentBuilds()           // Évite les builds concurrents
    }

    stages {
        // Étape 1: Checkout du code source
        stage('Checkout') {
            steps {
                script {
                    try {
                        checkout([
                            $class: 'GitSCM',
                            branches: [[name: env.BRANCH]],
                            extensions: [],
                            userRemoteConfigs: [[
                                url: env.REPO_URL,
                                credentialsId: 'github-ssh-key'  // Remplacez par l'ID de vos credentials Jenkins (SSH ou token)
                            ]]
                        ])
                    } catch (err) {
                        error("Échec du checkout : ${err.message}\nAssurez-vous que :\n1. Les credentials Jenkins sont corrects\n2. Le dépôt existe et est accessible")
                    }
                }
            }
        }

        // Étape 2: Restauration des dépendances .NET
        stage('Restore') {
            steps {
                script {
                    try {
                        sh "${env.DOTNET_PATH} restore"
                    } catch (err) {
                        error("Échec de la restauration : ${err.message}\nVérifiez :\n1. Le fichier .sln/.csproj\n2. La connexion à NuGet")
                    }
                }
            }
        }

        // Étape 3: Build du projet
        stage('Build') {
            steps {
                script {
                    try {
                        sh "${env.DOTNET_PATH} build --configuration Release --no-restore"
                    } catch (err) {
                        error("Échec du build : ${err.message}\nVérifiez les erreurs de compilation")
                    }
                }
            }
        }

        // Étape 4: Exécution des tests
        stage('Test') {
            steps {
                script {
                    try {
                        sh "${env.DOTNET_PATH} test --no-build --verbosity normal"
                    } catch (err) {
                        error("Échec des tests : ${err.message}\nCorrigez les tests unitaires")
                    }
                }
            }
        }

        // Étape 5: Publication (optionnelle)
        stage('Publish') {
            when {
                expression { env.BRANCH == 'main' }  // Exécuter seulement sur la branche main
            }
            steps {
                script {
                    try {
                        sh """
                            ${env.DOTNET_PATH} publish \\
                            --configuration Release \\
                            --output ./publish \\
                            --runtime linux-x64 \\
                            --self-contained false
                        """
                        archiveArtifacts artifacts: 'publish/**/*', fingerprint: true
                    } catch (err) {
                        error("Échec de la publication : ${err.message}")
                    }
                }
            }
        }
    }

    // Post-traitement (notifications, nettoyage)
    post {
        always {
            cleanWs()  // Nettoyer l'espace de travail
        }
        success {
            slackSend(color: "good", message: "SUCCÈS : Job ${env.JOB_NAME} - Build #${env.BUILD_NUMBER}")
        }
        failure {
            slackSend(color: "danger", message: "ÉCHEC : Job ${env.JOB_NAME} - Build #${env.BUILD_NUMBER}")
            emailext (
                subject: "ÉCHEC du pipeline : ${env.JOB_NAME}",
                body: "Vérifiez les erreurs ici : ${env.BUILD_URL}",
                to: "votre-email@example.com"
            )
        }
    }
}
