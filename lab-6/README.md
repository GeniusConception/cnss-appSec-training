# Lab 6 : Déploiement Sécurisé derrière un Reverse Proxy

Ce lab prépare les participants à déployer une application .NET sur un serveur Linux (VPS) derrière un proxy (Caddy).

---

## Étape 1 : Préparation du VPS Ubuntu

Avant toute chose, sécurisez votre serveur :

1. **Mises à jour système** :
   ```bash
   sudo apt update && sudo apt upgrade -y
   ```

2. **Configuration du Pare-feu (UFW)** :
   ```bash
   sudo ufw allow OpenSSH
   sudo ufw allow 80/tcp
   sudo ufw allow 443/tcp
   sudo ufw enable
   ```

3. **Dépendances système** :
   Même en mode Self-Contained, .NET a besoin de quelques bibliothèques natives sur Ubuntu :
   ```bash
   sudo apt install -y libicu-dev libssl-dev
   ```

---

## Étape 2 : Déploiement de l'API (Mode Self-Contained)

L'avantage du mode **Self-Contained** est que l'application embarque son propre runtime .NET 10. Vous n'avez donc **rien à installer** sur le VPS pour faire tourner l'application.

1. **Publier l'application** (depuis votre machine locale) :
   Le plus simple pour la version 10 est de publier l'application en mode **Self-Contained**. Elle embarquera son propre runtime .NET 10, ce qui évite de l'installer sur le VPS.
   ```bash
   dotnet publish Cnss.MinimalApi.csproj -c Release -r linux-x64 --self-contained true -o ./publish 
   ```

2. **Créer un service Systemd** :
   Créez le fichier `/etc/systemd/system/cnss-api.service` :
   ```ini
   [Unit]
   Description=CNSS Minimal API - Lab 6
   After=network.target

   [Service]
   WorkingDirectory=/var/www/cnss-api
   # Si mode Self-Contained, on lance directement l'exécutable du projet
   ExecStart=/var/www/cnss-api/Cnss.MinimalApi --urls "http://localhost:5000"
   Restart=always
   RestartSec=10
   SyslogIdentifier=cnss-api
   User=www-data
   Environment=ASPNETCORE_ENVIRONMENT=Production

   [Install]
   WantedBy=multi-user.target
   ```

3. **Démarrer le service** :
   ```bash
   sudo systemctl enable cnss-api
   sudo systemctl start cnss-api
   ```

---

## Étape 3 : Configuration du Reverse Proxy Caddy

Caddy est un serveur web moderne qui gère le HTTPS automatiquement.

1. **Installation** :
   ```bash
   sudo apt install -y debian-keyring debian-archive-keyring apt-transport-https
   curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | sudo gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
   curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' | sudo tee /etc/apt/sources.list.d/caddy-stable.list
   sudo apt update
   sudo apt install caddy
   ```

2. **Configuration (`/etc/caddy/Caddyfile`)** :
   ```caddy
   votre-domaine.com {
       reverse_proxy localhost:5000 {
           header_up X-Real-IP {remote_host}
       }

       # Durcissement des headers de sécurité
       header {
           Strict-Transport-Security "max-age=31536000; includeSubDomains; preload"
           X-Content-Type-Options "nosniff"
           X-Frame-Options "DENY"
           Referrer-Policy "no-referrer-when-downgrade"
           -Server  # Supprime le header Server du proxy
       }
   }
   ```

3. **Redémarrer Caddy** :
   ```bash
   sudo systemctl restart caddy
   ```

---

## Étape 4 : Test de Validation

Appelez votre domaine via HTTPS.
L'application doit afficher **votre adresse IP publique** et non l'IP locale (127.0.0.1), car l'API fait désormais confiance aux en-têtes transmis par Caddy.

Vérifiez les logs du service :
```bash
journalctl -u cnss-api.service -f
```
On doit y voir : `Requête reçue de l'IP : [VOTRE_IP]`
