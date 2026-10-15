# 🗄️ BANCO DE DADOS — ESTRATÉGIA DE MIGRATIONS E RECUPERAÇÃO

## 1. TECNOLOGIA E CONFIGURAÇÃO

- **ORM:** Entity Framework Core 9.x
- **Banco:** SQL Server 2022 (containerizado via Docker)
- **Estratégia:** Code-First com migrations versionadas

---

## 2. HISTÓRICO DE MIGRATIONS

| Migration | Data | Descrição |
|---|---|---|
| `20260423164609_InitialCreate` | Abr/2026 | Schema inicial: User, Patient, Appointment, MedicalRecord, Clinic |
| `20260504172915_AddProfessionalIdToAppointment` | Mai/2026 | Coluna `ProfessionalId` em `Appointments` |
| `20261009010306_AddCompleteSaasClinicalSchema` | Out/2026 | Módulos clínicos completos: Professional, DoctorAvailability, ScheduleBlock, Prescription, PrescriptionItem, PaymentRecord, AuditLog, OutboxMessage, AppointmentStatusHistory, ClinicalAttachment, ClinicalMarker, InAppNotification |
| `20261009022731_AddPatientPortalOwnershipAndAppointmentBoundRecords` | Out/2026 | `Patient.UserId`, constraint `CK_MedicalRecords_AppointmentOrLegacy` |

---

## 3. COMO CRIAR NOVAS MIGRATIONS

```powershell
# A partir da raiz do projeto API
cd ElloSaudeAPI

dotnet ef migrations add <NomeDaMigration> \
  --project ElloSaude.Infrastructure \
  --startup-project ElloSaude.Api \
  --output-dir Persistence/Migrations

# Verificar o SQL gerado antes de aplicar
dotnet ef migrations script --idempotent \
  --project ElloSaude.Infrastructure \
  --startup-project ElloSaude.Api
```

---

## 4. APLICAÇÃO AUTOMÁTICA (INICIALIZAÇÃO)

A migration é aplicada automaticamente em `Program.cs`:

```csharp
// Program.cs
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
await db.Database.MigrateAsync();
await DbInitializer.SeedAsync(db, hashService, seedPassword);
```

Isso garante que o banco sempre esteja na versão mais recente ao subir a API.

---

## 5. BACKUP E RESTAURAÇÃO

### 5.1. Backup Manual via Docker

```bash
# Fazer backup dentro do container
docker exec sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" \
  -Q "BACKUP DATABASE [ElloSaude] TO DISK = N'/var/opt/mssql/backups/backup_$(date +%Y%m%d_%H%M%S).bak' WITH NOFORMAT, NOINIT, NAME = 'ElloSaude Full Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10"

# Copiar backup para o host
docker cp sqlserver:/var/opt/mssql/backups/. ./backups/
```

### 5.2. Restauração de Backup

```bash
# Copiar arquivo de backup para o container
docker cp ./backups/backup_20261009.bak sqlserver:/var/opt/mssql/backups/

# Restaurar
docker exec sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" \
  -Q "RESTORE DATABASE [ElloSaude] FROM DISK = N'/var/opt/mssql/backups/backup_20261009.bak' WITH REPLACE, RECOVERY"
```

---

## 6. VOLUME PERSISTENTE (DOCKER)

O volume `sqlserver_data` garante que os dados sobrevivam ao reinício dos containers:

```yaml
# docker-compose.yml
volumes:
  sqlserver_data:

services:
  sqlserver:
    volumes:
      - sqlserver_data:/var/opt/mssql/data
```

**Nunca execute `docker compose down -v` em produção** — isso remove os volumes e apaga todos os dados.

---

## 7. VERIFICAÇÃO DO ESTADO DO BANCO

```bash
# Verificar migrations aplicadas
docker exec sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" \
  -Q "SELECT MigrationId, ProductVersion FROM ElloSaude.dbo.__EFMigrationsHistory ORDER BY MigrationId"

# Verificar tabelas existentes
docker exec sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" \
  -Q "SELECT TABLE_NAME FROM ElloSaude.INFORMATION_SCHEMA.TABLES ORDER BY TABLE_NAME"

# Verificar constraint de prontuário
docker exec sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" \
  -Q "SELECT name, definition FROM ElloSaude.sys.check_constraints WHERE name = 'CK_MedicalRecords_AppointmentOrLegacy'"
```

---

## 8. BOAS PRÁTICAS DE MIGRATION

1. **Nunca apague migrations já aplicadas** em produção — crie sempre novas.
2. **Teste migrations em staging** antes de aplicar em produção.
3. **Faça backup antes de qualquer migration** em produção.
4. **Migrations devem ser idempotentes** — use `MigrateAsync()` que verifica o histórico.
5. **Dados sensíveis** nunca em migrations — use seed separado via `DbInitializer`.
6. Mantenha o `IsDeleted` em soft delete — **nunca DELETE físico em produção**.

---

## 9. MONITORAMENTO DO BANCO

```bash
# Verificar uso de disco do volume SQL Server
docker system df -v | grep sqlserver

# Verificar conexões ativas
docker exec sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" \
  -Q "SELECT COUNT(*) AS ActiveConnections FROM sys.dm_exec_sessions WHERE database_id = DB_ID('ElloSaude')"

# Verificar tamanho do banco
docker exec sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" \
  -Q "SELECT SUM(size * 8 / 1024) AS SizeMB FROM ElloSaude.sys.database_files"
```
