# Lab 3 : Sécuriser l'API avec JWT Bearer et CORS

Ce lab se concentre sur l'étape de **l'Authentification** et la validation de la **Signature**.

---

## Étape 1 : Installation des packages
```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

## Étape 2 : Génération et Validation du Token

Puisque nous n'avons pas de serveur d'identité, l'API possède un endpoint temporaire pour générer des jetons de test.

1. **Obtenir un Token** :
   Appelez l'URL suivante (via le navigateur ou curl) :
   ```bash
   curl "http://localhost:5003/api/auth/token?userId=123"
   ```
   *Copiez la valeur `access_token` reçue.*

2. **Exercice : Valider la Signature sur jwt.io** :
   - Allez sur [jwt.io](https://jwt.io).
   - Collez votre token dans la partie gauche.
   - Dans la partie droite (**VERIFY SIGNATURE**), tapez la clé secrète suivante :
     `CléSecrèteSuperLonguePourLeLabSécuritéCNSS2026!`
   - **BIM !** : Le message **"Signature Verified"** s'affiche en bleu/vert. 
   
   *C'est la preuve que sans cette clé, personne ne peut fabriquer un jeton valide pour votre API.*

---

## Étape 3 : Tests de validation API

### Test 1 : Accès autorisé
Utilisez le token généré pour le userId `123` :
```bash
curl -i -H "Authorization: Bearer <VOTRE_TOKEN>" http://localhost:5003/api/dossier/123
```
*Attendu : 200 OK*

### Test 2 : Erreur BOLA (ID sub != ID URL)
Générez un token pour un autre utilisateur (`userId=456`) et tentez d'accéder au dossier `123`.
*Attendu : 403 Forbidden*

---

## Étape 4 : Démonstration pratique du CORS

Le **CORS (Cross-Origin Resource Sharing)** est une sécurité implémentée par le **navigateur**. Elle n'empêche pas les outils comme `curl` ou Postman d'appeler l'API, mais empêche un script malveillant sur un autre site Web de lire les données de votre API.

### Le test "La Preuve par la Console"
Pour faire "palper" le CORS à vos étudiants :

1. Assurez-vous que votre API tourne sur `http://localhost:5003`.
2. Ouvrez un navigateur sur un site qui n'est pas autorisé par votre politique (ex: `https://www.google.com`).
3. Appuyez sur `F12` (Outils de développement) et allez dans l'onglet **Console**.
4. Tapez et exécutez le code suivant :
   ```javascript
   fetch('http://localhost:5003/api/dossier/123')
     .then(response => console.log('Succès !'))
     .catch(err => console.error('Bloqué par CORS ! (Comme prévu)'));
   ```
5. **Résultat attendu** : Une erreur rouge s'affiche dans la console indiquant que la requête a été bloquée par la politique CORS.
6. **Pourquoi ?** Parce que l'origine `https://www.google.com` n'est pas présente dans la liste blanche (`https://portal.cnss.cd`) que nous avons configurée dans `Program.cs`.
