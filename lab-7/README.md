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

## Étape 3 : Déploiement Multi-App avec Caddy

Dans ce scénario, nous allons faire cohabiter l'application du **Lab 6** (port 5000) et celle du **Lab 7** (port 5007) sur le même domaine, mais sous des chemins (paths) différents.

### Configuration du Caddyfile (`/etc/caddy/Caddyfile`)

Caddy doit réécrire les chemins pour que `/lab7/api/transactions` devienne `/api/transactions` lorsqu'il l'envoie au serveur .NET.

```caddy
votre-domaine.com {
    # Application Lab 6 (Minimal API)
    handle_path /lab6* {
        reverse_proxy localhost:5000
    }

    # Application Lab 7 (HMAC API)
    handle_path /lab7* {
        reverse_proxy localhost:5007 {
            header_up X-Real-IP {remote_host}
        }
    }

    # Headers de sécurité globaux
    header {
        Strict-Transport-Security "max-age=31536000; includeSubDomains; preload"
        X-Content-Type-Options "nosniff"
        X-Frame-Options "DENY"
        Referrer-Policy "no-referrer-when-downgrade"
        -Server
    }
}
```

### Mise à jour du Client

N'oubliez pas de mettre à jour la constante `SERVER_URL` dans le `Program.cs` du client pour pointer vers l'URL publique :
```csharp
const string SERVER_URL = "https://votre-domaine.com/lab7";
```

---

## Conclusion

Vous avez maintenant :
1. De l'**Authentification** (Lab 3).
2. De la **Protection réseau** (Lab 4).
3. De l'**Intégrité des données** (Lab 5 & 7).
4. Un **Déploiement professionnel** et sécurisé (Lab 6).
