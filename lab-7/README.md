# Lab 7 : Signature de requête (HMAC)

L'objectif de ce lab est de garantir l'**intégrité** et l'**authenticité** des messages échangés avec un partenaire.

---

## Étape 1 : Configuration du Secret Partagé

Le serveur et le client doivent partager une clé secrète. En développement, nous utilisons les User Secrets.

1. **Sur le serveur (API)** :
   ```bash
   cd lab-7/Cnss.HmacServer
   dotnet user-secrets init
   dotnet user-secrets set "PartnerSecrets:BanqueABC" "CléSecrètePartagéeTrèsLongue"
   ```

2. **En Production (VPS)** :
   Ne stockez jamais la clé dans le code. Utilisez les variables d'environnement dans votre service systemd :
   ```ini
   # Dans /etc/systemd/system/cnss-hmac.service
   Environment="PARTNERSECRETS__BANQUEABC=VotreCleeSuperSecrete"
   ```

---

## Étape 2 : Utilisation du Client Interactif

Lancez le client pour simuler les requêtes du partenaire :
```bash
cd lab-7/Cnss.HmacClient
dotnet run
```

### Exercices de sécurité :

1. **Test Nominal** : Saisissez un montant et un destinataire. La requête doit être acceptée (200 OK).
2. **Attaque sur l'Intégrité** : 
   - Modifiez une seule lettre dans le JSON affiché du client avant qu'il ne soit envoyé (nécessite une petite modif de code pour simuler l'attaque).
   - L'API doit détecter que le hachage ne correspond plus et rejeter la requête.
3. **Attaque par Replay** :
   - Notez la signature et le timestamp d'une requête réussie.
   - Tentez de rejouer la même requête 10 minutes plus tard.
   - L'API doit rejeter la requête grâce à la vérification du délai (`X-Timestamp`).

---

## Étape 3 : Déploiement

Déployez l'API sur votre VPS Ubuntu comme au Lab 6.
N'oubliez pas de configurer la variable d'environnement pour le secret partenaire dans le service systemd, car sans elle, l'API renverra une erreur 500.
