# Lab 5 : Validation Robuste et Gestion des Secrets

Ce lab se concentre sur la **validation stricte** des données, la **prévention des injections SQL**, et la **gestion sécurisée des secrets**.

---

## Étape 1 : Installation des packages
```bash
dotnet add package FluentValidation.AspNetCore
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

## Étape 2 : Gestion des Secrets (User Secrets)
Nous ne stockons plus les chaînes de connexion dans `appsettings.json`.

1. **Initialiser les secrets** :
   ```bash
   dotnet user-secrets init
   ```

2. **Ajouter la chaîne de connexion SQLite** :
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Data Source=cnss_secure.db"
   ```

3. **Vérifier** :
   L'application lira `ConnectionStrings:DefaultConnection` depuis votre magasin de secrets local, pas depuis le fichier de configuration versionné.

---

## Étape 3 : Validation avec FluentValidation

Nous avons défini un validateur pour l'inscription d'un assuré (`RegisterAssureRequestValidator`).

### Test de validation (Cas d'échec) :
```bash
curl -i -X POST http://localhost:5005/api/assure/register \
     -H "Content-Type: application/json" \
     -d '{"nom": "J", "email": "invalide", "ssn": "999"}'
```
*Attendu : 400 Bad Request avec les messages d'erreur détaillés (RFC 7807).*

### Test de validation (Cas de succès) :
```bash
curl -i -X POST http://localhost:5005/api/assure/register \
     -H "Content-Type: application/json" \
     -d '{"nom": "Jean Dupont", "email": "jean@cnss.cd", "ssn": "1850102030405"}'
```
*Attendu : 201 Created.*

---

## Étape 4 : Protection des Données (Chiffrement applicatif)

Dans le code, nous utilisons `IDataProtectionProvider` pour chiffrer l'email avant de l'afficher dans les logs.

**Exercice** :
1. Lancez l'API.
2. Effectuez une inscription réussie.
3. Regardez la console : l'email affiché ressemble à une chaîne de caractères aléatoires (`CfDJ8...`).
4. **Pourquoi ?** Même si les logs tombent entre de mauvaises mains, les données sensibles (PII) restent protégées.

---

## Étape 5 : Prévention de l'Injection SQL

L'endpoint `/api/assure/search` illustre la différence entre un code vulnérable et un code sécurisé.

1. **Le Code Vulnérable (Commenté)** :
   Il utilise `FromSqlRaw` avec une chaîne concaténée. Si un utilisateur tape `Jean' OR '1'='1`, la requête finale devient :
   `SELECT * FROM Assures WHERE Nom = 'Jean' OR '1'='1'`.
   *Résultat* : L'attaquant récupère tous les assurés de la base.

2. **Le Code Sécurisé (Actif)** :
   Il utilise **LINQ**. Entity Framework transforme votre variable `name` en un **paramètre SQL** (`@p0`). La base de données traite le contenu de la variable comme une simple chaîne de texte, pas comme une commande.

### Démonstration de l'échec de l'attaque :
```bash
curl "http://localhost:5005/api/assure/search?name=Jean' OR '1'='1"
```
*Attendu : `[]` (Liste vide), car aucun assuré ne s'appelle littéralement "Jean' OR '1'='1".*

---

## Conclusion
Vous avez mis en place 3 barrières de sécurité majeures :
1. **Validation** : Les données malformées n'entrent pas dans le système.
2. **Secrets** : Vos clés ne sont plus dans Git.
3. **Data Protection** : Les données sensibles sont chiffrées au repos applicatif.
