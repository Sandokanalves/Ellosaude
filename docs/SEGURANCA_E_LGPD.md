# 🔐 SEGURANÇA E LGPD — ELLO SAÚDE

## 1. CONTROLES DE AUTENTICAÇÃO

### 1.1. JSON Web Tokens (JWT)
- Algoritmo: **HS256** com chave mínima de 256 bits.
- Chave configurada via variável de ambiente `Jwt__Key` — nunca hardcoded.
- Validação obrigatória de `Issuer`, `Audience` e tempo de expiração (`8h`).
- Expiração do token é verificada em cada requisição pelo middleware ASP.NET Core.

### 1.2. Senhas
- Senhas armazenadas com **hash PBKDF2** via `IHashService` (Microsoft.AspNetCore.Cryptography.KeyDerivation).
- Senhas temporárias de uso único para o Portal do Paciente — troca obrigatória no primeiro login.
- Após troca de senha, o token antigo (temporário) é rejeitado (`MustChangePassword` claim removida).

### 1.3. CORS
- Origens permitidas configuráveis via variável de ambiente `CORS_ORIGINS`.
- Em produção: CORS restringe a domínio(s) explicitamente configurado(s).
- Em desenvolvimento: fallback permissivo para localhost.

---

## 2. AUTORIZAÇÃO (RBAC)

### 2.1. Matriz de Acesso por Role

| Recurso | Admin | Profissional | Secretaria | Paciente |
|---|:---:|:---:|:---:|:---:|
| Listar / Criar Pacientes | ✅ | ✅ | ✅ | ❌ |
| Editar Dados do Paciente | ✅ | ✅ | ✅ | ❌ |
| Prontuários (leitura) | ❌ | ✅ | ❌ | ❌ |
| Prontuários (criação/adendo) | ❌ | ✅ | ❌ | ❌ |
| Receitas (emitir) | ❌ | ✅ | ❌ | ❌ |
| Receitas (listar próprias) | ❌ | ✅ | ❌ | ✅ (portal) |
| Agenda (criar/cancelar) | ✅ | ✅ | ✅ | ✅ (portal) |
| Agenda (ver todas) | ✅ | ✅ | ✅ | ❌ |
| Financeiro (pendentes) | ✅ | ✅ | ✅ | ❌ |
| Financeiro (relatório detalhado) | ✅ | ✅ | ❌ | ❌ |
| Configurações da Clínica | ✅ | ❌ | ❌ | ❌ |
| Gestão de Usuários e Equipe | ✅ | ❌ | ❌ | ❌ |
| Portal do Paciente | ❌ | ❌ | ❌ | ✅ |
| Sala de Espera (SignalR) | ✅ | ✅ | ❌ | ❌ |

### 2.2. Implementação
- Role armazenada como claim JWT: `Role`.
- Controllers usam `[Authorize(Roles = "Admin,Profissional")]` ou políticas customizadas.
- Handlers de prontuário verificam explicitamente a role via `IUserService.GetUserRole()`.

---

## 3. ISOLAMENTO MULTI-TENANT (LGPD - Art. 46)

### 3.1. Global Query Filter
Todos os DbSets com dados de tenant possuem filtro automático:
```csharp
modelBuilder.Entity<Patient>().HasQueryFilter(p => p.TenantId == _tenantId);
```
Isso garante que queries SQL sempre incluem `WHERE TenantId = @tenantId`, impedindo acesso cruzado.

### 3.2. Preenchimento Automático de TenantId
O `SaveChangesAsync` do `ApplicationDbContext` intercepta todas as entidades em estado `Added` e preenche automaticamente `TenantId` com o valor do token JWT, impedindo que um tenant grave dados no espaço de outro.

### 3.3. Testes de Isolamento
A suite `MultiTenantIsolationTests` comprova automaticamente:
- Pacientes são visíveis apenas dentro do próprio tenant.
- Paciente do portal não acessa receitas de outros pacientes.
- Agendamentos de ID estrangeiro retornam 404 (sem vazamento de informação).
- Secretária recebe 403 ao tentar acessar relatório financeiro restrito.

---

## 4. AUDITORIA (LGPD - Art. 37 e 48)

### 4.1. Entidade AuditLog
```
AuditLog {
  TenantId     - Identificação do tenant
  UserId       - Usuário que realizou a ação
  Action       - Leitura | Criação | Modificação | Exclusão
  EntityName   - Nome da entidade afetada
  EntityId     - ID do registro afetado
  Timestamp    - Data e hora (UTC)
  IpAddress    - IP de origem da requisição
}
```

### 4.2. Eventos Auditados
- Criação de prontuário médico.
- Leitura de prontuário médico.
- Criação de adendos no prontuário.
- Criação, download e cancelamento de receitas.
- Criação de conta no portal do paciente.

---

## 5. INTEGRIDADE DOS DADOS CLÍNICOS (CFM)

### 5.1. Prontuário Imutável
- Prontuários médicos **não podem ser editados ou excluídos** (sem rota DELETE, sem UPDATE do conteúdo).
- Correções e observações posteriores são adicionadas via **MedicalRecordAddendum** (adendos), preservando o histórico original.
- Constraint de banco (`CK_MedicalRecords_AppointmentOrLegacy`) garante que registros novos estejam vinculados a uma consulta realizada.

### 5.2. Soft Delete
- Pacientes, profissionais e outros registros são marcados como `IsDeleted = true` — nunca apagados fisicamente.
- Dados preservados para auditoria regulatória e LGPD.

---

## 6. PROTEÇÃO DE DADOS EM TRÂNSITO

- **HTTPS obrigatório** em produção (configurar no Nginx/reverse proxy).
- Headers de segurança recomendados no Nginx: `X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy`.
- Tokens JWT transmitidos exclusivamente via header `Authorization: Bearer <token>`.

---

## 7. SEGREDOS E VARIÁVEIS DE AMBIENTE

| Variável | Descrição | Obrigatório em Produção |
|---|---|:---:|
| `Jwt__Key` | Chave de assinatura JWT (mín. 256 bits) | ✅ |
| `Jwt__Issuer` | Identificador do emissor JWT | ✅ |
| `Jwt__Audience` | Identificador da audiência JWT | ✅ |
| `ConnectionStrings__DefaultConnection` | String de conexão SQL Server | ✅ |
| `RABBITMQ_HOST` | Host do RabbitMQ | ✅ |
| `RABBITMQ_USER` | Usuário do RabbitMQ | ✅ |
| `RABBITMQ_PASS` | Senha do RabbitMQ | ✅ |
| `CORS_ORIGINS` | Origens permitidas pelo CORS | ✅ |
| `SA_PASSWORD` | Senha do SA do SQL Server | ✅ |

> **NUNCA** commitar valores reais no repositório. Use `.env.example` como template e `.env` local ignorado pelo `.gitignore`.

---

## 8. INCIDENTE E NOTIFICAÇÃO (LGPD - Art. 48)

Em caso de violação de dados:
1. Identificar o escopo via `AuditLog` (quais TenantIds, UserId, IPs envolvidos).
2. Revogar tokens comprometidos alterando a `Jwt__Key` (invalida todos os tokens ativos).
3. Notificar a ANPD e os titulares afetados em até 72 horas.
4. Registrar o incidente e as medidas tomadas na documentação operacional.
