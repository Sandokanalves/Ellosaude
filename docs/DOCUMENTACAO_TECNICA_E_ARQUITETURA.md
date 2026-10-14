# ElloSaúde — documentação técnica e arquitetura

## 1. Escopo e estado do produto

Este documento descreve o código presente no repositório no momento da revisão. As funcionalidades marcadas como parciais ou pendentes não devem ser consideradas prontas para uso clínico ou operação comercial. O projeto é uma base SaaS em evolução, não uma certificação de conformidade ou uma declaração de prontidão para produção.

Stack observada:

- Backend: .NET 9 / ASP.NET Core Web API, MediatR, Entity Framework Core e SQL Server.
- Frontend: Vue 3, Composition API em JavaScript, Vite, Pinia, Vue Router e PrimeVue. O frontend ainda não usa TypeScript nem `<script setup lang="ts">`.
- Infraestrutura local: Docker Compose, SQL Server e RabbitMQ.
- Testes: xUnit, testes unitários e testes de integração com `WebApplicationFactory`.

## 2. Arquitetura e domínios

A solução backend está em `ElloSaudeAPI/ElloSaude.sln` e separa responsabilidades em:

| Projeto/camada | Responsabilidade |
| --- | --- |
| `ElloSaude.Domain` | Entidades, regras de estado e enums do domínio. Não depende de infraestrutura. |
| `ElloSaude.Application` | Casos de uso MediatR (commands/queries), DTOs, validação, mapeamentos e interfaces requeridas pela aplicação. |
| `ElloSaude.Infrastructure` | Persistência EF Core, migrations, repositórios, identidade/JWT, hash de senha, geração de PDF e mensageria. |
| `ElloSaude.Api` | Controllers REST, autenticação/autorização, configuração de DI, health checks e middleware de erros. |
| `ElloSaude.UnitTests` | Testes de domínio e de handlers, incluindo regras de agenda e isolamento. |
| `ElloSaude.IntegrationTests` | Testes HTTP da API e dos fluxos de autenticação/pacientes. |

O fluxo habitual é Controller → `IMediator.Send(...)` → handler da Application → interface de persistência/serviço → Infrastructure. `ApplicationDbContext` implementa `IApplicationDbContext`; `UnitOfWork` e `Repository<T>` oferecem acesso aos repositórios.

### 2.1 Multi-tenant e persistência

- `TenantService` obtém o tenant e o identificador do usuário das claims da identidade autenticada (`TenantId`, `NameIdentifier`/`sub`).
- Entidades que herdam de `BaseEntity` recebem filtro global por `TenantId` e `IsDeleted`; isso inclui `Clinic`.
- Entidades novas recebem o tenant do contexto em `SaveChangesAsync` quando não o definiram explicitamente. Handlers também devem obter o tenant pelo serviço, nunca confiar em um tenant informado pelo cliente.
- Os filtros globais não substituem autorização por objeto: handlers precisam verificar vínculo, titularidade e papel além de consultar por ID.
- A persistência relacional usa SQL Server e migrations EF Core. `DbInitializer`/startup aplica migrations em provedores relacionais; o seed é exclusivo de Development e depende de configuração.

### 2.2 Dicionário do domínio

**Entidades** (`ElloSaude.Domain/Entities`):

| Classe | O que faz e onde é usada |
| --- | --- |
| `BaseEntity` | Base de identidade, tenant, datas de auditoria e soft delete das entidades persistidas. |
| `Clinic` | Dados cadastrais básicos e identificador do tenant; criada por `RegisterClinicHandler`. |
| `User` | Credenciais, papel, tenant e estado de troca de senha; usada pelo serviço de identidade. |
| `Patient` | Cadastro do paciente, estado ativo e vínculo opcional com conta do portal. |
| `Professional` | Profissional, registro de classe, especialidade, preço, vínculo de usuário, disponibilidade e bloqueios. |
| `Appointment` | Horário, paciente, profissional, tipo, estado, preço, retorno gratuito e cancelamento. |
| `DoctorAvailability` | Janela semanal de atendimento, intervalo e duração do slot. |
| `ScheduleBlock` | Bloqueio temporário de agenda do profissional. |
| `MedicalRecord` | Registro clínico assinado, associado a paciente, profissional e agendamento; não tem fluxo de edição destrutiva. |
| `MedicalRecordAddendum` | Evolução/adendo associado a prontuário, usado para complementar o registro clínico. |
| `Prescription` | Receita emitida pelo profissional e associada à consulta. |
| `PrescriptionItem` | Medicamento, dose, frequência, duração, via e instruções de uma receita. |
| `PaymentRecord` | Lançamento previsto/recebido, forma de pagamento, estado e informações de retorno. |
| `AuditLog` | Evento de acesso/ação clínica com usuário, papel, entidade e tenant. |

**Enums** (`ElloSaude.Domain/Enums`): `AppointmentStatus`, `AppointmentType`, `PaymentStatus`, `PaymentMethod` e `PrescriptionStatus` descrevem os estados/tipos persistidos. `AppointmentStatus` mantém os valores persistidos de Pendente, Confirmado, Realizado e Cancelado, e acrescenta AguardandoRecepcao, EmAtendimento e Faltou sem renumerar os existentes. O horário de chegada e início do atendimento é guardado no agendamento.

**Value Objects:** não há Value Objects próprios no domínio atual; CPF, e-mail, endereço e registros profissionais não estão encapsulados como tipos de valor.

### 2.3 Application: casos de uso, contratos e interfaces

| Área | Casos de uso/contratos relevantes |
| --- | --- |
| Clínicas | `RegisterClinicCommand` e `RegisterClinicHandler` criam clínica e usuário inicial. |
| Identidade | `ChangePasswordCommand`; login e emissão do token são tratados por `UserService`. |
| Pacientes | `CreatePatientCommand`, `CreatePatientPortalAccountCommand`, `UpdatePatientCommand`, `DeletePatientCommand`, `GetPatientsQueries`. |
| Profissionais | `CreateProfessionalCommand`, `SetDoctorAvailabilityCommand`, `ScheduleBlockCommands`, `GetAvailableSlotsQuery`, `GetProfessionalsQuery`. |
| Agendamentos | `CreateAppointmentCommand`, `UpdateAppointmentStatusCommand`, `DeleteAppointmentCommand`, `GetAppointmentsQueries`. |
| Prontuário | `CreateMedicalRecordCommand`, `AddMedicalRecordAddendumCommand`, `GetMedicalRecordByIdQuery`, `GetRecordsByPatientQuery`. |
| Portal do paciente | `PatientPortalQueries` e `CancelMyAppointmentCommand`; consultas usam a conta autenticada vinculada ao paciente. |
| Receitas | `CreatePrescriptionCommand` e `GetPrescriptionDetailsQuery`. |
| Financeiro | `PaymentCommands` e `GetFinancialSummaryQuery` (inclui consultas de relatório/pagamentos). |

DTOs publicados na camada Application: `AppointmentDto`, `ClinicDto`, `FinancialSummaryDto`, `MedicalRecordDto`, `PatientDto` e `TokenDto`. Requests HTTP adicionais ficam junto aos Controllers (por exemplo, `BookPatientAppointmentRequest`, `CreatePrescriptionRequest` e `RegisterPaymentRequest`).

O catálogo de contratos DTO/HTTP também inclui `FinancialReportDto`, `PaymentByMethodDto`, `PaymentByProfessionalDto`, `PaymentTransactionDto`, `PatientAppointmentDto`, `PatientPrescriptionDto`, `PrescriptionItemDto`, `ProfessionalDto`, `TimeSlotDto`, `PrescriptionDetailsDto` e `PrescriptionItemDetailsDto`. Os requests/responses definidos na API são `ChangePasswordRequest`, `SetAvailabilityRequest`, `CreateScheduleBlockRequest`, `CreatePrescriptionRequest`, `PrescriptionItemRequest`, `PatientPortalAccountResponse`, `BookPatientAppointmentRequest`, `AddMedicalRecordAddendumRequest`, `RegisterPaymentRequest`, `FreeReturnRequest` e `CancelPaymentRequest`. `LoginRequest` vem do pacote ASP.NET Core Identity.

Interfaces de Application: `IApplicationDbContext`, `IRepository<T>`, `IUnitOfWork`, `ITenantService`, `IUserService`, `IHashService`, `IMessageBusService`, `IWaitingRoomNotifier` e `IPrescriptionPdfService`. `WaitingRoomPatientArrivedEvent` é o contrato assíncrono publicado pela outbox. `ValidationBehavior` executa os validadores FluentValidation no pipeline MediatR.

### 2.4 Infrastructure: serviços e repositórios

| Classe | O que faz e onde é usada |
| --- | --- |
| `ApplicationDbContext` | Configura entidades/relacionamentos/índices, filtros globais de tenant e persistência de auditoria. |
| `ApplicationDbContextDesignTimeFactory` | Cria contexto para ferramentas de design-time do EF Core. |
| `DbInitializer` | Seed de desenvolvimento; não substitui migrations. |
| `Repository<T>` | Operações genéricas de repositório. |
| `UnitOfWork` | Expõe repositórios e coordena a persistência. |
| `TenantService` | Lê tenant e usuário do principal HTTP autenticado. |
| `UserService` | Autentica usuários, cria/valida JWT e fornece dados do usuário atual. |
| `HashService` | Gera/verifica hash de senha com BCrypt. |
| `QuestPdfPrescriptionPdfService` | Gera documento PDF de prescrição. |
| `RabbitMqConnectionProvider` | Mantém a configuração central da conexão RabbitMQ e habilita recuperação automática, heartbeat e intervalo de reconexão. |
| `RabbitMqService` | Publica mensagens JSON persistentes com confirmação do broker e roteamento obrigatório. Serializa o uso do canal; as filas são declaradas pelo consumidor com sua topologia de retry/DLQ. |
| `OutboxPublisherService` | Busca eventos pendentes da outbox, publica na fila, marca confirmação e aplica backoff quando a conexão/publicação falha. |
| `WaitingRoomNotificationConsumerService` | Consome mensagens com ack manual, uma por vez; usa fila de retry com TTL de 5s e envia à dead-letter queue após cinco retries. Reconecta automaticamente e declara a topologia durável. |
| `WaitingRoomPatientArrivedHandler` | Valida tenant e vínculo atual do profissional, persiste notificação idempotente e entrega evento pelo hub SignalR. Mensagens antigas são descartadas se o atendimento já não estiver aguardando. |
| `OutboxMessage` | Evento de negócio persistido na mesma transação do status do agendamento; guarda tentativas, erro e próxima tentativa. A outbox não herda `BaseEntity` para permitir despacho cross-tenant controlado, e cada evento carrega TenantId explícito. |
| `InAppNotification` | Notificação persistente, isolada pelo filtro global de tenant e destinatário; registra leitura e entrega realtime. Payload não contém nome ou dados clínicos do paciente. |

As migrations versionadas e o `ApplicationDbContextModelSnapshot` estão em `ElloSaude.Infrastructure/Migrations`.

## 3. Dependências e justificativas

As versões abaixo são as declaradas nos arquivos `.csproj` e `package.json`.

### 3.1 NuGet

| Pacote | Versão | Projeto | Uso |
| --- | --- | --- | --- |
| AutoMapper | 16.1.1 | Application | Mapeamento entre entidades e DTOs. |
| FluentValidation.DependencyInjectionExtensions | 12.1.1 | Application | Validação de commands e registro no pipeline. |
| MediatR | 14.1.0 | Application, Api | Despacho de commands/queries e handlers. |
| Microsoft.EntityFrameworkCore | 9.0.0 | Application, Infrastructure | ORM, abstrações e consultas. |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.0 | Infrastructure | Provedor SQL Server. |
| Microsoft.EntityFrameworkCore.Design | 9.0.0 | Infrastructure, Api | Ferramentas de migrations/design-time; privado para consumidores. |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 9.0.0 | Infrastructure | Dependência de Identity/EF disponível na infraestrutura; o domínio atual mantém sua entidade `User` própria. |
| Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.0 | Infrastructure | Validação de tokens JWT Bearer. |
| System.IdentityModel.Tokens.Jwt | 8.0.1 | Infrastructure | Criação e manipulação de JWT. |
| BCrypt.Net-Next | 4.1.0 | Infrastructure | Hash de senhas. |
| QuestPDF | 2026.9.1 | Infrastructure | Geração do PDF de receitas. |
| RabbitMQ.Client | 7.2.1 | Infrastructure | Publicação confirmada, consumo com ack manual, filas de retry e dead-letter queue. |
| AspNetCore.HealthChecks.SqlServer | 9.0.0 | Api | Health check de prontidão do SQL Server. |
| Microsoft.AspNetCore.OpenApi | 9.0.0 | Api | Descrição OpenAPI. |
| Scalar.AspNetCore | 2.14.3 | Api | Interface de documentação OpenAPI Scalar. |
| Swashbuckle.AspNetCore | 7.0.0 | Api | Geração Swagger/OpenAPI. |
| Microsoft.NET.Test.Sdk | 17.12.0 | Testes | Execução/discovery de testes .NET. |
| xunit | 2.9.2; runner 2.8.2 | Testes | Framework e adapter dos testes. |
| Moq | 4.20.72 | UnitTests | Dublês de dependências em testes unitários. |
| FluentAssertions | 7.0.0 | Testes | Asserções legíveis. |
| Microsoft.EntityFrameworkCore.InMemory | 9.0.0 | Testes | Persistência efêmera para testes. |
| Microsoft.AspNetCore.Mvc.Testing | 9.0.0 | IntegrationTests | Hospedagem HTTP de teste com `WebApplicationFactory`. |

`ElloSaude.Domain` não declara dependências NuGet.

### 3.2 npm

| Pacote | Uso |
| --- | --- |
| `vue` | Framework de UI. |
| `vite`, `@vitejs/plugin-vue` | Servidor de desenvolvimento e build. |
| `vue-router` | Rotas e guarda por autenticação/papel. |
| `pinia` | Estado global, incluindo autenticação. |
| `axios` | Cliente HTTP e interceptação configurada em `src/api/axios.js`. |
| `primevue`, `primeicons`, `primeflex` | Componentes, ícones e utilitários de interface. |
| `@fullcalendar/core`, `@fullcalendar/daygrid`, `@fullcalendar/timegrid`, `@fullcalendar/interaction`, `@fullcalendar/vue3` | Calendário da agenda. |
| `date-fns` | Operações de datas. |
| `vee-validate`, `@vee-validate/zod`, `zod` | Formulários e validação. |

Não há suíte de testes frontend configurada no `package.json`; os scripts disponíveis são `dev`, `build` e `preview`.

## 4. Endpoints REST e autorização

Todos os endpoints abaixo usam o prefixo `/api`, salvo indicação. `Admin`, `Profissional`, `Secretaria` e `Paciente` são papéis de JWT; a autorização efetiva também deve respeitar tenant, vínculo e proprietário no handler. As respostas de erro passam pelo middleware global de exceções e seguem `ProblemDetails`/`ValidationProblemDetails`.

Controllers REST em `ElloSaude.Api/Controllers`: `AuthController`, `ClinicsController`, `AppointmentsController`, `NotificationsController`, `PatientsController`, `ProfessionalsController`, `MedicalRecordsController`, `PrescriptionsController`, `PatientPortalController` e `FinancialController`. Os controllers encaminham os casos de uso ao MediatR quando aplicável; `NotificationsController` filtra por tenant e usuário autenticado.

| Método e rota | Entrada → saída resumida | Acesso |
| --- | --- | --- |
| `POST /auth/login` | `LoginRequest` → token JWT | Público; credenciais validadas no serviço. |
| `POST /auth/change-password` | `ChangePasswordRequest` → `204` | Autenticado. |
| `POST /clinics/register` | `RegisterClinicCommand` → identificador da clínica | Público para onboarding; proteger contra abuso e definir fluxo comercial antes da abertura pública. |
| `GET /appointments` | Datas opcionais → `AppointmentDto[]` | Admin, Profissional, Secretaria; Profissional recebe somente os agendamentos dos próprios perfis ativos. |
| `GET /appointments/waiting-room` | → `AppointmentDto[]` ordenados por chegada | Admin, Profissional, Secretaria; Profissional recebe somente seus pacientes aguardando. |
| `GET /appointments/{id}` | → `AppointmentDto` | Admin, Profissional, Secretaria; Profissional só acessa o próprio agendamento. |
| `GET /appointments/professional/{professionalId}?date=` | Data → agenda profissional | Admin, Profissional, Secretaria; Profissional não pode consultar agenda alheia. |
| `POST /appointments` | `CreateAppointmentCommand` → id | Admin, Profissional, Secretaria; Profissional só agenda na própria agenda; portal usa endpoint próprio. |
| `PATCH /appointments/{id}/status` | `UpdateAppointmentStatusCommand` → `204` | Admin, Profissional, Secretaria; handler restringe o profissional ao próprio agendamento e o domínio valida transições Agendado → Confirmado → Aguardando na recepção → Em atendimento → Finalizado. A chegada cria evento na outbox na mesma gravação do status. |
| `GET /api/notifications` | `unreadOnly` opcional → até 50 notificações | Apenas Profissional autenticado; consulta restrita ao usuário e ao tenant do JWT. |
| `PATCH /api/notifications/{id}/read` | → `204` | Apenas o profissional destinatário; ID de outro usuário/tenant retorna `404`. |
| `DELETE /appointments/{id}` | → `204` | Admin, Profissional, Secretaria; regras de status no handler. |
| `GET /patients` | → lista de pacientes | Admin, Profissional, Secretaria. |
| `GET /patients/{id}` | → paciente | Admin, Profissional, Secretaria. |
| `POST /patients` | `CreatePatientCommand` → id | Admin, Profissional, Secretaria. |
| `PUT /patients/{id}` | `UpdatePatientCommand` → `204` | Admin, Profissional, Secretaria. |
| `DELETE /patients/{id}` | → `204` | Admin, Profissional, Secretaria. |
| `POST /patients/{id}/portal-account` | → `PatientPortalAccountResponse` temporária, `Cache-Control: no-store` | Admin, Profissional, Secretaria; entregar credenciais por canal separado. |
| `GET /professionals` | → profissionais ativos | Qualquer usuário autenticado. |
| `POST /professionals` | `CreateProfessionalCommand` → id | Admin. |
| `POST /professionals/{id}/availability` | `SetAvailabilityRequest` → id | Admin, Profissional. |
| `GET /professionals/{id}/available-slots?date=` | → slots | Autenticado. |
| `POST /professionals/{id}/blocks` | `CreateScheduleBlockRequest` → id | Admin, Profissional. |
| `DELETE /professionals/{id}/blocks/{blockId}` | → `204` | Admin, Profissional. |
| `POST /medicalrecords` | `CreateMedicalRecordCommand` → id | Profissional; handler exige vínculo com consulta realizada e profissional responsável. |
| `GET /medicalrecords/{id}` | → `MedicalRecordDto` | Profissional; handler verifica o profissional autor. Admin e Secretaria não têm papel de acesso clínico. |
| `GET /medicalrecords/patient/{patientId}` | → registros do paciente | Profissional; handler aplica verificação de acesso. |
| `POST /medicalrecords/{id}/addenda` | `AddMedicalRecordAddendumRequest` → `204` | Profissional; valida autoria do prontuário. |
| `POST /prescriptions` | `CreatePrescriptionRequest` → id | Profissional; handler valida consulta/profissional/paciente. |
| `GET /prescriptions/{id}` | → detalhes da receita | Profissional emissor, validado no handler. |
| `GET /prescriptions/{id}/pdf` | → PDF | Profissional emissor, validado no handler. |
| `GET /patient-portal/my-appointments` | → próprios agendamentos | Paciente; associação explícita User–Patient. |
| `GET /patient-portal/my-appointments/{id}` | → agendamento próprio | Paciente; titularidade validada no handler. |
| `GET /patient-portal/availability?professionalId=&date=` | → slots disponíveis | Paciente. |
| `POST /patient-portal/book` | `BookPatientAppointmentRequest` → id | Paciente; agenda para o próprio cadastro. |
| `POST /patient-portal/cancel/{id}` | → `204` | Paciente; somente agendamento próprio elegível. |
| `GET /patient-portal/my-prescriptions` | → próprias receitas | Paciente. |
| `GET /patient-portal/my-prescriptions/{id}/pdf` | → PDF | Paciente; receita própria. |
| `GET /financial/summary` | datas opcionais → resumo | Admin, Profissional, Secretaria. |
| `GET /financial/report` | datas/profissional opcionais → relatório | Admin, Profissional. |
| `GET /financial/pending` | datas opcionais → pendências | Admin, Secretaria. |
| `POST /financial/{id}/pay` | `RegisterPaymentRequest` → confirmação | Admin, Secretaria. |
| `POST /financial/{id}/free-return` | `FreeReturnRequest` → confirmação | Admin, Profissional. |
| `POST /financial/{id}/cancel` | `CancelPaymentRequest` → confirmação | Admin, Secretaria. |

Rotas operacionais: `/health` (liveness), `/health/ready` (prontidão incluindo SQL Server) e documentação OpenAPI/Swagger/Scalar conforme ambiente.

## 5. Frontend

### 5.1 Rotas

`ElloSaudeWeb/src/router/index.js` aplica autenticação e papéis:

| Rota | Página | Papéis |
| --- | --- | --- |
| `/auth/login` | `LoginPage.vue` | Público |
| `/auth/change-password` | `ChangePasswordPage.vue` | Autenticado com fluxo de troca de senha |
| `/` | `AgendaPage.vue` | Admin, Profissional, Secretaria |
| `/pacientes` | `PatientsListPage.vue` | Admin, Profissional, Secretaria |
| `/pacientes/:id` | `PatientDetailPage.vue` | Admin, Profissional, Secretaria |
| `/financeiro` | `FinancialPage.vue` | Admin, Profissional, Secretaria |
| `/configuracoes` | `SettingsPage.vue` | Hoje Admin, Profissional, Secretaria; a página ainda é placeholder. |
| `/portal` | `PatientPortalPage.vue` | Paciente |

O guarda do router é uma barreira de UX, não substitui autorização do backend.

### 5.2 Stores, serviços e componentes

- Store Pinia: `src/stores/auth.store.js` contém estado de sessão/claims.
- Cliente HTTP: `src/api/axios.js` centraliza configuração do Axios e tratamento do token.
- Serviços API: `appointments.service.js`, `notifications.service.js`, `patients.service.js`, `professionals.service.js`, `medicalRecords.service.js`, `prescriptions.service.js`, `financial.service.js` e `patientPortal.service.js`.
- Componentes compartilhados: `TheSidebar.vue`, `TheHeader.vue`, `PatientFormModal.vue`.
- Layouts: `DefaultLayout.vue` e `AuthLayout.vue`.
- Páginas em `src/pages`: agenda, pacientes, detalhes do paciente, portal, financeiro, configurações, login e troca de senha.

O frontend não tem interfaces TypeScript espelhando DTOs. A página de configurações não persiste dados de clínica atualmente. A agenda tem calendário diário/semanal/mensal e sala de espera com cronômetro; profissionais podem consultar notificações recentes e marcá-las como lidas. A chegada é gravada na outbox junto com a mudança de estado, publicada no RabbitMQ e consumida para persistir uma notificação e emitir SignalR. O hub autenticado está em `/hubs/waiting-room`; somente profissionais podem conectar e o servidor direciona a notificação ao usuário associado ao profissional do agendamento. O payload não contém dados clínicos nem nome do paciente. A fila de notificações e polling existente recuperam eventos quando o cliente está offline. Em produção, use HTTPS e mantenha `access_token` dos logs de proxy mascarado. Não há ainda histórico imutável de transições de estado.

### Fluxo RabbitMQ da sala de espera

- A transição para `AguardandoRecepcao` grava o agendamento e `OutboxMessage` na mesma transação do EF Core; se o banco falhar, nenhum evento fica publicado parcialmente.
- O `OutboxPublisherService` publica `WaitingRoomPatientArrivedEvent` com confirmação do broker; falhas permanecem na outbox e recebem backoff exponencial até 5 minutos. A entrega é **at-least-once**: após confirmar no broker e antes de atualizar a outbox, uma falha pode republicar a mensagem.
- O consumidor declara as filas duráveis `ellosaude.waiting-room.patient-arrived`, `.retry` e `.dead`, exchange DLX, prefetch 1 e ack manual. Mensagens com falha voltam após 5 segundos; depois de cinco retries são encaminhadas à DLQ para inspeção/reprocessamento operacional.
- `InAppNotification` usa índice único por `(TenantId, EventId)` e o consumidor verifica a associação atual entre agendamento e profissional. `GET /api/notifications`/`PATCH /api/notifications/{id}/read` permitem recuperar e reconhecer notificações persistidas; o consumidor não reenvia eventos cujo envio SignalR já foi registrado, embora uma falha entre o envio e a gravação possa produzir duplicidade (semântica at-least-once).
- O payload transporta IDs, tenant e horário de chegada, sem nome, diagnóstico ou conteúdo de prontuário. RabbitMQ Management e a porta AMQP no Compose são expostos somente em loopback local; configure usuário/senha fortes no `.env`.
- A fila `.dead` exige monitoramento e política operacional de replay/manual review; não existe dashboard de DLQ. A outbox também não tem retenção automática; eventos publicados devem ser arquivados/limpos conforme política operacional definida antes de alto volume.

## 6. Docker Desktop e execução de testes

O Compose local está em `ElloSaudeAPI/docker-compose.yml` e inclui SQL Server, RabbitMQ, API e frontend com volumes persistentes e health checks. A configuração documentada é de desenvolvimento local, não de produção.

No PowerShell, a partir da raiz do repositório:

```powershell
Set-Location .\ElloSaudeAPI
Copy-Item .env.example .env
# Edite .env: configure segredos locais fortes e exclusivos; nunca os versione.
docker compose config --quiet
docker compose up --build -d
docker compose ps
```

O `.env` deve definir `MSSQL_SA_PASSWORD`, `JWT_KEY`, `DEVELOPMENT_SEED_PASSWORD`, `RABBITMQ_DEFAULT_USER` e `RABBITMQ_DEFAULT_PASS`, além de eventuais portas. Não use valores reais de produção no ambiente local. Endereços padrão: frontend `http://localhost:8088`, API `http://localhost:5080`, Swagger `http://localhost:5080/swagger` em Development e RabbitMQ Management `http://localhost:15672`.

O teste RabbitMQ real é opt-in e requer o serviço RabbitMQ ativo. Configure `RabbitMq__Host`, `RabbitMq__Port`, `RabbitMq__UserName` e `RabbitMq__Password` no processo (para Compose local, use as credenciais do `.env`) e execute:

```powershell
$env:RUN_RABBITMQ_TESTS = "1"
dotnet test .\ElloSaudeAPI\ElloSaude.IntegrationTests\ElloSaude.IntegrationTests.csproj --filter "FullyQualifiedName~WaitingRoomRabbitMqIntegrationTests"
```

Em produção, a API recusa iniciar com host/credenciais RabbitMQ ausentes ou com o usuário/senha padrão `guest`. O broker não é exposto publicamente pelo Compose de desenvolvimento; use rede privada, segredos dedicados e TLS conforme a infraestrutura de produção.

Para parar sem apagar os dados persistidos:

```powershell
docker compose down
```

Evite `docker compose down -v` salvo quando a intenção for remover também os volumes e os dados locais.

Testes e build sem Docker, a partir da raiz:

```powershell
dotnet build .\ElloSaudeAPI\ElloSaude.sln
dotnet test .\ElloSaudeAPI\ElloSaude.sln
Set-Location .\ElloSaudeWeb
npm ci
npm run build
```

Não existe script `npm run test` no manifesto atual; antes de adicionar esse comando, é necessário escolher e configurar um runner de testes frontend.

## 7. Lacunas e próximos incrementos

Itens relevantes identificados na revisão que não devem ser confundidos com capacidades existentes:

1. Criar a configuração real da clínica (dados, marca/logotipo, tabela de preços e política de retorno) e restringir a gestão a papéis administrativos.
2. Complementar o ciclo de agendamento com histórico imutável de transições, confirmação/cancelamento por WhatsApp, lista de espera, encaixes e associação de sessões de pacotes.
3. Implementar confirmações/cancelamentos por WhatsApp, lista de espera, encaixes e associação de sessões de pacotes.
4. Criar matrizes e formulários clínicos por especialidade, histórico SOAP, conteúdo visual clínico, anexos/exames e documentação clínica avançada.
5. Criar cadastro/gestão de colaboradores e corrigir o fluxo de onboarding para produzir um administrador inicial coerente com os papéis da API.
6. Completar gestão financeira (caixa, despesas/DRE, comissões, estornos e retorno gratuito configurável).
7. Ampliar testes de autorização e isolamento para todos os recursos; validar migrations e deploy em SQL Server; criar suíte frontend.
8. Substituir gradualmente JavaScript por TypeScript e alinhar os contratos de frontend aos DTOs da API.

Esta lista é um registro do estado observado, não uma promessa de que os itens já tenham sido implementados.
