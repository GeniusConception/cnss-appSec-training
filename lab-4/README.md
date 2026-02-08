# Lab 4 : Durcir l'API face aux attaques par force brute et injection

Ce lab se concentre sur la protection contre le déni de service (Rate Limiting) et le renforcement des clients via les headers de sécurité (CSP, HSTS).

---

## Étape 1 : Configuration du Rate Limiting
Dans ce lab, nous avons configuré une politique "Fixed Window" sur la route `/api/login` :
- **Limite** : 5 requêtes.
- **Fenêtre** : 10 secondes.
- **Action** : Retourne une erreur `429 Too Many Requests` si la limite est dépassée.

### Test du Rate Limiting
Utilisez `curl` pour appeler rapidement le endpoint de login :
```bash
for i in {1..7}; do echo "----------------- ATTEMPT $i -----------------"; curl -i -X POST http://localhost:5004/api/login; sleep 0.5; done
```
*Attendu : Les 5 premières requêtes réussissent (200 OK), les 2 suivantes échouent (429 Too Many Requests).*

---

## Étape 2 : Headers de Sécurité (Protection du Navigateur)
Nous avons ajouté un middleware pour injecter des en-têtes qui ordonnent au navigateur de renforcer sa sécurité :
- **HSTS** : Force l'utilisation du HTTPS.
- **X-Frame-Options: DENY** : Empêche l'affichage de l'API dans une `<iframe>` (anti-Clickjacking).
- **Content-Security-Policy (CSP)** : Définit les sources de contenu autorisées (anti-XSS).

### Test des Headers
```bash
curl -I http://localhost:5004
```
*Vérifiez la présence de `X-Frame-Options` et `Content-Security-Policy` dans la réponse.*

---

## Étape 3 : Audit externe via Cloudflare Tunnel

Pour tester votre API locale sur [SecurityHeaders.com](https://securityheaders.com), vous devez l'exposer temporairement sur Internet via un tunnel sécurisé.

### Procédure Cloudflare Tunnel (cloudflared)

1. **Installer l'outil** :
   - Sur macOS : `brew install cloudflared`
   - Sur Linux/Windows : Téléchargez le binaire depuis [Cloudflare](https://github.com/cloudflare/cloudflared/releases).

2. **Lancer le tunnel** (sans compte requis pour le tunnel éphémère) :
   ```bash
   cloudflared tunnel --url http://localhost:5004
   ```

3. **Récupérer l'URL** :
   Dans la console, cherchez une ligne ressemblant à :
   `https://random-words-generated.trycloudflare.com`

4. **Passer en mode Production** (Indispensable pour le header HSTS et le A+) :
   ```bash
   export ASPNETCORE_ENVIRONMENT=Production
   dotnet run --urls "http://localhost:5004"
   ```

5. **Tester** :
   - Ouvrez [SecurityHeaders.com](https://securityheaders.com).
   - Collez votre URL `trycloudflare.com`.
   - Cliquez sur **Scan**.

**Objectif** : Obtenir la note **A** ou **A+** !

---

## Exercice : Content Security Policy (CSP)
Analysez la CSP actuelle : `default-src 'none'; script-src 'self'`.
- Que se passe-t-il si vous essayez d'inclure un script externe (ex: Google Analytics) ?
- Comment modifieriez-vous la politique pour l'autoriser ?
