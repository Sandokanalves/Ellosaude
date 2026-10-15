# 🏗️ ARQUITETURA DO SISTEMA — ELLO SAÚDE SAAS

## Visão Geral

ElloSaúde é um sistema SaaS multi-tenant para clínicas e consultórios médicos, construído com **Clean Architecture** no backend (.NET 9) e **Vue 3** no frontend. Cada clínica é um tenant isolado que compartilha a infraestrutura mas nunca visualiza dados de outro tenant.

---

## 1. STACK TECNOLÓGICA

| Camada | Tecnologia | Versão |
|---|---|---|
| Backend Framework | ASP.NET Core Web API | .NET 9 |
| ORM | Entity Framework Core | 9.x |
| Banco de Dados | SQL Server (Docker) | 2022 |
| CQRS / Mediação | MediatR | 12.x |
| Validação | FluentValidation | 11.x |
| Mensageria | RabbitMQ + Outbox Pattern | 3.x |
| PDF | QuestPDF | Community License |
| JWT | Microsoft.AspNetCore.Authentication.JwtBearer | 9.x |
| Frontend | Vue 3 + Vite | 3.x / 5.x |
| UI Library | PrimeVue | 4.x |
| Estado (Frontend) | Pinia | 2.x |
| HTTP Client | Axios | 1.x |
| Containerização | Docker + Docker Compose | Latest |

---

## 2. CAMADAS DA ARQUITETURA (BACKEND)

```
ElloSaudeAPI/
├── ElloSaude.Domain/          ← Entidades, enums, regras de negócio puras
├── ElloSaude.Application/     ← CQRS, Commands, Queries, Validators, Interfaces
├── ElloSaude.Infrastructure/  ← EF Core, SQL Server, JWT, PDF, RabbitMQ, Identity
└── ElloSaude.Api/             ← Controllers, Middleware, Program.cs
```

### 2.1. Domain (Núcleo)
- **Sem dependências externas**. Contém apenas as entidades e regras invariantes.
- `BaseEntity`: `Id (Guid)`, `CreatedAt`, `UpdatedAt`, `IsDeleted`.
- Entidades raiz: `Patient`, `Professional`, `Appointment`, `MedicalRecord`, `Prescription`, `PaymentRecord`, `Clinic`.
- Enums: `AppointmentStatus`, `AppointmentType`, `PaymentStatus`, `PaymentMethod`, `PrescriptionStatus`.

### 2.2. Application (Casos de Uso)
- Padrão CQRS via MediatR: cada funcionalidade é um `Command` (escrita) ou `Query` (leitura).
- `ValidationBehavior<TRequest, TResponse>`: Pipeline que executa FluentValidation antes de qualquer handler.
- `IApplicationDbContext`: Abstração do DbContext para desacoplamento.
- `ITenantService`: Provê `TenantId` e `UserId` do request atual.
- `IUserService`: Provê `UserRole` do usuário autenticado.

### 2.3. Infrastructure (Implementações)
- `ApplicationDbContext`: DbContext com **Global Query Filters** por `TenantId` para todas as entidades multi-tenant.
- `DbInitializer`: Seed de dados iniciais (admin, médico, secretária) na primeira execução.
- `QuestPdfPrescriptionPdfService`: Geração de PDF de receitas.
- `RabbitMqMessageBusService`: Publicação de mensagens no RabbitMQ.
- `OutboxPublisherService`: Worker que processa e publica `OutboxMessage` pendentes.
- `WaitingRoomNotificationConsumerService`: Consumer RabbitMQ para notificações em tempo real.

### 2.4. Api (Apresentação)
- **Controllers**: Mapeiam HTTP → MediatR `Send()`.
- **ExceptionMiddleware**: Captura exceções e retorna `ProblemDetails` (RFC 7807).
- **Program.cs**: Wiring de DI, JWT, CORS, Swagger, SignalR, HealthChecks.

---

## 3. MULTI-TENANCY

### Estratégia: Row-Level Isolation com Global Query Filter

Cada entidade multitenant possui `TenantId (string)`. O `ApplicationDbContext` aplica automaticamente:

```csharp
modelBuilder.Entity<Patient>().HasQueryFilter(p => p.TenantId == _tenantId);
```

O `TenantId` é extraído do **JWT claim** a cada requisição via `TenantService`:

```csharp
// Claim no token: "TenantId" = "clinic-abc-guid"
public string GetTenantId() => _httpContextAccessor.HttpContext?.User
    .FindFirstValue("TenantId") ?? throw new UnauthorizedAccessException();
```

O `SaveChangesAsync` automaticamente preenche `TenantId`, `CreatedAt`, `UpdatedAt` nas entidades.

### Diagrama de Isolamento

```
Banco de Dados (SQL Server compartilhado)
  ├── Tenant: clinic-A  → Patients: [A1, A2], Appointments: [AA1]
  └── Tenant: clinic-B  → Patients: [B1],     Appointments: [AB1]

Global Query Filter garante que clinic-A NUNCA vê dados de clinic-B.
```

---

## 4. AUTENTICAÇÃO E AUTORIZAÇÃO

### Fluxo JWT

```
Cliente → POST /api/auth/login → JWT Token (expiração 8h)
                                 Claims: UserId, Email, Role, TenantId, ClinicId
```

### Roles e Permissões

| Role | Acesso |
|---|---|
| `Admin` | Tudo: pacientes, prontuários, financeiro completo, config, equipe |
| `Profissional` | Prontuários, receitas, agenda própria, financeiro próprio |
| `Secretaria` | Pacientes, agenda (todos), financeiro pendente, sem prontuários |
| `Paciente` | Portal: próprios agendamentos, receitas liberadas |

---

## 5. PADRÃO OUTBOX (MENSAGERIA CONFIÁVEL)

Para garantir entrega de mensagens ao RabbitMQ mesmo com falhas de rede:

```
Handler → SaveChangesAsync (persiste OutboxMessage + entidade juntos, em transação)
       → OutboxPublisherService (worker) → publica OutboxMessage → marca como processado
       → WaitingRoomNotificationConsumerService → consome e notifica via SignalR
```

---

## 6. ESTRUTURA DO FRONTEND (VUE 3)

```
ElloSaudeWeb/src/
├── api/                ← Módulos Axios por domínio (patients.js, appointments.js, ...)
├── assets/             ← Estilos globais e ícones
├── components/         ← Componentes reutilizáveis (AnatomicalMarkerCanvas, modais)
├── layouts/            ← MainLayout (sidebar + header adaptativos por role)
├── pages/              ← Páginas por rota
│   ├── agenda/         ← AgendaPage.vue
│   ├── patients/       ← PatientsListPage, PatientDetailPage (5 abas)
│   ├── FinancialPage.vue
│   ├── PatientPortalPage.vue
│   └── SettingsPage.vue
├── router/             ← vue-router com guards de autenticação e role
└── stores/             ← Pinia stores (auth, etc.)
```

---

## 7. FLUXO DE DADOS TÍPICO (AGENDAMENTO)

```
Vue3 AgendaPage → POST /api/appointments
  → AppointmentsController.Create()
  → mediator.Send(CreateAppointmentCommand)
  → ValidationBehavior (FluentValidation)
  → CreateAppointmentHandler:
      1. Verifica disponibilidade do médico (DoctorAvailability)
      2. Verifica conflito de horário
      3. Cria Appointment + OutboxMessage (transação única)
      4. SaveChangesAsync (TenantId auto-preenchido)
  → Retorna 201 Created
  → OutboxPublisherService publica para RabbitMQ
  → WaitingRoomConsumer → SignalR Hub → UI atualizada
```

---

## 8. HEALTHCHECKS

- `GET /health` — Status vivo/morto da API
- `GET /health/ready` — API + SQL Server + dependências prontas

---

## 9. SEGURANÇA EM PRODUÇÃO

- Chave JWT configurável via variável de ambiente `Jwt__Key` (mínimo 256 bits).
- CORS configurável via `CORS_ORIGINS` — origens restritas em produção.
- Nenhum segredo nos arquivos commitados; `.env.example` documenta as variáveis.
- `ExceptionMiddleware` nunca expõe stack traces em produção.
- Soft delete (`IsDeleted`) preserva dados para auditoria LGPD.
- `AuditLog` registra todas as leituras e escritas críticas com timestamp e IP.
