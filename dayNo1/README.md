# Lab 1 : Initialisation d'une API Sécurisée

## Objectif
L'objectif de ce premier lab est de poser les bases de la sécurité applicative dans un projet .NET en adressant deux points critiques du **OWASP Top 10** :
1. **Security Misconfiguration** : En configurant correctement la gestion des erreurs.
2. **Identification and Authentication Failures** (indirectement) : En assurant une traçabilité (logging) robuste pour l'audit.

## Pourquoi est-ce important ?

### 1. Prévention de la fuite d'informations (Information Disclosure)
Par défaut, en cas d'erreur brute, une application peut renvoyer une "Stack Trace" complète. Pour un attaquant, c'est une mine d'or :
- Noms des classes et des méthodes.
- Structure des chemins de fichiers sur le serveur.
- Versions des bibliothèques utilisées.
- Parfois même des extraits de code ou des chaînes de connexion.

L'utilisation de la **RFC 7807 (Problem Details)** permet de renvoyer une erreur standardisée et anonymisée.

### 2. Logging Structuré
En cas d'attaque, les logs classiques (texte plat) sont difficiles à analyser à grande échelle. Le format **JSON** permet :
- Une ingestion facile dans des outils comme ELK (Elasticsearch, Logstash, Kibana) ou Splunk.
- La corrélation automatique via un `TraceId` (permet de suivre toutes les actions d'un utilisateur à travers les différents microservices).

---

## Instructions du Lab

### Étape 1 : Création du projet
```bash
dotnet new webapi -n Cnss.SecureApi
cd Cnss.SecureApi
```

### Étape 2 : Configuration du Pipeline de sécurité
Dans `Program.cs`, assurez-vous que `app.UseExceptionHandler();` est appelé avant les autres middlewares pour capturer toutes les erreurs en amont.

### Étape 3 : Implémentation de ProblemDetails
Utilisez `builder.Services.AddProblemDetails()` pour activer le support de la RFC 7807.

### Étape 4 : Logging Structuré
Ajoutez Serilog :
```bash
dotnet add package Serilog.AspNetCore
```
Configurez le logger dans `Program.cs` pour écrire en JSON dans la console.

### Étape 5 : Test et Validation
1. Lancez l'application : `dotnet run`
2. Appelez un point de terminaison qui génère une erreur (ex: `/error`).
3. Vérifiez que la réponse HTTP est propre (JSON standard) et que les logs console sont en JSON.

---

## Défi supplémentaire
Ajoutez un "Enricher" Serilog pour inclure le nom de la machine ou la version de l'application dans chaque ligne de log.
