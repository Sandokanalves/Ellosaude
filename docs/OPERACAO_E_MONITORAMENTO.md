# 📊 OPERAÇÃO E MONITORAMENTO — ELLO SAÚDE

## 1. VERIFICAÇÃO RÁPIDA DE SAÚDE

```bash
# Status de todos os containers
docker compose ps

# Healthcheck completo (deve retornar 200 com JSON)
curl http://localhost:7001/health/ready

# Healthcheck simples da API
curl http://localhost:7001/health
```

---

## 2. LOGS DOS SERVIÇOS

### 2.1. API
```bash
# Logs em tempo real
docker compose logs -f api

# Últimas 100 linhas
docker compose logs api --tail=100

# Filtrar apenas erros
docker compose logs api | grep -i "error\|exception\|fail"
```

### 2.2. SQL Server
```bash
docker compose logs sqlserver --tail=50
```

### 2.3. RabbitMQ
```bash
docker compose logs rabbitmq --tail=50
```

### 2.4. Frontend (Nginx)
```bash
docker compose logs frontend --tail=50
```

---

## 3. DIAGNÓSTICO DO BANCO DE DADOS

```bash
# Entrar no container do SQL Server
docker compose exec sqlserver /bin/bash

# Executar query de diagnóstico
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -Q "
SELECT 
  DB_NAME() AS Database,
  (SELECT COUNT(*) FROM dbo.Patients) AS TotalPatients,
  (SELECT COUNT(*) FROM dbo.Appointments) AS TotalAppointments,
  (SELECT COUNT(*) FROM dbo.MedicalRecords) AS TotalMedicalRecords,
  (SELECT COUNT(*) FROM dbo.OutboxMessages WHERE ProcessedAt IS NULL) AS PendingOutboxMessages
"
```

---

## 4. DIAGNÓSTICO DO RABBITMQ

```bash
# Interface web de gerenciamento
# Acesse: http://localhost:15672
# Usuário: ellosaude | Senha: conforme .env

# Via linha de comando dentro do container
docker compose exec rabbitmq rabbitmqctl list_queues name messages consumers

# Verificar filas com mensagens não processadas
docker compose exec rabbitmq rabbitmqctl list_queues name messages | grep -v "^0"
```

---

## 5. REINICIALIZAÇÃO DE SERVIÇOS

```bash
# Reiniciar apenas a API (sem perda de dados)
docker compose restart api

# Reiniciar o frontend
docker compose restart frontend

# Reiniciar tudo (sem apagar volumes)
docker compose down
docker compose up -d

# NUNCA em produção: apaga TODOS os dados!
# docker compose down -v  ← NÃO FAÇA ISSO EM PRODUÇÃO
```

---

## 6. ATUALIZAÇÃO DA APLICAÇÃO

```bash
# 1. Fazer pull das novas mudanças
cd ClinicaProjeto
git pull origin main

# 2. Rebuild e restart dos serviços de aplicação
cd ElloSaudeAPI
docker compose up --build -d api frontend

# 3. Verificar que a nova versão está rodando
docker compose logs api | grep "Application started"

# 4. Verificar healthcheck
curl http://localhost:7001/health/ready
```

---

## 7. LIMPEZA E MANUTENÇÃO

```bash
# Remover imagens antigas não utilizadas (libera espaço em disco)
docker image prune -f

# Verificar uso de espaço do Docker
docker system df

# Limpar logs antigos (com cuidado em produção)
docker compose exec sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" \
  -Q "
    -- Remove AuditLogs com mais de 1 ano
    DELETE FROM ElloSaude.dbo.AuditLogs WHERE Timestamp < DATEADD(YEAR, -1, GETUTCDATE());
    -- Remove OutboxMessages processados com mais de 30 dias
    DELETE FROM ElloSaude.dbo.OutboxMessages WHERE ProcessedAt < DATEADD(DAY, -30, GETUTCDATE());
  "
```

---

## 8. MÉTRICAS DE PERFORMANCE ESPERADAS

| Operação | Tempo Esperado (p95) |
|---|---|
| Login | < 500ms |
| Listagem de pacientes (paginada) | < 300ms |
| Criação de agendamento | < 400ms |
| Listagem de horários livres | < 500ms |
| Geração de PDF da receita | < 2s |
| Relatório financeiro (30 dias) | < 1s |

---

## 9. ALERTAS E NOTIFICAÇÕES RECOMENDADAS

| Condição | Severidade | Ação |
|---|---|---|
| `/health/ready` retorna não-200 por 2 min | 🔴 Crítico | Reiniciar API; verificar banco |
| `OutboxMessages` com > 100 mensagens pendentes | 🟡 Atenção | Verificar RabbitMQ |
| SQL Server com > 90% de CPU por 10 min | 🟡 Atenção | Verificar queries lentas |
| Erros HTTP 5xx > 1% em 5 min | 🟡 Atenção | Verificar logs da API |
| Disco com < 10% livre | 🔴 Crítico | Limpar logs; ampliar armazenamento |
| Falha de autenticação > 10 em 1 min (mesmo IP) | 🟡 Atenção | Possível ataque brute force |

---

## 10. COMANDOS ÚTEIS DE EMERGÊNCIA

```bash
# Ver todos os processos ativos no SQL Server
docker compose exec sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" \
  -Q "SELECT spid, status, loginame, cmd, hostname FROM master.dbo.sysprocesses WHERE dbid = DB_ID('ElloSaude')"

# Matar conexão específica (substituir <spid> pelo ID)
docker compose exec sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" \
  -Q "KILL <spid>"

# Verificar espaço do banco em disco
docker compose exec sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" \
  -Q "EXEC sp_spaceused"

# Forçar reconstrução completa da imagem Docker da API
docker compose build --no-cache api
docker compose up -d api
```
