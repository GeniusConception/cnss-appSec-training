# Lab 9 : Tests Automatisés et Observabilité (DAST & Logging)

Ce lab combine le test de sécurité proactif et la surveillance réactive.

---

## Étape 1 : Tests d'Intégration de Sécurité

Nous utilisons **xUnit** et `WebApplicationFactory` pour valider nos règles de sécurité sans déployer l'application.

1. **Lancer les tests** :
   Ouvrez la solution `Cnss.Observability.sln` (format classique) ou `Cnss.Observability.slnx` (format moderne) dans Visual Studio, ou utilisez la ligne de commande :
   ```bash
   cd lab-9/Cnss.ObservabilityTests
   dotnet test
   ```
2. **Ce qui est vérifié** :
   - L'accès anonyme à `/api/dossier/1` renvoie bien `401 Unauthorized`.
   - Les en-têtes de sécurité (`nosniff`, `DENY`) sont présents sur chaque réponse.
   - Le endpoint `/health` répond `Healthy`.

---

## Étape 2 : Analyse Dynamique (DAST) avec OWASP ZAP

L'analyse dynamique consiste à tester l'application en cours d'exécution.

1. **Lancez l'API localement** :
   ```bash
   cd lab-9/Cnss.ObservabilityApi
   dotnet run
   ```
2. **Quick Scan** :
   - Ouvrez **OWASP ZAP Desktop**.
   - Dans l'onglet "Automated Scan", entrez l'URL locale (ex: `http://localhost:5xxx`).
   - Cliquez sur **Attack**.
   - Analysez les alertes (drapeaux rouges/oranges).

---

## Étape 3 : Mode Proxy avec Postman

Pour tester des requêtes authentifiées :
1. Dans ZAP : Notez le port du proxy (souvent 8080 dans *Tools > Options > Local Proxies*).
2. Dans **Postman** : Allez dans *Settings > Proxy* et configurez ZAP comme proxy.
3. Envoyez une requête `GET` vers `/api/dossier/1` avec un token JWT valide.
4. Dans ZAP, vous verrez la requête apparaître. Faites un clic droit dessus > **Attack > Active Scan**.

---

## Étape 4 : Observabilité et Logs Structurés

Nous utilisons **Serilog** pour enrichir nos logs avec le contexte utilisateur.

1. Regardez les logs dans votre console lors d'un appel API.
2. Chaque log contient désormais une propriété `UserId` extraite du JWT (ou `Anonymous`).
3. **Avantage** : En cas d'incident (ex: une série d'erreurs 403), vous pouvez filtrer instantanément tous les logs d'un utilisateur spécifique dans un outil comme Seq ou ELK.

---

## Étape 5 : Health Checks

Accédez à `http://localhost:5xxx/health`. 
- Un statut `Healthy` garantit que l'application est prête à recevoir du traffic. 
- Dans un environnement de production (Kubernetes/Azure), ces endpoints sont utilisés pour redémarrer automatiquement les instances défaillantes.
