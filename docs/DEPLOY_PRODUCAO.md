# 🚢 DEPLOY EM PRODUÇÃO — ELLO SAÚDE

## 1. CHECKLIST PRÉ-DEPLOY

### 1.1. Segurança e Configuração
- [ ] Variável `Jwt__Key` configurada com chave aleatória de **mínimo 256 bits** (nunca reutilize chave de dev)
- [ ] `SA_PASSWORD` e `RABBITMQ_PASS` são senhas fortes e únicas
- [ ] `CORS_ORIGINS` configurado com domínio(s) de produção (ex: `https://app.ellosaude.com.br`)
- [ ] Arquivo `.env` de produção **nunca commitado** no repositório
- [ ] Swagger desabilitado em produção (verificar `Program.cs`)
- [ ] Stack traces nunca expostos na resposta de erro (verificar `ExceptionMiddleware`)

### 1.2. Banco de Dados
- [ ] Backup do banco criado antes de qualquer deploy
- [ ] Migrations testadas em ambiente de staging antes de produção
- [ ] Volume persistente `sqlserver_data` configurado e validado
- [ ] Usuário de banco de dados de produção com permissões mínimas necessárias

### 1.3. Validação de Build
- [ ] `dotnet test ElloSaude.sln` — **zero falhas**
- [ ] `npm --prefix ElloSaudeWeb run build` — **sem erros**
- [ ] `docker compose up --build -d` — **todos os containers saudáveis**
- [ ] `GET /health/ready` retorna `200 OK`

---

## 2. VARIÁVEIS DE AMBIENTE DE PRODUÇÃO

```env
# --- BANCO DE DADOS ---
SA_PASSWORD=SenhaUltraForteProducao!2024
ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=ElloSaude;User Id=sa;Password=SenhaUltraForteProducao!2024;TrustServerCertificate=True;

# --- JWT ---
Jwt__Key=ChaveAleatoria256BitsBase64AquiNuncaRepita==
Jwt__Issuer=ElloSaude
Jwt__Audience=ElloSaudeClient

# --- RABBITMQ ---
RABBITMQ_HOST=rabbitmq
RABBITMQ_USER=ellosaude
RABBITMQ_PASS=SenhaRabbitMQProducao!2024

# --- CORS ---
CORS_ORIGINS=https://app.ellosaude.com.br

# --- SEED ---
SEED_PASSWORD=SenhaSeedParaPrimeiroAdmin123!
```

---

## 3. DEPLOY VIA DOCKER COMPOSE

### 3.1. Preparação no Servidor

```bash
# Clone o repositório
git clone https://github.com/Sandokanalves/Ellosaude.git
cd ClinicaProjeto/ElloSaudeAPI

# Configure as variáveis de ambiente de produção
cp .env.example .env
nano .env   # Edite com os valores de produção
```

### 3.2. Build e Subida

```bash
# Build e inicialização de todos os serviços
docker compose up --build -d

# Verificar status
docker compose ps

# Verificar logs da API
docker compose logs api --tail=100

# Verificar logs do SQL Server
docker compose logs sqlserver --tail=50
```

### 3.3. Healthcheck

```bash
# API viva
curl http://localhost:7001/health

# API + dependências prontas
curl http://localhost:7001/health/ready
```

---

## 4. ESTRATÉGIA DE BACKUP

### 4.1. Backup Automático do SQL Server

```bash
# Criar backup do banco
docker exec sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" \
  -Q "BACKUP DATABASE [ElloSaude] TO DISK = '/var/opt/mssql/backups/ellosaude_$(date +%Y%m%d_%H%M%S).bak'"

# Copiar backup para o host
docker cp sqlserver:/var/opt/mssql/backups/ ./backups/
```

### 4.2. Recomendações de Backup
- Backup completo diário (antes das 06h).
- Backup diferencial a cada 6 horas.
- Retenção de 30 dias no mínimo.
- Backup testado mensalmente (restauração em ambiente isolado).

---

## 5. ATUALIZAÇÃO (ROLLING UPDATE)

```bash
# 1. Pull das mudanças
git pull origin main

# 2. Rebuild sem derrubar SQL Server e RabbitMQ
docker compose up --build -d api frontend

# 3. Verificar saúde após atualização
curl http://localhost:7001/health/ready

# 4. Se migration for necessária (aplica automaticamente na inicialização da API)
docker compose logs api | grep -i migration
```

---

## 6. ROLLBACK

```bash
# Se o deploy der errado, voltar para a versão anterior
git log --oneline -5    # Identifica o commit anterior

# Exemplo: reverter para o commit abc1234
git checkout abc1234
docker compose up --build -d api frontend
```

---

## 7. REVERSE PROXY (NGINX EXTERNO)

Exemplo de configuração Nginx para proxy reverso com HTTPS (via certbot/Let's Encrypt):

```nginx
server {
    listen 443 ssl;
    server_name app.ellosaude.com.br;

    ssl_certificate /etc/letsencrypt/live/app.ellosaude.com.br/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/app.ellosaude.com.br/privkey.pem;

    # Frontend (Vue 3 SPA)
    location / {
        proxy_pass http://localhost:80;  # Nginx do container frontend
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }

    # API
    location /api/ {
        proxy_pass http://localhost:7001;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header Authorization $http_authorization;
    }

    # SignalR (WebSocket)
    location /hubs/ {
        proxy_pass http://localhost:7001;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }

    # Healthchecks
    location /health {
        proxy_pass http://localhost:7001;
    }
}
```

---

## 8. MONITORAMENTO EM PRODUÇÃO

### 8.1. Healthchecks Automáticos
- Configure um monitor externo (ex: UptimeRobot, Betterstack) no endpoint `/health/ready`.
- Alerta via e-mail/SMS se o endpoint retornar não-200 por mais de 2 minutos.

### 8.2. Logs
```bash
# Logs da API em tempo real
docker compose logs -f api

# Logs do SQL Server
docker compose logs -f sqlserver

# Logs do RabbitMQ
docker compose logs -f rabbitmq

# Exportar logs para arquivo
docker compose logs api > logs_api_$(date +%Y%m%d).txt
```

### 8.3. Alertas Recomendados
- CPU do container API > 80% por 5 minutos.
- Memória do container SQL Server > 90%.
- Fila do RabbitMQ com mensagens > 1000 não processadas.
- Taxa de erro HTTP 5xx > 1% em 10 minutos.

---

## 9. DISASTER RECOVERY

### Cenário: Container do SQL Server corrompido, mas volume intacto

```bash
# 1. Parar o container corrompido
docker compose stop sqlserver
docker rm sqlserver_container

# 2. Recriar o container conectando ao mesmo volume
docker compose up -d sqlserver

# 3. Verificar que o banco subiu com os dados
docker compose exec sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" \
  -Q "SELECT name FROM sys.databases"
```

### Cenário: Servidor inteiro com falha (migração para novo servidor)

```bash
# 1. Restaurar backup do banco no novo SQL Server
# 2. Transferir volumes Docker ou restaurar backup
# 3. Clonar repositório e configurar .env no novo servidor
# 4. docker compose up --build -d
```
