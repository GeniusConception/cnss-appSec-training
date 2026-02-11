# Lab 8 : Sécurité dans le Pipeline CI/CD (Quality Gate)

L'objectif de ce lab est de mettre en œuvre une **Quality Gate** qui bloque automatiquement le déploiement si le code contient des vulnérabilités ou des secrets.

---

## Étape 1 : Audit de Sécurité Local

Avant de pousser votre code, lancez un audit des dépendances NuGet pour détecter les bibliothèques obsolètes ou piratées.

1. **Lancer l'audit** :
   ```bash
   cd lab-8/Cnss.VulnerableApi
   dotnet list package --vulnerable
   ```
2. **Observation** : Vous devriez voir une vulnérabilité de sévérité **High** sur `Newtonsoft.Json v11.0.1`.

---

## Étape 2 : Simulation d'une fuite de secret

Dans `Program.cs`, nous avons "oublié" une clé d'API Google en clair :
```csharp
const string GOOGLE_API_KEY = "AIzaSyA1234567890-fake-google-key";
```
Les outils de **Secret Scanning** (comme Gitleaks ou GitHub Secret Scanning) sont conçus pour détecter ce type de pattern et bloquer le commit ou envoyer une alerte immédiate.

---

## Étape 3 : GitHub Actions et SonarQube

Le pipeline est configuré dans `.github/workflows/security.yml`. Il comprend trois piliers :

1. **Restaurer & Build** : Vérifie que le code compile.
2. **Audit NuGet** : Échoue si une vulnérabilité connue est présente.
3. **Scan SonarQube** : Analyse la qualité du code et les failles logiques (SAST).

### Configuration de SonarQube :
Pour que le pipeline fonctionne sur votre GitHub personnel, vous devez ajouter trois **Secrets** dans les paramètres de votre dépôt (Settings > Secrets and variables > Actions) :
- `SONAR_TOKEN` : Votre jeton généré sur SonarQube (My Account > Security).
- `SONAR_ORGANIZATION` : Votre clé d'organisation sur SonarCloud (visible dans l'URL ou les paramètres d'organisation).
- `SONAR_HOST_URL` : L'URL `https://sonarcloud.io` (ou celle de votre serveur privé).

---

### Étape 4 : "Réparer" la Quality Gate

Pour faire repasser le pipeline au vert, vous devez corriger toutes les vulnérabilités détectées :

1.  **Audit NuGet** : Mettez à jour `Newtonsoft.Json` vers la version 13.0.3.
2.  **Secrets** : Supprimez `GOOGLE_API_KEY` et `DB_CONNECTION` du code.
3.  **Path Traversal** : Sécurisez l'endpoint `/api/files` avec `Path.GetFileName()`.
4.  **Cross-Site Scripting (XSS)** : Encodez la sortie de `/api/hello`.
5.  **Command Injection** : Utilisez une liste blanche pour le paramètre `host` dans `/api/ping`.
6.  **Insecure Deserialization** : Supprimez `TypeNameHandling.All` dans `/api/config`.

---

## Étape 5 : Comment rendre le pipeline BLOQUANT ?

Même si le pipeline échoue, GitHub permet par défaut de fusionner (merge) une Pull Request. Pour empêcher cela (rendre le pipeline "blocking"), vous devez configurer une **Branch Protection Rule** :

1.  Allez dans **Settings** > **Branches** de votre dépôt GitHub.
2.  Cliquez sur **Add branch protection rule**.
3.  Saisissez `main` (ou le nom de votre branche par défaut).
4.  Cochez **Require status checks to pass before merging**.
5.  Recherchez et cochez le nom de votre job GitHub Action (ex: `Build and Audit`).
6.  Cliquez sur **Create**.

Désormais, s'il reste une seule vulnérabilité détectée par le pipeline, le bouton "Merge" sera verrouillé !

---

## Conclusion
Vous avez mis en place un système de "Self-Healing" et de **Gouvernance**, où l'infrastructure refuse d'accepter du code non sécurisé. C'est le fondement de la culture **DevSecOps**.
